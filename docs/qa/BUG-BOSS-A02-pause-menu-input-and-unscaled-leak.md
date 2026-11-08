검증: CONFIRMED (심각도는 과대평가, 실제 낮음: 해킹 다이얼 중 P로 일시정지하면 다이얼이 닫힘 BroadcastHackUi.cs:58, 라디오 자막이 일시정지 중에도 흘러감 DialoguePlayer.cs:80)

# BUG-BOSS-A02 — 일시정지 메뉴(PauseMenu)가 열린 상태에서 인게임 UI 및 대화창 조작(L, E, 숫자키 등)이 그대로 입력됨

## 심각도
높음

## 재현 단계
1. STG-05(또는 플레이어가 있는 임의의 스테이지)에서 `Esc` 또는 `P` 키를 눌러 일시정지 메뉴(`PauseMenu`)를 연다.
2. **사례 A (해킹 다이얼):** 전광판 앞에서 `E`로 해킹 다이얼(`BroadcastHackUi`)을 연 뒤, `P` 키를 눌러 일시정지 메뉴를 연다.
3. **사례 B (정지 버튼 / 대화창 / 언어 전환):** 일시정지 메뉴가 열린 상태에서 `L` 키를 누르거나, 정지 버튼 선택 창(`StopButton`)이나 엔딩 대화(`DialoguePlayer`) 진행 중 일시정지 상태와 겹칠 때 키를 입력한다.

## 기대 결과
- 일시정지 메뉴가 열려 있는 동안(`PauseMenu` 활성, `Time.timeScale == 0`)에는 일시정지 메뉴 외의 인게임 조작(해킹 다이얼 열기 및 조작, 대화 넘기기, 디버그/단축키 `L` 등)이 동작하지 않아야 한다.

## 실제 결과
- **사례 A:** `BroadcastHackUi`는 다이얼이 열릴 때 `GameFlow.InputLocked`를 `true`로 설정하지 않는다 ([BroadcastHackUi.cs:107-113](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L107-L113)). 따라서 해킹 다이얼이 열린 상태에서 `P` 키를 누르면 `PauseMenu.CanOpen`이 `true`가 되어 **해킹 다이얼 위에 일시정지 메뉴가 겹쳐서 열린다** ([PauseMenu.cs:63-70](../../Assets/Scripts/UI/PauseMenu.cs#L63-L70)).(비록 다음 프레임 `BroadcastHackUi.Update`에서 `GameFlow.InputLocked` 때문에 다이얼이 닫히지만, 만약 다이얼이 열려 있을 때 `Esc`가 아닌 `P`로 일시정지하면 다이얼이 강제로 닫혀 버린다.)
- **사례 B:** `DialoguePlayer.Update()`는 `GameFlow.InputLocked`나 `Time.timeScale == 0` 여부를 확인하지 않고 매 프레임 `L` 키 입력을 받아 일시정지 메뉴가 열린 도중에도 언어를 강제 전환하며, `UpdateRadio()`와 `UpdateTalk()`를 `Time.unscaledDeltaTime`으로 계속 진행시킨다 ([DialoguePlayer.cs:62-67](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L62-L67), [DialoguePlayer.cs:80](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L80), [DialoguePlayer.cs:98](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L98)). 그 결과 **일시정지 메뉴가 열려 있어도 라디오 자막 타이머가 멈추지 않고 흘러가 사라진다.**

## 근거 파일 및 위치
- [PauseMenu.cs:63-87](../../Assets/Scripts/UI/PauseMenu.cs#L63-L87)
- [BroadcastHackUi.cs:55-64](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L55-L64), [BroadcastHackUi.cs:107-113](../../Assets/Scripts/Broadcast/BroadcastHackUi.cs#L107-L113)
- [DialoguePlayer.cs:62-83](../../Assets/Scripts/Dialogue/DialoguePlayer.cs#L62-L83) (`Time.unscaledDeltaTime` 사용으로 일시정지 중에도 라디오 대사가 진행 및 종료됨)
