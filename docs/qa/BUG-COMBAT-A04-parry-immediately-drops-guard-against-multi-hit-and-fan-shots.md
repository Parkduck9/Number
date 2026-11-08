검증: CONFIRMED / 중복: BUG-COMBAT-C01-fan-shots-unparryable.md

# BUG-COMBAT-A04: 패링 성공 시 즉시 `State.Locomotion`으로 전이되어 동시에 날아온 다중 공격(부채꼴 탄·다수 적)의 후속 타격에 무방비로 피격됨

## 심각도
**높음**

## 위반 규칙
- **DECISIONS.md 7-A `[확정]`**: "안드로이드 특수 공격: 황금 탄 3발 부채꼴 — 연타 사이에 가끔 0.5초 예고 후 황금 탄 3발. 패링 가능."
- **PROTOTYPE_ARCH.md 2절 / 3절**:
  - `Parry`: "0~0.20초(T) = 성공 창(`ParryActive`)"
  - "패링 성공 = 적 타격 순간에 플레이어가 `ParryActive`(패링 0~0.20초)이고 앞쪽 180°에서 온 공격. → 플레이어 피해 없음, 즉시 Locomotion."

## 코드 근거
- `Assets/Scripts/Player/PlayerController.cs:78, 262-274`:
  ```csharp
  public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveTime;
  ...
  if (ParryActive && fromFront && !info.Unparryable)
  {
      // No knockback for either side. Hit-stop + flash + parry FX.
      EnterState(State.Locomotion);
  ```
- `Assets/Scripts/Enemy/TestEnemy.cs:261-273`:
  안드로이드의 부채꼴 특수 공격(`FireFan`)은 같은 발사 위치에서 -15°, 0°, +15° 각도로 황금 탄 3발(`EnemyProjectile`)을 동시에 발사한다.
- 근접/중거리에서 플레이어가 우클릭(패링)을 눌러 부채꼴 탄을 막으면, 3발 중 플레이어 콜라이더(0.9×2.0)에 첫 번째로 닿은 탄(예: 0° 중앙 탄, $\cos 0^\circ = 1$로 가장 먼저 도착)이 `ReceiveEnemyHit`를 호출한다.
- 이때 첫 번째 탄이 패링되면서 `EnterState(State.Locomotion)`이 호출되어 **`ParryActive`가 즉시 `false`가 된다.**
- 바로 같은 프레임(두 탄이 동시에 겹친 경우) 또는 1~2프레임 뒤($\cos 15^\circ \approx 0.966$ 속도 차이로 수 밀리초 뒤 도착)에 두 번째·세 번째 황금 탄(혹은 동시에 공격한 다른 적의 타격)이 플레이어 콜라이더에 닿으면, 플레이어는 이미 `State.Locomotion`이고 패링 무적(grace) 시간도 전혀 없으므로(우클릭을 다시 누를 수도 없는 1프레임 차이) **패링에 성공했음에도 직후 탄에 무조건 피격(`HitResult.Hit`)된다.**

## 재현 단계
1. 안드로이드 적(`Kind.Android`, STG-04 또는 STG-05, `Test_Enemies`)과 전투하며 근~중거리(약 2~4u)에서 안드로이드가 황금 탄 3발 부채꼴 공격(`State.Special`)을 발사하게 한다(또는 시민 적 2명이 동시에 공격하게 한다).
2. 탄이 플레이어에게 닿는 순간 우클릭으로 패링한다.

## 기대 결과
한 번의 패링 입력(성공 창 0.20초)으로 동시에(또는 수 밀리초 차이로) 도달하는 부채꼴 황금 탄이나 다수 적의 동시 타격을 패링했을 때, 첫 탄만 막히고 같은 순간 도착한 나머지 탄에 억울하게 피격되지 않아야 한다.

## 실제 결과
첫 번째 탄이 패링되는 순간 `EnterState(State.Locomotion)`으로 인해 `ParryActive`가 즉시 꺼지고 어떠한 패링 직후 무적(parry grace window)도 부여되지 않아, 같은 프레임이나 직후 프레임에 도달하는 부채꼴의 나머지 탄(또는 겹친 두 번째 적의 공격)에 플레이어가 그대로 피격당한다.
