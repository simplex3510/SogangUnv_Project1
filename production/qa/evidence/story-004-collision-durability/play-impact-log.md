# Story 004 Play mode 관찰 — 2026-11-16

씬: `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (Play mode, Unity 6000.6.8f1)
방법: `unity command script eval`로 Player에 시험 물류 2개(일반 #9001, Fragile #9002, 내구도 100)를 적재하고,
무거운 시험 벽(질량 100000, 중력 끔)을 차량 정면에서 6 m/s로 밀어 충돌시켰다. 씬은 저장하지 않았다.

콘솔 `[CargoImpact]` 로그 (순서대로):

```
[CargoImpact] impulse=496.9 mass=100 → deltaVelocity=4.97
[CargoImpact] damage=min((4.97-1.5)*12, 60)=41.63
[CargoImpact] Parcel#9001 (Normal)  100 → 58.37
[CargoImpact] Parcel#9002 (Fragile ×1.5)  100 → 37.56
[CargoImpact] impulse=2975.56 mass=100 → deltaVelocity=29.76
[CargoImpact] 쿨다운 중 재접촉 deltaVelocity=29.76 → 무시
[CargoImpact] impulse=205.14 mass=100 → deltaVelocity=2.05
[CargoImpact] 쿨다운 중 재접촉 deltaVelocity=2.05 → 무시
[CargoImpact] impulse=268.41 mass=100 → deltaVelocity=2.68
[CargoImpact] 쿨다운 중 재접촉 deltaVelocity=2.68 → 무시
[CargoImpact] impulse=8024.8 mass=100 → deltaVelocity=80.25
[CargoImpact] damage=min((80.25-1.5)*12, 60)=60
[CargoImpact] Parcel#9001 (Normal)  58.37 → 0
[CargoImpact] Parcel#9002 (Fragile ×1.5)  37.56 → 0
[CargoImpact] deltaVelocity=0 < 하한선 1.5 → 무시   (이후 접촉 여러 번)
```

확인한 것: 일반 41.63, Fragile 62.44(=41.63×1.5) 감소 / 쿨다운 중 재접촉 무시 / 1회 상한 60 / 0 하한 / 하한선 미만 무시.
확인하지 못한 것: 실제 주행 입력으로 벽에 부딪힌 경우(시험 벽을 밀어 충돌시킴), 착지 충격 제외(법선 방향), 화면 캡처(`capture game_view`가 파일을 남기지 않음).
