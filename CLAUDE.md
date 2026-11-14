# CLAUDE.md

## 게임 개요

- 게임 이름: Over?Load!
- 게임 장르: 캐주얼, 시뮬레이터, 레이싱
- 게임 엔진 및 버전: Unity 6000.6.8f1
- 게임 개요: 플레이어는 배송 차량을 운행하며 물류센터에서 물류를 상차, 목표 배송지까지 도달하여 물류를 하차하고 수익을 내는 것을 주된 목표로 하는 물류 배송 캐주얼 레이싱 시뮬레이터 게임

## 언어

- 한국어로 대답하며, 전문적이고 공적인 말투를 사용함 (합니다체).
- 코드 식별자(class, field, method names)는 영어로 작성.
- 코드 주석은 한국어로 작성.

## 규칙

자세한 규칙은 `.claude/rules/`에 있음:

- `collaboration.md`: 사용자와 상호작용하는 방법
- `development.md`: 개발 원칙과 코드 스타일
- `unity-safety.md`: 직접 편집해서는 안 되는 파일과 자산

## 문서 및 기획

모든 문서는 `./.docs/` 디렉토리 하위에 존재함.

자세한 문서는 다음을 참고:

- `game-design.md`: 게임 기획의 전반을 서술한 문서
- `dev-plan.md`: 현재 마일스톤 개발 단계 및 후속 개발 지점 추적
  - 한 단계의 개발 작업이 마무리되면 해당 문서를 업데이트(진행 기록 갱신) 해야 함.

## Studio Mode (Claude Code Game Studios)

`.claude/` 하위에 CCGS 프레임워크가 설치되어 있음. 엔진·모드 설정은 `project.yaml`이 정본이고, 이미 `engine: Unity`, 코드 루트 `Assets/`로 해석됨.

- 진입 명령: `/start`(진행 단계 진단), `/next-step`(다음 작업 제안), `/help`(전체 목록)
- 전체 스킬·에이전트 목록: `.claude/docs/skills-reference.md`, `.claude/docs/agent-roster.md`
- 프레임워크 갱신: `git fetch template && git checkout template/main -- .claude`
  - `.claude/`는 git이 추적함(팀 공유). 개인 설정은 `.claude/settings.local.json`에 두며 `.gitignore`로 제외됨.

**문서 위치 규칙**: CCGS 스킬이 `design/`, `docs/`, `production/`에 산출물을 만들더라도, 게임 기획·개발 계획의 정본은 계속 `.docs/`임. CCGS 산출물은 파이프라인 중간물로만 취급하고, 확정된 내용은 `.docs/`로 옮겨 기록함.

## graphify

When the user types `/graphify`, use the installed graphify skill (`.claude/skills/graphify/SKILL.md`) before doing anything else.

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- When graphify-out/graph.json exists, run `graphify query "<question>"` before your first grep, search or source-file read for a codebase question, and put this rule in every subagent prompt that explores code. Read raw files only for lines the graph or an earlier read already located. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- A committed graphify-out/graph.json is rewritten whole on every build, so a large diff to it in a pull request is expected. Do not flag it as churn or suggest skipping the graph rebuild or its hooks.
