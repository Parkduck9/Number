검증: PLAUSIBLE (패링 성공 시 EnterState(Locomotion)으로 ParryActive 종료는 확인. 3발이 한 프레임 안에 겹치는지는 거리·속도에 따라 플레이 확인 필요)

# BUG-ENEMY-C06 — 안드로이드 부채꼴 3발을 패링하면 1발만 막히고 나머지가 그대로 맞음

## 재현 단계
1. `NUMBER/Build Enemy Variants Test Scene`의 Strong(안드로이드) 또는 STG-05 보스 3웨이브 안드로이드 앞에 선다. 거리는 적에게서 약 1 ~ 4u (연타 사거리 근처 — 특수 공격은 연타 사이에 나오므로 보통 이 거리다). C01과 겹치지 않게 0.8u보다는 멀리 선다.
2. 적이 황금색이 되면(Special) 0.5초 뒤 날아오는 탄에 맞춰 정면 패링을 한다.

## 기대 결과
- DECISIONS 7-A / `docs/design/impl/enemy-rules.md` 40줄: 부채꼴 탄은 "패링할 수 있다". 타이밍 맞춘 패링 한 번으로 한 번의 공격(부채꼴)을 막아 피해가 없어야 한다.

## 실제 결과
- 첫 탄(가운데, 가장 빠름)만 패링되고 같은 프레임이나 바로 다음 프레임에 도착한 옆 탄이 맞아 체력이 1 준다. 이 거리에서는 패링을 완벽히 해도 피해를 피할 수 없다. (탄이 갈라지는 먼 거리, 약 6.5u 밖에서만 1발만 닿아 막힌다.)

## 원인 (코드 근거)
- 세 발이 같은 총구에서 동시에 나간다: `Assets/Scripts/Enemy/TestEnemy.cs:262-273` (`AndroidFanSpreadDeg` 15°, 속도 8 — `Assets/Scripts/Broadcast/BroadcastRules.cs:18-20`).
- 계산: 총구 높이 +0.3, 탄 판정 0.5 x 0.5(`Assets/Scripts/Combat/EnemyProjectile.cs:10`), 플레이어 0.9 x 2 → 위쪽 탄은 총구에서 약 3.5u, 아래쪽 탄은 약 5.8u 안이면 플레이어에 닿는다. 가운데 탄과 옆 탄의 x 속도 차이는 8 vs 7.73이라 3u 거리에서 도착 시간 차이가 약 0.013초(1프레임 미만)다.
- 패링 성공 즉시 패링 상태가 끝난다: `Assets/Scripts/Player/PlayerController.cs:262-265` (`EnterState(State.Locomotion)`), `ParryActive`는 `state == State.Parry`일 때만 참(78줄).
- 다음 탄의 판정은 그대로 피격이 된다: `PlayerController.cs:276` `health.TakeDamage`. 패링 히트스톱(`Time.timeScale = 0`, 271줄) 중에도 탄은 움직이지 않을 뿐 매 프레임 겹침 검사를 한다 (`EnemyProjectile.cs:52-57`).

## 수정 제안
- 한 번의 부채꼴을 한 묶음으로 처리한다. 예: `EnemyProjectile`에 묶음 번호(int volley)를 두고, 한 발이 `Parried`되면 같은 묶음의 나머지 탄을 모두 없앤다 (`FireFan`이 같은 번호를 넘김).
- 또는 (플레이어 쪽, 영역 밖) 패링 성공 직후 짧은 시간(예: 0.1초) 동안 들어온 앞쪽 공격도 `Parried`로 처리한다.

## 심각도
중간 — 확정 규칙(7-A "패링 가능")이 실제 전투 거리에서 지켜지지 않는다. 안드로이드가 나오는 STG-04·보스 3웨이브에서 매번 일어난다.
