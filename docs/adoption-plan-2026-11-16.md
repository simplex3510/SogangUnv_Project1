# Adoption Plan

> **Generated**: 2026-11-16
> **Project phase**: Production (추정 — `project.stage` 없음, 소스 46개)
> **Engine**: Unity 6000.6.8f1
> **Template version**: v1.0+
> **Workflow**: minimal (감사 범위: game-brief, 엔진 레퍼런스, 프로젝트 설정, v1.0 마이그레이션. ADR·스토리·인프라 행은 minimal 경로가 아니라 **감사하지 않고 건너뜀**)

Work through these steps in order. Check off each item as you complete it.
Re-run `/adopt` anytime to check remaining gaps.

---

## Step 1: Fix Blocking Gaps

없음. (v1.0 마이그레이션도 불필요: `project.yaml` 존재, 레거시 설정 파일 없음)

---

## Step 2: Fix High-Priority Gaps

### 2a. `design/game-brief.md` 없음
minimal 단계의 유일한 필수 설계 문서이며 `/cut`, `/prototype`이 읽는다. 필수 6필드: Pitch, Core loop, Goal & fail state, Features, Out of scope, Build order. 기존 `design/gdd/game-concept.md`(핵심 루프·정산 수식 포함)에서 옮겨 적을 수 있다.
**명령**: `/reverse-document` · **시간**: 30분
- [ ] design/game-brief.md 작성

### 2b. 엔진 레퍼런스 불일치
- `CLAUDE.md`의 `ENGINE-REFERENCE-IMPORT` 아래 줄이 비어 있음 (세션 시작 훅도 경고).
- `docs/engine-reference/unity/VERSION.md`는 Unity 6.3 LTS 기준, 프로젝트는 6000.6.8f1. 6.6 API는 **NOT SOURCEABLE — 6.6은 docs/engine-reference/unity/ 가 다루지 않음**. 학습 데이터로 채우지 않는다.
**명령**: `/setup-engine unity` · **시간**: 30분
- [ ] VERSION.md를 프로젝트 핀에 맞게 갱신하거나 불일치를 명시
- [ ] CLAUDE.md에 `@docs/engine-reference/unity/VERSION.md` import 기록

---

## Step 3: Bootstrap Infrastructure

Not on the minimal path — the brief's build order is the plan. Next: `/create-stories` (or `/dev-story` once stories exist).

---

## Step 4: Medium-Priority Gaps

### 4a. 기존 GDD 4개가 표준 8섹션 제목을 쓰지 않음
`gdd-structure-check.sh`가 4개 파일 모두 8섹션 ABSENT로 보고(스크립트는 정상 실행, 제목이 한국어 번호 형식). minimal에서는 필수 아님(참고 사항).
- `game-concept.md`, `collision-durability-rules.md`, `delivery-cargo-vehicle-rules.md`, `delivery-cargo-vehicle-rules-plan.md`
- `*-plan.md`는 GDD가 아닌 작업 추적 문서이므로 `design/gdd/` 밖으로 옮길지 검토.
**명령**: 필요 시 `/design-system retrofit design/gdd/<파일>.md` · **시간**: 1세션
- [ ] (선택) 필요한 GDD만 retrofit

---

## Step 5: Optional Improvements

- [ ] `project.stage` 기록 (minimal에서는 갭 아님, 참고용)

---

## What to Expect from Existing Stories

기존 스토리 없음. 이후 생성되는 스토리는 모든 템플릿 스킬과 정상 동작한다.

---

## Re-run

Run `/adopt` again after completing Step 2 to verify all blocking and high gaps
are resolved. The new run will reflect the current state of the project.
