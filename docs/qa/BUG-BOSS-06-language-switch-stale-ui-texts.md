# BUG-BOSS-06: 인게임 언어 변경(일시정지 메뉴·L키) 시 정지 버튼·해킹 UI·보스 전광판·진행 중 엔딩 대사가 갱신되지 않음

## 심각도
**중간**

## 재현 단계
1. 기본 언어(영어)로 `STG05` 보스전에 진입한다.
2. 전광판(`BossBoard`) 앞으로 가서 `[E] HACK` 프롬프트와 해킹 다이얼(`BROADCAST HACK`)을 확인하고, 3웨이브에서 해킹해 전광판에 `"STOP BUTTON UNLOCKED"`가 뜨고 정지 버튼(`StopButton`)이 나타나게 한다.
3. 정지 버튼 가까이 다가가 `[E] STOP BUTTON` 프롬프트를 띄운 뒤, `Esc`(또는 `P`)로 일시정지 메뉴(`PauseMenu`)를 열어 `"Language: English"` 버튼을 눌러 한국어로 변경(또는 `L` 키 입력)하고 게임을 재개한다.
4. 전광판 문구, 해킹 프롬프트/다이얼, 정지 버튼 프롬프트 및 엔딩 선택창(`[1]`/`[2]`), 그리고 엔딩 대화 도중 `L` 키를 눌렀을 때 현재 표시 중인 대사가 한국어로 바뀌는지 확인한다.

## 기대 결과
- 일시정지 메뉴의 언어 변경 버튼이나 `L` 키로 언어를 전환하면(`Loc.SetKorean`), 화면에 표시되거나 이후 열리는 모든 게임 UI(`StopButton`, `BroadcastHackUi`, `BossBoardView`, `DialoguePlayer`)가 변경된 언어로 즉시 표시되어야 한다.

## 실제 결과
- `Loc.Changed` 이벤트를 구독하는 클래스는 `LocalizedText`(`TitleScreen`과 `PauseMenu`의 `UiKit.Label`에서만 사용)뿐이며, 나머지 인게임 UI는 생성 시점에 문자열을 한 번만 대입하고 `Loc.Changed`를 구독하지 않는다.
  1. **`StopButton` (`Assets/Scripts/Level/StopButton.cs`):**
     - 플레이어가 처음 정지 버튼 반경에 들어갈 때 `BuildUi()`가 1회 실행되며 프롬프트(`"[E] STOP BUTTON"`)와 엔딩 선택창(`"[1] Press the stop button..."`) 텍스트가 고정된다. 이후 일시정지 메뉴나 `L` 키로 언어를 바꿔도 영어로 계속 고정된다.
  2. **`BroadcastHackUi` (`Assets/Scripts/Broadcast/BroadcastHackUi.cs`):**
     - 씬 시작 시 `Awake()` → `BuildUi()`에서 `promptText`(`"[E] HACK"`), `title`(`"BROADCAST HACK"`), `dialHint`(`"0-9 / UP DOWN..."`)가 1회만 설정되어, 씬 진입 후 일시정지 메뉴나 `L` 키로 언어를 바꿔도 스테이지 내내 언어가 바뀌지 않는다. 또한 `dialWarn`(`"EVERYONE STOPS BREATHING"`)은 `Loc.T` 없이 영어 문자열로만 하드코딩되어 있다.
  3. **`BossBoardView` (`Assets/Scripts/Boss/BossBoardView.cs`):**
     - `Loc.Changed`를 구독하지 않아 대기 중(`"EMBECTOR"`), 웨이브 중(`"WAVE 1 - LUCKY 3"`), 해금 후(`"STOP BUTTON UNLOCKED"`)에 언어를 바꿔도 현재 전광판 문구가 갱신되지 않는다. 특히 `OnUnlocked()` 이후에는 `unlocked = true`로 인해 더 이상 새 문구 이벤트가 발생하지 않으므로 `"STOP BUTTON UNLOCKED"`가 영구히 이전 언어로 남는다.
  4. **`DialoguePlayer` (`Assets/Scripts/Dialogue/DialoguePlayer.cs`):**
     - `Update()`에서 직접 `L` 키로 `Loc.SetKorean(!Loc.Korean)`을 호출하면서도 정작 현재 화면에 떠 있는 `talkLine`/`radioLine`의 화자 이름(`nameText`)과 본문(`body`)을 다시 그리지 않아, 엔딩 대화 도중 `L` 키를 눌러도 다음 대사로 넘어가기 전까지 언어가 바뀌지 않는다.

## 코드 근거 (file:line)
- `Assets/Scripts/UI/Loc.cs:9, 16-22, 39` — `Loc.Changed` 이벤트 선언 및 `LocalizedText` 구독
- `Assets/Scripts/UI/PauseMenu.cs:116` — 인게임 언어 전환 버튼 `Loc.SetKorean(!Loc.Korean)`
- `Assets/Scripts/Level/StopButton.cs:93, 96-125` — `BuildUi()` 최초 1회 실행 시에만 `T(...)` 호출, `Loc.Changed` 미구독
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:45, 154, 192, 206, 221-222` — `Awake()`에서 `BuildUi()` 1회 호출 후 언어 미갱신, `:154` `"EVERYONE STOPS BREATHING"` 하드코딩
- `Assets/Scripts/Boss/BossBoardView.cs:13-44` — `Loc.Changed` 미구독, `:38-42` `OnUnlocked()` 이후 문구 갱신 수단 없음
- `Assets/Scripts/Dialogue/DialoguePlayer.cs:64, 123-135` — `L` 키 토글 시 현재 표시 중인 대사 박스 미갱신
- `docs/design/impl/boss.md:80` — Play 모드 확인 목록 9번(언어 변경 확인)
