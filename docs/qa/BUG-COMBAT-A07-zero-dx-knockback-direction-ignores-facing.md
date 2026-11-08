검증: CONFIRMED (PlayerController.cs:278 dx==0이면 항상 +1; 확률 매우 낮음)

# BUG-COMBAT-A07: 적과 플레이어의 X좌표가 정확히 일치할 때 피격 넉백 방향과 패링 판정 방향이 바라보는 방향(`Facing`)과 무관하게 고정됨

## 심각도
**낮음**

## 위반 규칙
- **PROTOTYPE_ARCH.md 2절**:
  - `Hurt`: "HP-1, 넉백 (적 반대 방향 x 5, y 4)(T)"
  - `패링 방향 조건 (Q-I)`: "바라보는 방향 기준 앞쪽 반원 180°(위 90°·아래 90°)에서 온 공격만 패링된다. 판정: `Mathf.Sign(info.SourcePosition.x - transform.position.x) == facing` (같은 x면 성공 처리)."

## 코드 근거
- `Assets/Scripts/Player/PlayerController.cs:260-281`:
  ```csharp
  float dx = info.SourcePosition.x - transform.position.x;
  bool fromFront = Mathf.Approximately(dx, 0f) || Mathf.Sign(dx) == Facing;
  if (ParryActive && fromFront && !info.Unparryable)
  {
      ...
  }

  health.TakeDamage(info.Damage);
  EnterState(State.Hurt);
  float away = dx > 0f ? -1f : 1f;
  PlayHitSpark(transform.position, away); // blow travels away from the source
  rb.linearVelocity = new Vector2(away * knockback.x, knockback.y);
  return HitResult.Hit;
  ```
- `Player ↔ Enemy` 물리 충돌이 꺼져 있어(`Physics2D.IgnoreLayerCollision(7, 8, true)`), 대시 후 정지하거나 공중에서 착지할 때 플레이어와 적(또는 위/아래로 지나가는 투사체)의 `x` 좌표가 일치(`dx == 0f`)할 수 있다.
- 이때 `dx == 0f`이면 `float away = dx > 0f ? -1f : 1f;`가 항상 `+1f`(오른쪽)가 된다.
- 만약 플레이어가 오른쪽(`Facing == 1`)을 바라보고 있거나 적이 플레이어의 오른쪽에서 걸어와 겹친 상태라도 무조건 오른쪽(`+1f`)으로 넉백되어, 적 반대 방향이 아니라 적이 있는 쪽으로 밀려난다(적의 바라보는 방향이나 플레이어의 `-Facing`을 고려하지 않음).

## 재현 단계
1. 대시 또는 점프로 적과 정확히 같은 X좌표(`dx == 0`)에 겹쳐 선다(오른쪽을 바라본 상태, `Facing == 1`).
2. 적의 공격에 피격된다.

## 기대 결과
`dx == 0`으로 겹친 상태에서 피격될 때는 플레이어가 바라보는 반대 방향(`-Facing`) 또는 적의 공격 진행 방향으로 넉백되어야 일관성이 유지된다.

## 실제 결과
`dx == 0`일 때 `away = dx > 0f ? -1f : 1f`가 무조건 `+1f`를 반환하여, 플레이어의 `Facing` 방향과 무관하게 항상 오른쪽으로만 넉백된다.
