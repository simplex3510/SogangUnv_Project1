---
name: feedback-no-vendor-presumption
description: "외부 서비스 업체(UGS, Steam 등)는 전제하지 말고 선택지로 따로 논의해 사용자가 고르게 함"
metadata:
  node_type: memory
  type: feedback
  originSessionId: a376914b-72e7-4203-ba75-7e081e0ed484
  modified: 2026-11-21T03:03:16.340Z
---

외부 서비스 업체(중계·인증·백엔드 등)를 ADR이나 설계에서 기본값으로 전제하지 않는다. 업체 선택은 별도 결정 항목으로 비교해 사용자에게 묻는다.

**Why:** 2026-11-21 ADR-0002(세션 참가 수단) 초안이 Unity Relay(UGS)를 전제해 작성됐고, 사용자가 "업체를 논의한 적 없다"며 재검토를 요청함.

**How to apply:** 대안 목록에 업체가 섞이면 "업체 선택"을 독립 질문으로 분리하고, 비용·계정·배포 플랫폼·플레이어 설치 부담 같은 기준을 먼저 확인한다. 관련: [[project-stage-pre-release]]
