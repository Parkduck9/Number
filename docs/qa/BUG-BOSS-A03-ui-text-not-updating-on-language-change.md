검증: CONFIRMED (일부: HackUi/StopButton/BossBoardView는 생성 시 한 번만 번역, 즉 C02와 중복. FaceHealthHud 영문 하드코딩은 영어 UI 규칙상 문제 아님)

# BUG-BOSS-A03 — 언어 변경(Loc.Changed) 시 실시간으로 갱신되지 않는 UI 및 보스 전광판 텍스트 다수 존재

## 심각도
중간

## 재현 단계
1. 타이틀 화면 또는 일시정지 메뉴에서 언어를 변경하거나(`Language: English` / `언어: 한국어`), 게임 중 `L` 키를 눌러 언어를 전환한다 ([DialoguePlayer.cs:64](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L64), [PauseMenu.cs:116](../../Assets/Scripts/UI/PauseMenu.cs#L116)).
2. 다음 UI 텍스트들을 확인한다:
   - 보스전 전광판 메시지 (`BossBoardView`: `"EMBECTOR"` / `"WAVE 1 - LUCKY 3"` / `"...cute."` / `"STOP BUTTON UNLOCKED"`)
   - 전광판 해킹 UI (`BroadcastHackUi`: `"[E] HACK"`, `"BROADCAST HACK"`, `"0-9 / UP DOWN / wheel: pick..."`, `"EVERYONE STOPS BREATHING"`)
   - 정지 버튼 UI (`StopButton`: `"[E] STOP BUTTON"`, `"[1] Press the stop button..."`)
   - 좌측 상단 체력 HUD 상태 라벨 (`FaceHealthHud`: `"NORMAL"`, `"EAR"`, `"NOSE"`, `"EYE"`, `"MOUTH"`, `"AI CHIP"`)
   - 엔딩 대화 등 현재 표시 중인 `DialoguePlayer` 대사 및 화자 이름

## 기대 결과
- 일시정지 메뉴에서 언어를 바꾸고 돌아오거나 `L` 키를 눌렀을 때, 화면에 떠 있는 모든 UI·전광판·프롬프트·대화 텍스트가 선택한 언어(영어/한국어)로 즉시 갱신되어야 한다. 또한 한국어 번역이 누락된 항목이 없어야 한다.

## 실제 결과
1. **`BroadcastHackUi`**: `Awake()`의 `BuildUi()`에서 한 번만 `T(...)`로 문자열을 넣기 때문에, 씬 로드 후 일시정지 메뉴나 `L` 키로 언어를 바꿔도 프롬프트(`"[E] HACK"`), 제목(`"BROADCAST HACK"`), 조작 힌트가 **씬이 끝날 때까지 기존 언어로 고정**된다 ([BroadcastHackUi.cs:192](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L192), [BroadcastHackUi.cs:206](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L206), [BroadcastHackUi.cs:221-222](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L221-L222)). 또한 `"EVERYONE STOPS BREATHING"` 경고 문구는 아예 `Loc.T`를 거치지 않고 영어 하드코딩되어 있다 ([BroadcastHackUi.cs:154](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L154)).
2. **`StopButton`**: 처음 버튼에 다가갈 때 `BuildUi()`를 한 번 호출하여 텍스트를 설정한 뒤 `Loc.Changed`를 구독하지 않아, 이후 일시정지 메뉴나 `L` 키로 언어를 바꿔도 `"[E] STOP BUTTON"` 및 엔딩 선택지 텍스트가 갱신되지 않는다 ([StopButton.cs:89](../../Assets/Scripts/Level/StopButton.cs#L89), [StopButton.cs:111](../../Assets/Scripts/Level/StopButton.cs#L111), [StopButton.cs:122-123](../../Assets/Scripts/Level/StopButton.cs#L122-L123)).
3. **`BossBoardView`**: `Loc.Changed`를 구독하지 않아 언어를 바꿔도 다음 이벤트(`WaveStarted`, `NumberChanged`, `Unlocked`)가 발생하기 전까지 전광판 문구가 이전 언어로 남아 있으며, 특히 `Unlocked` 이후에는 다시 갱신될 기회가 없다 ([BossBoardView.cs:13-44](../../Assets/Scripts/Boss/BossBoardView.cs#L13-L44)).
4. **`FaceHealthHud`**: `"NORMAL"`, `"EAR"`, `"NOSE"`, `"EYE"`, `"MOUTH"`, `"AI CHIP"` 문자열에 한국어 대응(`Loc.T`)이 없다 ([FaceHealthHud.cs:18](../../Assets/Scripts/Checkpoint/FaceHealthHud.cs#L18), [FaceHealthHud.cs:38](../../Assets/Scripts/Checkpoint/FaceHealthHud.cs#L38)).
5. **`DialoguePlayer`**: 대사창이 떠 있는 도중 `L` 키를 누르면 `Loc.SetKorean(!Loc.Korean)`만 호출하고 현재 표시 중인 `talkBox` / `radioBox`의 화자 이름과 본문을 다시 그리지 않아 다음 대사 줄로 넘어가기 전까지 언어가 바뀌지 않는다 ([DialoguePlayer.cs:64](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L64), [DialoguePlayer.cs:123-135](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L123-L135)).

## 근거 파일 및 위치
- [BroadcastHackUi.cs:154](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L154), [BroadcastHackUi.cs:192-222](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L192-L222)
- [StopButton.cs:96-125](../../Assets/Scripts/Level/StopButton.cs#L96-L125)
- [BossBoardView.cs:13-44](../../Assets/Scripts/Boss/BossBoardView.cs#L13-L44)
- [FaceHealthHud.cs:18-39](../../Assets/Scripts/Checkpoint/FaceHealthHud.cs#L18-L39)
- [DialoguePlayer.cs:64](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L64), [DialoguePlayer.cs:123-135](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L123-L135)
