# Unity AI 게임 개발 프레임워크 — 통합 설계서

> 대상: Ponytail · Graphify · OMC(oh-my-claudecode) · CCGS(Claude Code Game Studios) · Unity 플러그인/CLI · Aseprite · git CLI · gh CLI
> 조사 기준 버전 (2026-11-15): OMC 5.5.0 · Ponytail 5.2.0 · CCGS 1.3.0 (`abc0de7`) · Unity plugin 0.1.9-beta · graphify 0.9.88 · aseprite 0.8.2 · git 2.56.1 · git-lfs 3.8.1 · gh 2.101.0

---

## 0. 확정된 설계 방향

> **2026-11-15 결정: OMC 제외.** CCGS가 구현(`/dev-story`)·조율(`team-*`)·리뷰(`/code-review`, `/story-done`)를 이미 갖추고 있고, 2단계에서 고친 문제(프롬프트당 8초 훅, `git commit` 자동 승인, 키워드 오발동, README 주입)가 전부 OMC에서 나왔다. `DISABLE_OMC`/`OMC_SKIP_HOOKS`도 같은 지연 버그(8.1초)가 있어 부분 사용도 불가.
> 프로젝트 settings에서 `"oh-my-claudecode@omc": false`(전역 설치는 유지, 이 저장소에서만 꺼짐). 실행 경계는 **`/story-readiness` → `/dev-story` → `/story-done`** 으로 대체. 이 문서의 OMC 관련 서술(C1·C2·C5·C6의 OMC 부분, 리뷰표의 OMC 항목, `omc.jsonc`, OMC 권한 훅 대응)은 **폐기**된 것으로 읽는다. `git commit`/`git tag` `ask` 규칙은 CCGS "지시 없이 커밋 금지"를 강제하므로 유지. 이 PC에서 OMC 플러그인·마켓플레이스·HUD statusline 설정도 제거함(실측: 프롬프트 훅 8,129ms → 111ms).
>
> **2026-11-15 결정: brain 저장소 폐기 → 게임 저장소 안 `.claude/memory/`.** 저장소가 2개(template, game)로 줄고, 협업자도 같은 기억을 clone으로 받는다. C6 ③과 `~/claude-brain` 관련 서술은 폐기.
> - `autoMemoryDirectory`는 절대경로만 허용 → 커밋되는 `settings.json`에는 넣을 수 없고, **bootstrap이 PC마다 `.claude/settings.local.json`(gitignore)에 `<저장소 절대경로>/.claude/memory`를 기록**. 실측: 새 세션의 memory 경로가 `D:\Unity Projects\Custom_AI_Env\.claude\memory\`로 잡힘.
> - `.claude/memory/`는 커밋. `MEMORY.md`는 `merge=union`.
> - 공유 기억이므로 **게임·팀 작업 방식에 관한 사실만** 저장. 개인 선호는 각자 `~/.claude/CLAUDE.md` (PC 간 동기화 안 됨 — 필요하면 직접 복사). 이 규칙을 `MEMORY.md` 머리말에 적어 매 세션 로드되게 함.
> - SessionStart brain pull 훅, `brain-sync.mjs`는 불필요. `/handoff`·`/pickup`은 게임 저장소 하나만 push/pull.

| 항목 | 결정 |
|---|---|
| 작업 주도권 | **CCGS 주도 + OMC 실행**. 기획·설계·게이트는 CCGS 협업 흐름, 승인된 스토리 구현만 OMC 자율 실행 |
| Ponytail 범위 | **코드 에이전트에만 주입**. 기획·내러티브·아트·프로듀서 계열은 제외 |
| 사용 형태 | **1인, 여러 PC** → 플러그인·설정을 저장소에 고정, clone 한 번으로 동일 환경 |
| CCGS 관리 | **upstream 추적**. 수정은 오버레이 파일 위주, upstream 파일 패치는 최소 |
| 기억 저장소 | **역할 분리 + git 동기화**. 세션 인계=커밋되는 `production/handoff.md`, 구조=Graphify(재생성), 사용자 기억=`~/claude-brain` 전용 private 저장소 (C6) |
| PC 이동 | **명령 한 번**: `/handoff`(인계 작성→커밋→push) / `/pickup`(pull→인계 읽기) |
| GitHub 흐름 | **trunk + OMC 자율 작업만 PR**. 도구는 GitHub MCP 대신 `gh` CLI (C12) |
| Graphify 운영 | post-commit 훅 자동 갱신, `graphify-out/`은 커밋 안 함 |
| 추가 플러그인 | Aseprite 포함, Warp 제외(전역에만) |
| Unity 기본값 | Unity 6 + C# + Pipeline 패키지 + CLI만 고정. 2D/3D·렌더파이프라인은 프로젝트마다 |

---

## 1. 구성요소가 Claude Code에 닿는 지점

충돌은 전부 "같은 표면을 둘 이상이 쓰는 곳"에서 생긴다. 표면별로 정리하면:

| 표면 | OMC | Ponytail | CCGS | Graphify | Unity plugin | Aseprite |
|---|---|---|---|---|---|---|
| **CLAUDE.md** | drydock/deepinit가 생성·수정 | — | 본체 소유 (`@` import 6개) | `graphify claude install`이 섹션 추가 | — | — |
| **Hooks** | 12개 이벤트에 ~20개 (node) | SessionStart·SubagentStart·UserPromptSubmit | 8개 이벤트에 ~13개 (bash) | git post-commit (Claude 훅 아님) | 없음 | SessionStart |
| **Agents** | 19개 (`oh-my-claudecode:*`) | — | 49개 (프로젝트 로컬, 네임스페이스 없음) | — | — | 4개 (`aseprite:*`) |
| **Skills/명령** | ~40개 + 키워드 자동 트리거 | 6개 | 78개 | 1개 (사용자 전역) | 33개 | 14개 |
| **MCP** | `t` (LSP, notepad, wiki, state…) | — | — | (`--mcp` 선택) | 의도적으로 안 씀 (CLI 우선) | aseprite 브리지 |
| **Statusline** | 전역 HUD (현재 사용 중) | 자체 statusline 스크립트 | 프로젝트 `statusline.sh` | — | — | — |
| **상태/기억 저장** | `.omc/` (notepad, project-memory, wiki, state) | `~/.claude/.ponytail-active` | `production/session-state/active.md` 등 | `graphify-out/` | — | — |
| **권한** | PermissionRequest 훅(Bash) | — | allow 30개 / deny 140여개 | — | — | — |

---

## 2. 충돌·중복 지점과 해결책

심각도: 🔴 동작이 깨지거나 의도와 반대로 동작 · 🟠 혼선·낭비 · 🟢 사소/문서화로 충분

### C1 🔴 작업 철학 충돌 — CCGS 협업 vs OMC 자율

- CCGS: "Question → Options → Decision → Draft → Approval", 모든 Write 전 "May I write?", 커밋은 지시가 있을 때만.
- OMC: `autopilot`/`ralph`/`ultrawork`/`team`은 승인 없이 끝까지 실행하도록 설계됨. `persistent-mode` Stop 훅이 멈추지 못하게 재진입시킴.
- 게다가 OMC `keyword-detector`는 프롬프트의 단어만으로 이 모드들을 켠다(`ralph`, `autopilot`, `ultrawork`, `ulw`, `team` 등).

**해결 — "실행 경계(Execution Boundary)" 규칙**

```
[CCGS 영역: 협업]                          [OMC 영역: 자율]
/start → /brainstorm → GDD → /design-review
→ /create-architecture → ADR → /create-epics
→ /create-stories → /story-readiness  ──PASS──▶  /oh-my-claudecode:execute 또는 ralph
                                                  (스토리 1개 = 실행 단위, 범위 = 그 스토리의 파일)
                                    ◀──결과──    /story-done (CCGS가 다시 검수)
```

- OMC 자율 모드는 **`/story-readiness` 통과한 스토리 하나**에만 사용. 스토리 경로를 인자로 넘김.
- `/oh-my-claudecode:autopilot`(아이디어→코드 전체)은 CCGS 기획 단계를 건너뛰므로 **금지**. 프로토타입이 목적이면 CCGS `/prototype`을 씀.
- 키워드 오발동 방지: OMC 모드는 **슬래시 명령으로만** 켠다. 프로젝트 `.claude/omc.jsonc`의 `keywordDetector.disabled`에 키워드 13개(cancel 제외 전부)를 넣는다. C4의 "코드 리뷰" 오발동도 같이 사라짐. (실측: `team`은 OMC 5.5에서 이미 명시 호출 전용이라 원래 오발동 대상이 아니었음. `OMC_SKIP_HOOKS` 방식은 6절 참고 — 쓰지 않음)
- CCGS `modes.automation`은 `collaborative` 유지(기본값). OMC 실행 중에도 커밋은 CCGS 규칙대로 사용자 지시 시에만.

### C2 🟠 훅 중첩 — 같은 이벤트에 3~4개 플러그인

| 이벤트 | OMC | Ponytail | CCGS | Aseprite | 합계(대략) |
|---|---|---|---|---|---|
| SessionStart | 3 | 1 | 2 | 1 | 7 |
| UserPromptSubmit | 2 | 1 | — | — | 3 |
| PreToolUse | 1 (`*`, 모든 툴) | — | Bash 2 / Write 2 / Read 1 | — | 툴당 2~3 |
| PostToolUse | 4 (`*`) | — | Write/Edit 2 | — | 툴당 4~6 |
| PreCompact | 3 | — | 1 | — | 4 |
| Stop | 4 | — | 1 | — | 5 |
| SubagentStart/Stop | 1 / 2 | 1 / — | 1 / 2 | — | 4 / 4 |

- 훅은 모두 병합 실행된다(서로 덮어쓰지 않음). 기능 충돌은 없지만 **Windows에서 툴 호출 1회당 node/bash 프로세스 5~8개**가 뜬다. 체감 지연의 주원인.
- PreCompact에서 OMC 3개와 CCGS 1개가 각자 "중요한 상태"를 컨텍스트에 다시 주입 → 컴팩션 후 중복 주입.

**해결**: 아래 C6에서 OMC 기억 계열 훅을 끄면 PostToolUse·PreCompact·SessionStart가 각각 1~2개씩 줄어든다. 프로젝트 `.claude/settings.json`의 `env`에:

```json
"env": {
  "OMC_SKIP_HOOKS": "keyword-detector,project-memory,wiki"
}
```

> ⚠️ 검증 필요: `OMC_SKIP_HOOKS` 토큰명. HOOKS.md는 `notepad`, `project-memory`, `post-tool-use`, `keyword-detector`, `persistent-mode`를 예시로 든다. `wiki` 토큰은 문서에서 확인 못 함 — 구축 1단계에서 `scripts/lib`의 skip 판정 코드로 확인하고, 안 되면 `.omc/config`의 wiki 설정으로 끈다.

### C3 🟢 Statusline 3개

- 전역 `~/.claude/settings.json`: OMC HUD / CCGS 프로젝트: `statusline.sh` / Ponytail: 자체 스크립트(안내만).
- 우선순위는 프로젝트 > 사용자 → 이 저장소 안에서는 **CCGS statusline이 자동으로 이김**.

**해결**: 손대지 않는다(upstream diff 0). CCGS statusline은 프로젝트 단계·스토리를 보여주므로 이 프레임워크의 주 흐름과 맞다. OMC HUD가 필요한 PC는 `.claude/settings.local.json`에서 개별 override.

### C4 🔴 이름 충돌 — 스킬·키워드·에이전트

| 이름 | 충돌 대상 | 결과 | 해결 |
|---|---|---|---|
| `/code-review` | CCGS 프로젝트 스킬 ↔ Claude Code 내장 `code-review` ↔ OMC 키워드 "코드 리뷰"/"code review" | 어느 쪽이 뜰지 불명확, 한국어로 "코드 리뷰"라고만 말해도 OMC 리뷰 모드 주입 | 키워드 감지 끔(C1). 리뷰 역할 분담표(아래)로 고정. 내장 vs 프로젝트 스킬 우선순위는 구축 시 실측 |
| `/help` | CCGS 스킬 ↔ CLI 내장 `/help` | 내장이 이김 → CCGS `/help` 도달 불가 | 무해(CCGS에서도 `/next-step`의 옛 이름). 문서에 `/next-step` 사용 명시 |
| `/security-audit` | CCGS ↔ 내장 `security-review` ↔ OMC 키워드 | 중복 | 게임 보안(치트·세이브 변조)은 CCGS, 일반 코드 취약점은 내장 |
| `/launch-checklist`·`/release-checklist` | OMC `launch`, `release` | 중복 | 출시는 CCGS만. OMC `release`는 사용 안 함 |
| `/prototype` | OMC `loft`(버리는 시안) | 중복 | CCGS `/prototype` |
| `/start`·`/onboard`·`/adopt` | OMC `omc-setup`, `drydock`, `deepinit` | 🔴 drydock/deepinit은 CLAUDE.md·AGENTS.md를 생성 → CCGS 문서 체계와 이중화 | **drydock·deepinit 금지**. 온보딩은 CCGS `/start` |
| 에이전트 `qa-tester`, `writer` | CCGS 로컬 ↔ `oh-my-claudecode:qa-tester`, `:writer` | 네임스페이스로 구분돼 깨지진 않음. 자동 라우팅 시 혼동 | CCGS 쪽 사용. OMC는 `oh-my-claudecode:` 접두어로만 명시 호출 |

**리뷰 역할 분담 (4개 리뷰어 → 단계별 1개씩)**

| 시점 | 도구 | 이유 |
|---|---|---|
| 스토리 구현 직후 | `/ponytail-review` | 과잉 구현만 잡음(가장 쌈) |
| 스토리 완료 판정 | CCGS `/code-review` → `/story-done` | 코딩 표준·ADR·테스트 증거를 아는 유일한 리뷰어 |
| 버그 헌팅 (선택) | `oh-my-claudecode:code-reviewer` 에이전트 | 정확성 결함 전용. (실측: 프로젝트 안에서 `/code-review`는 CCGS 스킬 하나만 노출 → 내장 `/code-review`는 이름으로 호출 불가) |
| 마일스톤/아키텍처 | CCGS `/architecture-review` | — |
| (안 씀) | `omc-review`, 내장 `/code-review` | 위와 중복 / 이름이 가려짐 |

### C5 🟠 "최소 코드" 규칙 3중 중복 — Ponytail · OMC · CCGS 표준

- Ponytail 규칙 ≈ OMC `minimal-code-discipline` 스킬 ≈ OMC `code-simplifier` Stop 훅(기본 꺼짐, 그리고 대상 확장자에 `.cs` 없음) ≈ OMC `ai-slop-cleaner`.
- CCGS `coding-standards.md` + `.claude/rules/*.md`(engine-code, gameplay-code, ui-code …)는 반대로 "반드시 해야 할 것"(Addressables, 새 Input System, 테스트 스텁 등)을 요구.

**해결**
- 최소화 규칙은 **Ponytail 하나만**. OMC `minimal-code-discipline`·`ai-slop-cleaner`는 호출하지 않고, `codeSimplifier`는 켜지 않는다.
- 우선순위를 통합 문서에 한 줄로 못박음: **"CCGS 규칙·ADR이 *범위*(무엇이 필수인가)를 정하고, Ponytail은 그 범위 *밖*의 것을 깎는다."** Ponytail 자체 규칙("Keep the structure the codebase already has", "Never cut … anything the user asked for")과 모순되지 않음.
- 서브에이전트 주입 범위 (결정사항): `PONYTAIL_SUBAGENT_MATCHER` 정규식

```
programmer|unity-.*specialist|prototyper|technical-artist|devops|executor|debugger|test-engineer|qa-tester|general-purpose
```

  - 포함: CCGS `*-programmer`(6), `unity-*-specialist`(5), `prototyper`, `technical-artist`(셰이더), `devops-engineer`, `qa-tester`(테스트 코드) / OMC `executor`, `debugger`, `test-engineer`
  - 제외: `*-director`, `*-designer`, `producer`, `writer`, `art-director`, `accessibility-specialist`, Aseprite 에이전트, OMC `planner`/`analyst`/`critic` 등
  - 메인 세션은 Ponytail 유지(전역 설치). 기획 스킬 실행 중 답변이 짧아지는 정도의 영향만 있음.

> ⚠️ 검증 필요: 프로젝트 settings `env`의 값이 훅 프로세스 환경변수로 전달되는지. 안 되면 PC별 사용자 환경변수로 설정(bootstrap 스크립트가 처리).

### C6 🔴 기억 저장소 5중 + 여러 PC 동기화

> **v1 설계 정정**: CCGS는 `production/session-state/`(active.md)와 `production/session-logs/`를 **의도적으로 gitignore**한다("Ephemeral per-session checkpoint; never committed"). 즉 active.md도 다른 PC로 넘어가지 않는다. 아래가 수정된 설계다.

**원칙: 기억을 3층으로 나누고, 각 층마다 git 경로를 하나씩 둔다.**

| 층 | 무엇 | 위치 | PC 간 이동 | 쓰는 주체 |
|---|---|---|---|---|
| ① 프로젝트 지식 (영구) | GDD, ADR, 에픽/스토리, 스프린트, 아트 바이블 | `design/`, `docs/`, `production/`(session-* 제외) | 게임 저장소 git | CCGS 스킬 |
| ② 세션 인계 (가변) | 하던 스토리, 아직 ADR이 안 된 결정, 다음 할 일, 미해결 질문, WIP 브랜치 | **`production/handoff.md`** (신규, 커밋됨) | 게임 저장소 git, `/handoff`→`/pickup` | `/handoff` 스킬 |
| ③ 사용자 기억 (게임 공통) | 작업 선호, 피드백, 반복 실수 | **`~/claude-brain/`** (별도 private 저장소) | brain 저장소 git | Claude 자동 메모리 |
| (재생성) | 코드·문서 그래프 | `graphify-out/` | ❌ 커밋 안 함, PC마다 post-commit 훅으로 갱신 | Graphify |
| (PC 로컬 임시) | active.md, OMC notepad·state, 세션 로그 | `production/session-state/`, `.omc/` | ❌ — ②로 요약되어 넘어감 | CCGS 훅, OMC |

**OMC 쪽 정리**
- `project-memory`·`wiki` 훅은 끈다(C2). ①·②와 중복이고, OMC 문서도 `.omc/`를 "ignored operational artifacts"로 규정.
- 예외: OMC 문서가 **`.omc/skills/**`는 커밋 대상**이라고 명시 → gitignore를 `.omc/*` + `!.omc/skills/`로.
- OMC notepad은 PC 로컬 임시 메모로 유지하고, `/handoff`가 그 내용 중 남길 것만 ②로 옮긴다.

**② 인계 흐름 — `/handoff`, `/pickup` (신규 프로젝트 스킬)**

```
PC-A: /handoff
  1. active.md + OMC notepad + git status/diff 를 읽어 production/handoff.md 작성
     (브랜치, 현재 스토리 경로, 미결 결정, 다음 단계, 열린 질문)
  2. 코드 변경이 남아 있으면 사용자에게 선택지 제시:
     [현재 브랜치에 커밋] / [wip/handoff-<날짜> 브랜치로 커밋]
  3. handoff.md 커밋 → git push (게임 저장소)
  4. ~/claude-brain 커밋 → push
PC-B: /pickup
  1. git pull (게임 저장소) + ~/claude-brain pull
  2. handoff.md로 production/session-state/active.md 를 시드
     → 이후 CCGS의 세션 복구 흐름이 그대로 동작
  3. WIP 브랜치가 있으면 안내, 요약 출력
```

- 명령 입력 자체가 "커밋 지시"이므로 CCGS의 "지시 없이 커밋 금지" 규칙과 맞는다.
- 이름 주의: `/resume`은 Claude Code 내장 명령이라 쓸 수 없음 → `/pickup`. OMC 키워드 `handoff`는 키워드 감지를 꺼서(C1) 충돌 없음.
- handoff.md는 매번 덮어쓰는 단일 파일. 이전 인계는 git 히스토리에 남음.

**③ 사용자 기억 — `~/claude-brain`**

- Claude 자동 메모리 위치 설정 `autoMemoryDirectory`는 **절대경로 또는 `~/`만 허용**(저장소 상대경로 불가), 프로젝트 settings에서 지정 가능(워크스페이스 신뢰 필요). 기본 위치는 PC 로컬이라 PC 간 공유가 안 된다고 공식 문서에 명시.
- 그래서 게임 저장소의 `.claude/settings.json`에 `"autoMemoryDirectory": "~/claude-brain/memory/<게임-slug>"` 를 넣는다 → 경로가 `~/` 기준이라 모든 PC에서 같은 설정이 통함.
- `~/claude-brain` 은 `gh repo create claude-brain --private` 로 만든 별도 저장소:

```
~/claude-brain/
  memory/<게임-slug>/MEMORY.md, *.md   ← 게임별 자동 메모리
  global/CLAUDE.md                     ← ~/.claude/CLAUDE.md 가 @import (모든 PC 공통 지시)
  .gitattributes                       ← MEMORY.md merge=union (두 PC가 한 줄씩 추가해도 자동 병합)
```

- 동기화: **SessionStart 훅이 `pull`만 자동**(읽기 전용, 실패해도 통과), push는 `/handoff`에서. pull이 세션 시작 시점에 끝나야 자동 메모리 인덱스가 최신으로 로드된다.
- 기존 이 PC의 `~/.claude/projects/…/memory`는 1회 이관.

### C7 🔴 CLAUDE.md 소유권

- CCGS가 CLAUDE.md를 소유(upstream이 계속 수정). `graphify claude install`은 여기에 섹션을 추가, OMC drydock/deepinit도 덮어씀 → upstream merge마다 충돌.

**해결**
- 우리 규칙은 전부 **새 파일 `.claude/docs/framework/integration.md`**에 둔다.
- CLAUDE.md에는 **import 한 줄만** 추가: `@.claude/docs/framework/integration.md` (merge 충돌 지점 1줄).
- `graphify claude install` 대신 같은 내용을 integration.md의 Graphify 절에 직접 씀.
- `CLAUDE.local.md`는 gitignore라 PC 간 동기화 안 됨 → 공용 규칙을 두면 안 됨.

### C8 🟠 Unity 접근 경로 · CCGS 엔진 에이전트 ↔ Unity 플러그인 스킬

- 일관된 부분: Unity `unity-cli` 스킬은 "셸이 있으면 MCP 말고 CLI", CCGS `unity-specialist`는 "Editor가 닿으면 `.unity/.prefab` YAML 손편집 금지". → **둘이 같은 결론**.
- 중복 부분: CCGS `unity-specialist`·`unity-ui-specialist`·`unity-shader-specialist`·`unity-dots-specialist`·`unity-addressables-specialist`(판단·설계 에이전트) ↔ Unity 플러그인 33개 스킬(절차 지식).
- CCGS `/start`·`/setup-engine` ↔ Unity `new-unity-project`(둘 다 인터뷰).

**해결**
- Unity 조작은 **CLI 단일 경로**: `unity command … --caller plugin --skill <스킬명>`. Unity MCP 서버는 설치하지 않음.
- 라우팅: **CCGS 에이전트가 판단, Unity 스킬이 절차.** integration.md에 매핑표를 둔다 (upstream 에이전트 파일은 수정하지 않음):

| CCGS 에이전트 | 쓸 Unity 스킬 |
|---|---|
| unity-specialist | `unity-cli`, `unity-package-management`, `create-2d-physics`, `physics-3d-collision`, `initialize-ai-navigation` |
| unity-ui-specialist | `ui` → `ui-uitk` / `ui-ugui`, `optimize-text-mesh-pro`, `localization` |
| unity-shader-specialist / technical-artist | `shader-graph-create-custom-node`, `urp-postprocessing`, `validate-urp-render-graph-renderer-feature`, `migrate-birp-to-urp` |
| unity-addressables-specialist | `manage-sprite-atlas` |
| performance-analyst | `optimize-audio`, `optimize-web` |
| network-programmer / live-ops-designer | `setup-multiplayer-services`, `build-live-game` |

- 프로젝트 생성 순서: CCGS `/start` → `/setup-engine unity` (인터뷰·설정 기록 담당) → `unity-cli`로 실제 프로젝트 생성·`unity pipeline install`. `new-unity-project`의 인터뷰는 쓰지 않음.
- 코드 루트: CCGS `code-root-resolution.md`가 이미 Unity를 `Assets/`로 인식(`Assets/Scripts/<System>/`). 추가 작업 없음.

### C9 🟢 Aseprite ↔ CCGS 아트 에이전트

- `art-director`(아트 바이블·스타일 결정) / `technical-artist`(임포트·셰이더) ↔ `aseprite:*` 에이전트(실제 픽셀 작업).
- **흐름**: `/art-bible`(CCGS) → `aseprite:brief`·`palette-smith`·`draw`·`animate` → `aseprite:export` → `Assets/Art/…` → Unity `sprite-editor`·`manage-sprite-atlas`·`2d-pixel-perfect`.
- `aseprite:pixel-critic` 결과는 `/asset-audit`의 증거로 첨부.

### C10 🟢 Git · 권한 · 워크트리

- 커밋 검증: CCGS `validate-commit.sh`(Claude 훅) + Graphify post-commit(git 훅) → 레이어가 달라 공존 가능. Graphify가 기존 post-commit에 append하는 방식이라 덮어쓰지 않음.
- 권한: CCGS deny 규칙이 먼저 평가되고, OMC `permission-handler`는 프롬프트가 뜰 때만 동작 → deny는 유지됨. 단 OMC가 자동 승인하는 범위가 CCGS의 "ask-first"보다 넓을 수 있음 → 구축 시 확인.
- 워크트리: CCGS `.worktreeinclude`·prototyper 격리 / Unity "워크트리당 Editor 1개, `--project-path` 필수" / OMC `project-session-manager`. → Editor가 필요한 워크트리는 `unity vcs git worktree add`로만 만들고, OMC psm은 사용 안 함.
- 모델: CCGS 에이전트는 frontmatter에 sonnet/opus/inherit 지정, 전역 기본 모델이 `sonnet`. 그대로 둠.

### C11 🟠 Windows 전제조건

- CCGS 훅 전부 Git Bash 필요(없으면 조용히 꺼짐 — 위험). OMC·Ponytail은 Node. Graphify는 Python. Unity CLI 별도.
- 이 PC는 모두 있음. 다른 PC를 위해 bootstrap이 확인.

### C12 🔴 git · gh CLI

현재 이 PC: git 2.56.1, git-lfs 3.8.1, gh 2.101.0 (keyring 로그인, scope `repo, workflow, gist, read:org`).

**이미 git을 건드리는 것들**

| 주체 | 무엇 | 레이어 |
|---|---|---|
| CCGS `validate-commit.sh` | 커밋 시 staged JSON 등 검증, 실패 시 차단 | Claude PreToolUse |
| CCGS `validate-push.sh` | 보호 브랜치 push, tag, merge, `gh release` 시 경고(차단 안 함) | Claude PreToolUse |
| CCGS deny 규칙 | force push, `reset --hard`, `clean -f` 전부 금지 | Claude 권한 |
| CCGS 규칙 | "지시 없이 커밋 금지", "엔진·플러그인·벤더 저장소에 이슈/PR 금지" | CLAUDE.md |
| OMC `git-master` 에이전트 | 커밋 분리·리베이스 | 에이전트 |
| Graphify | post-commit 훅 | git 훅 |
| git-lfs | post-commit, post-checkout, post-merge, pre-push 훅 | git 훅 |
| Unity CLI | `unity vcs git worktree add` | CLI |

**충돌과 해결**

1. 🔴 **gh가 엉뚱한 저장소를 대상으로 잡을 수 있음.** 게임 저장소에는 remote가 3개(`origin`=내 게임, `template`=내 템플릿, `upstream`=CCGS 공개 저장소). gh는 remote가 여러 개면 기본 저장소를 묻거나 upstream 쪽을 고를 수 있다 → `gh pr create`가 **CCGS 공개 저장소에 PR**을 열 위험. CCGS "Nothing Goes Upstream" 규칙 위반.
   → bootstrap이 `gh repo set-default <나>/<게임>` 실행. 그리고 deny 규칙에 `Bash(gh * --repo *)`, `Bash(gh * -R *)` 추가 → gh는 기본 저장소에만 작동.
2. 🔴 **git 훅 설치 순서.** git-lfs와 Graphify가 둘 다 post-commit을 쓴다. Graphify는 기존 훅에 덧붙이지만, `git lfs install`은 기존 훅이 있으면 덮어쓰지 않고 실패한다.
   → bootstrap 순서 고정: `git lfs install` → `graphify hook install`.
3. 🟠 **GitHub MCP와 gh 중복.** 이 세션에 GitHub MCP가 인증 대기 상태로 있음.
   → Unity와 같은 원칙: **셸이 있으면 CLI.** GitHub MCP는 인증하지 않고 gh만 쓴다.
4. 🟠 **private 저장소는 공개 CCGS의 GitHub fork가 될 수 없다** (공개 저장소의 fork는 공개). → fork 대신 `upstream` remote로 추적(4절).
5. 🟠 **"Use this template"으로 만든 저장소는 히스토리가 끊긴다** → 이후 템플릿 개선을 merge하려면 매번 `--allow-unrelated-histories`가 필요하고 충돌이 커진다.
   → 새 게임은 `scripts/new-game.ps1`로 **clone 기반 생성**(히스토리 유지), `template` remote를 남겨 `git merge template/main`으로 개선을 받음.
6. 🟠 **자격증명 평문 저장.** 이 PC의 git `credential.helper=store` → 토큰이 `~/.git-credentials`에 평문으로 있음. gh는 keyring.
   → `gh auth setup-git`으로 git도 gh 자격증명을 쓰게 바꾸고 store 제거. deny에 `Read(~/.git-credentials)` 추가.
7. 🟠 **Unity 파일 병합·줄바꿈.** CCGS `.gitattributes`는 `.sh`·`.md`만 LF 고정. Unity YAML(`.unity`, `.prefab`, `.asset`, `.meta`)과 바이너리 에셋 규칙이 없다.
   → `.gitattributes`에 Unity YAML `merge=unityyamlmerge` + 바이너리 LFS 추가. UnityYAMLMerge 경로는 PC마다 Unity 설치 경로가 달라 **git config(PC별)** 이라 bootstrap이 설정.
8. 🟢 OMC `git-master`는 사용자가 커밋을 지시했을 때 커밋 분리용으로만 사용. CCGS 커밋 규칙이 우선.

**GitHub 흐름 (결정: trunk + OMC 자율 작업만 PR)**

```
평소 (CCGS 협업)        main 에 직접 커밋 (사용자 지시 시)
OMC 자율 실행           git switch -c story/<id>
                        → /oh-my-claudecode:execute 또는 ralph (해당 스토리만)
                        → git push -u origin story/<id>
                        → gh pr create --fill --base main
                        → /ponytail-review, CCGS /code-review (gh pr diff 기준) → /story-done
                        → 사용자 지시: gh pr merge --squash --delete-branch
```

- 같은 체크아웃에서 브랜치만 바꾼다. Unity는 워크트리마다 `Library/` 재임포트가 필요해 무겁기 때문에, 워크트리는 OMC를 병렬로 돌릴 때만 쓴다(그때는 `unity vcs git worktree add`).
- `gh pr merge`·push는 CCGS `validate-push.sh`가 경고를 띄움 → 의도된 확인 단계로 둔다.
- CI(GitHub Actions + Unity 빌드)는 Unity 라이선스 시크릿과 private 저장소 무료 실행 시간 제한이 걸려 있어 **이번 범위에서 제외**. 필요해지면 별도 단계로.

---

## 3. 통합 아키텍처

```
┌──────────────────────────── 프로세스 (무엇을, 언제) ───────────────────────────┐
│ CCGS: 단계 게이트 · GDD · ADR · 에픽/스토리 · 리뷰 · 출시 · active.md           │
└───────────────┬──────────────────────────────────────────────────────────┘
                │ 승인된 스토리
┌───────────────▼──────── 실행 (어떻게) ──────────────────────────────────────┐
│ OMC: execute / ralph / team, LSP·ast-grep, debugger/tracer, notepad          │
│ Ponytail: 코드 에이전트에 최소 변경 규칙                                       │
└───────────────┬──────────────────────────────────────────────────────────┘
                │
┌───────────────▼──────── 도메인 도구 ────────────────────────────────────────┐
│ Unity plugin 스킬 + Unity CLI(Pipeline)  │  Aseprite (아트)                   │
└───────────────┬──────────────────────────────────────────────────────────┘
                │
┌───────────────▼──────── 지식 ───────────────────────────────────────────────┐
│ Graphify: Assets/Scripts + design/ + docs/ 그래프 (질의 우선)                  │
└──────────────────────────────────────────────────────────────────────────┘
```

### 단계별 사용 도구

| 단계 | 주 도구 | 보조 |
|---|---|---|
| 시작 | CCGS `/start` → `/setup-engine unity` | `unity-cli` (프로젝트 생성, pipeline install), `bootstrap.ps1` |
| 컨셉·프로토 | CCGS `/brainstorm`, `/prototype` | Aseprite `brief`/`concept` |
| 설계 | CCGS GDD·`/design-review`·`/create-architecture`·ADR | Graphify(`design/` 질의) |
| 계획 | CCGS `/create-epics` → `/create-stories` → `/sprint-plan` | — |
| 구현 | `/story-readiness` → `story/<id>` 브랜치 → **OMC `execute`/`ralph`** (스토리 단위) → `gh pr create` | Unity 스킬, Unity CLI, OMC LSP, Ponytail |
| 검증 | `/ponytail-review` → CCGS `/code-review` → `/story-done` → `gh pr merge` | Unity CLI playmode 검증 루프, 내장 `/code-review` |
| PC 이동 | `/handoff` (떠나는 PC) → `/pickup` (도착한 PC) | git, `~/claude-brain` |
| 디버깅 | OMC `debugger`/`trace` | Graphify `path`/`explain` |
| 아트 | CCGS `/art-bible` → Aseprite → Unity 스프라이트 스킬 | `pixel-critic`, `/asset-audit` |
| 출시 | CCGS `/release-checklist`, `/launch-checklist`, `/patch-notes` | `unity-cli` 빌드 |

---

## 4. 저장소 구성 (upstream 추적 전략)

### 브랜치·remote

```
템플릿 저장소:  origin   = github.com/<나>/unity-ai-template (private)
                ccgs     = github.com/Donchitos/Claude-Code-Game-Studios (push 주소 DISABLED)
게임 저장소:    origin   = github.com/<나>/<게임> (private)       ← gh repo set-default
                template = github.com/<나>/unity-ai-template
                ccgs     = (선택) CCGS — 보통은 template 경유로 받음

※ CCGS remote 이름을 `upstream`으로 두면 안 됨: gh는 기본 저장소가 없을 때
  `upstream`이라는 이름의 remote를 우선 선택한다 (6절 실측).
별도:           ~/claude-brain = github.com/<나>/claude-brain (private)
```

- CCGS 업데이트: 템플릿에서 `git fetch ccgs && git merge ccgs/main` (squash 아님 — 히스토리 유지해야 다음 merge가 쉬움)
- 템플릿 개선을 게임에 반영: 게임에서 `git fetch template && git merge template/main`
- 새 게임: `scripts/new-game.ps1 <게임-slug>` — `gh repo create <게임> --private` → 템플릿 clone → remote 재구성(`origin`→`template`, 새 `origin` 추가) → `autoMemoryDirectory` 슬러그 기입 → push → `gh repo set-default`. "Use this template" 버튼은 히스토리가 끊겨서 쓰지 않음(C12-5).

### upstream 파일 패치 (최소, 충돌 가능 지점은 이 4개뿐)

| 파일 | 변경 |
|---|---|
| `CLAUDE.md` | `@.claude/docs/framework/integration.md` 1줄 추가 |
| `.claude/settings.json` | `enabledPlugins`, `extraKnownMarketplaces`, `env`, `autoMemoryDirectory` 키 추가, SessionStart 훅 1개·deny 규칙 몇 개 **추가** (기존 항목 수정 없음) |
| `.gitignore` | Unity 블록 주석 해제 + `graphify-out/`, `.omc/*`, `!.omc/skills/` 추가 |
| `.gitattributes` | Unity LFS(텍스처·오디오·모델) + `*.unity`/`*.prefab` UnityYAMLMerge 설정 |

Godot/Unreal 에이전트·스킬은 **삭제하지 않음**(삭제 시 upstream이 수정할 때마다 modify/delete 충돌). 쓰지 않을 뿐.

### 새로 추가하는 오버레이 파일 (충돌 없음)

```
.claude/docs/framework/
  integration.md        # 실행 경계, 리뷰 분담, 기억 규칙, Unity 라우팅표, Graphify 사용법
  versions.md           # 검증된 플러그인 버전 조합 기록
.claude/skills/
  handoff/SKILL.md      # 인계 작성 → 커밋 → push (게임 + brain)
  pickup/SKILL.md       # pull → handoff.md로 active.md 시드
production/handoff.md   # 세션 인계 (커밋됨)
scripts/
  bootstrap.ps1         # 새 PC 셋업 + 전제조건 점검
  doctor.ps1            # 훅·플러그인·CLI·gh 기본 저장소 상태 점검
  new-game.ps1          # 템플릿에서 새 게임 저장소 생성 (C12-5)
  brain-sync.mjs        # ~/claude-brain pull (SessionStart 훅) / push (/handoff)
.graphifyignore 또는 graphify 설정   # Library/ Temp/ Logs/ obj/ *.unity *.prefab *.asset 제외
Assets/ Packages/ ProjectSettings/   # /setup-engine 이후 생성 (템플릿엔 비워둠)
```

### `.claude/settings.json`에 추가할 키 (초안)

```json
{
  "extraKnownMarketplaces": {
    "omc":              { "source": { "source": "github", "repo": "Yeachan-Heo/oh-my-claudecode" } },
    "ponytail":         { "source": { "source": "github", "repo": "DietrichGebert/ponytail" } },
    "aseprite-ai-artist": { "source": { "source": "github", "repo": "with-pebbly/aseprite-ai-artist" } },
    "claude-plugins-official": { "source": { "source": "github", "repo": "anthropics/claude-plugins-official" } }
  },
  "enabledPlugins": {
    "oh-my-claudecode@omc": true,
    "ponytail@ponytail": true,
    "unity@claude-plugins-official": true,
    "aseprite@aseprite-ai-artist": true
  },
  "env": {
    "PONYTAIL_SUBAGENT_MATCHER": "programmer|unity-.*specialist|prototyper|technical-artist|devops|executor|debugger|test-engineer|qa-tester|general-purpose"
  },
  "autoMemoryDirectory": "~/claude-brain/memory/<게임-slug>",
  "hooks": {
    "SessionStart": [
      { "matcher": "", "hooks": [ { "type": "command", "command": "node \"$CLAUDE_PROJECT_DIR/scripts/brain-sync.mjs\" pull", "timeout": 15 } ] }
    ]
  },
  "permissions": {
    "deny": [ "Bash(gh * --repo *)", "Bash(gh * -R *)", "Read(~/.git-credentials)" ],
    "ask":  [ "Bash(git commit*)", "Bash(git tag*)" ]
  }
}
```

추가로 커밋되는 `.claude/omc.jsonc` (키워드 자동 실행 차단), 그리고 bootstrap이 PC마다 쓰는 `.claude/settings.local.json`(같은 `ask` 규칙 — OMC 권한 훅은 이 파일만 읽음, 6절).

(`hooks.SessionStart`와 `permissions.deny`는 CCGS 기존 배열 **뒤에 항목을 추가**하는 것이지 덮어쓰는 것이 아님)

### `bootstrap.ps1`가 하는 일

1. 전제조건 확인: Git for Windows(Git Bash), git-lfs, gh(`gh auth status`), Node, Python, Unity Hub/Editor 6.x, `unity` CLI(`unity self-update --check`), Aseprite.
2. git/gh: `gh auth setup-git`(평문 store 대체), `gh repo set-default <나>/<게임>`, UnityYAMLMerge 경로를 git config에 등록, `~/claude-brain` 없으면 `gh repo clone <나>/claude-brain ~/claude-brain`, `~/.claude/CLAUDE.md`에 `@~/claude-brain/global/CLAUDE.md` import 확인.
3. 훅 설치 순서 고정: `git lfs install` → Graphify 설치 확인(`graphify --version`, 없으면 `pip install graphifyy` 후 skill 설치) → `graphify hook install`.
4. (C5 env 전달이 안 될 경우) 사용자 환경변수 `PONYTAIL_SUBAGENT_MATCHER`, `OMC_SKIP_HOOKS` 설정.
5. Unity 프로젝트가 있으면 `unity pipeline install` 상태 확인.
6. `doctor.ps1` 실행 결과 출력.

---

## 5. 구축 단계

| 단계 | 작업 | 완료 기준 |
|---|---|---|
| 1. 기반 | `gh repo create`로 템플릿·`claude-brain` private repo 생성, CCGS를 upstream으로 merge, 4개 파일 패치 | `claude` 시작 시 CCGS 훅 + 플러그인 4개 로드, brain pull 훅 동작 |
| 2. 실측 검증 | 아래 "검증 필요" 10개 항목 확인, 결과로 settings/integration.md 확정 | 체크리스트 전부 통과 또는 대안 적용 |
| 3. 통합 문서 | `integration.md` 작성 (이 설계서 2·3절을 규칙 형태로 압축) | CLAUDE.md import로 세션에 로드됨 |
| 4. 스크립트·스킬 | `bootstrap.ps1`, `doctor.ps1`, `new-game.ps1`, `brain-sync.mjs`, `/handoff`, `/pickup` | 빈 PC(또는 다른 PC)에서 clone→bootstrap→정상 |
| 5. 드라이런 | `new-game.ps1`로 시험 게임 생성 → `/start` → `/setup-engine unity` → 스토리 1개를 `story/<id>` 브랜치에서 OMC `execute` → `gh pr create` → `/story-done` → merge | 전 흐름 1회 무사 통과, 훅 지연 측정 |
| 5b. PC 이동 드라이런 | PC-A에서 작업 도중 `/handoff` → PC-B에서 `/pickup` | PC-B가 같은 스토리·결정·다음 단계를 이어받고, 자동 메모리도 동일 |
| 6. 버전 고정 | 통과한 플러그인 버전 조합을 `versions.md`에 기록, 템플릿 태그(`v0.1`) | — |

### 단계 2: 검증 필요 항목 (조사로 확정 못 한 것)

1. `OMC_SKIP_HOOKS`의 정확한 토큰명(특히 wiki 계열).
2. 프로젝트 settings `env`가 훅 프로세스에 전달되는지 (Ponytail matcher, OMC skip).
3. 프로젝트 스킬 `code-review`(CCGS) vs 내장 `/code-review` 중 어느 쪽이 호출되는지.
4. OMC `permission-handler`가 CCGS ask-first 대상을 자동 승인하는지.
5. OMC `persistent-mode`(ralph 재진입)와 CCGS `session-stop.sh`(async)가 Stop에서 서로 간섭하는지.
6. 툴 호출 1회당 훅 총 지연(OMC skip 적용 전/후).
7. Graphify가 Unity C#(`.cs`)에서 의미 있는 그래프를 만드는지 + 제외 규칙 적용 여부.
8. `autoMemoryDirectory`의 `~/`가 Windows에서 `C:\Users\<계정>`으로 풀리는지, 프로젝트 settings에서 지정했을 때 실제로 적용되는지(워크스페이스 신뢰 조건).
9. remote가 여러 개일 때 `gh repo set-default` 없이 `gh pr create`가 어느 저장소를 고르는지 (위험 확인용).
10. `git lfs install` 후 `graphify hook install` 순서에서 post-commit 훅에 둘 다 남는지.

---

### 3·4단계 진행 상황 (2026-11-15)

- ✅ `.claude/docs/framework/integration.md` (세션마다 로드되는 규칙) + `CLAUDE.md` import 1줄
- ✅ `README.md` 교체 + `.gitattributes` `README.md merge=ours` (CCGS README 5천 토큰 주입 문제 해소)
- ✅ `/handoff`, `/pickup` 스킬 — CCGS `active.md`가 이미 "콜드 재개용" 체크포인트라, 그 파일을 `production/handoff.md`로 복사·커밋하는 방식
- ✅ `scripts/bootstrap.ps1` — 도구 확인, LFS→Graphify 훅, ours/UnityYAMLMerge 드라이버, `settings.local.json` 기억 경로, gh 기본 저장소, `upstream` remote·평문 자격증명 경고. 이 PC에서 실행 확인
- ✅ `scripts/new-game.ps1` — `-WhatIf`로 미리보기만 확인 (실제 저장소 생성은 5단계 드라이런)
- ⏸ `doctor.ps1`은 만들지 않음 — bootstrap이 재실행 가능한 점검을 겸함
- ⏳ 아직: `/handoff`·`/pickup` 실제 동작, `new-game.ps1` 실제 실행, Unity 프로젝트 생성 (5단계)

## 6. 2단계 실측 결과 (2026-11-15)

새 Claude 세션(headless)과 훅 스크립트 단독 실행으로 확인. 이 PC는 CLI 로그인이 안 되어 모델 응답 없이 **세션 초기화·훅·설정 로딩까지만** 검증함.

| # | 항목 | 결과 | 조치 |
|---|---|---|---|
| 1 | `OMC_SKIP_HOOKS` 토큰 | 🔴 `wiki`·`project-memory` 토큰은 존재하지 않음(정확한 이름은 `project-memory-posttool`, `post-tool-use`). **더 큰 문제: 스킵을 켜면 오히려 느려짐** — keyword-detector 155ms→**8,110ms**, project-memory-posttool 135ms→**2,610ms** (Windows, 반복 측정 동일). 실제 세션의 `prompt_submit_hooks_ms`도 8,129ms | `OMC_SKIP_HOOKS` 제거. 키워드는 `.claude/omc.jsonc`로 차단(264ms, 차단 확인). project-memory는 켜 둠(`.omc/`라 gitignore됨). wiki 훅은 `.omc/wiki/`가 없으면 아무것도 안 함 |
| 2 | 프로젝트 `env` → 훅 전달 | ✅ 훅 프로세스에서 `PONYTAIL_SUBAGENT_MATCHER` 값 확인 | 없음 |
| 3 | `/code-review` 충돌 | 프로젝트 안에서는 CCGS 스킬 하나만 노출. 내장 리뷰는 이름으로 못 부름 | 리뷰 분담표 수정(C4) |
| 4 | OMC 권한 훅 | 🔴 heredoc 형식 `git commit`·`git tag`를 **자동 승인**. `ask` 규칙이 있으면 양보하지만 **프로젝트 `settings.json`은 안 읽고 `settings.local.json`과 전역 설정만 읽음** | `settings.json`에 `ask` 추가(Claude Code용) + bootstrap이 `settings.local.json`에 같은 `ask`를 씀(OMC용). 실측: local 파일이 있으면 `defer-to-user`로 바뀜 |
| 5 | Stop 훅 간섭 | ✅ 코드상 CCGS `session-stop.sh`는 async 로그 기록, OMC `persistent-mode`는 OMC 모드 활성 시에만 차단 → 간섭 없음 (실제 ralph 실행은 5단계에서) | 없음 |
| 6 | 훅 지연 | 세션 시작 훅 초기화 ~1.8초. 툴 호출당 OMC Pre 155ms + Post 4개 약 640ms + CCGS 88ms ≈ **0.9초** (스킵 미사용 기준) | 수용. 5단계에서 체감 확인 |
| 7 | Graphify on C# | ✅ 클래스·메서드·상속(`MonoBehaviour`)·호출·`GetComponent<T>` 참조까지 추출, `.gitignore`의 `Library/` 제외 확인. 이벤트 구독 관계는 안 잡힘. **post-commit 훅은 워크트리에서는 스킵** | 워크트리에서 작업 후엔 main에서 갱신됨을 문서화 |
| 8 | `autoMemoryDirectory` `~/` | ✅ `C:\Users\simplex3510\claude-brain\memory\unity-ai-template\` 로 해석됨 | 없음 |
| 9 | gh 기본 저장소 | 🔴 기본 저장소 미설정 시 gh가 **`upstream` 이름의 remote(CCGS 공개 저장소)를 선택** (`gh pr list`가 CCGS PR을 보여줌). remote 이름을 `ccgs`로 바꾸면 `origin`을 선택 | 템플릿 remote `upstream`→`ccgs` 변경 완료, push 주소 DISABLED. set-default도 계속 설정 |
| 10 | git 훅 순서 | ✅ `git lfs install` → `graphify hook install` 순서에서 post-commit·post-checkout에 둘 다 남음. Graphify는 `graph.json` merge 드라이버도 등록 | 없음 |

**추가 발견**
- 🟠 OMC `post-tool-directory-context-injector`가 파일을 처음 건드릴 때 **루트 README.md를 컨텍스트에 주입**(세션당 1회, 최대 약 5,000토큰). 지금 README는 CCGS 홍보용 30KB 문서라 토큰 낭비. → 3단계에서 README를 템플릿 사용법으로 교체하고, `.gitattributes`에 `README.md merge=ours`(드라이버는 bootstrap이 PC별 등록)로 CCGS 업데이트 때 우리 README를 유지.
- 🟢 이 세션은 CCGS 설정이 생긴 뒤 deny 규칙이 즉시 적용됨(`rm -rf` 차단 확인). 훅·플러그인은 세션 재시작 후 적용.

---

## 7. 남는 리스크

- **플러그인 버전 고정 불가**: 마켓플레이스는 최신을 따라가므로 PC마다 버전이 어긋날 수 있음. `versions.md`에 검증 조합을 기록하고 `doctor.ps1`이 차이를 경고하는 수준이 현실적 한계.
- **CCGS 업데이트 속도**: 패치한 4개 파일은 merge마다 확인 필요. 특히 `.claude/settings.json`은 upstream이 자주 바꿈.
- **Unity 플러그인이 beta**(0.1.9-beta): 스킬 이름·CLI 명령이 바뀔 수 있음 → 라우팅표는 integration.md 한 곳에만 둔다.
- **`/handoff`를 잊으면 인계가 안 된다**: 자동 push는 CCGS 규칙 때문에 일부러 넣지 않았다. 대신 `/pickup`이 "마지막 handoff가 다른 PC의 마지막 커밋보다 오래됐다"를 감지하면 경고하도록 한다.
- **brain 저장소 동시 편집**: 두 PC가 같은 기억 파일을 동시에 고치면 충돌. `MEMORY.md`는 union merge로 막지만 개별 파일 충돌은 수동 해결.
- **brain 저장소의 민감도**: 자동 메모리에는 개인 작업 습관·프로젝트 정보가 쌓인다 → 반드시 private 유지, 공개 저장소로 바꾸지 말 것.
- **Git LFS 용량**: GitHub private 저장소의 LFS 저장공간·대역폭은 무료 한도가 있다. 아트·오디오가 커지면 유료 데이터 팩이 필요할 수 있음.
- **세 플러그인의 서브에이전트 주입 누적**: SubagentStart에 OMC·Ponytail·CCGS가 모두 컨텍스트를 붙임 → 서브에이전트 시작 토큰이 늘어남. 단계 5에서 측정.
