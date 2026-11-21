# Epic: networking

> **Goal**: 2~4인 협동 배달에서 서버 권한 차량·물류·시계·정산을 분리 실행 서버로 동기화한다. NGO + Unity Transport, 서버는 Dedicated Server 빌드를 호스트 클라이언트가 자식 프로세스로 기동한다. *(ADR-0001 Summary)*
> **GDD**: `design/gdd/networking.md`
> **Governing ADRs**: ADR-0001 (Accepted), ADR-0002 (Accepted)
> **Order**: 설치·서버 빌드 → 에디터 멀티 테스트 → Direct 참가(1단계) → 이후 스토리는 `/create-stories networking`으로 추가
> **주의**: 첫 릴리즈 전 개발 단계(GUI 임시). 1인 개발, 마감 없음.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | NGO 설치와 Dedicated Server 빌드·기동 확인 | Integration | Complete | ADR-0001 |
| 002 | 서버 부트스트랩 역할 분기와 에디터 멀티 테스트 | Integration | Complete | ADR-0001 |
| 003 | 같은 LAN 2대 Direct 참가 (AC-NET-30) | Integration | Ready | ADR-0002 |
