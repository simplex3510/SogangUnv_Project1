---
paths:
  - "Assets/**"
  - "Packages/**"
  - "ProjectSettings/**"
---

# Unity Asset Rules

Unity tracks assets by GUID through `.meta` files and stores serialized values by
field name. A file-system or text change the Editor did not make can break
references with no error. `scene-files.md` covers scene and prefab YAML; these
rules cover the rest.

Below, "through the Editor" means: with an Editor connected (`unity status` is
`ready`), run the matching `unity command …` (find it with `unity command --query
<term>`); with none connected, give the user the Editor steps.

## Moving, renaming and deleting
- Never move, rename or delete a file or folder under `Assets/` with the shell or
  file tools. Do it through the Editor, so the `.meta` file goes with it.
- A new `.cs` file may be written directly; the Editor creates its `.meta` on the
  next refresh.

## Serialization
- Before renaming a `[SerializeField]` field or changing its type, or renaming a
  serialized class, tell the user that values already set in scenes, prefabs and
  assets will be lost, and offer `[FormerlySerializedAs("oldName")]` for a rename.
- A MonoBehaviour or ScriptableObject class name must match its file name. Rename
  both together, through the Editor.

## Packages and project settings
- Never edit `Packages/manifest.json`, `Packages/packages-lock.json` or files
  under `ProjectSettings/` by hand. Add, remove or change them through the Editor
  (Package Manager, Project Settings). Reading them is fine.
- Explain the impact of an `.asmdef` change (which assemblies, what may stop
  compiling) before making it.

## Generated folders
- Do not read, edit or delete `Library/`, `Temp/`, `Logs/`, `obj/` or
  `UserSettings/`. Read the Editor console through the CLI, not `Logs/`.

## Editing through the CLI
- After changing C# with an Editor connected, check it compiles: `unity
  recompile`, then read the console for errors and warnings.
- Before saving a scene, tell the user if it already has unsaved changes: the
  save writes theirs too.
- Restore and save again any value changed only to verify something before you
  report.
