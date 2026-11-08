검증: CONFIRMED (코드상 사실: BossFight.cs:59가 waveIndex만 확인하고 StartWave가 Announce 시작 시 waveIndex를 올리며 switch에 Unlocked 케이스가 없어 3웨이브 적이 소환되지 않음. 다만 boss.md:17의 의도 범위인지는 기획 판단 필요. 관련: A05의 일부)

# BUG-BOSS-C05 3웨이브 제목이 뜨는 2초 안에 해킹하면 안드로이드가 한 마리도 나오지 않고 이긴다

- 영역: 보스 (STG-05) / 웨이브 진행
- 확인 방법: 코드 읽기 (Play 모드 미실행)

## 재현 단계
1. STG05에서 1·2웨이브를 정상으로 깬다.
2. 2웨이브 마지막 적을 기절시킨 뒤 웨이브 사이 1.5초(`betweenWavesSeconds`) 동안 전광판 아래로 가서 E로 다이얼을 열어 둔다 (다이얼은 웨이브와 상관없이 열린다).
3. 전광판에 "WAVE 3 - LUCKY 0"이 뜨자마자(적이 떨어지기 전, 제목 표시 2초 안) 다이얼에서 E를 누른다.
   - 2웨이브에 걸어 둔 해킹이 아직 남아 있어도 같다: 다시 해킹하면 역해킹 진행률이 0으로 돌아가 "새 해킹"으로 잡힌다(`BossFight.cs:139`).

## 기대 결과
- DECISIONS 4-A는 3웨이브(안드로이드 2 + 시민 3)를 "졸업 시험"으로 둔다: "마지막 웨이브에서 전광판을 해킹하면 정지 버튼이 열린다" (`docs/design/DECISIONS.md:67, 70`). boss.md도 "3웨이브 적을 다 쓰러뜨리지 않아도 된다"고만 하지, 적이 아예 안 나오는 경우는 적혀 있지 않다 (`docs/design/impl/boss.md` 1장).
- 적어도 3웨이브 적이 나온 상태(안드로이드와 마주친 상태)에서 해킹해야 정지 버튼이 열린다.

## 실제 결과
- `StartWave(2)`가 `waveIndex = 2`를 먼저 넣고 `Announce` 상태로 들어간다 (`Assets/Scripts/Boss/BossFight.cs:94-104`).
- 해금 줄은 상태를 보지 않고 `waveIndex`만 본다:
  `Assets/Scripts/Boss/BossFight.cs:59`
  ```csharp
  if (NewHackThisFrame() && waveIndex == waves.Length - 1 && state != State.Unlocked) Unlock();
  ```
- 프레임 흐름: 프레임 N `StartWave(2)` (Announce, 소환 0) → 프레임 N+1 다이얼 E → `BroadcastBoard.Hack()` → 같은/다음 프레임 `NewHackThisFrame()` true → `Unlock()` → `Enter(State.Unlocked)` (`:147`).
- `switch (state)`에 `Unlocked` 경우가 없어서(`:61-89`) `Announce → Spawning`으로 넘어가지 않는다. 3웨이브 적 5마리(안드로이드 2 포함)가 한 마리도 소환되지 않는다.
- 같은 이유로 3웨이브 소환 도중(`Spawning`) 해킹해도 남은 적 소환이 그 자리에서 끊긴다.
- 결과: 마지막 보스전의 핵심인 안드로이드 전투를 키 두 번(E, E)으로 건너뛸 수 있다.

## 심각도
중간 — 진행이 막히지는 않지만, 최종 보스의 마지막 시험이 통째로 빠질 수 있다. 심사 플레이에서 우연히도 나올 수 있다(2웨이브 직후 전광판 근처에 서 있으면 그대로 일어남).

## 수정 제안 (코드 담당 참고, 기획 확인 필요)
- 기획 확인: "마지막 웨이브 중 해킹"이 제목 표시 중에도 되는 것이 의도인지.
- 의도가 아니라면 `BossFight.cs:59`에 상태 조건을 더한다. 예: `(state == State.Spawning || state == State.Fighting)` 및 `spawned > 0`, 또는 `state == State.Fighting`(모두 소환된 뒤).
- 해금 뒤에도 남은 적을 다 내보내려면 `Unlock()`에서 상태를 바꾸지 말고 별도 `bool unlocked` 플래그로 처리한다.
