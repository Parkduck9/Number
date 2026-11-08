# BUG-BOSS-05: 웨이브 전환 대기(`betweenWavesSeconds`) 및 시작 시 기존 해킹이 유지되어 다음 웨이브 방송 숫자와 3웨이브 해금 판정이 어긋남

## 심각도
**중간**

## 재현 단계
1. `STG05` 보스전에서 2웨이브(`broadcastNumber = 7`)의 마지막 적을 기절시킨다.
2. 마지막 적이 기절한 직후 다음 웨이브까지 대기하는 1.5초(`betweenWavesSeconds`) 동안(또는 마지막 적 기절 직전 1초 이내에) 전광판을 해킹하여 숫자를 `3`(유지 시간 4초)으로 바꾼다.
3. 잠시 후 3웨이브(`"WAVE 3 - LUCKY 0"`)가 시작될 때 전광판/HUD의 방송 숫자와 소환되는 안드로이드의 개인 숫자, 그리고 정지 버튼 해금 여부를 확인한다.

## 기대 결과
- 2웨이브의 모든 적이 기절한 뒤 전광판을 해킹하거나 3웨이브가 시작될 때:
  1. 새 웨이브가 시작되면 `DECISIONS.md` 4-A("웨이브마다 방송을 3 → 7 → 0으로 바꾸고 적을 부른다")에 따라 전광판과 HUD가 해당 웨이브의 방송 숫자(3웨이브는 `0`)로 즉시 전환되고, 소환되는 적들도 새 웨이브의 방송 규칙을 적용받아야 한다.
  2. 3웨이브 진입 시점에 해킹 진행 바가 켜져 있는데도 정지 버튼이 열리지 않는 모순된 상태가 발생하지 않아야 한다.

## 실제 결과
1. **2웨이브 클리어 후 대기 시간(1.5초) 중 해킹 시 무효 처리 + 역해킹 바만 3웨이브로 이월:**
   - 2웨이브 적이 전멸해 `WaveCleared`가 발생했더라도 `betweenWavesSeconds(1.5초)` 동안은 `waveIndex`가 아직 `1`이므로 이때 한 해킹은 정지 버튼을 열지 못한다.
   - 직후 `StartWave(2)`가 실행되어 전광판 문구는 `"WAVE 3 - LUCKY 0"`으로 바뀌지만, `BroadcastBoard.SetAiNumber(0)`는 `if (!IsHacked) SetNumber(aiNumber);` 때문에 **기존 해킹 숫자(`3`)와 하늘색 역해킹 진행 바를 그대로 유지**한다. 플레이어 입장에서는 3웨이브에서 전광판이 해킹된 상태인데 정지 버튼이 열리지 않은 것처럼 보인다.
2. **새 웨이브 적들에게 이전 웨이브 해킹 숫자가 적용됨:**
   - 2웨이브 말미 또는 전환 대기 중에 `3`(유지 시간 4초)을 해킹하면, `betweenWavesSeconds(1.5초)` + `Announce(2.0초)`가 지난 뒤 `State.Spawning`에서 첫 적(`t = 0.6초`, 합계 4.1초 미만)이 소환될 때까지 전광판 숫자가 `0`이 아닌 `3`으로 남아 있다.
   - 그 결과 3웨이브의 안드로이드가 방송 `0`이 아니라 방송 `3`(`NUM-3` 규칙: 근접 적 숫자를 `3`으로 고정)을 받아 개인 숫자가 `1~7` 대신 `3`으로 고정된 채 소환되며, 해킹이 만료되기 전에 첫 공격(`State.Windup`)을 시작하면 첫 연타 내내 숫자 `3`이 유지된다.

## 코드 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:59, 79-80, 94-105`:
  - `:59` `waveIndex == waves.Length - 1`일 때만 해금(`betweenWavesSeconds` 동안은 이전 `waveIndex` 유지)
  - `:79-80` `WaveCleared` 후 `betweenWavesSeconds(1.5f)` 대기 뒤 `StartWave(waveIndex + 1)` 호출
  - `:102` `board.SetAiNumber(w.broadcastNumber);`
- `Assets/Scripts/Broadcast/BroadcastBoard.cs:83-87` — `SetAiNumber(int n)`에서 `if (!IsHacked) SetNumber(aiNumber);`로 해킹 중일 때 AI 방송 숫자 반영을 보류함
- `Assets/Scripts/Enemy/TestEnemy.cs:98-99, 152-157` — 소환 시점의 `zoneBoard.Number`를 읽어 `NUM-3` 숫자 고정(`number.Pin(3)`)을 적용함
- `Assets/Editor/Stg05Builder.cs:78-79` — `holdSeconds = 4f`, `holdSecondsForZero = 3f`
- `docs/design/DECISIONS.md:67` (4-A) / `docs/design/impl/boss.md:9, 65`
