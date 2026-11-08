검증: CONFIRMED (코드상 경로 확인: PlayerController.cs:265 Locomotion 복귀 후 EnemyProjectile.cs:52-57이 히트스톱 중에도 판정; 실제 거리는 플레이 확인 필요)
중복: BUG-COMBAT-A04-parry-immediately-drops-guard-against-multi-hit-and-fan-shots.md

# BUG-COMBAT-C01 — 안드로이드 황금 탄 3발(부채꼴)은 패링해도 사실상 한 발은 맞는다

**영역:** COMBAT (패링, EnemyProjectile)
**위반 규칙:** DECISIONS.md 7-A `[확정]` "황금 탄 3발 부채꼴 … **패링 가능**"

## 재현 단계
1. STG-04(또는 Android 적이 있는 테스트 씬)에서 안드로이드에게 근접(공격 사거리 1.8 안쪽)한다.
2. 연타 도중 안드로이드가 황금색으로 변하며(Special, 0.5초 예고) 탄 3발을 쏜다.
3. 안드로이드를 바라본 채 탄이 닿기 직전에 우클릭으로 패링한다.

## 기대 결과
패링 창(0~0.20초) 안에서 앞쪽에서 온 탄 3발이 모두 막힌다. HP 감소 없음.

## 실제 결과 (코드 근거)
- 첫 탄이 닿으면 `PlayerController.ReceiveEnemyHit`가 패링 성공 처리 후 **즉시 `EnterState(State.Locomotion)`** 한다 (`Assets/Scripts/Player/PlayerController.cs:265`). 패링 창이 그 순간 끝난다.
- 같은 프레임(또는 다음 프레임) 두 번째 탄의 `EnemyProjectile.Update`가 플레이어와 겹친 상태로 다시 `ReceiveEnemyHit`를 부른다 (`Assets/Scripts/Combat/EnemyProjectile.cs:52-57`). 이때 상태는 Locomotion, 무적도 없으므로 **Hit → HP -1** (`PlayerController.cs:276`).
- 히트스톱(timeScale 0) 중에도 탄의 겹침 판정은 계속 돈다 (위치만 안 움직임). 그래서 "히트스톱이 끝난 뒤 다시 패링" 할 기회도 없다.
- 세 번째 탄은 피격 무적으로 Dodged → 통과.
- 기하 계산: 각도 15° (`Assets/Scripts/Broadcast/BroadcastRules.cs:18-20`), 머즐 y +0.3 (`TestEnemy.cs:43`), 플레이어 박스 높이 2, 탄 0.5. 머즐에서 수평 약 3.5u 안쪽이면 3발이 모두 플레이어 박스에 들어오고, x 속도 차(8 vs 7.73)가 작아 거의 같은 프레임에 닿는다. Special은 연타 중(근접 거리)에만 나오므로 (`TestEnemy.cs:255-258`) 실제 플레이에서는 거의 항상 이 경우다.
- 결과: "패링 가능" 특수 공격이 실제로는 대시로만 피해 없이 넘길 수 있다.

같은 원인의 부수 현상 (설계 확인 필요, 불확실): 근접 시민 여러 명이 0.2초 안에 연달아 때리면 첫 타 패링 직후 두 번째 타격은 무조건 맞는다 (PROTOTYPE_ARCH 2절 "패링 성공 시 즉시 Locomotion"은 1적 기준으로 쓰인 규칙).

## 심각도
높음 — `[확정]` 규칙 위반, Android 특수 공격마다 HP 1 손실.

## 수정 제안 (참고)
패링 성공 후 짧은 시간(예: 히트스톱 + 0.1초(T)) 동안 같은 방향의 추가 타격도 Parried로 처리하는 `parryGraceUntil`(Time.time 기준)을 두거나, 패링 성공 시 상태를 바로 Locomotion으로 바꾸지 않고 패링 창을 남긴다. 선택은 Sonnet/Opus 판단.

## 확신도
코드 경로: 높음. 정확한 거리 수치: 중간 (콜라이더 크기·프레임 타이밍에 따라 다름).
