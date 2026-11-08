검증: CONFIRMED (TestEnemy.cs:436-443 ForceStagger는 Stunned/Frozen만 제외하고 Open 상태도 1초 스태거로 덮어씀)

# BUG-ENEMY-C04 — 이미 반격 창(Open)·NUM-7 탈진 중인 적에게 충격파를 쓰면 빈틈이 오히려 짧아짐

## 재현 단계
1. 충격파를 해금한다 (STG-03 또는 에디터 F9).
2. A) 방송 3 구역에서 적의 3연타가 끝나 Open(1.5초)이 시작된 직후, 또는 B) 7 해킹 후 7초가 지나 탈진(Open 2초)이 시작된 직후에 Space로 충격파를 맞힌다.
3. 적이 다시 움직이기 시작하는 시간을 잰다.

## 기대 결과
- 충격파(DECISIONS 8-A)는 적을 1초 경직시켜 빈틈을 **만들어 주는** 기술이다. 이미 열려 있는 빈틈(1.5초 / 2초)을 줄이면 안 된다.

## 실제 결과
- `ForceStagger`가 Open 상태를 확인하지 않고 Staggered로 덮어쓴다. 경직은 1초이므로 A)는 1.5초 → 1초, B)는 2초 → 1초로 빈틈이 줄어든다. 숫자도 1초 뒤 재추첨되어 다음 연타가 바로 시작된다.

## 원인 (코드 근거)
- `Assets/Scripts/Enemy/TestEnemy.cs:436-443` `ForceStagger`: `Stunned`·`Frozen`만 제외하고 무조건 `EnterState(State.Staggered)` 후 `stateEnteredAt`을 `seconds`(기본 1초, `Shockwave.cs:14`)에 맞춰 당긴다.
- Open 길이: `TestEnemy.cs:362` (NUM-3이면 `Num3OpenTime` 1.5초), 탈진 `TestEnemy.cs:175-176` (`Num7ExhaustSeconds` 2초).
- 호출: `Assets/Scripts/Player/Shockwave.cs:73` (범위 안 모든 적).

## 수정 제안
- `ForceStagger`에서 이미 빈틈인 상태는 남은 시간만 늘린다. 예:
  `if (state == State.Open) { float left = openDuration - (Time.time - stateEnteredAt); if (left >= seconds) return; }` 를 넣고, 남은 시간이 더 짧을 때만 경직으로 바꾼다. (Staggered도 같은 방식.)

## 심각도
낮음 — 충격파를 Open 중인 적에게 썼을 때만. 다만 보스 웨이브처럼 여러 적이 섞인 곳에서 범위 경직을 쓰면 자주 겹친다.
