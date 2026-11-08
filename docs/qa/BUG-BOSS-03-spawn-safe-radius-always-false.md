# BUG-BOSS-03: 보스전 소환 금지 반경(`spawnSafeRadius`)이 천장 소환점(`y=14`)과의 2D 거리를 계산해 항상 비활성화됨

## 심각도
**중간**

## 재현 단계
1. `STG05` 보스전을 시작한 뒤 왼쪽 소환 지점 바로 아래 바닥(`x = 2, y = 3`) 또는 오른쪽 소환 지점 바로 아래 바닥(`x = 28, y = 3`)에 서 있는다.
2. 웨이브의 적 소환(`BossFight.SpawnNext()`)이 진행될 때 플레이어 머리 위 소환 지점이 건너뛰어지는지 확인한다.

## 기대 결과
- `BossFight.spawnSafeRadius = 4f`(`docs/design/impl/boss.md` 3절: "플레이어 근처 소환 금지 반경 4u") 설정에 따라, 플레이어가 소환 지점으로부터 가로 4u 이내(`x = 2` 또는 `x = 28` 근처)에 서 있으면 반대쪽 소환 지점으로 대체되어 플레이어 머리 바로 위로 적이 떨어지지 않아야 한다.

## 실제 결과
- 소환 지점 `Spawn-L (2, 14)`과 `Spawn-R (28, 14)`은 천장 높이(`y = 14`)에 배치되어 있고, 플레이어는 바닥(`y = 3`, 최대 점프 시에도 `y ≈ 5.5`)에 위치한다.
- `BossFight.SpawnNext()`는 가로 거리(`Mathf.Abs(player.x - point.x)`)가 아니라 2D 유클리드 거리 `Vector2.Distance(player.transform.position, point.position) < spawnSafeRadius`를 비교한다.
- 플레이어와 소환 지점 사이의 수직 거리만 최소 `8.5u ~ 11.0u`이므로 `Vector2.Distance(...)`는 항상 `8.5f` 이상이며, `spawnSafeRadius (4f)`보다 작아지는 경우가 **절대 발생하지 않는다(항상 `false`)**.
- 따라서 플레이어가 `x = 2`나 `x = 28`에 서 있어도 바로 머리 위에서 적이 그대로 소환되어 떨어진다.

## 코드 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:28` — `[SerializeField] float spawnSafeRadius = 4f; // (T) never spawn this close to the player`
- `Assets/Scripts/Boss/BossFight.cs:65` — 전투 시작 거리 판정은 `Mathf.Abs(player.transform.position.x - board.transform.position.x) <= startDistance`로 x축 거리만 비교함
- `Assets/Scripts/Boss/BossFight.cs:114-116` — `Vector2.Distance(player.transform.position, point.position) < spawnSafeRadius`로 y축 차이가 포함된 2D 거리를 비교함
- `Assets/Editor/Stg05Builder.cs:62, 91-92` / `Assets/Scenes/STG05.unity:1025, 1560, 2025` — 플레이어 시작 위치 `(4, 3, 0)`, 소환 지점 `Spawn-L (2, 14, 0)`, `Spawn-R (28, 14, 0)`
- `docs/design/impl/boss.md:34` — "플레이어 근처 소환 금지 반경 | 4u | BossFight.cs:28"
