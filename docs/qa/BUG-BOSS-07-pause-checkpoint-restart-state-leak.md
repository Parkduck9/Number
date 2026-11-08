# BUG-BOSS-07: 타이틀 복귀 시 `RespawnManager` 정적 상태 미초기화 및 `STG00` 일시정지 재시작 시 `respawnPending` 누수

## 심각도
**낮음**

## 재현 단계
1. 게임을 플레이하여 체크포인트가 있는 스테이지(예: `STG01`)의 중간 체크포인트에 도달한 뒤, 일시정지 메뉴(`PauseMenu`)의 `"To title"`로 타이틀 화면(`Title`)에 돌아오거나 `STG05` 엔딩을 보고 타이틀 화면으로 돌아온다.
2. 타이틀 화면에서 `"Start"`를 눌러 새 게임(`STG00` 오프닝)을 시작한다.
3. `STG00`에서 `Esc`(또는 `P`)로 일시정지 메뉴를 열고 `"Restart from checkpoint"`를 클릭한다.
4. `STG00`이 다시 로드된 뒤 오른쪽 출구로 나가 `STG01`에 진입했을 때 플레이어의 시작 위치를 확인한다.

## 기대 결과
- 타이틀 화면에서 `"Start"`로 새 게임을 시작하면 이전 회차의 체크포인트 기록이 초기화되어야 하며, `RespawnManager`가 없는 `STG00`에서 `"Restart from checkpoint"`를 누르더라도 `respawnPending` 플래그가 다음 스테이지(`STG01`)까지 누수되지 않아야 한다.

## 실제 결과
1. `TitleScreen.Start()`는 새 게임을 위해 `Shockwave.SetUnlocked(false)`는 초기화하지만, `RespawnManager`의 정적 체크포인트 기록(`hasRecord`, `recordPos`, `recordScene`, `respawnPending`)은 초기화하지 않는다.
2. `STG00`(`Stg00Builder.cs:64`)에는 `PlayerController`는 존재하여 `PauseMenu`를 열 수 있지만, 씬에 `RespawnManager` 컴포넌트가 존재하지 않는다.
3. 따라서 `STG00`에서 일시정지 메뉴의 `"Restart from checkpoint"`(`RespawnManager.RestartFromCheckpoint()`)를 실행하면 `respawnPending = true`로 설정된 뒤 `STG00`이 재로드되지만, `STG00`에는 `RespawnManager.Start()`를 실행할 인스턴스가 없어 **`respawnPending`이 `true`인 채로 남아 다음 씬까지 넘어간다.**
4. 이 상태에서 `STG01`로 진입하면 `RespawnManager.Start()`에서 `respawnPending && hasRecord && recordScene == "STG01"` 조건이 참이 되어, 새 게임인데도 이전 회차에 저장된 `STG01` 중간 체크포인트 위치로 플레이어가 순간이동한다.

## 코드 근거 (file:line)
- `Assets/Scripts/UI/TitleScreen.cs:10-15` — `Shockwave.SetUnlocked(false)`만 호출하고 `RespawnManager` 정적 상태를 초기화하지 않음
- `Assets/Scripts/UI/PauseMenu.cs:115, 121-125` — `RespawnManager` 유무와 무관하게 `RespawnManager.RestartFromCheckpoint()` 호출
- `Assets/Scripts/Checkpoint/RespawnManager.cs:11-14, 30-51` — `RestartFromCheckpoint()`에서 `respawnPending = true` 설정 후 `RespawnManager.Start()`가 호출될 때만 `respawnPending = false`로 해제됨
- `Assets/Editor/Stg00Builder.cs:41, 64` — `Player`는 배치하지만 `RespawnManager`는 배치하지 않음(`// no HUD, no RespawnManager, no checkpoints`)
