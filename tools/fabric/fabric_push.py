#!/usr/bin/env python3
"""
Push a local Microsoft Fabric Data Pipeline JSON definition.

Examples:
  py -3 tools/fabric/fabric_push.py Execute_Notes.json --dry-run
  py -3 tools/fabric/fabric_push.py Execute_Notes.json
  py -3 tools/fabric/fabric_push.py --save-token "eyJ..."
"""

from __future__ import annotations

import argparse
import base64
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any


FABRIC_API_BASE = "https://api.fabric.microsoft.com/v1"
DEFAULT_CONFIG = Path(__file__).with_name("fabric_pipelines.json")
TOKEN_FILE = Path.home() / ".fabric_token"


def read_json(path: Path) -> dict[str, Any]:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise SystemExit(f"Invalid JSON in {path}: {exc}") from exc


def load_config(path: Path) -> dict[str, Any]:
    if not path.exists():
        raise SystemExit(f"Config file not found: {path}")

    config = read_json(path)
    config["workspace_id"] = os.environ.get("FABRIC_WORKSPACE_ID", config.get("workspace_id", ""))
    if not config["workspace_id"]:
        raise SystemExit("Missing workspace_id. Set it in config or FABRIC_WORKSPACE_ID.")

    config.setdefault("pipelines", {})
    return config


def get_token() -> str:
    token = os.environ.get("FABRIC_BEARER_TOKEN", "").strip()
    if token:
        return token

    if TOKEN_FILE.exists():
        token = TOKEN_FILE.read_text(encoding="utf-8").strip()
        if token:
            return token

    token = get_token_from_azure_cli()
    if token:
        return token

    raise SystemExit(
        "No Fabric token found. Use one of these:\n"
        "  set FABRIC_BEARER_TOKEN=your-token\n"
        "  python tools/fabric/fabric_push.py --save-token your-token\n"
        "  az login"
    )


def get_token_from_azure_cli() -> str:
    az_exe = shutil.which("az") or shutil.which("az.cmd")
    if not az_exe:
        return ""

    try:
        completed = subprocess.run(
            [
                az_exe,
                "account",
                "get-access-token",
                "--resource",
                "https://api.fabric.microsoft.com",
                "--output",
                "json",
            ],
            check=False,
            capture_output=True,
            text=True,
        )
    except FileNotFoundError:
        return ""

    if completed.returncode != 0:
        return ""

    try:
        return json.loads(completed.stdout).get("accessToken", "").strip()
    except json.JSONDecodeError:
        return ""


def save_token(token: str) -> None:
    TOKEN_FILE.write_text(token.strip(), encoding="utf-8")
    try:
        TOKEN_FILE.chmod(0o600)
    except OSError:
        pass
    print(f"Token saved to {TOKEN_FILE}")


def encode_part(path: str, content: str) -> dict[str, str]:
    payload = base64.b64encode(content.encode("utf-8")).decode("ascii")
    return {"path": path, "payload": payload, "payloadType": "InlineBase64"}


def build_definition(pipeline_path: Path, platform_path: Path | None = None) -> dict[str, Any]:
    pipeline_json = read_json(pipeline_path)

    if "definition" in pipeline_json and isinstance(pipeline_json["definition"], dict):
        return pipeline_json["definition"]

    if "parts" in pipeline_json and isinstance(pipeline_json["parts"], list):
        return pipeline_json

    if "properties" not in pipeline_json:
        raise SystemExit(
            "This does not look like a Fabric pipeline-content JSON. "
            "Expected top-level 'properties' or an exported 'definition'."
        )

    pretty = json.dumps(pipeline_json, indent=2, ensure_ascii=False)
    parts = [encode_part("pipeline-content.json", pretty)]

    if platform_path:
        parts.append(encode_part(".platform", platform_path.read_text(encoding="utf-8")))

    return {"parts": parts}


def request_json(method: str, url: str, token: str, body: dict[str, Any] | None = None) -> tuple[int, dict[str, str], str]:
    data = None
    headers = {"Authorization": f"Bearer {token}"}

    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method)

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.status, dict(response.headers), response.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as exc:
        return exc.code, dict(exc.headers), exc.read().decode("utf-8", errors="replace")
    except urllib.error.URLError as exc:
        raise SystemExit(f"Network error: {exc}") from exc


def pipeline_id_for(path: Path, config: dict[str, Any], explicit_id: str | None) -> tuple[str, str]:
    name = path.stem
    if explicit_id:
        return name, explicit_id

    pipeline_id = config["pipelines"].get(name)
    if not pipeline_id:
        known = ", ".join(sorted(config["pipelines"])) or "(none)"
        raise SystemExit(f"Unknown pipeline '{name}'. Known pipelines: {known}. Or pass --pipeline-id.")

    return name, pipeline_id


def push_pipeline(args: argparse.Namespace) -> None:
    config = load_config(args.config)
    pipeline_path = args.file.resolve()
    if not pipeline_path.exists():
        raise SystemExit(f"File not found: {pipeline_path}")

    name, pipeline_id = pipeline_id_for(pipeline_path, config, args.pipeline_id)
    definition = build_definition(pipeline_path, args.platform)
    content_part = next((p for p in definition.get("parts", []) if p.get("path") == "pipeline-content.json"), None)

    if args.dry_run:
        print(f"DRY RUN: would push {name} to pipeline {pipeline_id}")
        print(f"Workspace: {config['workspace_id']}")
        print(f"Definition parts: {', '.join(p.get('path', '?') for p in definition.get('parts', []))}")
        if content_part:
            raw = base64.b64decode(content_part["payload"]).decode("utf-8")
            activity_count = len(json.loads(raw).get("properties", {}).get("activities", []))
            print(f"Activities: {activity_count}")
        return

    token = get_token()
    update_metadata = "True" if args.update_metadata else "False"
    url = (
        f"{FABRIC_API_BASE}/workspaces/{config['workspace_id']}"
        f"/dataPipelines/{pipeline_id}/updateDefinition?updateMetadata={update_metadata}"
    )

    status, headers, text = request_json("POST", url, token, {"definition": definition})

    if status == 200:
        print(f"Pushed: {name}")
        return

    if status == 202:
        print(f"Accepted: {name}. Fabric is applying the update.")
        if args.wait and headers.get("Location"):
            wait_for_operation(headers["Location"], token, int(headers.get("Retry-After", "10")))
        return

    print(f"Failed: HTTP {status}")
    if text:
        print(text[:1000])
    raise SystemExit(1)


def wait_for_operation(url: str, token: str, delay_seconds: int) -> None:
    for _ in range(30):
        time.sleep(max(delay_seconds, 1))
        status, headers, text = request_json("GET", url, token)
        delay_seconds = int(headers.get("Retry-After", str(delay_seconds)))
        if status >= 400:
            print(f"Operation check failed: HTTP {status}")
            print(text[:1000])
            raise SystemExit(1)
        if not text:
            continue
        payload = json.loads(text)
        state = payload.get("status") or payload.get("state")
        print(f"Operation status: {state or 'unknown'}")
        if str(state).lower() in {"succeeded", "completed", "failed", "cancelled"}:
            if str(state).lower() not in {"succeeded", "completed"}:
                raise SystemExit(1)
            return
    raise SystemExit("Timed out waiting for Fabric operation.")


def list_pipelines(config_path: Path) -> None:
    config = load_config(config_path)
    print(f"Workspace: {config['workspace_id']}")
    for name, pipeline_id in sorted(config["pipelines"].items()):
        print(f"{name}: {pipeline_id}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Push local pipeline JSON to Microsoft Fabric.")
    parser.add_argument("file", nargs="?", type=Path, help="Pipeline JSON file to push.")
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG, help="Pipeline map config file.")
    parser.add_argument("--pipeline-id", help="Pipeline item ID. Overrides config lookup by filename.")
    parser.add_argument("--platform", type=Path, help="Optional .platform file to include.")
    parser.add_argument("--update-metadata", action="store_true", help="Allow .platform metadata updates.")
    parser.add_argument("--dry-run", "-d", action="store_true", help="Validate locally without pushing.")
    parser.add_argument("--wait", action="store_true", help="Wait for long-running Fabric update to finish.")
    parser.add_argument("--list", "-l", action="store_true", help="List known pipelines.")
    parser.add_argument("--save-token", metavar="TOKEN", help="Save a Fabric bearer token locally.")
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.save_token:
        save_token(args.save_token)
        return

    if args.list:
        list_pipelines(args.config)
        return

    if not args.file:
        raise SystemExit("Provide a pipeline JSON file, or use --list / --save-token.")

    push_pipeline(args)


if __name__ == "__main__":
    main()
