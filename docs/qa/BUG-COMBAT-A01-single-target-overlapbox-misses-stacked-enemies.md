검증: CONFIRMED / 중복: BUG-COMBAT-C02-attack-hits-only-one-collider.md (PlayerController.cs:237)

# BUG-COMBAT-A01: 근접 공격이 범위 내 다수의 적 중 1명에게만 적용되어 겹친 무적 적 뒤에 기절 가능 적이 숨겨짐

## 심각도
**높음**

## 위반 규칙
- **GDD 4장 `[확정]`**: 세뇌된 시민은 "근접 공격, 떼로 몰려옴".
- **GDD 12장 / PROTOTYPE_ARCH 3절**: `Player ↔ Enemy` 물리 충돌 비활성화로 적들이 서로 및 플레이어와 겹칠 수 있으며, 다수 적 배치 스테이지(STG-01 `E-B1`/`E-B2`, STG-02 `E-A2`/`E-A3`, STG-04, STG-05 보스 웨이브)에서 히트박스 내의 적들을 정상 타격할 수 있어야 함. (PROTOTYPE_ARCH 3절 메모: "나중에 떼가 나오면 `OverlapBox(…, ContactFilter2D, List<Collider2D>)`로 바꾼다.")

## 코드 근거
- `Assets/Scripts/Player/PlayerController.cs:233-243`:
  ```csharp
  void DoAttackHit()
  {
      attackHitDone = true;
      ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
      Collider2D col = Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask);
      if (col == null) return;
      TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
      if (enemy == null) return;
      bool damaged = enemy.ReceivePlayerHit(new HitInfo { Damage = attackDamage, SourcePosition = transform.position, IsSpecial = false });
      if (damaged) PlayHitSpark(col.ClosestPoint(AttackCenter()), Facing);
  }
  ```
- 반면 `Shockwave.cs:70`에서는 `Physics2D.OverlapBoxAll`을 사용하여 범위 내 모든 적을 정상적으로 처리하고 있음.
- 또한 기절(`Stunned`) 상태의 적도 콜라이더가 그대로 유지되므로(`PROTOTYPE_ARCH.md:132`, `TestEnemy.cs:372-376`), `Physics2D.OverlapBox`가 이미 기절한 적(`Stunned`)이나 정면 공격을 튕겨내는 체인 진행 중인 적(`strikesLeft > 0`)의 콜라이더를 먼저 반환하면 같은 공격 범위 안에 있는 다른 적(`Open`/`Staggered`/`Turn`/`Frozen` 상태)은 아예 타격 판정을 받지 못함.

## 재현 단계
1. STG-01 SEG-B(`E-B1`, `E-B2`), STG-02, STG-04 또는 STG-05 보스전과 같이 적 2명 이상이 동시에 등장하는 구간으로 이동한다.
2. 적 1명을 먼저 3회 타격하여 기절(`Stunned`)시키거나, 한 적만 공격 체인이 끝나 반격 창(`Open`)이 열리고 다른 적은 체인 진행 중(또는 이미 `Stunned`)인 상태에서 두 적이 같은 공격 범위 내에 겹치게 유도한다.
3. 플레이어가 좌클릭으로 기본 공격(`DoAttackHit`)을 실행한다.

## 기대 결과
공격 히트박스(`attackSize` 1.6×1.4) 내에 있는 타격 가능한 적이 정상적으로 피해를 입어야 하며, 이미 기절한 적(`Stunned`)이나 겹쳐 있는 다른 적의 콜라이더 순서 때문에 반격 창(`Open`/`Staggered`)이 열린 적에 대한 공격이 씹히지 않아야 한다.

## 실제 결과
`Physics2D.OverlapBox`는 조건에 맞는 콜라이더를 **단 1개만** 임의(물리 내부 순서)로 반환한다. 반환된 1개의 콜라이더가 이미 기절한 적(`Stunned`)이거나 정면 무적 상태의 적이면 `ReceivePlayerHit`가 `false`를 반환하고 끝나버려, 같은 히트박스 안에 있는 `Open`/`Staggered` 상태의 적이 아무런 피해도 입지 않는다.
