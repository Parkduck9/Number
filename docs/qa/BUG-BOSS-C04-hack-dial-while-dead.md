검증: CONFIRMED (BroadcastHackUi.cs:56-63에 사망 확인 없음. C01과 같은 계열, 낮음)

# BUG-BOSS-C04 죽은 상태에서도 해킹 다이얼을 열고 확정할 수 있어, 리스폰 직전에 "정지 버튼 열림"이 떴다가 1웨이브로 되돌아간다

- 영역: 보스 (STG-05) / 해킹 UI (다른 스테이지에도 같은 코드)
- 확인 방법: 코드 읽기 (Play 모드 미실행)
- 관련: BUG-BOSS-C01은 정지 버튼(`StopButton`) 쪽이고, 이 문서는 해킹 다이얼(`BroadcastHackUi`) 쪽이다.

## 재현 단계
1. STG05 3웨이브 중 전광판 아래(전광판 기준점 (15, 3.5)에서 3u 안)에 선다.
2. 적에게 맞아 체력이 0이 된다 (Hurt → Dead). 다이얼이 이미 열려 있었거나, 이때 E를 눌러 연다.
3. 리스폰까지 약 1.5초(`RespawnManager.respawnDelay`) 안에 다이얼에서 E를 누른다.

## 기대 결과
- 죽은 상태에서는 해킹 프롬프트가 사라지고 다이얼이 닫히며 E가 먹지 않는다. (일시정지 메뉴는 죽은 상태에서 열리지 않게 막혀 있다: `Assets/Scripts/UI/PauseMenu.cs:63`)

## 실제 결과
- `BroadcastHackUi.Update`는 거리와 `GameFlow.InputLocked`만 본다. 죽음을 보지 않는다. 죽어도 `InputLocked`는 바뀌지 않는다.
  - `Assets/Scripts/Broadcast/BroadcastHackUi.cs:56-63`
    ```csharp
    if (dialBoard != null)
    {
        if (GameFlow.InputLocked || near != dialBoard) Close(); // walked away / dialogue took over
        else UpdateDial();
    }
    else if (near != null && !GameFlow.InputLocked && !lockedLastFrame)
    ```
  - 죽은 플레이어는 움직이지 않으므로(`PlayerController.cs:214-216` Dead에서 속도 0) `near != dialBoard`로 닫히지도 않는다.
- 프레임 흐름: 프레임 N 체력 0 → Hurt 끝나면 Dead → 프레임 N+k 다이얼에서 E → `BroadcastBoard.Hack()` (`BroadcastHackUi.cs:128`) → 다음 프레임 `BossFight.NewHackThisFrame()`이 새 해킹으로 보고(`BossFight.cs:59`, `:134-143`) → `Unlock()` → 전광판 "STOP BUTTON UNLOCKED", 빨간 버튼 등장 (`BossFight.cs:145-150`) → 1.5초 뒤 `RespawnManager.Update`가 씬을 다시 불러와(`RespawnManager.cs:62-68`) 1웨이브부터 다시 시작.
- 플레이어는 "이겼다"는 화면을 본 직후 처음으로 돌아가 혼란스럽다. 다른 스테이지에서도 죽은 채로 방송 숫자를 바꿀 수 있다(리스폰하면 원래대로라 영향은 작다).

## 심각도
낮음 — 리스폰하면 원래대로 돌아가고 진행이 막히지는 않는다. 다만 보스전 마지막 순간에 겪으면 승리 연출이 거짓으로 보인다.

## 수정 제안 (코드 담당 참고)
- `BroadcastHackUi`에서 플레이어 `PlayerHealth`를 한 번 찾아 두고, `health.IsDead`이면 `InputLocked`와 같게 취급한다 (다이얼 닫기, 프롬프트 숨김, 열기 막기). `PauseMenu.CanOpen`과 같은 방식. 2~3줄.
