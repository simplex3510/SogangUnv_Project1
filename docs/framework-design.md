# Unity AI 게임 개발 프레임워크 — 설계서

> 구성: CCGS(Claude Code Game Studios) · Ponytail · Graphify · Unity 플러그인/CLI · Aseprite · git · gh
> 검증한 버전: CCGS 1.3.0 (`abc0de7`) · Ponytail 5.2.0 · Unity plugin 0.1.9-beta · Unity CLI 1.0.0 · graphify 0.9.88 · aseprite 0.8.2 · git 2.56.1 · git-lfs 3.8.1 · gh 2.101.0
> 사용법은 [사용 가이드](framework-guide.md), 매 세션 로드되는 규칙은 [`integration.md`](../.claude/docs/framework/integration.md). 이 문서는 그 규칙이 왜 그렇게 정해졌는지를 담는다.

---

## 1. 설계 결정

| 항목 | 결정 |
|---|---|
| 작업 흐름 | **CCGS 협업 흐름 하나.** 기획·설계·게이트·리뷰는 CCGS 스킬, 구현은 `/story-readiness` → `/dev-story` → `/story-done` |
| Ponytail 범위 | 코드를 쓰는 에이전트에만 주입. 기획·내러티브·아트·프로듀서 계열은 제외 |
| 사용 형태 | 1인, 여러 PC. 플러그인·설정을 저장소에 고정하고 clone + bootstrap 한 번으로 같은 환경 |
| CCGS 관리 | `ccgs` remote로 원본을 추적. 우리 변경은 오버레이 파일 위주, CCGS 파일 패치는 5개 |
| 기억 | 프로젝트 지식·공유 메모리·세션 인계 모두 게임 저장소 안에서 git으로 이동 (C5) |
| PC 이동 | `/handoff`(떠나는 PC) / `/pickup`(도착한 PC) |
| GitHub | trunk 기반, GitHub MCP 대신 `gh` CLI (C9) |
| 업데이트 | 게임은 마일스톤 사이에 템플릿 태그만 받고, PC 도구는 최신을 따라가되 bootstrap이 점검 (5절) |
| Graphify | 게임 코드·게임 문서만 추적(`.graphifyignore`가 프레임워크·Unity 설정·이미지 제외). post-commit 훅으로 자동 갱신(LLM 없음), `graphify-out/`은 커밋 안 함 |
| Unity | Unity 6 + C# + Pipeline 패키지 + CLI만 고정. 2D/3D·렌더 파이프라인은 프로젝트마다 `/setup-engine`에서 |
| 언어 | 대화는 한국어. 문서 본문 한국어, 템플릿 제목·YAML 키·상태값은 영어(CCGS 스크립트가 영어 문자열을 찾음). 코드 식별자 영어·주석 한국어, 커밋은 `feat: 한국어 요약`. 프레임워크 문서는 한국어만 둔다: 사용자가 한국어 사용자 한 명이고, Claude가 매 세션 읽는 규칙(integration.md)은 이미 영어라 영어판의 토큰 이점보다 두 벌 유지 비용이 크다 |

### 쓰지 않는 것

| 대상 | 이유 |
|---|---|
| OMC (oh-my-claudecode) | CCGS가 구현·팀 조율·리뷰를 이미 갖춤. Windows에서 프롬프트마다 훅 지연이 8초가 넘었고, `git commit` 자동 승인과 키워드 자율 모드가 CCGS 규칙과 충돌 |
| 별도 기억 저장소 | 저장소가 늘고 협업자와 공유되지 않음. 게임 저장소의 `.claude/memory/`로 대체 |
| Unity MCP 서버 | Unity CLI가 더 많은 기능을 제공하고, Unity 스킬 스스로 "셸이 있으면 CLI"를 요구 |
| GitHub MCP 서버 | `gh`로 충분 |

---

## 2. 구조

```
┌──────────────── 프로세스 (무엇을, 언제) ─────────────────────────────┐
│ CCGS: 단계 게이트 · GDD · ADR · 에픽/스토리 · 구현 · 리뷰 · 출시     │
│ Ponytail: 코드 에이전트에 최소 변경 규칙                              │
└───────────────┬──────────────────────────────────────────────────┘
┌───────────────▼──────── 도메인 도구 ────────────────────────────────┐
│ Unity plugin 스킬 + Unity CLI(Pipeline)  │  Aseprite (아트)          │
└───────────────┬──────────────────────────────────────────────────┘
┌───────────────▼──────── 지식 · 이동 ────────────────────────────────┐
│ Graphify 코드 그래프 │ .claude/memory │ /handoff·/pickup │ git·gh    │
└──────────────────────────────────────────────────────────────────┘
```

구성요소가 Claude Code에 닿는 지점:

| 표면 | CCGS | Ponytail | Graphify | Unity plugin | Aseprite |
|---|---|---|---|---|---|
| CLAUDE.md | 소유 | — | — (설치 명령 안 씀) | — | — |
| Hooks | 8개 이벤트 (bash) | SessionStart·SubagentStart·UserPromptSubmit | git 훅 | — | SessionStart |
| Agents | 49개 | — | — | — | 4개 |
| Skills | 78개 | 6개 | 1개 (전역) | 33개 | 14개 |
| MCP | — | — | — | 안 씀 | aseprite 브리지 |

---

## 3. 충돌 지점과 해결

### C1 Ponytail ↔ CCGS 코딩 표준

- Ponytail은 "최소 코드", CCGS 규칙은 "반드시 할 것"(테스트, Addressables, 새 Input System 등)을 요구한다.
- 우선순위: CCGS 규칙·ADR·control manifest가 **무엇이 필수인지** 정하고, Ponytail은 그 밖의 것만 깎는다.
- 주입 대상은 `settings.json`의 `env.PONYTAIL_SUBAGENT_MATCHER`(에이전트 이름 정규식):
  `programmer|unity-.*specialist|prototyper|technical-artist|devops|qa-tester|general-purpose`
  - 제외: `*-director`, `*-designer`, `producer`, `writer`, Aseprite 에이전트 등. 메인 세션은 전역 설치로 항상 켜짐.

### C2 Statusline

프로젝트 설정이 우선이라 CCGS `statusline.sh`가 쓰인다. 다른 것이 필요하면 PC별 `.claude/settings.local.json`에서 override.

### C3 이름 충돌

| 이름 | 결과 | 쓰는 것 |
|---|---|---|
| `/code-review` | 프로젝트 안에서는 CCGS 스킬만 보임 | CCGS 것 |
| `/help` | CLI 내장이 이김 | `/next-step` |
| `/resume` | CLI 내장 | 인계 스킬은 `/pickup` |
| `/security-audit` ↔ 내장 `security-review` | 역할이 다름 | 게임 보안은 CCGS, 일반 코드 취약점은 내장 |

리뷰 분담: 구현 직후 `/ponytail-review`(과잉 구현) → 스토리 완료 시 CCGS `/code-review` → `/story-done` → 마일스톤에 `/architecture-review`.

### C4 CLAUDE.md 소유권

CCGS가 CLAUDE.md를 계속 고치므로 우리 규칙은 전부 `integration.md`에 두고 CLAUDE.md에는 import 한 줄만 둔다. `graphify claude install`도 쓰지 않고 같은 내용을 integration.md에 직접 적었다. `CLAUDE.local.md`는 PC 간에 안 넘어가므로 공용 규칙을 두지 않는다.

### C5 기억과 여러 PC

CCGS는 `production/session-state/`(active.md)를 의도적으로 gitignore한다. 그래서 다음 세션이 알아야 할 것은 아래처럼 git으로 넘긴다.

| 층 | 위치 | PC 간 이동 |
|---|---|---|
| 프로젝트 지식 (GDD, ADR, 스토리, 스프린트) | `design/`, `docs/`, `production/` | git |
| 공유 메모리 | `.claude/memory/` (`MEMORY.md`는 `merge=union`) | git |
| 세션 인계 | `production/handoff.md` | `/handoff` → `/pickup` |
| 코드 그래프 | `graphify-out/` | PC마다 재생성 |
| 세션 체크포인트 | `production/session-state/` | 로컬 (인계로 넘어감) |

- `autoMemoryDirectory`는 절대경로만 받으므로 커밋되는 `settings.json`에 넣을 수 없다. bootstrap이 PC마다 `settings.local.json`에 기록한다.
- 공유 메모리에는 게임과 작업 방식에 관한 사실만 저장한다. 개인 선호는 각자 `~/.claude/CLAUDE.md`.
- `/handoff`는 `active.md`를 `handoff.md`로 복사해 커밋·push하고, `/pickup`은 pull 후 되돌려 놓는다. 인계 뒤에 다른 커밋이 있으면 `/pickup`이 경고한다. 두 스킬은 사용자가 직접 입력할 때만 실행된다(입력 자체가 커밋 지시).

### C6 Unity: CCGS 엔진 에이전트 ↔ Unity 플러그인 스킬

- Unity 조작은 CLI 한 경로: `unity command … --caller plugin --skill <스킬명>`. 에디터가 열려 있으면 `.unity`/`.prefab` 손편집 금지.
- 역할: CCGS `unity-*-specialist`가 판단, Unity 스킬이 절차. 매핑표는 integration.md에만 둔다.
- **서브에이전트에는 Skill 도구가 없다.** 그래서 메인 세션이 스킬을 불러와 필요한 절차를 에이전트 지시에 담아 넘긴다. 에이전트 파일에 Skill 도구를 추가하면 CCGS 파일 패치가 늘어 merge 충돌이 잦아지므로 택하지 않았다.
- 새 프로젝트: CCGS `/setup-engine`은 Unity 프로젝트를 만들지 않고 `ProjectSettings/ProjectVersion.txt`를 감지만 한다. 그래서 Unity 프로젝트를 먼저 만든다.
  1. `unity projects create <이름> --output-dir <임시 폴더> --editor-version <버전> --no-cloud` (CLI는 항상 새 폴더를 만듦)
  2. `Assets/`, `Packages/`, `ProjectSettings/`만 저장소 루트로 옮긴다. `Library/`는 옮기지 않는다: 임포트 캐시가 임시 폴더 절대경로를 기억해 URP 셰이더 임포트가 매번 실패한다.
  3. `unity projects remove <임시 경로>` → `unity projects add <저장소>` (임시 경로는 `unity projects --all`에 나온 표기 그대로)
  4. `unity pipeline install --project-path <저장소>` (Unity 6.6 템플릿엔 이미 포함)
  5. `/start` → `/setup-engine unity`. Unity의 `new-unity-project` 인터뷰는 쓰지 않는다.

### C7 Aseprite ↔ CCGS 아트

`/art-bible`(art-director) → `aseprite:*`로 그리기 → `aseprite:export`로 `Assets/Art/` → Unity `sprite-editor`·`manage-sprite-atlas`·`2d-pixel-perfect`. `aseprite:pixel-critic` 결과는 `/asset-audit`에 첨부.

### C8 Windows 전제조건

CCGS 훅은 Git Bash가 없으면 조용히 꺼진다. PATH의 `bash`는 WSL일 수 있으므로 bootstrap은 `git.exe` 위치에서 `bin\bash.exe`를 찾는다.

### C9 git · gh

| 문제 | 해결 |
|---|---|
| gh는 기본 저장소가 없으면 `upstream` remote를 골라 CCGS 공개 저장소를 대상으로 삼는다 | CCGS remote 이름은 `ccgs`(push 주소 `DISABLED`), bootstrap이 `gh repo set-default`, deny `gh * --repo *`/`-R *` |
| git-lfs와 Graphify가 같은 post-commit 훅을 씀 | 순서 고정: `git lfs install --local` → `graphify hook install` |
| private 저장소는 공개 저장소의 fork가 될 수 없음 | fork 대신 `ccgs` remote |
| GitHub "Use this template"는 히스토리를 끊음 | `new-game.ps1`이 clone으로 만들고 `template` remote를 남김 |
| `credential.helper=store`면 토큰이 평문 | bootstrap이 경고, `gh auth setup-git` 권장, deny `Read(~/.git-credentials)` |
| Unity YAML 병합·줄바꿈 | `.gitattributes`에 `eol=lf merge=unityyamlmerge`, `Assets/` 바이너리는 LFS |
| README 하나를 CCGS·템플릿·게임이 함께 씀 | 사용 가이드는 `docs/framework-guide.md`로 분리. `README.md merge=ours`로 템플릿은 CCGS README를, 게임은 자기 README를 지킴. `new-game.ps1`이 게임 README를 첫 커밋으로 만들어 이후 merge에서 항상 게임 쪽이 남음 |

커밋은 사용자 지시가 있을 때만(`git commit`/`git tag`는 `ask`). 워크트리는 `Library/` 재임포트가 무거워 꼭 필요할 때만 쓰고, 워크트리 커밋은 Graphify를 갱신하지 않는다. CI(Unity 빌드)는 라이선스 시크릿과 private 저장소 실행 시간 제한 때문에 범위 밖.

---

## 4. 저장소 구성

### remote

```
템플릿:  origin   = github.com/<나>/unity-ai-template (private)
         ccgs     = github.com/Donchitos/Claude-Code-Game-Studios (push DISABLED)
게임:    origin   = github.com/<나>/<게임> (private)
         template = github.com/<나>/unity-ai-template
```

### 고친 CCGS 파일 (merge 때 확인할 곳)

| 파일 | 변경 |
|---|---|
| `CLAUDE.md` | `@.claude/docs/framework/integration.md` import 1줄 |
| `.claude/settings.json` | 추가만: `extraKnownMarketplaces`(`autoUpdate: true`), `enabledPlugins`, `env`, deny 3개, ask 2개 |
| `.gitignore` | `graphify-out/`, `*.slnx`, `.vscode/` |
| `.gitattributes` | Unity YAML·LFS·`MEMORY.md merge=union`·`README.md merge=ours` |
| `README.md` | 템플릿 소개와 가이드 링크로 교체 |

Godot/Unreal 에이전트·스킬은 지우지 않는다. 지우면 CCGS가 그 파일을 고칠 때마다 merge 충돌이 난다.

### 추가한 파일

```
.claude/docs/framework/integration.md   # 매 세션 로드되는 규칙
.claude/memory/MEMORY.md                # 공유 메모리
.claude/skills/handoff/, pickup/        # PC 이동
scripts/bootstrap.ps1                   # PC 셋업·점검 (재실행 가능)
scripts/new-game.ps1                    # 새 게임 저장소 생성
scripts/template.url                    # 게임 저장소가 template remote를 복구할 주소
.graphifyignore                         # Graphify가 추적하지 않을 프레임워크 파일
docs/framework-guide.md                 # 사용 가이드 (게임에도 그대로 따라감)
docs/framework-design.md                # 이 문서
```

### `settings.json`에 추가한 키

```json
{
  "extraKnownMarketplaces": {
    "ponytail":                { "source": { "source": "github", "repo": "DietrichGebert/ponytail" } },
    "aseprite-ai-artist":      { "source": { "source": "github", "repo": "with-pebbly/aseprite-ai-artist" } },
    "claude-plugins-official": { "source": { "source": "github", "repo": "anthropics/claude-plugins-official" } }
  },
  "enabledPlugins": {
    "ponytail@ponytail": true,
    "unity@claude-plugins-official": true,
    "aseprite@aseprite-ai-artist": true
  },
  "env": { "PONYTAIL_SUBAGENT_MATCHER": "programmer|unity-.*specialist|…" },
  "permissions": {
    "deny": [ "Bash(gh * --repo *)", "Bash(gh * -R *)", "Read(~/.git-credentials)" ],
    "ask":  [ "Bash(git commit*)", "Bash(git tag*)" ]
  }
}
```

OMC가 전역 설치된 PC라면 `"oh-my-claudecode@omc": false`를 추가해 이 저장소에서 꺼야 한다.

### `bootstrap.ps1`

1. 도구 확인: git, git-lfs, gh, node, unity, graphify, Git Bash, gh 로그인
2. git 훅: LFS → Graphify (LFS 훅이 이미 있으면 건너뜀)
3. merge 드라이버: `ours`, `unityyamlmerge`(설치된 Unity에서 경로 탐색)
4. `settings.local.json`에 `autoMemoryDirectory`
5. `gh repo set-default`, `template` remote 복구, `upstream` remote·평문 자격증명 경고
6. 버전 출력(템플릿 태그, 플러그인, Unity CLI, Graphify)과 Unity CLI 새 버전 안내. integration.md 라우팅표의 Unity 스킬이 설치된 플러그인에 모두 있는지 확인

---

## 5. 업데이트 정책

전부 최신으로 따라가면 개발 도중 작업 흐름이 흔들리고, 전부 유지하면 PC마다 버전이 갈라지고 수정을 못 받는다. 그래서 층마다 다르게 한다.

| 층 | 대상 | 방식 | 언제 |
|---|---|---|---|
| 게임 저장소 | 템플릿, Unity 에디터, Pipeline 패키지 | **고정.** 템플릿은 태그(`template-v*`)를 merge | 마일스톤 사이에만 |
| 템플릿 | CCGS | `git merge ccgs/main` → bootstrap 점검 → 짧은 드라이런 → 새 태그 | 필요한 기능이 생겼을 때나 몇 달에 한 번 |
| PC | Unity 플러그인·CLI, Ponytail, Aseprite, Graphify | **최신을 따라감.** 플러그인 3개 모두 `autoUpdate: true`(외부 마켓플레이스는 기본이 꺼짐이라 명시), CLI·Graphify는 bootstrap 안내를 보고 직접 | 언제든 |

- PC 층이 깨질 수 있는 곳은 우리 파일에 적힌 스킬 이름뿐이다. bootstrap이 라우팅표 이름을 실제 설치본과 대조해 잡는다.
- 플러그인 버전 고정(`ref`에 커밋 지정)은 가능하지만 올릴 때마다 커밋 ID를 바꿔야 하고, 공식 마켓플레이스는 예약 이름이라 고정이 안 될 수 있어 택하지 않았다.
- 템플릿 태그는 `template-v<번호>`. CCGS 히스토리에 딸려 온 `v*` 태그와 구분하기 위해서다.

---

## 6. 진행 상태

검증 완료 (Windows, 시험 저장소 `unity-ai-dryrun`):
- 새 게임 생성, bootstrap(첫 실행·재실행·두 번째 clone), 템플릿 → 게임 merge
- Unity 프로젝트 생성·Pipeline 설치·에디터 연결, CLI로 읽기·편집 왕복·명령 검색, 콘솔 에러 0
- `unity-specialist` 에이전트가 CLI로 에디터를 다루고 Ponytail 지시를 받음
- 세션 시작 훅, 공유 메모리 경로, 프로젝트 `env` 전달

남은 것:
- CCGS 대화형 흐름: `/start` → `/setup-engine unity` → 스토리 1개 `/story-readiness` → `/dev-story` → `/story-done`
- 실제 PC 이동: `/handoff` → 다른 PC에서 bootstrap → `/pickup`
- 한국어 규칙과 Unity 스킬 전달 규칙이 실제 세션에서 지켜지는지
- 첫 템플릿 태그 `template-v0.1`

---

## 7. 리스크

- **PC 도구는 최신을 따라간다**: 플러그인이 동작 방식을 바꾸면 우리 규칙과 어긋날 수 있다. 스킬 이름은 bootstrap이 잡지만, 이름이 같고 내용이 바뀐 경우는 못 잡는다.
- **CCGS 업데이트**: 4절의 고친 파일 5개는 merge마다 확인. 특히 `settings.json`.
- **Unity 플러그인이 beta**: CLI 명령도 바뀔 수 있다. 새 프로젝트 절차(C6)는 플러그인을 올린 뒤 처음 쓸 때 확인한다.
- **`/handoff`를 잊으면 인계가 안 된다**: CCGS 규칙 때문에 자동 push는 없다.
- **공유 메모리 충돌**: `MEMORY.md`는 union merge지만 개별 메모리 파일은 수동 해결.
- **Git LFS 용량**: GitHub 무료 한도를 넘으면 데이터 팩이 필요하다.
