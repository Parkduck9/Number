검증: PLAUSIBLE / 중복: BUG-ENEMY-C01 (등 뒤 투사체 패링 판정 문제의 다른 표현. 정면 패링이 뚫리는 시나리오 2는 미검증)

# BUG-ENEMY-A16: 투사체 패링 전후방 판정이 발사 방향이 아닌 충돌 프레임의 투사체 중심 좌표(pos.x)를 사용하여 등 뒤 투사체가 패링되거나 정면 패링이 뚫리는 문제

## 재현 단계
1. **등 뒤 패링 재현:** 원거리 적 또는 안드로이드(`FireFan`)가 플레이어 등 뒤 근접 거리(`0.8u` 이내)에서 플레이어 등을 향해 탄을 발사할 때, 플레이어가 적 반대쪽(앞쪽)을 바라본 채 우클릭(패링)을 누른다.
2. **정면 패링 실패 재현:** 날아오는 투사체(`속도 8~9 u/s`)를 향해 플레이어가 마주 보고 이동(`8 u/s`)하거나 대시 직후 우클릭(패링)을 눌러, 프레임 이동 후 투사체 중심(`pos.x`)이 플레이어 중심(`transform.position.x`)을 살짝 넘어선 상태(`0 < |pos.x - player.x| <= 0.70u`, 여전히 콜라이더 겹침 범위 안)에서 충돌 판정이 일어나게 한다.

## 기대 결과
- `docs/design/GDD.md` 18장 및 `docs/design/PROTOTYPE_ARCH.md:83, 198` (`Q-I` `[확정]`)에 따라, 플레이어가 바라보는 앞쪽 180° 방향에서 날아오는 투사체만 패링되어야 하며, 등 뒤에서 날아오는 투사체는 패링 창(`ParryActive`) 중이어도 반드시 피격(Hurt)되어야 한다.

## 실제 결과
- `TestEnemy.DoStrike()`(`Assets/Scripts/Enemy/TestEnemy.cs:290, 299`)와 `FireFan()`(`265, 271줄`)은 발사 시 `hit.SourcePosition = transform.position`으로 적의 위치를 전달하지만, `EnemyProjectile.Update()`(`Assets/Scripts/Combat/EnemyProjectile.cs:56`)가 충돌 직전 `info.SourcePosition = pos;`로 덮어써서 이번 프레임 `velocity * Time.deltaTime`만큼 이동한 뒤의 투사체 중심 좌표를 기준으로 전후방을 판정한다(`Assets/Scripts/Player/PlayerController.cs:260-261`: `float dx = info.SourcePosition.x - transform.position.x; bool fromFront = Mathf.Approximately(dx, 0f) || Mathf.Sign(dx) == Facing;`).
- 적의 총구 오프셋은 `muzzleOffset.x = 0.8f`(`TestEnemy.cs:43`)이고 플레이어 콜라이더(반폭 `0.45u`)와 투사체(반폭 `0.25u`)의 합산 충돌 반경은 `±0.70u`이므로:
  1. 적이 플레이어 등 뒤 `0.8u` 안에서 쏘면 탄의 초기 위치 `pos.x`가 플레이어 중심 앞쪽에 생성되어, 플레이어가 등을 보인 채 앞을 향해 패링해도 `fromFront`가 `true`가 되어 **등 뒤에서 쏜 투사체가 패링된다**(패링하지 않고 맞을 때도 `PlayerController.cs:278`에서 넉백 방향 `away`가 반대로 계산되어 적 쪽으로 끌려간다).
  2. 반대로 정면에서 날아오는 투사체와 마주 보며 충돌할 때 한 프레임 이동으로 `pos.x`가 플레이어 중심을 조금이라도 지나치면(합산 반경 `0.70u` 이내), 정면 투사체임에도 `Mathf.Sign(dx) != Facing`이 되어 **정면 패링이 실패하고 피해를 입는다.**

## 심각도
높음
