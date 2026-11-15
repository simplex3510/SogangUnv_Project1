# unity-ai-template

[English](README-en.md)

Claude Code로 Unity 게임을 만들기 위한 private 템플릿이에요. CCGS(Claude Code Game Studios)에 Ponytail, Graphify, Aseprite, Unity 플러그인과 CLI, git, gh를 얹었어요.

- 모든 세션이 읽는 규칙: `.claude/docs/framework/integration.md`
- 설계와 근거: `docs/framework-design.md`
- CCGS 자체 안내: https://github.com/Donchitos/Claude-Code-Game-Studios

## 새 PC에서

```powershell
git clone https://github.com/<계정>/<게임>.git
cd <게임>
pwsh scripts/bootstrap.ps1
```

## 새 게임 만들기

```powershell
pwsh scripts/new-game.ps1 <게임-slug>
```

그다음 새 폴더에서 Claude Code를 열고 `/start`를 실행하세요.

## PC를 옮길 때

떠나는 PC에서 `/handoff`, 도착한 PC에서 `/pickup`.

## 업데이트

- CCGS 변경을 템플릿에: `git fetch ccgs && git merge ccgs/main`
- 템플릿 변경을 게임에: `git fetch template && git merge template/main`
