검증: CONFIRMED (중복: BUG-BOSS-C02, BUG-BOSS-A03. 영어는 항상 표시되므로 영어 UI 규칙은 충족, 낮음)

# BUG-BOSS-A11: 대화·엔딩 선택·보스전 도중 언어 전환(`L` 키 또는 일시정지 메뉴) 시 현재 대사 및 UI 문구가 갱신되지 않음

## 심각도
중간 (Medium)

## 재현 단계
1. `STG05` 보스전에 진입한 뒤, 전광판에 `"EMBECTOR"`, `"WAVE 1 - LUCKY 3"`, `"...cute."`, 또는 `"STOP BUTTON UNLOCKED"`가 표시된 상태에서 `L` 키(또는 일시정지 메뉴의 `Language` 버튼)로 언어를 전환한다.
2. 전광판(`BossBoard`) 근처에서 `[E] HACK` 프롬프트나 해킹 다이얼(`BROADCAST HACK`)을 열고 언어를 전환한 뒤 문구와 `0` 선택 시 경고 문구(`EVERYONE STOPS BREATHING`)를 확인한다.
3. 3웨이브에서 정지 버튼(`StopButton`)을 해금한 뒤 버튼에 한 번 다가가 `[E] STOP BUTTON` 프롬프트를 띄우고, `E`를 눌러 엔딩 선택 창(`[1] ... [2] ...`)을 연 상태에서 `L` 키를 누른다.
4. `1` 또는 `2`를 눌러 엔딩 대화가 출력되는 도중 `L` 키를 눌러 언어를 전환한다.

## 기대 결과
- 언어를 전환(`Loc.SetKorean`)하면 현재 화면에 표시 중인 대화창의 화자 이름과 대사 본문, 정지 버튼 프롬프트 및 엔딩 1/2 선택지 문구, 해킹 UI 프롬프트·다이얼 제목·경고·도움말, 그리고 보스 전광판 메시지가 모두 즉시 바뀐 언어로 갱신되어야 한다.

## 실제 결과
1. **엔딩 대화 도중 언어 미갱신 (`DialoguePlayer.cs`):** `DialoguePlayer.Update()`(`Assets/Scripts/Dialogue/DialoguePlayer.cs:64`)는 대화가 진행 중일 때도 매 프레임 `L` 키 입력을 받아 `Loc.SetKorean(!Loc.Korean)`을 호출하지만, 정작 `DialoguePlayer` 자신은 `Loc.Changed`를 구독하지 않으며 `Show()`(`DialoguePlayer.cs:123-135`)는 대사 줄이 처음 큐에서 빠질 때(`76, 95, 112번째 줄`)만 호출된다. 따라서 엔딩 대사 도중 `L`을 누르면 **다음 줄로 넘어가기 전까지 현재 줄의 화자 이름(`nameText`)과 대사(`body`)가 이전 언어로 남아 있다.**
2. **정지 버튼 프롬프트 및 엔딩 선택창 언어 영구 고정 (`StopButton.cs`):** `StopButton.BuildUi()`(`Assets/Scripts/Level/StopButton.cs:96-125`)는 플레이어가 처음 정지 버튼 반경(`2.5u`)에 들어왔을 때 단 한 번 실행되며, `p.text`(`111번째 줄`)와 엔딩 선택지 `t.text`(`122-123번째 줄`)에 `LocalizedText`를 붙이지도 않고 `Loc.Changed`를 구독하지도 않는다. 심지어 엔딩 선택 창이 열린 상태(`Phase.Choosing`)에서도 `DialoguePlayer.Update()`가 `L` 키로 언어를 바꾸지만, **정지 버튼의 프롬프트와 엔딩 1/2 선택지 텍스트는 처음 생성된 언어로 영구히 고정되어 바뀌지 않는다.**
3. **해킹 UI 언어 영구 고정 및 경고문 한국어 누락 (`BroadcastHackUi.cs`):** `BroadcastHackUi.BuildUi()`(`Assets/Scripts/Broadcast/BroadcastHackUi.cs:45, 192, 206, 221-222`)는 씬 시작(`Awake`) 시점의 언어로만 `[E] HACK`, `BROADCAST HACK`, 조작 안내를 설정하고 `Loc.Changed`를 구독하지 않아 스테이지 도중 언어를 바꿔도 갱신되지 않는다. 또한 `0`을 골랐을 때 뜨는 경고문(`154번째 줄`: `"EVERYONE STOPS BREATHING"`)은 아예 한국어 번역 없이 영어 하드코딩되어 있다.
4. **보스 전광판 문구 미갱신 및 역해킹 복귀 후 문구 고착 (`BossBoardView.cs`):** `BossBoardView`(`Assets/Scripts/Boss/BossBoardView.cs:13-45`)는 `Loc.Changed`를 구독하지 않아서, 특히 3웨이브 해킹 성공 후 `"STOP BUTTON UNLOCKED"`가 뜬 뒤에는 더 이상 이벤트가 발생하지 않아 언어를 바꿔도 영영 갱신되지 않는다. 게다가 1·2웨이브 중 플레이어가 해킹하면 `OnNumberChanged`(`32-36번째 줄`)가 웨이브 제목을 `"...cute."`(`"...귀엽군."`)로 덮어쓰는데, 4초 뒤 AI 역해킹이 끝나 원래 숫자로 돌아올 때는 `!b.IsHacked` 조건 때문에 조기 반환하여 웨이브 제목이 복구되지 않고 끝까지 `"...cute."`로 남는다.

## 근거 (file:line)
- `Assets/Scripts/Dialogue/DialoguePlayer.cs:64, 76, 95, 112, 123-135` — `L` 키로 `Loc.SetKorean`을 호출하지만 현재 표시 중인 `talkLine` / `radioLine` 텍스트를 다시 그리지 않음
- `Assets/Scripts/Level/StopButton.cs:89, 96-125` — `BuildUi()` 최초 1회만 `T(en, ko)`를 호출하고 `Loc.Changed` 미구독
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:45, 154, 192, 206, 221-222` — `Awake()` 최초 1회만 `T(en, ko)`를 호출하고 `Loc.Changed` 미구독, 154줄 경고문 한국어 누락
- `Assets/Scripts/Boss/BossBoardView.cs:13-45` — `Loc.Changed` 미구독 및 역해킹 완료(`!b.IsHacked`) 시 웨이브 제목 미복구
