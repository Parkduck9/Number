검증: CONFIRMED (PlayerController.cs:237 단일 OverlapBox)
중복: BUG-COMBAT-A01-single-target-overlapbox-misses-stacked-enemies.md

# BUG-COMBAT-C02 — 플레이어 공격이 콜라이더 하나만 검사해서, 기절한 적이 뒤의 적을 가린다

**영역:** COMBAT (플레이어 공격 판정)
**관련 규칙:** GDD 5장 `[확정]` 세뇌된 시민 "떼로 몰려옴", PROTOTYPE_ARCH 3절 "1적 프로토타입엔 충분. (나중에 떼가 나오면 `OverlapBox(…, ContactFilter2D, List<Collider2D>)`로 바꾼다.)", 4절 Stunned "콜라이더 유지, 영구"

## 재현 단계
1. STG-01처럼 시민이 여러 명 나오는 구간에서 시민 A를 3타로 기절시킨다 (회색, 눕힘).
2. 시민 B가 A의 시체 위(또는 같은 공격 범위 안)로 다가와 연타를 끝내고 반격 타이밍(Open)이 된다.
3. B를 향해 좌클릭 공격한다.

## 기대 결과
반격 타이밍인 B가 맞는다 (빨강 플래시, HP -1).

## 실제 결과 (코드 근거)
- `PlayerController.DoAttackHit`는 `Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask)` 로 **콜라이더 하나만** 받는다 (`Assets/Scripts/Player/PlayerController.cs:237`). 여러 개가 겹치면 어느 것이 돌아올지 정해져 있지 않다.
- 기절한 적은 Enemy 레이어와 콜라이더를 그대로 유지한다 (`Assets/Scripts/Enemy/TestEnemy.cs:372-376`, 레이어 변경 없음).
- 돌아온 콜라이더가 기절한 A면 `ReceivePlayerHit`이 false (`TestEnemy.cs:390`) → B는 맞지 않는다. 연타 중인 다른 적 C가 돌아오면 튕김(흰 플래시)만 나고 역시 B는 안 맞는다.
- 반격 창(Open)은 1초뿐이라 공격 한 번이 사라지는 것이 체감된다. 겹친 적이 많을수록 무작위로 "공격이 안 들어가는" 현상이 생긴다.

## 심각도
중간 — 떼 전투(STG-01~)에서 반격이 무작위로 무시된다. 진행 불가는 아님.

## 수정 제안 (참고)
`Shockwave.Fire`처럼 `OverlapBoxAll`(또는 미리 할당한 배열 + ContactFilter2D, GC 없음)로 모두 받아, Stunned가 아닌 적 중 하나(가장 가까운 적) 또는 전부에게 `ReceivePlayerHit`을 보낸다. 한 명만 때릴지 전부 때릴지는 설계 결정 필요.

## 확신도
높음 (코드상 명확). 체감 빈도는 적 배치에 따라 다름.
