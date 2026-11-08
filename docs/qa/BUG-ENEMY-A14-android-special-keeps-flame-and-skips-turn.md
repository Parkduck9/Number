검증: CONFIRMED / 중복: BUG-ENEMY-A04 (불꽃), BUG-ENEMY-A05 (Turn 누락)

# BUG-ENEMY-A14: 안드로이드가 마지막 타격 직전(strikesLeft == 1) 특수 공격(State.Special)에 진입하면 붉은 불꽃이 켜진 채 발사되고 예고 중 등 뒤 이동 시 State.Turn을 건너뛰는 문제

## 재현 단계
1. 안드로이드(`kind == Kind.Android`, 예: `Test_Enemies`의 `Strong` 또는 `STG-05`의 안드로이드)가 연타를 진행하여 남은 타수가 1(`strikesLeft == 1`)이 된 직후 `State.Recover`를 거쳐 `State.Special`(황금 탄 3발 부채꼴 예고 `0.5초`)에 진입하게 한다.
2. `State.Special` 예고 동안 안드로이드의 머리 위 숫자에 붙은 이펙트를 확인한다.
3. 이어서 `State.Special`의 `0.5초` 예고 도중 대시(Shift)나 이동으로 안드로이드의 등 뒤로 넘어간 뒤, 탄 발사 방향과 직후 안드로이드의 상태 및 등 뒤 공격 적중 여부를 확인한다.

## 기대 결과
- `docs/design/GDD.md` 5장 `[확정]` 및 `docs/design/impl/enemy-rules.md:34-41`에 따라 빨간 불꽃(`number-ember`)은 마지막 근접 일격 직전에만 표시되어야 하며, 별도 원거리 패턴인 황금 탄 3발 특수 공격(`State.Special`) 중에는 마지막 일격 불꽃이 켜져 있지 않아야 한다.
- 또한 예고 도중 플레이어가 등 뒤로 넘어가면 황금 탄 발사 후 남은 체인을 이어가기 전에 `State.Turn`(`0.35초`)을 거쳐 돌아서야 하며, 돌아서는 동안 플레이어의 등 뒤 공격에 피해를 입어야 한다(`docs/design/PROTOTYPE_ARCH.md:102, 212`).

## 실제 결과
- 직전 타격의 `DoStrike()`(`Assets/Scripts/Enemy/TestEnemy.cs:320`)에서 `strikesLeft == 1`이 되는 순간 `number.SetFlame(true)`로 빨간 불꽃을 켜는데, 이후 `NextAfterChain()`(`257줄`)을 통해 진입하는 `EnterState(State.Special)`(`368-371줄`)이 `number.SetFlame(false)`를 호출하지 않는다. 그 결과 패링 가능한 황금 탄 3발을 예고하는 동안에도 마지막 강타(숫자 7일 때 패링 불가)를 뜻하는 빨간 불꽃이 그대로 타오른다.
- 또한 `EnterState(State.Special)`(`369줄`)은 진입 순간에만 `FacePlayer()`를 호출하고, `0.5초` 뒤 발사 시점(`Assets/Scripts/Enemy/TestEnemy.cs:245-250`)에는 플레이어 위치를 다시 확인하지 않은 채 기존 `facing` 방향(플레이어 반대쪽)으로 `FireFan()`을 발사한 뒤 `State.Turn` 없이 곧바로 `EnterState(State.Windup)`(`249줄`)으로 넘어간다.
- 이때 `state`가 `State.Turn`이 아니므로 플레이어가 안드로이드의 등 뒤에서 공격해도 `ReceivePlayerHit`(`Assets/Scripts/Enemy/TestEnemy.cs:391`: `if (strikesLeft > 0 && state != State.Turn && state != State.Frozen) return false;`)에 막혀 등 뒤 공격이 전부 튕겨 나간다.

## 심각도
중간
