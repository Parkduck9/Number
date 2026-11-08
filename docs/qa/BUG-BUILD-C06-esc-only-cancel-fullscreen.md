검증: CONFIRMED (BroadcastHackUi.cs:125, StopButton.cs:49 Esc 전용 확인. 브라우저 동작은 PLAUSIBLE)

# BUG-BUILD-C06 — 해킹 다이얼과 정지 버튼 선택창은 Esc로만 취소되는데, 브라우저 전체 화면에서는 Esc가 게임에 오지 않는다

## 재현 단계
1. WebGL 빌드를 itch.io에서 전체 화면(fullscreen 버튼)으로 실행한다.
2. 전광판 근처에서 E로 해킹 다이얼을 연다 → Esc를 누른다.
   - 근거: `Assets/Scripts/Broadcast/BroadcastHackUi.cs:125` — 취소는 `escapeKey`뿐 (E는 해킹 실행, `:126-131`).
3. 마지막 스테이지 정지 버튼에서 E로 엔딩 선택창을 연다 → Esc를 누른다.
   - 근거: `Assets/Scripts/Level/StopButton.cs:49` — 취소는 `escapeKey`뿐 (주석 `:8` "Esc cancels").

## 기대 결과
- 전체 화면에서도 취소할 수 있다. 일시정지 메뉴는 이미 이 문제 때문에 P 키를 같이 쓴다 (`Assets/Scripts/UI/PauseMenu.cs:6, :68`).

## 실제 결과
- 브라우저는 전체 화면에서 Esc를 먼저 가로채 전체 화면만 끄고, 키 입력을 페이지에 넘기지 않는다.
  첫 Esc로는 다이얼/선택창이 닫히지 않고 전체 화면만 풀린다. 다이얼은 E를 누르면 해킹이 실행돼 버리므로, 전체 화면 플레이어는 "취소" 수단이 사실상 없다.

## 수정 제안 (담당: 코드)
- 두 곳 모두 취소 키에 다른 키를 추가한다 (예: PauseMenu와 맞춰 `pKey`, 또는 `backspaceKey`/우클릭). 안내 문구에도 함께 표시.

## 심각도
낮음 — 막히지는 않지만(전체 화면이 풀린 뒤 Esc 한 번 더) 조작감이 나쁘고 실수로 해킹이 실행될 수 있다.
