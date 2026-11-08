검증: PLAUSIBLE (B: BroadcastBoard.cs:56-62에 InputLocked 보정 없음은 사실, 영향은 낮음. A: PauseMenu.LateUpdate가 timeScale을 0으로 되돌리므로 REJECTED. 번호 A07이 두 파일에 겹침)

# BUG-BOSS-A07: 일시정지 및 입력 잠금(`GameFlow.InputLocked`) 중 패리 히트스톱 unscaled 타이머와 전광판 역해킹 타이머가 멈추지 않음

## 심각도
중간 (Medium)

## 재현 단계
1. **시나리오 A (패리 히트스톱 중 일시정지):** `STG05` 보스전에서 적의 공격이나 투사체를 패리하여 히트스톱(`hitStopTime = 0.06f`, `Time.timeScale = 0f`)이 걸린 직후 `Esc` 또는 `P`를 눌러 일시정지 메뉴(`PauseMenu`)를 연 뒤 0.06초 이상 기다린다.
2. **시나리오 B (역해킹 중 엔딩 선택창/대화 진입):** `STG05` 3웨이브에서 전광판을 해킹(유지 시간 4초, 0일 때 3초)하여 정지 버튼을 해금한 즉시 정지 버튼(`StopButton`)에서 `E`를 눌러 엔딩 선택 창(`GameFlow.InputLocked = true`)을 열고 3~4초 동안 기다린다.

## 기대 결과
- **시나리오 A:** 일시정지 메뉴가 열려 있는 동안에는 패리 히트스톱 타이머가 만료되어 `Time.timeScale = 1f`로 풀리거나 같은 프레임의 `Update()`가 진행되지 않아야 하며, 일시정지를 해제했을 때 남은 히트스톱이 정상 유지되어야 한다.
- **시나리오 B:** `docs/design/ARCHITECTURE_PATTERNS.md:48`의 타이머 규칙(`게임플레이 타이머는 Time.time(적·전광판·충격파 쿨다운·보스 웨이브). InputLocked 동안은 deltaTime만큼 밀어서 멈춘 것처럼 만든다`)에 따라, `GameFlow.InputLocked == true`인 동안에는 `BroadcastBoard`의 역해킹 타이머도 멈추어야 하며 잠금 중에 방송 숫자가 바뀌어 적 상태(`TestEnemy.OnBroadcastChanged`)가 변하지 않아야 한다.

## 실제 결과
- **시나리오 A:** `PlayerController.ReceiveEnemyHit()`(`Assets/Scripts/Player/PlayerController.cs:268-272`)는 `hitStopEndsAt = Time.unscaledTime + hitStopTime`으로 실제 시간(`unscaledTime`)을 기록한다. 일시정지 중에도 `Time.unscaledTime`은 흐르므로 0.06초 뒤 `PlayerController.Update()`(`PlayerController.cs:103-106`)에서 `GameFlow.InputLocked` 확인 없이 `EndHitStop()`(`PlayerController.cs:94-99`)이 호출되어 **`Time.timeScale = 1f`로 되돌려지고 그 프레임의 `PlayerController.Update()` 나머지 로직과 뒤이은 스크립트들의 `Update()`가 실행된다**(프레임 끝 `PauseMenu.LateUpdate()`에서만 다시 0으로 덮어씀).
- **시나리오 B:** `StopButton` 엔딩 선택 창이나 `DialoguePlayer` 대화 창이 열리면 `GameFlow.InputLocked = true`가 되지만 `Time.timeScale`은 `1f`로 유지된다. 그런데 `BroadcastBoard.Update()`(`Assets/Scripts/Broadcast/BroadcastBoard.cs:49, 60-67`)에는 `if (GameFlow.InputLocked) { hackedAt += Time.deltaTime; return; }` 처리가 누락되어 있다. 그 결과 엔딩 선택 창을 띄워 놓은 동안 뒤에서 역해킹 타이머(`Time.time - hackedAt >= hackDuration`)가 그대로 만료되어 `IsHacked = false` 및 `SetNumber(aiNumber)`가 호출되고, `TestEnemy.OnBroadcastChanged()`(`Assets/Scripts/Enemy/TestEnemy.cs:147-163`, 여기도 `InputLocked` 가드 없음)가 실행되어 굳어 있던 3웨이브 시민이 깨어나거나 전광판 숫자가 바뀐다. 또한 `BossFight.Update()`(`Assets/Scripts/Boss/BossFight.cs:58-59`)는 `if (GameFlow.InputLocked) return;`으로 `NewHackThisFrame()` 호출을 건너뛰어 `wasHacked`/`lastProgress` 상태 갱신이 어긋난다.

## 근거 (file:line)
- `Assets/Scripts/Player/PlayerController.cs:94-99, 103-106, 268-272` — `hitStopEndsAt`이 `Time.unscaledTime`을 사용하며 `Update()`에서 `GameFlow.InputLocked`나 일시정지 여부를 확인하지 않고 `Time.timeScale = 1f`를 설정함
- `Assets/Scripts/UI/PauseMenu.cs:74-77` — `LateUpdate()`(실행 순서 1000)에서만 `Time.timeScale = 0f`를 재설정하여, 같은 프레임 내 `PlayerController.Update()` 이후 실행되는 다른 `Update()`들이 `timeScale == 1f`에 노출됨
- `Assets/Scripts/Broadcast/BroadcastBoard.cs:49, 60-67` — `GameFlow.InputLocked` 동안 `hackedAt += Time.deltaTime` 보정이 없어 입력 잠금 중에도 역해킹이 진행·만료됨
- `Assets/Scripts/Boss/BossFight.cs:58-59, 134-143` — `GameFlow.InputLocked`일 때 `NewHackThisFrame()` 폴링이 중단되는 반면 `BroadcastBoard`는 계속 변함
- `docs/design/ARCHITECTURE_PATTERNS.md:48` — 전광판 타이머도 `InputLocked` 동안 멈춰야 한다는 설계 규칙 명시
