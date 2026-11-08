검증: CONFIRMED (DoStrike의 SetFlame(strikesLeft==1) 후 NextAfterChain→Special 진입, EnterState(Special)은 불꽃을 끄지 않음 — 코드 확인, 표시 문제)

# BUG-ENEMY-A04 — 안드로이드 특수 공격(황금 탄 3발) 시전 중에도 마지막 근접 일격 예고 불꽃이 켜져 있음

## 재현 단계
1. 안드로이드(`kind = Kind.Android`)와 전투를 시작한다.
2. 안드로이드의 남은 연타 횟수가 1회(`strikesLeft == 1`, 예: 개인 숫자가 2인 안드로이드의 1타 직후, 또는 개인 숫자 N인 안드로이드의 N-1번째 타격 직후)가 되었을 때 부채꼴 특수 공격(`State.Special`) 쿨다운이 준비된 상태를 만든다.
3. 안드로이드가 `State.Special`(0.5초 황금색 예고 후 황금 탄 3발 발사)을 시전하는 동안 머리 위 숫자의 불꽃 이펙트(`number-ember`)를 관찰한다.

## 기대 결과
- 기획서(`docs/design/GDD.md:91, 105, 116` `[확정]`, `docs/design/DECISIONS.md:94` 7-A 확정, `docs/design/impl/enemy-rules.md:35-39`)에 따라, 안드로이드의 황금 탄 3발 특수 공격은 마지막 강한 일격과는 별도의 공격이며, 빨간 불꽃 이펙트(`SetFlame(true)`)는 **마지막 일격 직전 예고**에서만 표시되어야 한다.

## 실제 결과
- `DoStrike()` 끝에서 다음 상태가 `State.Special`인지 확인하지 않고 무조건 `number.SetFlame(strikesLeft == 1);`을 먼저 호출한다.
- 이어서 `Recover`(0.15초) 후 `NextAfterChain()`이 `EnterState(State.Special)`로 진입하는데, `EnterState(State.Special)`에서 `number.SetFlame(false)`를 호출하지 않아 황금 탄 3발 부채꼴 공격을 예고·발사하는 내내 머리 위 숫자에 마지막 일격용 빨간 불꽃(`number-ember`)과 붉은 색상 보정이 켜져 있다.
- 그 결과 플레이어는 다음 공격이 황금 탄 3발(특수 공격)인지 마지막 근접 일격(7일 때 패링 불가)인지 불꽃 예고로 구분할 수 없게 된다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:317-320` — `DoStrike()`에서 `strikesLeft--` 직후 `number.SetFlame(strikesLeft == 1);`을 즉시 켬.
- `Assets/Scripts/Enemy/TestEnemy.cs:255-259` — `NextAfterChain()`에서 `strikesLeft == 1`일 때도 `Time.time >= nextSpecialAt`이면 `State.Windup` 대신 `State.Special`로 진입함.
- `Assets/Scripts/Enemy/TestEnemy.cs:368-371` — `EnterState(State.Special)`에서 `FacePlayer(); number.SetPulse(true);`만 수행하고 `number.SetFlame(false)`를 호출하지 않음.
- `Assets/Scripts/Enemy/EnemyNumber.cs:103-109, 129` — `flameOn == true`인 동안 `number-ember` 스프라이트 이펙트와 붉은 색상 보정(`Color.Lerp(numberColor, Red, 0.6f)`)이 유지됨.
