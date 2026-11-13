# Unity Safety Rules

These rules protect files that Unity generates or tracks by GUID. Editing them as text can silently break references.
When a change to a protected file is needed:
- If the Unity CLI is connected to this project (`unity status` shows it in `ready` state), perform the change through Editor commands (`unity command ...`) instead of editing the file.
- Otherwise, guide the user through the Unity Editor procedure.
Direct text editing of protected files remains forbidden in both cases.

## `.meta` Files
- Do not create, modify, or delete `.meta` files directly
- Do not delete, move, or rename any file or folder under `Assets/` directly. The `.meta` pair would go out of sync and break GUID references. Use `unity command asset move|rename|delete` when connected; otherwise guide the user to do it in the Unity Editor (Project window).
- Creating a new `.cs` file is allowed only when the user requests it. Unity generates its `.meta` file when the Editor next refreshes.

## Unity YAML Assets (Never Edit Directly)
- Scenes (`.unity`), prefabs (`.prefab`)
- ScriptableObject assets (`.asset`)
- Materials (`.mat`), animations (`.anim`), animator controllers (`.controller`), sprite atlases (`.spriteatlas`), and other Unity-serialized YAML assets
- Everything under `ProjectSettings/`
- For any needed change, use connected Editor commands (for example `component set_field`, `prefab ...`, `asset create`). When the CLI is not connected, provide step-by-step Editor instructions (which asset, which Inspector field, which value) and, when useful, the expected result.

## Editor Wiring Through the CLI
- Scene wiring is part of the pipeline, not a handoff to the user: create objects with `gameobject create`, add components with `component add`, connect fields and asset references with `component set_field`, then persist with `scene save` / `editor save_all`.
- Before saving, tell the user if the open scene already has unsaved changes, since saving writes those changes too.
- Values changed only for verification (for example a shortened deadline) must be restored and saved again before reporting.
- If the CLI is not connected, hand the same steps to the user as explicit Editor instructions.

## Package Files
- Do not edit `Packages/manifest.json` or `Packages/packages-lock.json` directly. Use `unity command package add|remove|resolve` when connected; otherwise guide the user to use the Package Manager.
- Reading these files to check package versions is allowed.

## Generated Folders (Do Not Touch)
- Do not read for analysis, edit, or delete anything under `Library/`, `Temp/`, `Logs/`, `obj/`, or `UserSettings/`.
- To read the Editor console, use `unity command console get|status` instead of reading `Logs/` directly.

## Assembly Definitions (`.asmdef`)
- Before changing any `.asmdef` (references, `noEngineReferences`, defines, platforms), explain the scope of impact (which assemblies are affected, and what could fail to compile), then get approval.
- Never add a reference that breaks the confirmed dependency direction.
- Creating a new `.asmdef` follows the same rule: propose it, explain the impact, then proceed on approval.

## Temporary Scripts Slated for Removal
- Do not delete them on your own. Always confirm with the user before deletion. When connected, delete via `unity command asset delete --asset <path> --confirm true`; otherwise guide the deletion as an Editor operation.
- Remove this section once both scripts have been deleted.

## Serialization Cautions
- Do not rename or change the type of a `[SerializeField]` field, or rename a serialized class, without warning the user. Doing so can silently drop values already set in scenes, prefabs, or assets. Explain the impact first, and mention `[FormerlySerializedAs]` when renaming is intended.
- Do not rename a MonoBehaviour or ScriptableObject class unless its file name is changed to match. When connected, rename via `unity command asset rename` or `asset move` so the GUID and file name stay consistent; otherwise guide that rename through the Editor.