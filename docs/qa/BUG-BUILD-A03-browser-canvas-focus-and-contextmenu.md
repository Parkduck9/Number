검증: PLAUSIBLE (OnApplicationFocus 미구현은 확인, 브라우저 컨텍스트 메뉴·포커스 영향은 실기 확인 필요)

# BUG-BUILD-A03 — WebGL 브라우저 캔버스 포커스 상실 및 우클릭(패링) 컨텍스트 메뉴·포커스 이탈 대비 부재

## 심각도
중간

## 관련 파일 및 근거 (`file:line`)
- `Assets/Settings/InputSystem_Actions.inputactions:278, 289` (`Attack`이 `<Mouse>/leftButton`, `Parry`가 `<Mouse>/rightButton`에 바인딩됨)
- `Assets/Scripts/Player/PlayerInputReader.cs:21-51` (마우스 커서 잠금/가두기 설정 없음, 패링에 키보드 보조 바인딩 없음)
- `Assets/Scripts/UI/PauseMenu.cs:65-77` (`OnApplicationFocus(bool hasFocus)` 미구현 — 캔버스 포커스 상실 시 자동 일시정지 없음)
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:121-144` 및 `Assets/Scripts/Level/StopButton.cs:46-50` (마우스 클릭으로 조작 가능한 UI 버튼이 없고 키보드 전용으로만 동작)
- `ProjectSettings/ProjectSettings.asset:89` (`runInBackground: 0`)

## 재현 단계
1. itch.io 브라우저 페이지에 임베드된 WebGL 게임을 실행한다.
2. 전투 중 마우스를 움직이며 좌클릭(공격)과 우클릭(패링)을 반복하다가 마우스 커서가 WebGL `<canvas>` 영역(또는 iframe) 바깥으로 살짝 벗어난 상태에서 클릭/우클릭한다.
3. 또는 `StopButton` 엔딩 선택 창(`[1]`, `[2]`)이나 `BroadcastHackUi` 해킹 다이얼이 열린 상태에서 마우스로 선택지를 클릭해 보거나, 캔버스 바깥을 클릭한 뒤 `A/D`, `W`, `E`, `1/2` 키를 누른다.

## 기대 결과
1. 전투 중 마우스 커서가 캔버스 밖으로 나가 클릭되더라도 키보드 조작(`A/D`, `W`, `Shift`, `Space`, `E`, `P`)이 먹통이 되지 않거나, 캔버스 포커스를 잃었을 때(`OnApplicationFocus(false)`) 게임이 자동으로 일시정지(`PauseMenu`)되어 플레이어가 무방비로 피격되지 않아야 한다.
2. 마우스 우클릭(패링) 시 브라우저 기본 컨텍스트 메뉴가 뜨지 않고, 마우스를 쓰지 않거나 노트북 트랙패드를 쓰는 환경을 위해 공격/패링의 키보드 보조 키(예: `J`/`K`)가 함께 제공되는 것이 안전하다.

## 실제 결과
1. 게임 코드 어디에도 `Cursor.lockState` 또는 `OnApplicationFocus(bool)` 처리가 없다. 마우스 커서가 잠겨 있지 않아 전투 중 커서가 캔버스 경계 밖으로 나가 좌/우클릭하는 순간 브라우저 포커스가 `<iframe>`/`<canvas>` 밖으로 이동하며, 우클릭 시 브라우저 컨텍스트 메뉴가 열린다.
2. 포커스가 빠진 상태에서도 같은 브라우저 탭 안에 있으면 게임 루프와 적 공격은 계속 진행되지만 모든 키보드 입력이 전달되지 않아 캐릭터가 무방비 상태로 피격된다.
3. `StopButton.cs:115-124` 및 `BroadcastHackUi.cs:198-224`의 선택 UI는 클릭 가능한 `Button`이 없는 텍스트 전용 패널(`raycastTarget = false`)이라 마우스 클릭으로 선택하거나 포커스를 복구할 때 직관적이지 않다.
