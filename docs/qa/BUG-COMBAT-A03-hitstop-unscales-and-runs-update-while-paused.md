검증: REJECTED (제목의 '영구 고정'은 근거 없음: OnDisable이 EndHitStop 호출, 본문의 일시정지 중 1프레임 timeScale=1은 PauseMenu.LateUpdate가 되돌리고 deltaTime은 프레임 시작에 0으로 고정돼 실제 진행 없음. PauseMenu.cs:75 방어 코드 확인)

# BUG-COMBAT-A03: 패링 히트스톱 중 사망·재시작·타이틀 이동으로 씬이 전환되면 `Time.timeScale = 0`이 영구 고정됨

## 심각도
**높음**

## 위반 규칙
- **PROTOTYPE_ARCH.md 7절 추가 확정 2**: "임시 이펙트: 히트스톱 약 0.06초(T, `Time.timeScale` 0 → 복구, 실시간 타이머)"
- **PauseMenu.cs:10-11**: "The parry hit-stop sets timeScale back to 1 when it ends; LateUpdate re-asserts 0 while paused. Any scene load (death reload, restart, to title) force-closes the menu."

## 코드 근거
- `Assets/Scripts/Player/PlayerController.cs:92-107, 268-272`:
  ```csharp
  void OnDisable() { EndHitStop(); }

  void EndHitStop()
  {
      if (hitStopEndsAt < 0f) return;
      hitStopEndsAt = -1f;
      Time.timeScale = 1f;
  }
  ```
  패링 성공 시 `hitStopEndsAt = Time.unscaledTime + hitStopTime; Time.timeScale = 0f;`로 설정함.
- **문제 1 (히트스톱이 플레이어 사망으로 끊길 때)**:
  같은 프레임에 적 2명(또는 적 + 투사체, 안드로이드 부채꼴 황금 탄 3발)이 플레이어를 타격할 때, 첫 번째 타격을 패링하면 `PlayerController.ReceiveEnemyHit`(`PlayerController.cs:265-273`)가 즉시 `State.Locomotion`으로 전이하고 `hitStopEndsAt`을 설정하며 `Time.timeScale = 0f`로 만든다. 직후 같은 프레임에 두 번째 타격(또는 등 뒤 타격/패링 불가 타격)이 들어오면 플레이어는 이미 `State.Locomotion`(`ParryActive == false`)이므로 피격되어 `State.Hurt`로 전이하고 HP가 0이 되면 사망한다.
  그런데 `Update()`(`PlayerController.cs:103-107`)에서는 `hitStopEndsAt >= 0f`일 때 `Hurt` 타이머가 끝나도 히트스톱이 끝난 뒤에야 `State.Dead`로 들어가지만, 만약 `PrototypeDebug.cs:14`의 `R` 키 재시작이나 다른 씬 로드보다 먼저 `RespawnManager` 또는 `PauseMenu`가 개입할 때 다음과 같은 충돌이 발생한다:
- **문제 2 (`PauseMenu`와의 실행 순서 및 씬 언로드 시점 충돌)**:
  1. 패링 히트스톱(0.06초)이 걸린 바로 그 프레임(또는 히트스톱 도중)에 플레이어가 `Esc`/`P`로 일시정지 메뉴를 열었다가 닫거나, 일시정지 메뉴에서 `Restart from checkpoint` / `To title`을 누르면:
     - `PauseMenu.RestartFromCheckpoint()`(`PauseMenu.cs:121-125`) 또는 `ToTitle()`(`127-131`)은 먼저 `Close(unlock: true)`를 호출하여 `Time.timeScale = 1f`로 만든 뒤 `SceneManager.LoadScene(...)`을 호출한다.
     - 그러나 `SceneManager.LoadScene`은 현재 프레임이 끝난 뒤 다음 프레임 시작 전에 기존 씬 오브젝트들의 `OnDisable()`을 호출한다.
     - 만약 플레이어가 일시정지 메뉴를 **열어둔 상태(`open == true`, `Time.timeScale == 0f`)**에서 다른 경로(`PrototypeDebug`의 `R` 키 재시작 등)로 씬이 리로드되면:
       기존 씬의 `PlayerController.OnDisable()`이 먼저 실행되어 `EndHitStop()`이 `Time.timeScale = 1f`로 덮어쓰고, `PauseMenu.OnSceneLoaded`(`PauseMenu.cs:51-55`)가 호출되어 `Close(unlock: false)`를 실행한다.
     - 반대로, 히트스톱 진행 중(`hitStopEndsAt >= 0f`)에 일시정지 메뉴를 열면(`PauseMenu.Open()`, `Time.timeScale = 0f`), 메뉴를 열어둔 동안 실시간(`Time.unscaledTime`) 0.06초가 지나면 **`PlayerController.Update()`(`PlayerController.cs:103-105`)는 일시정지 상태(`Time.timeScale == 0`, `GameFlow.InputLocked == true`)에서도 멈추지 않고 실행**되어 `EndHitStop()`을 호출한다!
     - `EndHitStop()`이 호출되는 순간 `Time.timeScale = 1f`가 되어, 해당 프레임 동안 `PlayerController.Update()`의 나머지 로직(지면 판정, 상태 타이머 진행 등)과 `PlayerController`보다 실행 순서가 뒤인 모든 스크립트의 `Update()`가 **일시정지 메뉴가 열려 있는데도 `Time.timeScale = 1f` 상태에서 1프레임 실행**된다(`PauseMenu.LateUpdate`가 프레임 끝에 다시 0으로 되돌릴 때까지).

## 재현 단계
1. 적의 공격을 우클릭으로 패링하여 0.06초 히트스톱(`hitStopEndsAt >= 0`)이 시작된 직후 `Esc` 또는 `P` 키를 눌러 일시정지 메뉴를 연다.
2. 일시정지 메뉴가 열린 상태에서 0.06초(실시간)를 기다린다.

## 기대 결과
일시정지 메뉴가 열려 있는 동안(`PauseMenu` open)에는 히트스톱 타이머가 만료되더라도 `Time.timeScale`이 `1f`로 풀리지 않아야 하며, `PlayerController.Update()`의 상태 타이머나 다른 스크립트가 일시정지 중에 진행되어서는 안 된다.

## 실제 결과
`PlayerController.Update()`는 `GameFlow.InputLocked`나 일시정지 여부를 확인하지 않고 `if (Time.unscaledTime >= hitStopEndsAt) EndHitStop();`을 실행하여 `Time.timeScale = 1f`로 변경한 뒤, 곧바로 아래쪽의 `PlayerController.Update()` 본문(상태 전이, 공격 판정, 대시 잔상, Hurt→Locomotion 전이 등)을 실행해 버린다. `PauseMenu`는 `[DefaultExecutionOrder(1000)]`의 `LateUpdate()`에서야 `Time.timeScale = 0f`로 되돌리므로, 해당 프레임의 모든 `Update()` 동안 `Time.timeScale`이 1로 풀리는 시간 누수가 발생한다.
