검증: CONFIRMED (A는 C06과 중복: BUG-BOSS-C06-spawn-safe-radius-never-triggers.md. B 정적 초기화 누락은 에디터 Domain Reload 비활성 때만 해당해 WebGL 빌드와 무관, REJECTED)

# BUG-BOSS-A12: 씬 리로드 및 플레이 모드 재진입 시 정적(static) 상태 초기화 누락과 천장 스폰 거리 계산(2D) 오류

## 심각도
중간 (Medium)

## 재현 단계
1. **시나리오 A (보스방 천장 스폰 안전 반경 무효화):** `STG05` 보스전에서 왼쪽 벽 구석(`x = 2`, `y = 3`) 또는 오른쪽 벽 구석(`x = 28`, `y = 3`)에 서서 웨이브 적 소환(`State.Spawning`)을 기다린다.
2. **시나리오 B (정적 상태 초기화 누락 및 씬 재로드):**
   - Unity 에디터에서 Enter Play Mode Options(Domain Reload 비활성화)를 켜고, 대화 중이거나 정지 버튼 선택 창이 열린 상태(`GameFlow.InputLocked == true`)에서 Play 모드를 종료했다가 다시 시작한다.
   - 또는 `Stg05Builder.Build()`(`NUMBER/Build STG-05`)를 실행했을 때 `ProjectSettings/EditorBuildSettings.asset`에 `STG05.unity`가 비활성(`enabled: 0`) 상태로 들어 있거나 순서가 어긋나 있는 경우 빌드 목록 동기화 여부를 확인한다.

## 기대 결과
- **시나리오 A:** `BossFight`의 `spawnSafeRadius = 4f`(`"never spawn this close to the player"`) 설정에 따라, 플레이어가 왼쪽 스폰 지점(`x = 2`) 아래에 서 있으면 오른쪽(`x = 28`)에서, 오른쪽(`x = 28`) 아래에 서 있으면 왼쪽(`x = 2`)에서 적이 소환되어 플레이어 머리 바로 위로 적이 떨어지지 않아야 한다.
- **시나리오 B:** `docs/design/ARCHITECTURE_PATTERNS.md:31, 47` 규칙(`static 상태에는 반드시 [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics()를 둔다`)에 따라 모든 정적 필드가 플레이 시작 시 초기화되어야 하며, `Stg05Builder`도 `StageBuildKit.SyncBuildSettings()`와 일관되게 빌드 씬 활성화 및 순서를 보장해야 한다.

## 실제 결과
- **시나리오 A (`BossFight.SpawnNext` 안전 반경 데드 코드):** `Stg05Builder.cs:91-92`(`STG05.unity:1560, 2025`)에서 스폰 지점 `Spawn-L`과 `Spawn-R`의 위치는 천장 높이인 `(2, 14, 0)`과 `(28, 14, 0)`이고, 바닥에 서 있는 플레이어의 위치는 `y = 3`(최대 점프 시에도 `y ≈ 5.5`)이다. 그런데 `BossFight.SpawnNext()`(`Assets/Scripts/Boss/BossFight.cs:114-116`)는 시작 거리 판정(`65번째 줄`: `Mathf.Abs(player.transform.position.x - board.transform.position.x)`)과 달리 **2D 유클리드 거리 `Vector2.Distance(player.transform.position, point.position) < spawnSafeRadius`(`4f`)**를 계산한다. 높이 차이(`14 - 3 = 11u`, 점프 시에도 `8.5u`) 때문에 이 2D 거리는 최소 `8.5u ~ 11u` 이상이므로 **절대로 `4f` 미만이 될 수 없어 안전 반경 검사가 한 번도 작동하지 않고 플레이어 머리 바로 위에 적이 떨어진다.**
- **시나리오 B (정적 상태 초기화 누락):**
  - `GameFlow`(`Assets/Scripts/Core/GameFlow.cs:7-9`의 `InputLocked`, `EscConsumedFrame`), `DialoguePlayer.Instance`(`Assets/Scripts/Dialogue/DialoguePlayer.cs:11`), `BroadcastBoard.All`(`Assets/Scripts/Broadcast/BroadcastBoard.cs:18`), `LevelTransition.fadeImage`(`Assets/Scripts/Level/LevelTransition.cs:17`), `UiTheme.current`(`Assets/Scripts/UI/UiTheme.cs:24`)에 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 초기화 메서드가 없다.
  - 또한 `Stg05Builder.AppendBuildScene()`(`Assets/Editor/Stg05Builder.cs:260-266`)은 `if (list.Any(s => s.path == ScenePath)) return;`만 확인하므로, `STG05.unity`가 Build Settings에 비활성(`enabled = false`) 상태로 등록되어 있으면 활성화하지 않으며, 새로 추가할 때도 비활성 테스트 씬(`Prototype`, `SampleScene`) 뒤에 추가한다.

## 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:28, 114-116` / `Assets/Editor/Stg05Builder.cs:62, 91-92` — 천장 스폰 지점 `(2, 14)`, `(28, 14)`와 바닥 플레이어 `(x, 3)` 사이의 거리를 `Vector2.Distance`로 비교하여 `< 4f` 조건이 영구 불발됨
- `Assets/Scripts/Core/GameFlow.cs:7-9` — `SubsystemRegistration` 정적 초기화 누락
- `Assets/Scripts/Dialogue/DialoguePlayer.cs:11, 42-47` — `SubsystemRegistration` 정적 초기화 누락
- `Assets/Scripts/Broadcast/BroadcastBoard.cs:18` — `SubsystemRegistration` 정적 초기화 누락
- `Assets/Editor/Stg05Builder.cs:260-266` — `StageBuildKit.SyncBuildSettings()` 대신 단순 `AppendBuildScene()` 사용으로 `enabled` 상태 및 순서 미보장
