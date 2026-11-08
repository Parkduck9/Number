검증: PLAUSIBLE (EnemyProjectile.cs:56 SourcePosition=투사체 위치로 앞/뒤 판정. 적이 플레이어 중심 0.8u 이내일 때만 해당; 실제 적 접근 거리에서 발생하는지는 플레이 확인 필요. 관련: BUG-COMBAT-C05)

# BUG-COMBAT-A11 — 근접 거리(0.8u 이내) 발사 또는 중심 통과 프레임에서 투사체 정면 패링이 실패하고 적 쪽으로 넉백됨

## 근거 파일 및 위치
- `Assets/Scripts/Combat/EnemyProjectile.cs:43, 52-57`
- `Assets/Scripts/Player/PlayerController.cs:260-262, 278-280`
- `Assets/Scripts/Enemy/TestEnemy.cs:43, 261-273, 298-299`

## 재현 단계
1. 안드로이드 적(`kind = Android`, 근접 사거리 `attackRangeX = 1.8f`) 또는 구석에 몰린 원거리 적과 수평 거리 `0.8u` 이내(예: `0.3u ~ 0.7u`)에 마주 서서 적을 바라본다(`Facing = 1`, 적은 왼쪽 `facing = -1`).
2. 안드로이드가 연타 사이 특수 공격(`State.Special`, 황금 탄 3발 부채꼴, `TestEnemy.cs:261-273`)을 발사하거나 원거리 적이 탄을 쏠 때 우클릭으로 정면 패링(`ParryActive == true`)을 유지한다.
3. 또는 날아오는 투사체를 향해 이동/대시하다가 투사체 중심점이 플레이어 중심(`transform.position.x`)을 막 지나친 프레임(여전히 플레이어 콜라이더 반폭 `0.45u` + 투사체 반폭 `0.25u` = `0.70u` 겹침 범위 안)에 패링 판정이 일어나게 한다.

## 기대 결과
- 앞에서 날아오는 투사체(`velocity.x`와 플레이어의 `Facing`이 반대 방향이거나 발사원 적이 플레이어 앞쪽에 있는 경우)는 정면 패링(`ParryActive`)으로 정상 패링되어 사라져야 한다 ([DECISIONS.md](../design/DECISIONS.md) 7-A `[확정]`: "황금 탄 3발. 패링 가능").
- 패링하지 못해 피격되더라도 적/투사체가 날아온 반대 방향(`away`)으로 밀려나야 한다.

## 실제 결과
1. `TestEnemy`의 투사체 생성 위치(`muzzle`)는 `transform.position.x + muzzleOffset.x * facing`(`muzzleOffset.x = 0.8f`, `TestEnemy.cs:43, 264, 298`)이고, `EnemyProjectile.Update()`(`EnemyProjectile.cs:43, 56`)는 먼저 `transform.position += velocity * Time.deltaTime`으로 이동한 뒤 **`info.SourcePosition = pos`(투사체의 현재 위치)**로 덮어써서 `pc.ReceiveEnemyHit(info)`를 호출한다.
2. 적과 플레이어의 수평 거리가 `0.8u`보다 가까우면(안드로이드 근접 교전 거리 `1.8u` 안에서는 흔히 발생), 투사체가 생성·이동한 첫 프레임의 `pos.x`가 **이미 플레이어의 중심(`transform.position.x`) 뒤쪽**에 놓인 채 플레이어 콜라이더(`±0.70u` 범위)와 충돌한다.
3. `PlayerController.ReceiveEnemyHit()`(`PlayerController.cs:260-262`)는 투사체의 비행 방향(`velocity.x`)이나 발사한 적의 위치가 아니라 `dx = info.SourcePosition.x - transform.position.x`의 부호로 정면 여부(`Mathf.Sign(dx) == Facing`)를 판정하므로, 플레이어가 적을 똑바로 마주 보고 패링해도 **등 뒤 공격(`fromFront == false`)으로 판정되어 패링이 실패하고 피해를 입는다** (반대로 적에게 등을 돌리고 패링하면 패링에 성공한다).
4. 이어지는 피격 넉백 방향(`float away = dx > 0f ? -1f : 1f;`, `PlayerController.cs:278`)도 반대로 계산되어 플레이어가 **투사체를 쏜 적 쪽으로 끌려가듯 넉백**된다.

## 심각도
높음
