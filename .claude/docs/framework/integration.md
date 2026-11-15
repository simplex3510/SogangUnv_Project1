# Framework Integration Rules

This repo is CCGS plus a thin overlay for Unity, Ponytail, Graphify, Aseprite, git and gh.
Design and rationale: `docs/framework-design.md`. These rules win over plugin defaults.

## Language

The team is Korean. This rule wins over the English wording of every skill, agent, hook and template.

- Speak Korean to the user: replies, questions, AskUserQuestion questions and option labels/descriptions, summaries, verdict explanations, "May I write …?" prompts. Translate English skill text when you present it; keep commands, paths, file names and code as they are.
- Documents (GDD, ADR, stories, art bible, handoff notes): write the body in Korean, but keep the template's section headings, YAML keys and status/verdict values in English (`## Overview`, `Status: Accepted`, `PASS`/`FAIL`). CCGS scripts match those exact English strings.
- Code: identifiers in English, comments in Korean. Commit messages: Conventional Commits type in English, summary in Korean (`feat: 점프 입력 추가`).

## Work flow

- Design, planning and review follow CCGS skills. Implementation is `/story-readiness` → `/dev-story` → `/story-done`.
- Ponytail limits how much code gets written. CCGS rules, ADRs and the control manifest decide what is required; Ponytail never cuts a required item.
- Commits and tags only on the user's instruction (`git commit`/`git tag` ask first). Conventional Commits, story ID in the body.

## Unity

- Drive the Editor with the Unity CLI (`unity command … --caller plugin --skill <skill>`), never the Unity MCP server.
- Never hand-edit `.unity`, `.prefab`, `.asset` or `.meta` while an Editor may be open on this project.
- CCGS agents decide, Unity plugin skills supply the procedure:

| CCGS agent | Unity skills |
|---|---|
| unity-specialist | unity-cli, unity-package-management, create-2d-physics, physics-3d-collision, initialize-ai-navigation |
| unity-ui-specialist | ui → ui-uitk / ui-ugui, optimize-text-mesh-pro, localization |
| unity-shader-specialist, technical-artist | shader-graph-create-custom-node, urp-postprocessing, validate-urp-render-graph-renderer-feature, migrate-birp-to-urp |
| unity-addressables-specialist | manage-sprite-atlas |
| performance-analyst | optimize-audio, optimize-web |
| network-programmer, live-ops-designer | setup-multiplayer-services, build-live-game |

- Subagents have no Skill tool, so a CCGS agent cannot load these skills itself. Before you delegate Unity work, load the matching skill in the main session and copy the steps the task needs into the agent's prompt, along with `--project-path <project>` and `--caller plugin --skill <that skill>`. Where the prompt carries no procedure, the agent finds Editor commands with `unity command --query <term>`.

- New project: create the Unity project at the repo root first (CCGS never creates one): `unity projects create <name> --output-dir <scratch> --editor-version <ver> --no-cloud`, move only `Assets/`, `Packages/`, `ProjectSettings/` into the repo root (never `Library/`: its import cache keeps the scratch folder's absolute paths and URP `.urtshader` imports then fail on every Editor start), re-point the Hub (`unity projects remove <scratch path as listed by unity projects --all>`, `unity projects add <repo>`), `unity pipeline install`. Then `/start` → `/setup-engine unity`, which detects `ProjectSettings/ProjectVersion.txt` and records it. Do not run the `new-unity-project` interview.

## Art (Aseprite)

`/art-bible` sets the style → `aseprite:*` draws and exports into `Assets/Art/` → Unity `sprite-editor`, `manage-sprite-atlas`, `2d-pixel-perfect`. Attach `aseprite:pixel-critic` output to `/asset-audit`.

## Reviews

| When | Use |
|---|---|
| Right after implementing | `/ponytail-review` |
| Closing a story | `/code-review` (CCGS) → `/story-done` |
| Architecture or milestone | `/architecture-review` |

## Graphify

- If `graphify-out/graph.json` exists, answer code-structure questions with `graphify query`, `path` or `explain` before reading files.
- The graph holds only game code and game docs. `.graphifyignore` keeps out the framework (`.claude/`, CCGS docs, `scripts/`), Unity settings and images; to exclude more, add a path there.
- The post-commit hook rebuilds the graph from code and document headings, with no LLM. `/graphify --update` adds LLM-extracted concepts and links for changed game docs; run it only when that matters. Worktree commits do not refresh the graph.
- `graphify-out/` is not committed; each PC rebuilds it.

## Memory and moving between PCs

- Shared memory lives in `.claude/memory/` and is committed. Save only facts about this game and how the team works on it. Personal preferences go in your own `~/.claude/CLAUDE.md`.
- `production/session-state/active.md` is local. To continue on another PC: `/handoff` here, `/pickup` there.

## GitHub

- Use the `gh` CLI, not a GitHub MCP server. gh acts only on this repo's default (`gh repo set-default`); `--repo`/`-R` is denied.
- The CCGS remote is named `ccgs`, never `upstream` (gh would pick it). Never open issues or PRs there.
