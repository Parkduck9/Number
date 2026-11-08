# BUG-BOSS-02: 3웨이브 예고(Announce)·소환(Spawning) 중 전광판 해킹 시 상태가 Unlocked로 덮어써져 적 소환이 중단됨

## 심각도
**높음**

## 재현 단계
1. `STG05` 보스전에서 1웨이브와 2웨이브의 적을 모두 기절시킨다.
2. 중앙 전광판(`BossBoard`, `x = 15`) 근처에 서서 1.5초(`betweenWavesSeconds`) 뒤 전광판에 `"WAVE 3 - LUCKY 0"`(`State.Announce`, 2초간 유지)이 표시되는 순간 `E` → `E`를 눌러 전광판을 해킹한다. (또는 적 1~2마리만 소환된 `State.Spawning` 도중에 전광판을 해킹한다.)
3. 이후 3웨이브 적(안드로이드 2 + 시민 3)이 소환되는지 확인한다.

## 기대 결과
- 기획 결정(`docs/design/DECISIONS.md` 4-A 및 `docs/design/impl/boss.md` 1절)에 따라 3웨이브는 안드로이드 2기 + 시민 3명을 소환해야 하며, 3웨이브 중 전광판을 해킹해 정지 버튼이 열리더라도 3웨이브의 적 소환 자체가 취소·누락되어서는 안 된다(또는 적이 소환된 `State.Fighting` 상태에서 해금이 이루어져야 한다).

## 실제 결과
- `StartWave(2)`가 호출되는 즉시 `waveIndex`가 `2`(`waves.Length - 1`)가 되고 `state`는 `State.Announce`(2.0초) → `State.Spawning`(3.0초, 0.6초 간격 5마리) 순서로 진행된다.
- 그러나 `BossFight.Update()` 첫 줄의 해금 조건(`NewHackThisFrame() && waveIndex == waves.Length - 1 && state != State.Unlocked`)은 `state`가 `State.Announce`나 `State.Spawning`일 때도 참이 된다.
- `Unlock()`이 호출되면 `Enter(State.Unlocked)`로 인해 단일 상태 머신의 `state`가 `State.Unlocked`로 덮어써진다.
- 그 결과 `Update()`의 `switch (state)`에서 `State.Announce` 및 `State.Spawning` 분기가 더 이상 실행되지 않아, **예고 2초 동안 해킹하면 3웨이브 적이 단 한 마리도 소환되지 않은 채(`spawned == 0`) 보스전이 즉시 끝나는 스킵 현상**이 발생한다(`State.Spawning` 중 해킹하면 남은 적이 소환되지 않고 중단됨).

## 코드 근거 (file:line)
- `Assets/Scripts/Boss/BossFight.cs:43` — `enum State { Idle, Announce, Spawning, Fighting, Unlocked }` (단일 상태로 웨이브 진행과 해금 상태를 겸함)
- `Assets/Scripts/Boss/BossFight.cs:59` — `if (NewHackThisFrame() && waveIndex == waves.Length - 1 && state != State.Unlocked) Unlock();`
- `Assets/Scripts/Boss/BossFight.cs:68-78` — `State.Announce` 및 `State.Spawning`이 `state`가 유지될 때만 동작함
- `Assets/Scripts/Boss/BossFight.cs:94-105` — `StartWave(int i)`에서 `State.Announce` 진입과 동시에 `waveIndex = i` 설정
- `Assets/Scripts/Boss/BossFight.cs:145-150` — `Unlock()`이 `Enter(State.Unlocked)`를 호출하여 진행 중인 `Announce`/`Spawning` 상태를 덮어씀
- `docs/design/DECISIONS.md:67` (4-A) / `docs/design/impl/boss.md:9-17` — 3웨이브 적 구성(안드로이드 2 + 시민 3) 및 소환 규칙
