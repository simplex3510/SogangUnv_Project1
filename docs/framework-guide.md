# unity-ai-template 사용 가이드

Claude Code로 Unity 게임을 만드는 프레임워크예요. 이 저장소를 템플릿 삼아 게임마다 private 저장소를 만들고, Claude와 함께 기획부터 출시까지 진행해요.

| 구성요소 | 맡는 일 |
|---|---|
| **CCGS** (Claude Code Game Studios) | 작업 흐름 전체. 49개 전문 에이전트와 기획·설계·구현·리뷰·출시 명령 |
| **Unity 플러그인 + CLI** | Claude가 열린 Unity 에디터를 직접 조작 (씬·오브젝트·컴포넌트·패키지·빌드) |
| **Ponytail** | 코드를 쓰는 에이전트가 필요한 만큼만 코드를 쓰게 함 |
| **Graphify** | 코드 구조를 그래프로 만들어 "이 클래스는 어디서 쓰여?" 같은 질문에 답함 |
| **Aseprite** | Claude가 Aseprite에서 픽셀 아트를 그리고 Unity로 내보냄 |
| **git · gh** | 저장소 생성, PC 간 이동, 템플릿·CCGS 업데이트 |

---

## 준비물

Windows 기준이에요. 모두 PATH에 있어야 해요.

- [Claude Code](https://claude.com/claude-code)
- Git for Windows (Git Bash 포함 — CCGS 훅이 Git Bash로 실행돼요), Git LFS
- GitHub CLI `gh` (`gh auth login`으로 로그인, `gh auth setup-git` 권장)
- Node.js (Ponytail)
- Unity CLI (`irm https://unity.com/install.ps1 | iex`)와 Unity Hub, Unity 6 에디터
- Graphify (`uv tool install graphifyy`)
- Aseprite (픽셀 아트를 쓸 때만)

플러그인(Ponytail, Unity, Aseprite)은 저장소 설정에 들어 있어서, 저장소에서 Claude Code를 처음 열면 설치를 안내해요.

---

## 시작하기

### 1. 새 게임 저장소 만들기

템플릿 폴더에서:

```powershell
pwsh scripts/new-game.ps1 <게임-slug>
```

GitHub에 private 저장소를 만들고, 템플릿 옆 폴더에 clone한 뒤 PC 셋업(`bootstrap.ps1`)까지 해요. 템플릿 업데이트를 나중에 받을 수 있게 `template` remote도 남겨요.

게임용 `README.md`도 함께 만들어져요. 이 파일은 게임 소개로 자유롭게 고치세요. 템플릿 업데이트를 받아도 덮어쓰이지 않아요(`.gitattributes`의 `README.md merge=ours`). 이 가이드는 `docs/framework-guide.md`에 있어서 템플릿 업데이트로 계속 최신이 돼요.

### 2. Unity 프로젝트 만들기

새 게임 폴더에서 Claude Code를 열고 "Unity 프로젝트 만들어줘"라고 하면, Claude가 정해진 절차대로 만들어요. 직접 하려면:

```powershell
unity projects create <이름> --output-dir <임시 폴더> --editor-version <버전> --no-cloud
# 임시 폴더에서 Assets/, Packages/, ProjectSettings/ 만 게임 저장소 루트로 옮기기 (Library/는 옮기지 않기)
unity projects remove <임시 폴더 경로>
unity projects add <게임 저장소 경로>
unity pipeline install --project-path <게임 저장소 경로>
```

### 3. CCGS 시작

```
/start
/setup-engine unity
```

`/setup-engine`이 Unity 버전을 감지해 기록해요. 그다음은 CCGS가 단계마다 다음 할 일을 안내해요. 막히면 `/next-step`.

---

## 게임 만드는 흐름

Claude는 매 단계 **질문 → 선택지 → 결정 → 초안 → 승인** 순서로 진행하고, 파일을 쓰기 전에 꼭 물어봐요.

| 단계 | 명령 |
|---|---|
| 아이디어 | `/brainstorm`, 빠르게 만들어 보기 `/prototype` |
| 기획 | `/map-systems` → 시스템마다 `/design-system` → `/design-review` |
| 아트 방향 | `/art-bible` |
| 설계 | `/create-architecture`, `/architecture-decision`(ADR) → `/architecture-review` |
| 계획 | `/create-epics` → `/create-stories` → `/sprint-plan` |
| 구현 | `/story-readiness` → `/dev-story` |
| 리뷰·완료 | `/ponytail-review` → `/code-review` → `/story-done` |
| 점검 | `/sprint-status`, `/gate-check`, `/scope-check` |
| 출시 | `/release-checklist`, `/patch-notes` |

전체 명령과 단계 설명은 [CCGS 안내](https://github.com/Donchitos/Claude-Code-Game-Studios)를 보세요.

---

## Unity 에디터 다루기

에디터가 열려 있으면 Claude가 Unity CLI로 직접 조작해요. 평소 말로 요청하면 돼요.

- "Player에 Rigidbody2D 붙이고 중력 0으로 해줘"
- "Input System 패키지 추가해줘"
- "플레이 모드로 실행해서 콘솔 에러 확인해줘"
- "지금 씬 구조 보여줘"

알아둘 것:
- 에디터가 꺼져 있으면 Claude가 `unity open`으로 열 수 있어요.
- 에디터가 열린 동안 Claude는 `.unity`·`.prefab` 파일을 직접 고치지 않고 항상 에디터를 거쳐요.
- 씬 저장은 따로 요청하거나 에디터에서 직접 하세요.

---

## 픽셀 아트 (Aseprite)

Aseprite를 열어 둔 상태에서:

1. `/art-bible`로 스타일을 정해요.
2. "주인공 idle 애니메이션 그려줘"처럼 요청하면 Aseprite 스킬(`/aseprite:brief`, `draw`, `animate` 등)로 그려요.
3. `/aseprite:export`로 `Assets/Art/`에 내보내고, Unity 스프라이트 설정까지 이어서 해요.
4. `/asset-audit`으로 점검해요.

---

## 코드 구조 질문 (Graphify)

커밋할 때마다 코드 그래프(`graphify-out/`)가 자동으로 갱신돼요(LLM 비용 없음). "PlayerController는 어디서 참조돼?", "점프 입력이 이동 코드까지 어떻게 이어져?" 같은 질문에 Claude가 그래프를 먼저 써서 답해요.

- 그래프에는 게임 코드와 게임 문서만 들어가요. 프레임워크 파일(`.claude/`, CCGS 문서 등), Unity 설정, 이미지는 `.graphifyignore`로 빠져 있어요. 더 빼고 싶은 경로는 이 파일에 추가하세요.
- 기획 문서의 개념 관계까지 그래프에 넣고 싶으면 `/graphify --update`를 실행하세요(LLM 사용).

---

## 여러 PC에서 작업하기

| 상황 | 할 일 |
|---|---|
| PC를 떠날 때 | `/handoff` — 진행 상태를 커밋하고 push |
| 다른 PC에서 이어갈 때 | `/pickup` — pull 후 진행 상태 복원·요약 |
| 처음 쓰는 PC | 아래처럼 clone 후 bootstrap |

```powershell
git clone https://github.com/<계정>/<게임>.git
cd <게임>
pwsh scripts/bootstrap.ps1
```

`bootstrap.ps1`은 도구 확인, git 훅, merge 설정, 공유 메모리 경로, gh 기본 저장소를 맞추고, 도구 버전과 Unity 스킬 이름을 점검해요. 몇 번 다시 실행해도 괜찮고, 문제가 있으면 마지막에 목록으로 알려줘요.

### `bootstrap.ps1` 실행 시점

세션이 열릴 때 자동으로 돌지 않아요. 아래 경우에 직접 실행하거나 Claude에게 실행해 달라고 하세요.

| 언제 | 왜 |
|---|---|
| **꼭** 다른 PC에서 처음 clone했을 때 | git 훅, merge 설정, 메모리 경로, gh 기본 저장소, `template` remote는 clone에 따라오지 않아요 |
| **꼭** 저장소 폴더를 옮기거나 이름을 바꿨을 때 | 공유 메모리 경로가 절대경로로 저장돼 있어요 |
| **꼭** Unity 에디터를 새로 설치하거나 지웠을 때 | 씬·프리팹 병합 도구(UnityYAMLMerge) 경로를 다시 찾아요 |
| 템플릿 태그나 CCGS를 merge한 뒤 | 바뀐 규칙과 설치된 도구가 맞는지 확인해요 |
| 가끔 (예: 마일스톤 시작 때) | 플러그인은 자동 업데이트되므로, 스킬 이름이 바뀌었거나 Unity CLI 새 버전이 나왔는지 확인해요 |
| 문제가 생겼을 때 | 훅이 안 돌거나 `gh`가 엉뚱한 저장소를 가리킬 때 대부분 이걸로 고쳐져요 |

`new-game.ps1`로 새 게임을 만들 때는 자동으로 한 번 실행돼요.

**Claude의 기억**: 게임과 작업 방식에 관한 기억은 `.claude/memory/`에 저장돼 git으로 함께 이동해요. 개인 취향은 각자 `~/.claude/CLAUDE.md`에 두세요.

---

## 업데이트 받기

구성요소마다 받는 시점이 달라요.

| 대상 | 방법 | 언제 |
|---|---|---|
| Unity·Ponytail·Aseprite 플러그인 | 자동 (Claude Code가 백그라운드로 받고 다음 실행 때 적용) | 항상 |
| Unity CLI, Graphify | `unity self-update`, `uv tool upgrade graphifyy` | `bootstrap.ps1`이 새 버전을 알려줄 때 |
| 템플릿 개선 (게임 저장소) | 태그 단위로 merge | 마일스톤 사이에만 |
| CCGS 새 버전 (템플릿 저장소) | merge → 점검 → 새 태그 | 필요한 기능이 생겼을 때 |

```powershell
# 게임 저장소에서: 템플릿 새 태그 받기
git fetch template --tags
git merge template-v0.2

# 템플릿 저장소에서: CCGS 새 버전 받기
git fetch ccgs
git merge ccgs/main
pwsh scripts/bootstrap.ps1   # 점검 후 새 태그를 붙여요
```

플러그인이 업데이트되면서 스킬 이름이 바뀌면, `bootstrap.ps1`이 규칙 파일에 적힌 이름 중 사라진 것을 알려줘요.

---

## 정해진 규칙

- **언어**: Claude는 한국어로 대화해요. 문서 본문도 한국어지만, 템플릿 제목(`## Overview`)·상태값(`PASS`)은 CCGS 검사 스크립트 때문에 영어로 남아요. 커밋은 `feat: 점프 입력 추가` 형식.
- **커밋**: Claude는 지시가 있을 때만 커밋하고, 커밋·태그 전에 항상 확인을 받아요.
- **안전장치**: force push, `reset --hard`, `rm -rf`는 막혀 있어요. gh는 이 저장소에만 동작하고, CCGS 원본 저장소에 이슈나 PR을 올리지 않아요.

규칙 원문은 [`.claude/docs/framework/integration.md`](../.claude/docs/framework/integration.md), 설계 이유는 [`docs/framework-design.md`](framework-design.md)에 있어요.

---

## 문제 해결

| 증상 | 해결 |
|---|---|
| CCGS 훅이 동작하지 않음 | Git for Windows(Git Bash)를 설치하고 `bootstrap.ps1` 재실행 |
| Claude가 에디터에 연결 못 함 | `unity status`로 확인. 컴파일 에러가 있으면 Safe Mode라 연결이 안 돼요 — 에러부터 고치기 |
| 에디터를 열 때마다 셰이더 임포트 에러 | 다른 폴더에서 `Library/`를 옮겨 온 경우. 에디터를 닫고 `unity projects clean --yes` 후 다시 열기 |
| `gh`가 엉뚱한 저장소를 가리킴 | `bootstrap.ps1` 재실행 (`gh repo set-default`) |
| 다른 PC의 진행 상태가 안 보임 | 떠난 PC에서 `/handoff`를 했는지, push가 성공했는지 확인 |
