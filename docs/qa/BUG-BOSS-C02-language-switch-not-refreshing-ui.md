검증: CONFIRMED (중복: BUG-BOSS-A03-ui-text-not-updating-on-language-change.md. 영어는 항상 표시되므로 영어 UI 규칙은 충족, 낮음)

# BUG-BOSS-C02 게임 중 언어를 바꿔도 정지 버튼·해킹 UI·현재 대사 줄은 예전 언어로 남는다

- 영역: 보스 (STG-05) / UI 언어
- 확인 방법: 코드 읽기 (Play 모드 미실행)

## 재현 단계
1. 영어 상태로 STG05에 들어간다.
2. 전광판 근처에서 "[E] HACK" 프롬프트를 본다. 3웨이브에서 정지 버튼을 열고 버튼 근처로 가서 "[E] STOP BUTTON" 프롬프트를 한 번 띄운다.
3. L 키(어디서나 동작, `DialoguePlayer.cs:64`) 또는 일시정지 메뉴의 "Language" 버튼으로 한국어로 바꾼다.
4. 다시 해킹 프롬프트·다이얼, 정지 버튼 프롬프트·엔딩 선택창을 연다.
5. (추가) 엔딩 대화 도중 L을 누른다.

## 기대 결과
- 언어를 바꾸면 화면의 UI 문구가 곧바로(늦어도 다음에 열 때) 새 언어로 나온다. 타이틀·일시정지 메뉴는 `LocalizedText`로 그렇게 동작한다 (`Assets/Scripts/UI/Loc.cs:28-46`).

## 실제 결과
- 정지 버튼 UI는 처음 프롬프트를 띄울 때 한 번만 문구를 정한다. 이후 언어를 바꿔도 씬을 다시 불러오기 전까지 그대로다.
  - `Assets/Scripts/Level/StopButton.cs:111` (`[E] STOP BUTTON`), `:122-123` (엔딩 선택 문구) — `BuildUi()` 안에서만 설정.
- 해킹 UI도 `Awake`에서 한 번만 정한다: `Assets/Scripts/Broadcast/BroadcastHackUi.cs:192` (`[E] HACK`), `:206` (`BROADCAST HACK`), `:221-222` (조작 안내).
- 대화 상자는 줄을 띄울 때(`Show`)만 글자를 넣으므로, 대화 도중 L을 누르면 지금 줄은 예전 언어로 남고 다음 줄부터 바뀐다 (`Assets/Scripts/Dialogue/DialoguePlayer.cs:127, 130`). 엔딩 대화도 같다.
- 전광판 메시지(`BossBoardView`)도 다음 문구부터 바뀐다. 이것은 `docs/design/impl/boss.md` 8장 9번에 "다음 문구부터 바뀜"으로 적혀 있어 의도된 동작으로 본다.
- 참고: `BroadcastHackUi.cs:154`의 "EVERYONE STOPS BREATHING"은 한국어가 없다(영어는 있으므로 규칙 위반은 아님, 한국어는 선택).

## 심각도
낮음 — 영어(필수)는 항상 나오고, 한국어는 선택 사항이다. 씬을 다시 불러오면(죽음·재시작) 고쳐진다.

## 수정 제안 (코드 담당 참고)
- 코드로 만드는 TMP 텍스트에 기존 `LocalizedText`를 붙인다 (`go.AddComponent<LocalizedText>().Set(text, en, ko)`). `StopButton`, `BroadcastHackUi` 모두 3~4줄 변경.
- 대화 줄은 `Loc.Changed`를 구독해 현재 `talkLine`/`radioLine`을 다시 `Show`하면 된다(선택).
