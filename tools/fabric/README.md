# Fabric pipeline push helper

This folder lets Codex/Cursor push a local Microsoft Fabric Data Pipeline JSON file directly to Fabric without opening the UI.

## Is it possible?

Yes. Microsoft Fabric supports updating a Data Pipeline definition through the REST API. The raw pipeline JSON is sent as the `pipeline-content.json` definition part, base64 encoded, to:

```text
POST /workspaces/{workspaceId}/dataPipelines/{dataPipelineId}/updateDefinition
```

## One-time setup

1. Confirm the workspace and pipeline IDs in `fabric_pipelines.json`.
2. Sign in with Azure CLI:

```powershell
az login
```

The helper will use your Azure CLI login to get a fresh Fabric token when it pushes.

You can also save a bearer token, but this usually only helps briefly because access tokens expire:

```powershell
py -3 tools/fabric/fabric_push.py --save-token "YOUR_TOKEN"
```

## Daily use

Check first:

```powershell
py -3 tools/fabric/fabric_push.py Execute_Notes.json --dry-run
```

Push:

```powershell
py -3 tools/fabric/fabric_push.py Execute_Notes.json --wait
```

Then you can tell Codex:

```text
Edit Execute_Notes.json and push it to Fabric.
```

Codex can edit the JSON and run the push command for you.
