검증: CONFIRMED (EnemyProjectile.cs:56 이 발사체 현재 위치를 SourcePosition으로 쓰고, TestEnemy.cs:264 머즐이 0.8u 앞 → 근접 시 뒤에서 온 것으로 판정; PlayerController.cs:260-262 확인)

# BUG-ENEMY-C01 — 가까이 붙으면 정면에서 오는 황금 탄(부채꼴·원거리 탄)을 패링할 수 없음

## 재현 단계
1. `NUMBER/Build Enemy Variants Test Scene` 또는 STG-05 보스 3웨이브에서 안드로이드(Strong)에게 바짝 붙는다 (적과 0.8u 안쪽, 플레이어와 적은 서로 밀어내지 않으므로 겹쳐 설 수 있다 — `StageBuildKit.cs:148` `IgnoreLayerCollision(PlayerLayer, EnemyLayer, true)`).
2. 적 쪽을 바라본 채로 기다린다. 연타 사이에 적이 황금색으로 바뀌며(Special) 0.5초 뒤 부채꼴 3발을 쏜다.
3. 탄이 닿는 순간에 맞춰 정면 패링을 한다.
- 같은 현상은 근접 거리에서 원거리 적(Bug)의 일반 탄(`DoStrike` 원거리 분기)에도 일어난다.

## 기대 결과
- 플레이어가 적을 바라보고 있으므로 탄은 "정면"에서 온 것으로 판정되어 패링된다 (enemy-rules.md 6장 "정면 패링으로 탄이 사라진다", DECISIONS 7-A "패링 가능").

## 실제 결과
- 탄이 플레이어 **등 뒤**에서 생성되어 "뒤에서 맞음"으로 판정되고, 패링이 무시된 채 피해를 입는다.

## 원인 (코드 근거)
- 탄 생성 위치가 적 몸에서 앞쪽으로 0.8u 떨어진 총구다:
  - `Assets/Scripts/Enemy/TestEnemy.cs:264` `muzzle = position + (muzzleOffset.x * facing, ...)` (`muzzleOffset = (0.8, 0.3)`, 43줄), 271줄 `EnemyProjectile.Spawn(muzzle, ...)`
  - 원거리 일반 탄도 같다: `TestEnemy.cs:298-299`
- 탄은 앞/뒤 판정 기준을 **탄의 현재 위치**로 덮어쓴다: `Assets/Scripts/Combat/EnemyProjectile.cs:56` `info.SourcePosition = pos;`
- 플레이어 판정: `Assets/Scripts/Player/PlayerController.cs:260-262` `dx = info.SourcePosition.x - transform.position.x; fromFront = Sign(dx) == Facing`.
- 플레이어가 적과 총구 사이(적에서 0.8u 이내)에 있으면 총구 x가 이미 플레이어 뒤쪽이다. 적이 오른쪽을 보고(facing=+1) 플레이어가 적 오른쪽 0.4u에서 왼쪽(Facing=-1)을 보면, dx = (적x+0.8) - (적x+0.4) = +0.4 → Sign = +1 ≠ -1 → `fromFront = false` → 패링 무시, `TakeDamage`.

## 수정 제안
- `EnemyProjectile`이 탄 위치 대신 **진행 방향**으로 앞/뒤를 판정하게 한다. 예: `info.SourcePosition = pos - velocity.normalized * 큰값;` (탄이 날아온 쪽), 또는 Spawn 때 받은 발사자 위치를 그대로 두고 덮어쓰지 않는다.

## 심각도
중간 — 확정 규칙(탄은 패링 가능)이 근접 거리에서 깨진다. 플레이어와 적이 서로 통과하므로 근접 거리는 자주 생긴다.
