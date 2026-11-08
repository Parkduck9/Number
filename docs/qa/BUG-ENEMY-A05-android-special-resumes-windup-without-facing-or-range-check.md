검증: CONFIRMED (Special은 진입 시 FacePlayer 1회뿐, UpdateState Special→FireFan→Windup 전환에 방향 재확인 없음 — 코드 확인. 발생은 대시로 등 뒤로 넘어갈 때만)

# BUG-ENEMY-A05 — 안드로이드 특수 공격(State.Special) 직후 플레이어 방향·사거리 확인 없이 제자리에서 연타를 재개함

## 재현 단계
1. 안드로이드(`kind = Kind.Android`)가 연타 도중 `State.Special`(0.5초 황금색 예고)에 진입할 때, 대시 또는 이동으로 안드로이드의 등 뒤로 넘어가거나 근접 공격 사거리(`attackRangeX = 1.8f`) 밖으로 벗어난다.
2. 0.5초 예고가 끝나 황금 탄 3발(`FireFan()`)이 발사되고 이어서 다음 근접 연타(`State.Windup` → `State.Strike`)가 나가는 방향과 위치를 확인한다.

## 기대 결과
- `docs/design/impl/enemy-rules.md:35-39` 및 `docs/design/PROTOTYPE_ARCH.md:101, 209`에 따라, 안드로이드가 특수 공격 후 남은 연타를 이어갈 때 플레이어가 등 뒤에 있으면 회전(`State.Turn` 또는 `FacePlayer()`)해야 하고, 직전 타격 넉백 등으로 근접 사거리 밖에 있으면 거리를 좁혀 연타를 이어가야 한다.

## 실제 결과
- `EnterState(State.Special)` 진입 순간에만 `FacePlayer()`를 1회 호출하고, `0.5초`(`AndroidFanTell`) 동안 안드로이드는 제자리에 완전히 멈춰 있다(`FixedUpdate`에서 `Special` 상태는 `vx = 0f`).
- 0.5초 예고 동안 플레이어가 안드로이드 뒤로 이동하더라도, `FireFan()` 발사 시점과 그 직후 `EnterState(State.Windup)` 전환 시점에 방향이나 거리를 전혀 확인하지 않는다.
- 그 결과 안드로이드는 등 뒤의 플레이어 반대쪽으로 탄을 발사한 뒤, 돌아서지도 않고 빈 허공에 대고 남은 근접 연타(`State.Windup` → `State.Strike`)를 제자리에서 휘두른다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:132-138` — `FixedUpdate()`는 `State.Approach`와 `State.Recover`에서만 플레이어와의 거리(`attackRangeX`)를 좁히며 `State.Special`에서는 이동하지 않음.
- `Assets/Scripts/Enemy/TestEnemy.cs:244-252` — `State.Special`에서 `t >= BroadcastRules.AndroidFanTell` 경과 시 플레이어의 전후방 위치나 사거리 검사 없이 `FireFan()` 호출 후 무조건 `EnterState(State.Windup)`으로 진입함.
- `Assets/Scripts/Enemy/TestEnemy.cs:368-371` — `FacePlayer()`가 `State.Special` 진입 시점에만 호출됨.
