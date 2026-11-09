작성: Claude Opus (QA 3라운드, 정적 검사)

# BUG-R3-01 우클릭 갈고리 줄이 게임 안 조작 힌트에 없음

- 관련: DECISIONS 16 (우클릭 = 네온 갈고리 줄), DECISIONS 15 (세로 맵)
- 근거 코드: `Assets/Scripts/UI/ControlHints.cs:18-35` (힌트 표), `Assets/Scripts/UI/UiKit.cs:104-105` (일시정지·타이틀 조작 패널에만 있음)
- 아이콘: `Assets/Resources/DraftUi/ctrl_mouse_right.png`는 있으나 어디에서도 쓰지 않음

## 재현 단계
1. 새로 시작해 STG-00 → STG-01로 진행한다 (일시정지·타이틀의 Controls 패널은 열지 않는다).
2. STG-01 화면 아래에 뜨는 조작 힌트 띠를 본다.
3. 첫 세로 구간(SH-A1 지그재그 통로, x 38~46)에 도착한다.

## 기대 결과
- 세로 맵을 오르는 핵심 이동 수단(DECISIONS 16 "세로 맵을 점프로 왔다 갔다 오르는 게 너무 번거롭다")인 우클릭 갈고리를 게임 안에서 한 번은 알려 준다 (예: STG-01 힌트 띠에 `ctrl_mouse_right` + "Grapple / 갈고리").

## 실제 결과
- `ControlHints.Hints`의 "STG01" 항목은 좌클릭(공격·패링) 하나뿐이다 (`ControlHints.cs:27-30`). 우클릭은 일시정지/타이틀의 Controls 패널(`UiKit.cs:104-105`)에서만 나온다.
- 패널을 열지 않는 플레이어는 갈고리가 있다는 것을 모른 채 세로 맵을 점프로만 오르게 된다 (STG-01 SEG-A 오르기 2곳, SEG-B 8칸 통로).

## 심각도
중간 (기능은 정상, 발견 불가로 세로 맵 경험이 DECISIONS 16 의도와 달라짐. 잼 심사자는 보통 일시정지 메뉴를 열지 않는다)

## 참고 (수정 방향, 코드는 Sonnet 담당)
- `Hints["STG01"]`에 `new[] { "ctrl_mouse_right", "", "Grapple", "갈고리" }` 한 줄 추가. 아이콘·문구는 이미 있음.
