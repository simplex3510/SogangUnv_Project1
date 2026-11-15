# unity-ai-template

[한국어](README.md)

A framework for making Unity games with Claude Code. Use this repository as a template: each game gets its own private repository, and you work with Claude from concept to release.

| Component | What it does |
|---|---|
| **CCGS** (Claude Code Game Studios) | The whole workflow: 49 specialist agents and commands for design, implementation, review and release |
| **Unity plugin + CLI** | Claude drives your open Unity Editor (scenes, GameObjects, components, packages, builds) |
| **Ponytail** | Agents that write code write only as much as the task needs |
| **Graphify** | Builds a code graph so Claude can answer "where is this class used?" |
| **Aseprite** | Claude draws pixel art in Aseprite and exports it to Unity |
| **git · gh** | Repository creation, moving between PCs, template and CCGS updates |

---

## Requirements

Windows. Everything must be on PATH.

- [Claude Code](https://claude.com/claude-code)
- Git for Windows (with Git Bash — CCGS hooks run in it), Git LFS
- GitHub CLI `gh` (log in with `gh auth login`; `gh auth setup-git` recommended)
- Node.js (Ponytail)
- Unity CLI (`irm https://unity.com/install.ps1 | iex`), Unity Hub and a Unity 6 Editor
- Graphify (`uv tool install graphifyy`)
- Aseprite (only for pixel art)

The plugins (Ponytail, Unity, Aseprite) are declared in the repository settings; Claude Code offers to install them the first time you open the repository.

---

## Getting started

### 1. Create a game repository

From the template folder:

```powershell
pwsh scripts/new-game.ps1 <game-slug>
```

This creates a private GitHub repository, clones it next to the template, and runs the PC setup (`bootstrap.ps1`). It keeps a `template` remote so you can pull template updates later.

### 2. Create the Unity project

Open Claude Code in the new game folder and ask "create the Unity project"; Claude follows the set procedure. To do it yourself:

```powershell
unity projects create <name> --output-dir <scratch folder> --editor-version <version> --no-cloud
# move only Assets/, Packages/, ProjectSettings/ from the scratch folder to the game repo root (never Library/)
unity projects remove <scratch folder path>
unity projects add <game repo path>
unity pipeline install --project-path <game repo path>
```

### 3. Start CCGS

```
/start
/setup-engine unity
```

`/setup-engine` detects and records the Unity version. From there CCGS tells you the next step at each stage. Stuck? `/next-step`.

---

## Making the game

At every step Claude goes **question → options → decision → draft → approval**, and asks before writing any file.

| Stage | Commands |
|---|---|
| Ideas | `/brainstorm`, quick try-outs with `/prototype` |
| Design | `/map-systems` → `/design-system` per system → `/design-review` |
| Art direction | `/art-bible` |
| Architecture | `/create-architecture`, `/architecture-decision` (ADR) → `/architecture-review` |
| Planning | `/create-epics` → `/create-stories` → `/sprint-plan` |
| Implementation | `/story-readiness` → `/dev-story` |
| Review and done | `/ponytail-review` → `/code-review` → `/story-done` |
| Checks | `/sprint-status`, `/gate-check`, `/scope-check` |
| Release | `/release-checklist`, `/patch-notes` |

For every command and stage, see the [CCGS guide](https://github.com/Donchitos/Claude-Code-Game-Studios).

---

## Working with the Unity Editor

When the Editor is open, Claude drives it through the Unity CLI. Just ask in plain words:

- "Add a Rigidbody2D to Player and set gravity to 0"
- "Add the Input System package"
- "Enter Play mode and check the console for errors"
- "Show me the current scene hierarchy"

Good to know:
- If the Editor is closed, Claude can open it with `unity open`.
- While the Editor is open, Claude never edits `.unity` or `.prefab` files directly; it always goes through the Editor.
- Saving the scene is your call: ask for it, or save in the Editor.

---

## Pixel art (Aseprite)

With Aseprite open:

1. Set the style with `/art-bible`.
2. Ask for art ("draw the hero's idle animation"); Claude uses the Aseprite skills (`/aseprite:brief`, `draw`, `animate`, …).
3. `/aseprite:export` writes to `Assets/Art/`, and Claude continues with the Unity sprite setup.
4. Check the result with `/asset-audit`.

---

## Questions about code structure (Graphify)

Every commit refreshes the code graph (`graphify-out/`). Claude uses it first for questions like "where is PlayerController referenced?" or "how does jump input reach the movement code?". After changing design docs, run `/graphify --update`.

---

## Working on several PCs

| When | Do |
|---|---|
| Leaving a PC | `/handoff` — commits and pushes where you are |
| Continuing on another PC | `/pickup` — pulls, restores and summarizes where you are |
| A PC you have not used yet | clone, then bootstrap (below) |

```powershell
git clone https://github.com/<you>/<game>.git
cd <game>
pwsh scripts/bootstrap.ps1
```

`bootstrap.ps1` checks the tools and sets up git hooks, merge drivers, the shared memory path and the gh default repository. It is safe to re-run and lists any problems at the end.

**Claude's memory**: what Claude learns about the game and how you work on it is saved in `.claude/memory/` and travels with git. Personal preferences go in your own `~/.claude/CLAUDE.md`.

---

## Updates

```powershell
# In the template repository: take a new CCGS version
git fetch ccgs
git merge ccgs/main

# In a game repository: take template improvements
git fetch template
git merge template/main
```

---

## Rules in place

- **Language**: Claude speaks Korean. Document bodies are Korean, but template headings (`## Overview`) and status values (`PASS`) stay English because CCGS scripts match them. Commits look like `feat: 점프 입력 추가`.
- **Commits**: Claude commits only when told to, and always asks before a commit or tag.
- **Safety**: force push, `reset --hard` and `rm -rf` are blocked. gh acts only on this repository and never files issues or PRs on the CCGS repository.

The rules themselves: [`.claude/docs/framework/integration.md`](.claude/docs/framework/integration.md). Why they are the way they are: [`docs/framework-design.md`](docs/framework-design.md).

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| CCGS hooks do nothing | Install Git for Windows (Git Bash) and re-run `bootstrap.ps1` |
| Claude cannot reach the Editor | Check `unity status`. With compile errors the Editor is in Safe Mode and cannot connect — fix the errors first |
| Shader import errors every time the Editor opens | `Library/` was moved from another folder. Close the Editor, run `unity projects clean --yes`, reopen |
| `gh` points at the wrong repository | Re-run `bootstrap.ps1` (`gh repo set-default`) |
| The other PC's progress is missing | Check that `/handoff` ran on the PC you left and that its push succeeded |
