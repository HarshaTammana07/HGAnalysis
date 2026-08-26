"""Refresh P1 Reference bronze Copy translator mappings to PascalCase sink column names."""

from __future__ import annotations

import json
import re
from pathlib import Path

BASE_DIR = Path(__file__).parent
SOURCE_PATH = BASE_DIR / "pl_p1_reference.txt"

NOTEBOOK_SECTION_MARKER = (
    "================================================================================\n"
    "P1 REFERENCE BRONZE TO SILVER NOTEBOOKS"
)

METADATA_COLUMNS = [
    "SiteCode",
    "SourceDatabase",
    "IngestRunId",
    "ExtractedAt",
    "SourceQueryStartDate",
    "SourceQueryEndDate",
    "LookbackDate",
]

COPY_ACTIVITIES = {
    "cp_clinic_to_bronze",
    "cp_3p_setup_to_bronze",
    "cp_codes_to_bronze",
    "cp_services_to_bronze",
    "cp_dropdown_list_items_to_bronze",
    "cp_custom_answers_to_bronze",
    "cp_custom_questions_to_bronze",
    "cp_pre_admission_v6_to_bronze",
    "cp_preadmission_referral_source_to_bronze",
}


def to_pascal_column(name: str) -> str:
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


CHILD_JSON_HEADER = "Child json:"
PARENT_JSON_HEADER = "Parent Json:"
SILVER_CHILD_HEADER = "bronztosilverchild json(Inside):"


def split_reference_doc(text: str) -> tuple[str, dict, str, dict, str, dict, str]:
    notebook_index = text.index(NOTEBOOK_SECTION_MARKER)
    body = text[:notebook_index]
    notebook_section = text[notebook_index:]

    child_start = body.index(CHILD_JSON_HEADER)
    parent_start = body.index(PARENT_JSON_HEADER)
    silver_start = body.index(SILVER_CHILD_HEADER)

    child_prefix = body[: child_start + len(CHILD_JSON_HEADER)] + "\n\n"
    child_json_text = body[child_start + len(CHILD_JSON_HEADER) : parent_start].strip()

    parent_prefix = body[parent_start : parent_start + len(PARENT_JSON_HEADER)] + "\n"
    parent_json_text = body[parent_start + len(PARENT_JSON_HEADER) : silver_start].strip()

    silver_prefix = body[silver_start : silver_start + len(SILVER_CHILD_HEADER)] + "\n\n"
    silver_json_text = body[silver_start + len(SILVER_CHILD_HEADER) :].strip()

    return (
        child_prefix,
        json.loads(child_json_text),
        parent_prefix,
        json.loads(parent_json_text),
        silver_prefix,
        json.loads(silver_json_text),
        notebook_section,
    )


def load_pipeline_json(_: str) -> dict:
    raise NotImplementedError("Use split_reference_doc()")


def write_reference_doc(
    child_prefix: str,
    child_pipeline: dict,
    parent_prefix: str,
    parent_pipeline: dict,
    silver_prefix: str,
    silver_pipeline: dict,
    notebook_section: str,
) -> None:
    parts = [
        child_prefix,
        json.dumps(child_pipeline, indent=4),
        "\n\n\n\n",
        parent_prefix,
        json.dumps(parent_pipeline, indent=4),
        "\n\n",
        silver_prefix,
        json.dumps(silver_pipeline, indent=4),
        "\n\n",
        notebook_section,
    ]
    SOURCE_PATH.write_text("".join(parts), encoding="utf-8")


def extract_source_columns_from_sql(sql_value: str) -> list[str]:
    cols: list[str] = []
    seen: set[str] = set()

    def add(name: str) -> None:
        name = name.strip()
        if not name or name in seen:
            return
        seen.add(name)
        cols.append(name)

    for name in METADATA_COLUMNS:
        add(name)

    for match in re.finditer(r"\[([^\]]+)\] AS \[([^\]]+)\]", sql_value):
        add(match.group(2))

    for match in re.finditer(r" AS \[([^\]]+)\]", sql_value):
        add(match.group(1))

    for match in re.finditer(r"(\w+) = CHECKSUM\(", sql_value):
        add(match.group(1))

    for match in re.finditer(r"(\w+) = CASE WHEN", sql_value):
        add(match.group(1))

    if "c.*" in sql_value or "c.\\*" in sql_value:
        # Clinic uses explicit translator mappings; SQL also emits mapped helper columns.
        for match in re.finditer(r"AS \[(_[^\]]+_mapped)\]", sql_value):
            add(match.group(1))
        for match in re.finditer(r"AS \[(enableCommentsOnMultiCheckin|PullPicsFromDB)\]", sql_value):
            add(match.group(1))

    return cols


def mappings_for_columns(source_columns: list[str]) -> list[dict]:
    mappings: list[dict] = []
    seen: set[str] = set()
    for col in source_columns:
        key = col.lower()
        if key in seen:
            continue
        seen.add(key)
        mappings.append(
            {
                "source": {"name": col},
                "sink": {"name": to_pascal_column(col)},
            }
        )
    return mappings


def pascalize_existing_mappings(mappings: list[dict]) -> list[dict]:
    updated: list[dict] = []
    seen: set[str] = set()
    for item in mappings:
        source_name = item["source"]["name"]
        key = source_name.lower()
        if key in seen:
            continue
        seen.add(key)
        updated.append(
            {
                "source": {"name": source_name},
                "sink": {"name": to_pascal_column(source_name)},
            }
        )
    return updated


def walk_activities(activities: list[dict]) -> list[dict]:
    found: list[dict] = []
    for activity in activities:
        name = activity.get("name", "")
        if activity.get("type") == "Copy" and name in COPY_ACTIVITIES:
            found.append(activity)
        for key in ("activities", "ifTrueActivities", "ifFalseActivities"):
            nested = activity.get("typeProperties", {}).get(key)
            if isinstance(nested, list):
                found.extend(walk_activities(nested))
    return found


def apply_pascal_translators(pipeline: dict) -> int:
    updated = 0
    copy_activities = walk_activities(pipeline["properties"]["activities"])
    by_name = {activity["name"]: activity for activity in copy_activities}

    if set(by_name) != COPY_ACTIVITIES:
        missing = COPY_ACTIVITIES - set(by_name)
        extra = set(by_name) - COPY_ACTIVITIES
        raise RuntimeError(f"Copy activity mismatch. missing={sorted(missing)} extra={sorted(extra)}")

    for name, activity in sorted(by_name.items()):
        translator = activity["typeProperties"].setdefault("translator", {"type": "TabularTranslator"})
        sql_value = activity["typeProperties"]["source"]["sqlReaderQuery"]["value"]

        existing_mappings = translator.get("mappings")
        if existing_mappings:
            source_columns = [item["source"]["name"] for item in existing_mappings]
        else:
            source_columns = extract_source_columns_from_sql(sql_value)

        mappings = (
            pascalize_existing_mappings(existing_mappings)
            if existing_mappings
            else mappings_for_columns(source_columns)
        )

        translator["type"] = "TabularTranslator"
        translator["mappings"] = mappings
        translator["typeConversion"] = True
        translator["typeConversionSettings"] = {
            "allowDataTruncation": True,
            "treatBooleanAsNumber": False,
        }
        updated += 1

    return updated


def write_pipeline(source_text: str, child_pipeline: dict) -> None:
    (
        child_prefix,
        _,
        parent_prefix,
        parent_pipeline,
        silver_prefix,
        silver_pipeline,
        notebook_section,
    ) = split_reference_doc(source_text)
    write_reference_doc(
        child_prefix,
        child_pipeline,
        parent_prefix,
        parent_pipeline,
        silver_prefix,
        silver_pipeline,
        notebook_section,
    )


def main() -> None:
    source_text = SOURCE_PATH.read_text(encoding="utf-8")
    (
        child_prefix,
        child_pipeline,
        parent_prefix,
        parent_pipeline,
        silver_prefix,
        silver_pipeline,
        notebook_section,
    ) = split_reference_doc(source_text)
    count = apply_pascal_translators(child_pipeline)
    write_reference_doc(
        child_prefix,
        child_pipeline,
        parent_prefix,
        parent_pipeline,
        silver_prefix,
        silver_pipeline,
        notebook_section,
    )
    print(f"Updated PascalCase bronze Copy translators for {count} activities in {SOURCE_PATH}")


if __name__ == "__main__":
    main()
