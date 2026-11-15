# Unity AI 게임 개발 프레임워크 — 통합 설계서

> 구성: CCGS(Claude Code Game Studios) · Ponytail · Graphify · Unity 플러그인/CLI · Aseprite · git · gh
> 검증 기준 버전 (2026-11-15): CCGS 1.3.0 (`abc0de7`) · Ponytail 5.2.0 · Unity plugin 0.1.9-beta · Unity CLI 1.0.0 · graphify 0.9.88 · aseprite 0.8.2 · git 2.56.1 · git-lfs 3.8.1 · gh 2.101.0
> 세션마다 로드되는 규칙 요약은 [`.claude/docs/framework/integration.md`](../.claude/docs/framework/integration.md). 이 문서는 그 규칙의 근거와 구조를 담는다.

---

## 0. 확정된 설계 방향

| 항목 | 결정 |
|---|---|
| 작업 흐름 | **CCGS 협업 흐름 하나.** 기획·설계·게이트·리뷰는 CCGS 스킬, 구현은 `/story-readiness` → `/dev-story` → `/story-done` |
| Ponytail 범위 | **코드 에이전트에만 주입.** 기획·내러티브·아트·프로듀서 계열은 제외 |
| 사용 형태 | **1인, 여러 PC** → 플러그인·설정을 저장소에 고정, clone + bootstrap 한 번으로 같은 환경 |
| CCGS 관리 | **upstream 추적** (`ccgs` remote). 수정은 오버레이 파일 위주, CCGS 파일 패치는 최소 |
| 기억 | 프로젝트 지식·공유 메모리·세션 인계 전부 **게임 저장소 안**에서 git으로 이동 (C5) |
| PC 이동 | **명령 한 번**: `/handoff`(떠나는 PC) / `/pickup`(도착한 PC) |
| GitHub | trunk 기반, 도구는 GitHub MCP 대신 `gh` CLI (C9) |
| Graphify | post-commit 훅 자동 갱신, `graphify-out/`은 커밋 안 함 |
| 추가 플러그인 | Aseprite 포함, Warp 제외(전역에만) |
| Unity 기본값 | Unity 6 + C# + Pipeline 패키지 + CLI만 고정. 2D/3D·렌더 파이프라인은 프로젝트마다 `/setup-engine`에서 |

### 검토 후 제외한 것

| 대상 | 제외 이유 |
|---|---|
| **OMC (oh-my-claudecode)** | CCGS가 구현(`/dev-story`)·팀 조율(`team-*`)·리뷰(`/code-review`, `/story-done`)를 이미 갖춤. 2단계 실측 문제가 전부 OMC에서 나옴: Windows에서 프롬프트당 훅 8.1초(끄는 스위치 `OMC_SKIP_HOOKS`/`DISABLE_OMC`에도 같은 지연), heredoc `git commit` 자동 승인, 프롬프트 키워드로 자율 모드 발동, 루트 README 컨텍스트 주입. 이 PC에서 플러그인·마켓플레이스·HUD 설정을 제거함. 제거 후 프롬프트 훅 8,129ms → 111ms |
| **별도 brain 저장소** (`~/claude-brain`) | 저장소가 3개로 늘고 협업자와 공유되지 않음. 자동 메모리를 게임 저장소의 `.claude/memory/`로 옮겨 대체 (C5) |
| **Unity MCP 서버** | Unity CLI가 더 많은 기능을 제공하고 Unity 스킬 스스로 "셸이 있으면 CLI"를 요구 |
| **GitHub MCP 서버** | `gh`로 충분. 같은 원칙 |

---

## 1. 구성요소가 Claude Code에 닿는 지점

충돌은 "같은 표면을 둘 이상이 쓰는 곳"에서 생긴다.

| 표면 | CCGS | Ponytail | Graphify | Unity plugin | Aseprite |
|---|---|---|---|---|---|
| **CLAUDE.md** | 본체 소유 (`@` import 여러 개) | — | `graphify claude install`이 섹션 추가 (안 씀) | — | — |
| **Hooks** | 8개 이벤트에 ~13개 (bash) | SessionStart·SubagentStart·UserPromptSubmit | git post-commit/post-checkout (Claude 훅 아님) | 없음 | SessionStart |
| **Agents** | 49개 (프로젝트 로컬) | — | — | — | 4개 (`aseprite:*`) |
| **Skills/명령** | 78개 | 6개 | 1개 (사용자 전역) | 33개 | 14개 |
| **MCP** | — | — | (`--mcp` 선택, 안 씀) | 안 씀 (CLI 우선) | aseprite 브리지 |
| **Statusline** | 프로젝트 `statusline.sh` | 자체 스크립트(안내만) | — | — | — |
| **상태 저장** | `production/session-state/` (gitignore) | `~/.claude/.ponytail-active` | `graphify-out/` | — | — |
| **권한** | allow 30개 / deny 140여개 | — | — | — | — |

---

## 2. 충돌·중복 지점과 해결책

심각도: 🔴 동작이 깨지거나 의도와 반대로 동작 · 🟠 혼선·낭비 · 🟢 사소/문서화로 충분

### C1 🟠 Ponytail ↔ CCGS 코딩 표준

- Ponytail은 "최소 코드"를, CCGS `coding-standards.md` + `.claude/rules/*.md`는 "반드시 할 것"(Addressables, 새 Input System, 테스트 스텁 등)을 요구한다.
- **해결 — 우선순위 한 줄**: "CCGS 규칙·ADR·control manifest가 *범위*(무엇이 필수인가)를 정하고, Ponytail은 그 범위 *밖*의 것을 깎는다." Ponytail 자체 규칙("Keep the structure the codebase already has", "Never cut … anything the user asked for")과 모순되지 않는다.
- 서브에이전트 주입 범위: 프로젝트 `env`의 `PONYTAIL_SUBAGENT_MATCHER`

```
programmer|unity-.*specialist|prototyper|technical-artist|devops|executor|debugger|test-engineer|qa-tester|general-purpose
```

  - 포함: CCGS `*-programmer`(6), `unity-*-specialist`(5), `prototyper`, `technical-artist`(셰이더), `devops-engineer`, `qa-tester`(테스트 코드), `general-purpose`
  - 제외: `*-director`, `*-designer`, `producer`, `writer`, `accessibility-specialist`, Aseprite 에이전트 등
  - 메인 세션은 Ponytail 유지(전역 설치). 기획 스킬 중에는 답변이 짧아지는 정도의 영향만 있음.
  - 정규식의 `executor|debugger|test-engineer`는 OMC 시절 이름이라 지금은 맞는 에이전트가 없다(무해). 다음 정리 때 빼도 됨.

### C2 🟢 Statusline

- 프로젝트 설정이 사용자 설정보다 우선 → 이 저장소 안에서는 **CCGS `statusline.sh`가 이김**. 프로젝트 단계·스토리를 보여주므로 그대로 둔다(CCGS diff 0). 다른 statusline이 필요한 PC는 `.claude/settings.local.json`에서 override.

### C3 🟠 이름 충돌 — 스킬·에이전트

| 이름 | 충돌 대상 | 결과 | 해결 |
|---|---|---|---|
| `/code-review` | CCGS 프로젝트 스킬 ↔ Claude Code 내장 `code-review` | 실측: 프로젝트 안에서는 **CCGS 스킬만 노출**, 내장은 이름으로 호출 불가 | 리뷰 분담표(아래)에서 CCGS 것만 사용 |
| `/help` | CCGS 스킬 ↔ CLI 내장 `/help` | 내장이 이김 | 무해(CCGS에서도 `/next-step`의 옛 이름). `/next-step` 사용 |
| `/resume` | (신규 인계 스킬 이름 후보) ↔ CLI 내장 | 사용 불가 | 인계 스킬 이름을 `/pickup`으로 |
| `/security-audit` | CCGS ↔ 내장 `security-review` | 역할이 다름 | 게임 보안(치트·세이브 변조)은 CCGS, 일반 코드 취약점은 내장 |

**리뷰 역할 분담**

| 시점 | 도구 | 이유 |
|---|---|---|
| 구현 직후 | `/ponytail-review` | 과잉 구현만 잡음(가장 쌈) |
| 스토리 완료 판정 | CCGS `/code-review` → `/story-done` | 코딩 표준·ADR·테스트 증거를 아는 유일한 리뷰어 |
| 마일스톤/아키텍처 | CCGS `/architecture-review` | — |

### C4 🔴 CLAUDE.md 소유권

- CCGS가 CLAUDE.md를 소유하고 upstream에서 계속 수정한다. `graphify claude install`도 여기에 섹션을 추가하려 한다 → CCGS를 merge할 때마다 충돌.
- **해결**: 우리 규칙은 전부 [`.claude/docs/framework/integration.md`](../.claude/docs/framework/integration.md)에 둔다. CLAUDE.md에는 import 한 줄만(충돌 지점 1줄). `graphify claude install` 대신 같은 내용을 integration.md의 Graphify 절에 직접 씀.
- `CLAUDE.local.md`는 gitignore라 PC 간에 안 넘어감 → 공용 규칙을 두지 않는다.

### C5 🔴 기억 저장소와 여러 PC 동기화

> CCGS는 `production/session-state/`(active.md)와 `production/session-logs/`를 **의도적으로 gitignore**한다("Ephemeral per-session checkpoint; never committed"). 그대로 두면 진행 상태가 다른 PC로 넘어가지 않는다.

**원칙: 다음 세션이 알아야 하는 것은 전부 게임 저장소의 git으로 넘어간다.**

| 층 | 무엇 | 위치 | PC 간 이동 | 쓰는 주체 |
|---|---|---|---|---|
| ① 프로젝트 지식 | GDD, ADR, 에픽/스토리, 스프린트, 아트 바이블 | `design/`, `docs/`, `production/`(session-* 제외) | git | CCGS 스킬 |
| ② 공유 메모리 | 이 게임과 작업 방식에 관한 사실 | **`.claude/memory/`** (커밋, `MEMORY.md`는 `merge=union`) | git | Claude 자동 메모리 |
| ③ 세션 인계 | 하던 스토리, 다음 할 일, 막힌 것, 열린 질문 | **`production/handoff.md`** (커밋) | git, `/handoff`→`/pickup` | 인계 스킬 |
| (재생성) | 코드 그래프 | `graphify-out/` | ❌ PC마다 post-commit 훅으로 갱신 | Graphify |
| (PC 로컬) | active.md, 세션 로그 | `production/session-state/` | ❌ — ③으로 넘어감 | CCGS 훅 |

**② 공유 메모리**
- 자동 메모리 위치 `autoMemoryDirectory`는 **절대경로(또는 `~/`)만 허용**한다 → 커밋되는 `settings.json`에는 넣을 수 없다. `bootstrap.ps1`이 PC마다 `.claude/settings.local.json`(gitignore)에 `<저장소 절대경로>/.claude/memory`를 기록한다. 실측: 새 세션의 메모리 경로가 `D:\Unity Projects\Custom_AI_Env\.claude\memory\`로 잡힘.
- 공유되는 기억이므로 **게임·팀 작업 방식에 관한 사실만** 저장. 개인 선호는 각자 `~/.claude/CLAUDE.md`(PC 간 동기화 안 됨). 이 규칙을 `MEMORY.md` 머리말에 적어 매 세션 로드.

**③ 인계 흐름**

```
PC-A: /handoff
  1. production/session-state/active.md 의 CHECKPOINT 갱신
     (브랜치, 현재 작업, 다음 단계, 막힌 것, 작업 중 파일, 열린 질문)
  2. 그 파일을 production/handoff.md 로 복사 (첫 줄에 날짜·브랜치 주석)
  3. 다른 변경이 있으면 선택: [현재 브랜치에 커밋] / [wip/handoff-<날짜> 브랜치] / [커밋 안 함]
  4. chore(handoff) 커밋 → git push
PC-B: /pickup
  1. 로컬 변경이 있으면 멈춤 → 없으면 git fetch + pull --ff-only
  2. handoff.md 헤더의 브랜치와 현재 브랜치 비교, wip/handoff-* 브랜치 안내
  3. 인계 이후에 다른 커밋이 있으면 "체크포인트가 오래됐을 수 있음" 경고
  4. handoff.md → production/session-state/active.md 복원 (기존 파일이 다르면 먼저 물음)
  5. 다섯 줄 요약 후 대기
```

- `/handoff` 입력 자체가 커밋 지시이므로 CCGS의 "지시 없이 커밋 금지" 규칙과 맞는다. 두 스킬 모두 `disable-model-invocation: true` — 사용자가 직접 입력할 때만 실행.
- CCGS의 `active.md`가 이미 "콜드 재개용" 체크포인트 형식이라 별도 요약 포맷을 만들지 않고 복사만 한다.

### C6 🟠 Unity 접근 경로 · CCGS 엔진 에이전트 ↔ Unity 플러그인 스킬

- 일관된 부분: Unity `unity-cli` 스킬은 "셸이 있으면 MCP 말고 CLI", CCGS `unity-specialist`는 "Editor가 닿으면 `.unity/.prefab` YAML 손편집 금지". 둘이 같은 결론.
- 중복 부분: CCGS `unity-*-specialist` 5종(판단·설계) ↔ Unity 플러그인 33개 스킬(절차 지식). CCGS `/start`·`/setup-engine` ↔ Unity `new-unity-project`(둘 다 인터뷰).

**해결**
- Unity 조작은 **CLI 단일 경로**: `unity command … --caller plugin --skill <스킬명>`.
- 라우팅: **CCGS 에이전트가 판단, Unity 스킬이 절차.** 매핑표는 integration.md 한 곳에 둔다(CCGS 에이전트 파일은 수정하지 않음).

| CCGS 에이전트 | 쓸 Unity 스킬 |
|---|---|
| unity-specialist | `unity-cli`, `unity-package-management`, `create-2d-physics`, `physics-3d-collision`, `initialize-ai-navigation` |
| unity-ui-specialist | `ui` → `ui-uitk` / `ui-ugui`, `optimize-text-mesh-pro`, `localization` |
| unity-shader-specialist / technical-artist | `shader-graph-create-custom-node`, `urp-postprocessing`, `validate-urp-render-graph-renderer-feature`, `migrate-birp-to-urp` |
| unity-addressables-specialist | `manage-sprite-atlas` |
| performance-analyst | `optimize-audio`, `optimize-web` |
| network-programmer / live-ops-designer | `setup-multiplayer-services`, `build-live-game` |

- 프로젝트 생성 순서: CCGS `/start` → `/setup-engine unity`(인터뷰·설정 기록) → `unity projects create`로 실제 프로젝트 생성 → `unity pipeline install`. `new-unity-project` 인터뷰는 쓰지 않음.
- 코드 루트: CCGS `code-root-resolution.md`가 이미 Unity를 `Assets/`로 인식(`Assets/Scripts/<System>/`).

### C7 🟢 Aseprite ↔ CCGS 아트 에이전트

- `art-director`(아트 바이블·스타일 결정) / `technical-artist`(임포트·셰이더) ↔ `aseprite:*`(실제 픽셀 작업).
- **흐름**: `/art-bible` → `aseprite:brief`·`palette-smith`·`draw`·`animate` → `aseprite:export` → `Assets/Art/…` → Unity `sprite-editor`·`manage-sprite-atlas`·`2d-pixel-perfect`. `aseprite:pixel-critic` 결과는 `/asset-audit`의 증거로 첨부.

### C8 🟠 Windows 전제조건

- CCGS 훅은 전부 Git Bash가 필요하다(없으면 **조용히 꺼짐**). Ponytail은 Node, Graphify는 Python, Unity CLI는 별도 설치.
- `bootstrap.ps1`이 도구 존재를 확인하고 없으면 실패로 보고한다.

### C9 🔴 git · gh

**git을 건드리는 것들**

| 주체 | 무엇 | 레이어 |
|---|---|---|
| CCGS `validate-commit.sh` | 커밋 검증, 실패 시 차단 | Claude PreToolUse |
| CCGS `validate-push.sh` | 보호 브랜치 push, tag, merge, `gh release` 경고(차단 안 함) | Claude PreToolUse |
| CCGS deny 규칙 | force push, `reset --hard`, `clean -f`, `rm -rf` 금지 | Claude 권한 |
| CCGS 규칙 | "지시 없이 커밋 금지", "엔진·플러그인·벤더 저장소에 이슈/PR 금지" | CLAUDE.md |
| 프레임워크 `ask` 규칙 | `git commit*`, `git tag*`는 항상 확인 | Claude 권한 |
| git-lfs | post-commit, post-checkout, post-merge, pre-push | git 훅 |
| Graphify | post-commit, post-checkout + `graph.json` merge 드라이버 | git 훅 |
| Unity CLI | `unity vcs git worktree add` | CLI |

**충돌과 해결**

1. 🔴 **gh가 CCGS 공개 저장소를 대상으로 잡음.** 기본 저장소가 없으면 gh는 **`upstream`이라는 이름의 remote를 우선 선택**한다(실측: `gh pr list`가 CCGS의 PR을 보여줌). CCGS "Nothing Goes Upstream" 규칙 위반 위험.
   → CCGS remote 이름은 **`ccgs`**(실측: 그러면 `origin` 선택), push 주소 `DISABLED`. bootstrap이 `gh repo set-default`를 설정하고 `upstream` remote가 있으면 경고. deny `Bash(gh * --repo *)`, `Bash(gh * -R *)`.
2. 🔴 **git 훅 설치 순서.** git-lfs와 Graphify가 둘 다 post-commit을 쓴다. Graphify는 기존 훅에 덧붙이지만 `git lfs install`은 기존 훅이 있으면 실패한다.
   → bootstrap 순서 고정: `git lfs install --local` → `graphify hook install` (실측: 두 훅 모두 남음).
3. 🟠 **private 저장소는 공개 CCGS의 GitHub fork가 될 수 없다** → fork 대신 `ccgs` remote로 추적.
4. 🟠 **"Use this template" 버튼은 히스토리가 끊긴다** → 이후 템플릿 개선 merge가 어려워짐.
   → `scripts/new-game.ps1`로 **clone 기반 생성**(히스토리 유지), `template` remote를 남겨 `git merge template/main`.
5. 🟠 **자격증명 평문 저장.** 이 PC의 git `credential.helper=store` → 토큰이 `~/.git-credentials`에 평문.
   → `gh auth setup-git`으로 gh keyring을 쓰게 하고 store 제거(사용자 작업). bootstrap이 경고, deny `Read(~/.git-credentials)`.
6. 🟠 **Unity 파일 병합·줄바꿈.** `.gitattributes`에 Unity YAML `eol=lf merge=unityyamlmerge`, `Assets/` 아래 바이너리 LFS. UnityYAMLMerge 경로는 PC마다 다르므로 bootstrap이 git config에 등록(없으면 일반 텍스트 병합).
7. 🟢 **README**: 루트 README는 템플릿 사용법으로 교체하고 `README.md merge=ours`(드라이버는 bootstrap이 등록) → CCGS merge 때 우리 README 유지.

**GitHub 흐름**: trunk 기반. 평소에는 main에 직접 커밋(사용자 지시 시). 큰 작업은 사용자가 원할 때만 브랜치 → `gh pr create` → merge. Unity는 워크트리마다 `Library/` 재임포트가 필요해 무거우므로 워크트리는 꼭 필요할 때만(`unity vcs git worktree add`, Editor는 워크트리당 1개). CI(GitHub Actions + Unity 빌드)는 Unity 라이선스 시크릿과 private 저장소 실행 시간 제한 때문에 범위에서 제외.

---

## 3. 통합 아키텍처

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

### 단계별 사용 도구

| 단계 | 주 도구 | 보조 |
|---|---|---|
| 시작 | `new-game.ps1` → CCGS `/start` → `/setup-engine unity` | `unity projects create`, `unity pipeline install` |
| 컨셉·프로토 | CCGS `/brainstorm`, `/prototype` | Aseprite `brief`/`concept` |
| 설계 | CCGS GDD·`/design-review`·`/create-architecture`·ADR | Graphify |
| 계획 | CCGS `/create-epics` → `/create-stories` → `/sprint-plan` | — |
| 구현 | `/story-readiness` → `/dev-story` | Unity 스킬, Unity CLI, Ponytail |
| 검증 | `/ponytail-review` → CCGS `/code-review` → `/story-done` | Unity CLI playmode 검증 루프 |
| PC 이동 | `/handoff` → `/pickup` | git |
| 디버깅 | CCGS 에이전트 | Graphify `path`/`explain` |
| 아트 | CCGS `/art-bible` → Aseprite → Unity 스프라이트 스킬 | `pixel-critic`, `/asset-audit` |
| 출시 | CCGS `/release-checklist`, `/launch-checklist`, `/patch-notes` | Unity CLI 빌드 |

---

## 4. 저장소 구성

### remote

```
템플릿 저장소:  origin   = github.com/<나>/unity-ai-template (private)
                ccgs     = github.com/Donchitos/Claude-Code-Game-Studios (push DISABLED)
게임 저장소:    origin   = github.com/<나>/<게임> (private)   ← gh repo set-default
                template = github.com/<나>/unity-ai-template
```

- CCGS 업데이트: 템플릿에서 `git fetch ccgs && git merge ccgs/main` (squash 아님 — 히스토리가 있어야 다음 merge가 쉬움)
- 템플릿 개선을 게임에 반영: 게임에서 `git fetch template && git merge template/main`
- 새 게임: `pwsh scripts/new-game.ps1 <게임-slug>` — `gh repo create --private` → 템플릿 clone → `origin`을 `template`으로 바꾸고 새 `origin` 추가 → push → bootstrap 실행

### CCGS 파일 패치 (merge 때 확인할 지점은 이 5개뿐)

| 파일 | 변경 |
|---|---|
| `CLAUDE.md` | `@.claude/docs/framework/integration.md` import 1줄 |
| `.claude/settings.json` | 키·항목 **추가만**: `enabledPlugins`, `extraKnownMarketplaces`, `env`, deny 3개, ask 2개 |
| `.gitignore` | `graphify-out/` |
| `.gitattributes` | Unity YAML 병합·LF, `Assets/` 바이너리 LFS, `MEMORY.md merge=union`, `README.md merge=ours` |
| `README.md` | 템플릿 사용법(한국어)으로 교체 (`merge=ours`로 유지). 영어판은 오버레이 파일 `README-en.md` |

Godot/Unreal 에이전트·스킬은 **삭제하지 않는다**(삭제하면 CCGS가 그 파일을 고칠 때마다 modify/delete 충돌). 쓰지 않을 뿐.

### 오버레이 파일 (충돌 없음)

```
.claude/docs/framework/integration.md   # 세션마다 로드되는 규칙
.claude/memory/MEMORY.md                # 공유 자동 메모리 (커밋)
.claude/skills/handoff/SKILL.md         # 인계 작성 → 커밋 → push
.claude/skills/pickup/SKILL.md          # pull → active.md 복원
production/handoff.md                   # 세션 인계 (첫 /handoff 때 생성)
scripts/bootstrap.ps1                   # PC 셋업 + 재실행 가능한 점검
scripts/new-game.ps1                    # 템플릿에서 새 게임 저장소 생성
Assets/ Packages/ ProjectSettings/      # 게임 저장소에서만 생성 (템플릿엔 없음)
```

### `.claude/settings.json`에 추가한 키

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

`permissions.deny`는 CCGS 기존 배열 뒤에 **추가**한 것. OMC는 이 PC에서 제거했으므로 설정에 항목이 없다. 다른 PC에 OMC가 전역 설치돼 있다면 `"oh-my-claudecode@omc": false`를 추가해 이 저장소에서 꺼야 한다.

### `bootstrap.ps1` (PC마다, 재실행 가능)

1. 도구 확인: git, git-lfs, gh, node, Git Bash, graphify, unity. 없으면 실패로 보고.
2. git 훅: `git lfs install --local` → `graphify hook install` (순서 고정).
3. merge 드라이버(PC별 git config): `ours`(README), `unityyamlmerge`(설치된 Unity에서 경로 탐색, 없으면 건너뜀).
4. `.claude/settings.local.json`에 `autoMemoryDirectory` = 이 clone의 `.claude/memory` 절대경로.
5. GitHub: `gh repo set-default <origin>`, `upstream` remote가 있으면 경고, `credential.helper=store`면 경고.

---

## 5. 구축 단계

| 단계 | 작업 | 상태 |
|---|---|---|
| 1. 기반 | 템플릿 private 저장소 생성, CCGS를 `ccgs` remote로 merge, 설정 파일 패치 | ✅ |
| 2. 실측 검증 | 6절 항목 확인, 결과로 설정 확정 | ✅ (OMC·brain 제외 결정으로 이어짐) |
| 3. 통합 문서 | `integration.md` + CLAUDE.md import, README 교체 | ✅ |
| 4. 스크립트·스킬 | `bootstrap.ps1`, `new-game.ps1`, `/handoff`, `/pickup` | ✅ (bootstrap은 이 PC에서 실행 확인, new-game은 `-WhatIf`만) |
| 5. 드라이런 | `new-game.ps1`로 시험 게임 생성 → `/start` → `/setup-engine unity` → `unity projects create` + `pipeline install` → 스토리 1개 `/story-readiness` → `/dev-story` → `/story-done` | ⏳ |
| 5b. PC 이동 드라이런 | 작업 도중 `/handoff` → 다른 PC(또는 다른 clone)에서 bootstrap → `/pickup` | ⏳ |
| 6. 버전 고정 | 통과한 플러그인 버전 조합 기록, 템플릿 태그 `v0.1` | ⏳ |

---

## 6. 2단계 실측 결과 (2026-11-15)

새 Claude 세션(headless)과 훅 스크립트 단독 실행으로 확인. 이 PC는 CLI 로그인이 안 되어 **세션 초기화·훅·설정 로딩까지만** 검증했다. OMC 관련 항목은 제외 결정의 근거로만 남긴다.

| 항목 | 결과 | 반영 |
|---|---|---|
| 프로젝트 `env` → 훅 전달 | ✅ 훅 프로세스에서 `PONYTAIL_SUBAGENT_MATCHER` 확인 | 환경변수는 프로젝트 settings로 충분 |
| `/code-review` 이름 충돌 | 프로젝트 안에서는 CCGS 스킬만 노출 | C3 |
| `autoMemoryDirectory` | `~/` 경로는 정상 해석. 저장소 상대경로는 불가 | bootstrap이 `settings.local.json`에 절대경로 기록 (C5) |
| gh 기본 저장소 | 🔴 기본 저장소가 없으면 `upstream` 이름의 remote를 선택 | remote 이름 `ccgs` + set-default (C9-1) |
| git 훅 순서 | ✅ LFS → Graphify 순서면 둘 다 남음 | bootstrap 순서 고정 (C9-2) |
| Graphify on C# | ✅ 클래스·메서드·`MonoBehaviour` 상속·호출·`GetComponent<T>` 참조 추출, `.gitignore`의 `Library/` 제외. 이벤트 구독 관계는 안 잡힘. **워크트리 커밋에서는 훅이 갱신을 건너뜀** | integration.md에 기록 |
| CCGS 설정 적용 시점 | deny 규칙은 실행 중 세션에도 즉시 적용, 훅·플러그인은 세션 재시작 후 | — |
| (OMC) 훅 지연·자동 승인·키워드·README 주입 | 🔴 0절 "제외한 것" 참고 | OMC 제외 |

---

## 7. 남는 리스크

- **플러그인 버전 고정 불가**: 마켓플레이스는 최신을 따라가므로 PC마다 버전이 어긋날 수 있다. 6단계에서 검증 조합을 기록하는 수준이 현실적 한계.
- **CCGS 업데이트 속도**: 4절의 패치 5개 파일은 merge마다 확인. 특히 `.claude/settings.json`은 CCGS가 자주 바꾼다.
- **Unity 플러그인이 beta**(0.1.9-beta): 스킬 이름·CLI 명령이 바뀔 수 있다 → 라우팅표는 integration.md 한 곳에만.
- **`/handoff`를 잊으면 인계가 안 된다**: CCGS 규칙 때문에 자동 push는 넣지 않았다. `/pickup`이 인계 이후 커밋을 감지하면 경고한다.
- **공유 메모리 충돌**: 두 PC가 같은 메모리 파일을 동시에 고치면 충돌. `MEMORY.md`는 union merge로 막지만 개별 파일은 수동 해결.
- **Git LFS 용량**: GitHub private 저장소의 LFS 저장공간·대역폭은 무료 한도가 있다. 아트·오디오가 커지면 유료 데이터 팩이 필요할 수 있음.
- **CLI 로그인 미확인**: 이 PC에서 `claude` CLI가 로그인되지 않아 모델이 응답하는 headless 검증은 하지 못했다. 5단계는 데스크톱 앱 세션에서 진행.
