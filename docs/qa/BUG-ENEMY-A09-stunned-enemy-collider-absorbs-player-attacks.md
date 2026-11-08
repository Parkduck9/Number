검증: PLAUSIBLE / 중복: BUG-ENEMY-C05-stunned-body-absorbs-player-attack.md (심각도는 중간~높음 사이, 프리팹 콜라이더 미확인)

# BUG-ENEMY-A09 — 3타 피격 후 기절(Stunned)한 적의 콜라이더가 Enemy 레이어에 남아 다른 적에 대한 공격 판정을 가로챔

## 재현 단계
1. 적 2명 이상이 함께 등장하는 구간(STG-01 SEG-B, STG-02, STG-04, STG-05 보스전)에서 적 1명을 반격 창에 3회 타격하여 기절(`State.Stunned`)시킨다.
2. 기절하여 바닥에 누운 적의 위치로 다른 살아 있는 적을 유인해 겹치게 만든다.
3. 살아 있는 적의 반격 창(`State.Open` / `State.Turn` / `State.Frozen`)에 좌클릭으로 일반 공격을 시도한다.

## 기대 결과
- 기획서(`docs/design/GDD.md:84, 369` `[확정]`)에 따라, 이미 기절(`State.Stunned`)한 적은 전투에서 이탈한 상태이므로 플레이어의 공격 판정을 가로채지 않아야 하며, 같은 위치에 서 있는 살아 있는 적에게 정상적으로 피해가 들어가야 한다.

## 실제 결과
- `TestEnemy`가 `hp <= 0`이 되어 `EnterState(State.Stunned)`에 진입할 때, 숫자만 숨기고 스프라이트만 회전시킬 뿐 루트 오브젝트의 `BoxCollider2D`(`0.9 × 2.0`)와 `EnemyLayer`(`layer = 8`)를 그대로 유지한다.
- 반면 `PlayerController.DoAttackHit()`은 `OverlapBoxAll`이 아닌 단일 `Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask)`를 호출하여 콜라이더 딱 1개만 검사한다.
- 물리 엔진이 기절한 적의 콜라이더를 먼저 반환하면 `enemy.ReceivePlayerHit(...)`은 `if (state == State.Stunned) return false;`로 즉시 종료되어, 같은 공격 범위 내에 있는 살아 있는 적이 아무런 피해를 입지 않는다.

## 심각도
높음

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:372-376` — `EnterState(State.Stunned)`에서 콜라이더 비활성화나 레이어 변경 없이 `Enemy` 레이어의 `BoxCollider2D`를 그대로 유지함.
- `Assets/Scripts/Enemy/TestEnemy.cs:388-390` — `ReceivePlayerHit`에서 `if (state == State.Stunned) return false;` 반환.
- `Assets/Scripts/Player/PlayerController.cs:237-242` — `DoAttackHit()`에서 `Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask)`로 단일 콜라이더만 가져와 `ReceivePlayerHit`을 1회 호출하고 종료함(반면 `Assets/Scripts/Player/Shockwave.cs:70`은 `OverlapBoxAll`을 사용함).
