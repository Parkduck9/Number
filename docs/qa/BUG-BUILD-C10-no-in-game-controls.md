검증: CONFIRMED (TitleScreen.cs:20-30에 조작법 텍스트 없음)

# BUG-BUILD-C10 — 게임 안 어디에도 기본 조작(이동·점프·공격·패링)이 안 나온다

## 재현 단계
1. WebGL 빌드(또는 Title 씬 Play)를 처음 켠다. itch 페이지 설명은 읽지 않는다고 가정한다.
2. 타이틀 화면을 본다 — `Assets/Scripts/UI/TitleScreen.cs:22-29`: 제목 "NUMBER", Start, Language 버튼만 만든다. 조작 안내 텍스트 없음.
3. Start → STG00. 진행하면서 화면에 나오는 안내를 모은다.
   - 대시만 대사로 안내: `Assets/Data/Dialogue/stg00_line03_placeholder.txt:4` "(Dash: Shift)" (placeholder 파일)
   - 충격파만 대사로 안내: `Assets/Data/Dialogue/stg03_luddite_placeholder.txt:7` "Press Space" (placeholder 파일)
   - 상호작용·다이얼은 화면 안내 있음: `BroadcastHackUi.cs:192`, `:221-222`, `StopButton.cs:111`, `:122`
4. 이동(A/D), 점프(W), 공격(좌클릭/Enter), 패링(우클릭), 대사 넘기기(E/좌클릭), 일시정지(Esc/P) 안내를 찾는다.
   - `Assets/Scripts` 전체 문자열 검색: 위 키 안내는 `Core/PrototypeDebug.cs:29` 한 곳뿐이고, 이 스크립트는 빌드에서 빠진 Prototype 씬에만 있다.

## 기대 결과
- 처음 하는 사람(심사자)이 설명서 없이 핵심 조작, 특히 게임의 중심인 **패링(우클릭)**을 알 수 있다.
  (JAM_RULES.md 심사 기준 "디자인: 설명서 없이도 스스로 익혀지고")
- 점프 키가 Space가 아니라 W라는 것(Space는 STG03 이후 충격파)도 처음에 알 수 있다.

## 실제 결과
- 빌드 씬(Title, STG00~05)에는 이동·점프·공격·패링·일시정지 안내가 하나도 없다.
- 플랫포머에서 흔한 Space 점프를 누르면 STG03 전까지 아무 일도 없다(`PlayerInputReader.cs:29` Space=충격파, `InputSystem_Actions.inputactions:398` 점프=W만).
- 대사 안내 2줄은 placeholder 파일 안에 있어 최종 대사로 바꿀 때 사라질 수 있다.
- 제출 페이지 Controls(SUBMISSION_CHECKLIST.md:61)만으로는 itch 임베드 화면에서 바로 안 보인다.

## 수정 제안 (담당: 코드 / 기획 확인)
- 가장 작은 방법: 타이틀 화면 아래쪽에 조작 한 줄(영/한) 라벨 추가. `UiKit.Label(...)` 한 번이면 된다. 예:
  `A/D move  W jump  LMB attack  RMB parry  Shift dash  E talk/use  Esc/P pause`
- 또는 STG00 시작 시 첫 대사/라디오에 이동·점프·공격·패링을 넣는다(대사는 사용자가 쓰므로 사용자 확인).
- 최종 대사로 교체할 때 Shift·Space 안내 줄이 남아 있는지 확인.

## 심각도
중간 — 막히는 버그는 아니지만 첫 1분 경험과 디자인 점수에 직접 영향. 패링을 모르면 게임의 핵심 규칙(숫자 = 공격 예고 → 패링)을 못 쓴다.
