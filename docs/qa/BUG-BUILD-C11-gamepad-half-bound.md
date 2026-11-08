검증: PLAUSIBLE (패드 바인딩 일부 확인, 전체 경로는 미확인)

# BUG-BUILD-C11 — 게임패드는 일부만 연결돼 있어, 패드로는 패링·일시정지·해킹·대사 넘기기를 못 한다

## 재현 단계
1. PC에 게임패드(Xbox 등)를 꽂고 브라우저에서 WebGL 빌드를 연다 (Chrome·Edge·Firefox 모두 Gamepad API 지원).
2. 패드로 이동·점프·공격·대시를 해 본다 → 된다.
   - `Assets/Settings/InputSystem_Actions.inputactions:102` 이동 leftStick, `:409` 점프 buttonSouth, `:267` 공격 buttonWest, `:376` 대시 leftStickPress
   - `Assets/Scripts/Player/PlayerInputReader.cs:30` 충격파 buttonEast
3. 패드로 패링, 일시정지, 해킹 다이얼, 대사 넘기기, 정지 버튼 선택을 해 본다.

## 기대 결과
- 둘 중 하나: (가) 패드로 처음부터 끝까지 플레이할 수 있다, 또는 (나) 키보드+마우스 전용이라고 Controls에 적혀 있고 패드 바인딩이 섞여 있지 않다.

## 실제 결과
- 패링: `InputSystem_Actions.inputactions:286-296` — Parry 바인딩은 `<Mouse>/rightButton` 하나뿐. 패드로는 게임의 핵심 행동인 패링이 불가.
- 일시정지: `Assets/Scripts/UI/PauseMenu.cs:67-68` — `Keyboard.current`의 Esc/P만 본다.
- 대사 넘기기: `Assets/Scripts/Dialogue/DialoguePlayer.cs:103-104` — E 키 또는 마우스 좌클릭만. 대화 모드는 입력을 잠그므로(`:94`) 패드만 쥔 사람은 대사에서 멈춘다.
- 해킹 다이얼 / 정지 버튼: `Assets/Scripts/Broadcast/BroadcastHackUi.cs:63`, `:123-142`, `Assets/Scripts/Level/StopButton.cs:35` — 키보드·마우스만.
- 패드 대시가 왼쪽 스틱 누르기(`:376`)라 누르기 어렵고, buttonEast가 충격파와 템플릿의 Crouch(`:475`, 미사용)에 같이 걸려 있다.
- 결과적으로 패드는 "반쯤 되는" 상태라, 패드로 시작한 심사자는 첫 대사나 첫 패링에서 막힌다.

## 수정 제안 (담당: 코드 + 사용자 결정)
- 6일 잼 범위로는 (나) 권장: 제출 페이지 Controls에 "Keyboard + Mouse only"를 적는다. 코드 변경 없음.
- (가)를 고르면 최소 추가: Parry에 `<Gamepad>/leftShoulder`(또는 rightShoulder), 대사 넘기기·다이얼 확정에 buttonSouth/buttonNorth, 일시정지에 `<Gamepad>/start`.
  11/13 이후는 기능 추가 금지이므로 그 전에 결정.

## 심각도
낮음 — 키보드+마우스 플레이에는 영향 없음. 패드 사용자에게만 막힘.
