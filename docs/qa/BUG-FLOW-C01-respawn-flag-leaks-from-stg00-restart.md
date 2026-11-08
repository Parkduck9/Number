검증: CONFIRMED (RespawnManager.cs:31-35,45 + Stg00Builder.cs:64 확인; 이전 판에 STG01 기록이 있을 때만 발생)

# BUG-FLOW-C01 — STG-00에서 "체크포인트에서 재시작"을 누르면 리스폰 플래그가 남아, 나중에 STG-01이 엉뚱한 위치에서 시작된다

## 재현 단계
1. 타이틀 → 시작 → STG-00 → STG-01까지 진행한다.
2. STG-01에서 대문을 지나 SEG-B에 도착한다 (도착 지점의 CP-B0에 닿음).
3. 일시정지(Esc/P) → "To title"로 타이틀에 돌아간다.
4. 타이틀 → 시작으로 새 게임을 한다. STG-00이 시작된다.
5. STG-00에서 일시정지 → "Restart from checkpoint"를 누른다 (STG-00이 다시 로드됨).
6. 오프닝을 끝내고 출구로 나가 STG-01에 들어간다.

## 기대 결과
새 게임이므로 STG-01은 SEG-A 시작 지점(P-START)에서 시작한다.

## 실제 결과
STG-01이 지난 판에서 마지막으로 닿은 체크포인트(예: SEG-B의 CP-B0)에서 시작한다. SEG-A(개인 숫자 학습 구간)를 통째로 건너뛴다.

## 원인 (코드 근거)
- `Assets/Scripts/Checkpoint/RespawnManager.cs:31-35` `RestartFromCheckpoint()`는 static `respawnPending = true`로 둔 뒤 씬을 다시 로드한다.
- 이 플래그를 소비하고 지우는 곳은 `RespawnManager.Start()` 하나뿐이다 (`RespawnManager.cs:45-46`).
- STG-00에는 RespawnManager가 없다: `Assets/Editor/Stg00Builder.cs:64` "no HUD, no RespawnManager, no checkpoints". 그런데 일시정지 메뉴는 플레이어만 있으면 열린다 (`Assets/Scripts/UI/PauseMenu.cs:63`), 버튼도 그대로 있다 (`PauseMenu.cs:115`, `121-125`).
- 그래서 STG-00에서 누른 재시작의 `respawnPending = true`가 지워지지 않고 다음 씬까지 남는다.
- STG-01의 `RespawnManager.Start()`에서 `respawnPending && hasRecord && recordScene == "STG01"` (`RespawnManager.cs:45`)이 참이 되면 기록 위치로 순간이동한다 (`RespawnManager.cs:53-57`). `hasRecord`/`recordScene`/`recordPos`는 타이틀로 돌아가도 지워지지 않는 static이다 (`RespawnManager.cs:11-14`; 초기화는 앱 시작 때 `ResetStatics` 한 번뿐, `RespawnManager.cs:20-21`).
- 지난 판의 마지막 기록 씬이 STG01이어야 증상이 보인다 (위 2~3단계). 기록 씬이 다르면 플래그만 조용히 남았다가 STG-01 Start에서 지워진다.

## 수정 제안 (참고)
- `RestartFromCheckpoint()`가 RespawnManager가 없는 씬에서는 `respawnPending`을 세우지 않게 하거나, 새 게임 시작(`TitleScreen.Start`) 때 RespawnManager 기록(`hasRecord`, `respawnPending`)을 지우는 static 메서드를 부른다.
- 또는 STG-00에서는 일시정지 메뉴의 "Restart from checkpoint"를 숨긴다.

## 심각도
중간 — 소프트락은 아니지만, 새 게임에서 스테이지 절반을 건너뛰고 진행 순서가 깨진다. 재현 경로가 특정하다 (STG-00에서 재시작 버튼을 눌러야 함).
