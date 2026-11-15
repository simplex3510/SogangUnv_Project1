# unity-ai-template

Private template for Unity games made with Claude Code: CCGS (Claude Code Game Studios) plus Ponytail, Graphify, Aseprite, the Unity plugin and CLI, git and gh.

- Rules every session loads: `.claude/docs/framework/integration.md`
- Design and rationale: `docs/framework-design.md`
- CCGS's own guide: https://github.com/Donchitos/Claude-Code-Game-Studios

## New PC

```powershell
git clone https://github.com/<you>/<game>.git
cd <game>
pwsh scripts/bootstrap.ps1
```

## New game

```powershell
pwsh scripts/new-game.ps1 <game-slug>
```

Then open Claude Code in the new folder and run `/start`.

## Moving between PCs

`/handoff` on the PC you leave, `/pickup` on the PC you arrive at.

## Updates

- CCGS into the template: `git fetch ccgs && git merge ccgs/main`
- Template into a game: `git fetch template && git merge template/main`
