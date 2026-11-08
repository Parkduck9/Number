검증: CONFIRMED (일부: 3웨이브 Announce 중 해킹은 C05와 중복: BUG-BOSS-C05-wave3-skipped-by-announce-hack.md. 지연 부분은 boss.md:65 문서화된 의도라 REJECTED)

# BUG-BOSS-A05 — 보스전(STG-05) 3웨이브 시작 시 이전 웨이브의 해킹이 남아 있으면 방송 숫자 0 전환 및 시민 동결(NUM-0)이 지연됨

## 심각도
중간

## 재현 단계
1. STG-05 보스전 2웨이브(방송 숫자 7)에서 마지막 적을 쓰러뜨리기 직전(또는 마지막 적을 기절시킨 뒤 다음 웨이브 대기 시간 `betweenWavesSeconds = 1.5초` 동안) 전광판을 해킹하여 숫자를 `3`이나 `7` 등으로 바꾼다 (`holdSeconds = 4초`, [Stg05Builder.cs:78](../../Assets/Editor/Stg05Builder.cs#L78)).
2. 1.5초 뒤 3웨이브(`StartWave(2)`, `broadcastNumber = 0`)가 시작된다.
3. 3웨이브의 시민 적 3명과 안드로이드 2명이 소환되는 것을 관찰한다.

## 기대 결과
- 기획서(DECISIONS 4-A 및 `docs/design/impl/boss.md` 1절)에 따라 3웨이브가 시작되면 보스 AI가 방송을 `0`으로 바꾸고, 3웨이브에 등장하는 시민 3명은 방송 숫자 `0`(NUM-0) 규칙에 의해 즉시 숨을 참고 굳어야(`State.Frozen`) 한다.

## 실제 결과
- `BossFight.StartWave(2)`는 `board.SetAiNumber(w.broadcastNumber)`를 호출하지만 ([BossFight.cs:102](../../Assets/Scripts/Boss/BossFight.cs#L102)), `BroadcastBoard.SetAiNumber(int n)`는 현재 해킹 중(`IsHacked == true`)일 때 **`aiNumber` 필드만 바꾸고 실제 방송 숫자(`Number`)는 바꾸지 않는다** ([BroadcastBoard.cs:83-87](../../Assets/Scripts/Broadcast/BroadcastBoard.cs#L83-L87)).
- 그 결과:
  1. 전광판에는 `"WAVE 3 - LUCKY 0"`라는 예고 문구가 뜨지만 ([BossBoardView.cs:26-30](../../Assets/Scripts/Boss/BossBoardView.cs#L26-L30)), 정작 전광판의 큰 숫자와 상단 HUD 방송 숫자는 이전 웨이브에서 플레이어가 해킹해 둔 숫자(예: `3` 또는 `7`)로 최대 4초 동안 유지된다.
  2. `announceSeconds = 2초`가 지나고 첫 적이 소환될 때(`2.6초`, `3.2초` 시점) 아직 이전 해킹(최대 4초)이 끝나지 않았다면, 소환된 3웨이브 시민 적들이 `0`이 아닌 이전 해킹 숫자를 받아 **굳지 않고(`Frozen` 미진입) 플레이어를 향해 달려들며 공격한다**.
  3. 심지어 플레이어가 2웨이브 클리어 후 1.5초 휴식 시간(`betweenWavesSeconds`)이나 3웨이브 `Announce` 중(적이 나오기 전)에 해킹을 걸어버리면, 3웨이브 초반 내내 방송 숫자 `0`이 적용되지 않는다. (더욱이 3웨이브 `Announce`가 시작된 직후 적이 한 마리도 나오기 전에 해킹하면 3웨이브 전투 없이 즉시 정지 버튼이 열려버린다 — [BossFight.cs:59](../../Assets/Scripts/Boss/BossFight.cs#L59).)

## 근거 파일 및 위치
- [BossFight.cs:59](../../Assets/Scripts/Boss/BossFight.cs#L59), [BossFight.cs:80](../../Assets/Scripts/Boss/BossFight.cs#L80), [BossFight.cs:94-105](../../Assets/Scripts/Boss/BossFight.cs#L94-L105)
- [BroadcastBoard.cs:83-87](../../Assets/Scripts/Broadcast/BroadcastBoard.cs#L83-L87) (`public void SetAiNumber(int n) { aiNumber = Mathf.Clamp(n, 0, 9); if (!IsHacked) SetNumber(aiNumber); }`)
- [Stg05Builder.cs:78-79](../../Assets/Scripts/Boss/BossFight.cs#L78-L79) (`holdSeconds = 4f`, `betweenWavesSeconds = 1.5f`, `announceSeconds = 2f`)
- [docs/design/impl/boss.md:65](../../docs/design/impl/boss.md#L65)
