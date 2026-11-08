검증: CONFIRMED / 중복: BUG-COMBAT-C04-inputs-lost-during-hitstop.md (심각도는 '높음'이 아닌 '낮음' 권장: 0.06초 창; 후반부 일시정지-히트스톱 충돌은 PauseMenu.LateUpdate가 방어하며 영향 미미)

# BUG-COMBAT-A08 — 패링 히트스톱(`Time.timeScale = 0`) 및 일시정지 중 플레이어 단발 입력 증발과 히트스톱 타이머 충돌

## 근거 파일 및 위치
- `Assets/Scripts/Player/PlayerController.cs:94-107, 122-124, 268-272`
- `Assets/Scripts/Player/PlayerInputReader.cs:5, 36-51`
- `Assets/Scripts/Player/Shockwave.cs:49-59`
- `Assets/Scripts/UI/PauseMenu.cs:73-98`

## 재현 단계
1. 적의 공격을 정면에서 우클릭으로 패링하여 `hitStopTime`(0.06초) 동안 히트스톱(`Time.timeScale = 0f`, `hitStopEndsAt = Time.unscaledTime + 0.06f`)이 걸리게 한다 (`PlayerController.cs:268-272`).
2. **케이스 A (히트스톱 중 반격/연속 패링/대시/충격파 입력):** 히트스톱이 유지되는 프레임(60~144Hz 기준 약 4~9프레임) 동안 좌클릭(공격), 우클릭(다음 연타 패링), Shift(대시), W(점프), 또는 Space(충격파)를 누른다.
3. **케이스 B (히트스톱 중 일시정지 열기/닫기):** 패링 직후 히트스톱 구간 안에 `Esc` 또는 `P`로 일시정지 메뉴(`PauseMenu`)를 열었다가 0.06초가 지나기 전/후에 닫는다.

## 기대 결과
- 패링 직후 히트스톱 연출(0.06초) 중에 누른 공격·패링·대시·점프·충격파 입력은 씹히지 않고 버퍼링되거나 히트스톱 종료 직후 즉시 실행되어야 한다 (특히 NUM-3 방송의 0.10초 고속 연타 패링 및 마지막 타격 패링 직후 1초 반격 창 공격).
- 히트스톱 도중 일시정지 메뉴를 열면 히트스톱 남은 시간이 일시정지 동안 소모되지 않아야 하며, 일시정지 중 `EndHitStop()`이 `Time.timeScale = 1f`로 풀거나 일시정지 해제 시 남은 히트스톱 동안 플레이어만 멈추는 불일치가 없어야 한다.

## 실제 결과
1. **히트스톱 중 모든 단발 입력(`WasPressedThisFrame`) 증발:**
   - 히트스톱 중에는 `Time.timeScale = 0f`이지만 `GameFlow.InputLocked`는 `false`이므로, `[DefaultExecutionOrder(-100)]`인 `PlayerInputReader.Update()`(`PlayerInputReader.cs:36-51`)가 매 프레임 실행되어 `WasPressedThisFrame()` 값을 읽고 소모한다.
   - 그러나 `PlayerController.Update()`는 `hitStopEndsAt >= 0f`일 때 106줄(`else { UpdateVisual(Time.time); return; }`)에서 조기 반환하므로, 122~124줄의 점프/공격 버퍼(`jumpBufferedUntil`, `attackBufferedUntil`)조차 기록되지 않고 `UpdateLocomotion()`도 실행되지 않는다.
   - `Shockwave.Update()`(`Shockwave.cs:49`) 역시 `if (Time.timeScale == 0f) return;`으로 조기 반환하여 해당 프레임의 `ShockwavePressed` 입력이 그대로 버려진다.
2. **히트스톱과 `PauseMenu` 간 `Time.timeScale` 및 업데이트 충돌:**
   - `hitStopEndsAt`은 `Time.unscaledTime` 기준(`PlayerController.cs:105, 270`)이므로 일시정지 메뉴(`PauseMenu`)를 열어 둔 상태에서도 0.06초 뒤 만료된다.
   - 만료되는 프레임에 `EndHitStop()`(`PlayerController.cs:94-99`)이 호출되면서 일시정지 상태임에도 `Time.timeScale = 1f`로 설정되고(`PauseMenu.LateUpdate` 전까지 다른 `Update`들이 `timeScale == 1`로 동작), `PlayerController.Update()`가 `return` 없이 108~151줄을 그대로 실행한다.
   - 반대로 `hitStopEndsAt` 만료 전에 `PauseMenu.Close()`(`PauseMenu.cs:94`)가 호출되면 `Time.timeScale = 1f`로 풀려 적과 투사체는 움직이는데 `PlayerController.Update()`만 남은 히트스톱 시간 동안 106줄에서 조기 반환하며 굳어 있게 된다.

## 심각도
높음
