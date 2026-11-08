검증: PLAUSIBLE (A는 A05와 같은 주장으로 boss.md:65에 문서화된 의도라 REJECTED. B: TestEnemy.cs:436-443 ForceStagger가 Open 상태를 막지 않아 반격 창이 줄 수 있음은 코드상 사실, 낮음. 충격파 잠김은 A06-shockwave와 중복)

# BUG-BOSS-A09: 2웨이브 말미 해킹이 3웨이브("LUCKY 0") 시민 굳음을 무효화하며, 반격 창(`Open`) 상태 적에게 충격파 사용 시 무방비 시간이 단축됨

## 심각도
중간 (Medium)

## 재현 단계
1. **시나리오 A (2웨이브 해킹 잔류로 3웨이브 시민 미동결):** `STG05` 2웨이브 마지막 적을 기절시키기 직전이나, 2웨이브 클리어 후 다음 웨이브 시작 전 대기 시간(`betweenWavesSeconds = 1.5f`, `BossFight.cs:80`) 도중 전광판을 `0`이 아닌 숫자(예: `3` 또는 `7`, 유지 시간 `holdSeconds = 4f`)로 해킹한다.
2. 이어서 시작되는 3웨이브(`WAVE 3 - LUCKY 0`)에서 전광판 숫자와 소환되는 시민 적 3명의 상태를 확인한다.
3. **시나리오 B (충격파 업그레이드와 반격 창 / STG05 단독 실행):**
   - 1웨이브(방송 `3`, 반격 창 `1.5초`) 또는 2웨이브(방송 `7` 탈진, 반격 창 `2.0초`)에서 적이 `State.Open` 상태에 들어간 직후 `Space`(충격파)를 사용한다.
   - 또는 에디터/테스트에서 `STG05.unity`를 바로 실행하거나 타이틀을 거쳐 `STG05`만 불러왔을 때 충격파(`Shockwave.Unlocked`) 사용 가능 여부를 확인한다.

## 기대 결과
- **시나리오 A:** 3웨이브가 시작되어 전광판에 `"WAVE 3 - LUCKY 0"`이 선언되면 방송 숫자도 즉시 `0`이 되어야 하며, 3웨이브에 소환되는 시민 3명은 확정 규칙(NUM-0)에 따라 모두 숨을 참고 굳은 상태(`State.Frozen`)로 소환되어야 한다.
- **시나리오 B:** 이미 `1.5초`(NUM-3) 또는 `2.0초`(NUM-7 탈진) 동안 무방비(`State.Open`) 상태인 적에게 충격파(`staggerSeconds = 1f`)를 썼을 때 기존 무방비 시간이 `1.0초`로 줄어들어 손해를 보지 않아야 한다. 또한 GDD 6장·10장 `[확정]` 규칙("두 엔딩 모두 유도는 업그레이드(AI 없는 특수 공격 부품)를 장착한 채 AI 정지 버튼에 도달한다")에 따라 `STG05` 진입 시에는 충격파 업그레이드가 항상 해금되어 있어야 한다.

## 실제 결과
- **시나리오 A:** `BossFight.StartWave(2)`(`Assets/Scripts/Boss/BossFight.cs:102`)는 `board.SetAiNumber(0)`을 호출하지만, `BroadcastBoard.SetAiNumber()`(`Assets/Scripts/Broadcast/BroadcastBoard.cs:83-87`)는 `if (!IsHacked) SetNumber(aiNumber);` 조건 때문에 플레이어의 해킹이 남아 있으면 방송 숫자를 바꾸지 않는다. 그 결과 2웨이브 클리어 직후(`betweenWavesSeconds` 1.5초 동안) `3`이나 `7`로 해킹해 두면, 3웨이브가 시작되어 화면 문구는 `"WAVE 3 - LUCKY 0"`인데도 실제 방송 숫자는 최대 4초 동안 `3` 또는 `7`로 유지된다. 이 시간 동안 소환되는 3웨이브 적들은 `TestEnemy.Start()`(`Assets/Scripts/Enemy/TestEnemy.cs:99`)에서 `0`이 아닌 숫자를 받아 **시민들이 굳지 않고(`State.Approach` 또는 도핑 상태) 플레이어를 공격하며 충격파(`ForceStagger`)에도 경직된다.**
- **시나리오 B:**
  - `TestEnemy.ForceStagger(float seconds)`(`Assets/Scripts/Enemy/TestEnemy.cs:436-443`)는 `State.Stunned`와 `State.Frozen`만 예외 처리하고 `State.Open`은 막지 않은 채 `EnterState(State.Staggered)`로 덮어씌운다. 따라서 1웨이브(`NUM-3` 반격 창 `1.5초`, `BroadcastRules.cs:7`)나 2웨이브(`NUM-7` 탈진 반격 창 `2.0초`, `BroadcastRules.cs:13`)에서 방금 `Open`이 된 적에게 충격파(`1.0초`)를 쓰면 **오히려 공격 가능한 무방비 시간이 `1.5~2.0초`에서 `1.0초`로 줄어들고 일찍 `FinishReset()`이 호출되어 적이 다시 공격을 시작한다.**
  - `Shockwave.Unlocked`는 `STG03`의 `LudditeMeeting.Finish()`(`Assets/Scripts/Level/LudditeMeeting.cs:28`)에서만 `true`가 되고 `TitleScreen.Start()`(`Assets/Scripts/UI/TitleScreen.cs:11`)에서 `false`로 초기화되므로, `STG05`에 진입할 때(`BossFight.cs` 또는 `Stg05Builder.cs`) 해금을 보장하는 코드가 없어 단독 플레이 시 GDD `[확정]` 조건과 달리 업그레이드 없이 보스전을 치르게 된다.

## 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:80, 94-105` — 웨이브 클리어 후 1.5초 대기 중 해킹 가능, `StartWave`는 `board.SetAiNumber(w.broadcastNumber)`만 호출함
- `Assets/Scripts/Broadcast/BroadcastBoard.cs:83-87` — `IsHacked == true`이면 `SetAiNumber(0)`이 현재 `Number`를 `0`으로 갱신하지 않음
- `Assets/Scripts/Enemy/TestEnemy.cs:99, 161, 359-363, 436-443` — 스폰 시점 `zoneBoard.Number`가 0이 아니면 시민이 `Frozen`에 들어가지 않으며, `ForceStagger`가 더 긴 `State.Open` 지속 시간을 `1.0초`(`staggerSeconds`)로 단축시킴
- `Assets/Scripts/Player/Shockwave.cs:14, 19-27` / `docs/design/GDD.md:155, 202` — `STG05` 진입 시 업그레이드 해금 보장 누락
