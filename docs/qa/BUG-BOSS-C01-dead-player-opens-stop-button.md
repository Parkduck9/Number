검증: CONFIRMED (중복: BUG-BOSS-A07-stopbutton-usable-while-dead-and-respawn-interrupts-ending.md)

# BUG-BOSS-C01 죽은 상태에서도 정지 버튼(엔딩 선택)을 열 수 있고, 엔딩 도중 씬이 다시 불러와진다

- 영역: 보스 (STG-05) / 엔딩
- 확인 방법: 코드 읽기 (Play 모드 미실행)

## 재현 단계
1. STG05에서 3웨이브 중 전광판을 해킹해 정지 버튼(x=26)을 연다. 3웨이브 적은 아직 살아 있다.
2. 정지 버튼 옆(2.5u 안)에서 적에게 맞아 체력이 0이 된다 (Hurt → Dead).
3. 리스폰까지 약 1.5초(`RespawnManager.respawnDelay`) 안에 E를 누른다 → 엔딩 선택창이 열린다.
4. 1 또는 2를 눌러 엔딩 대화를 시작한다.

## 기대 결과
- 죽은 상태(`PlayerHealth.IsDead` 또는 `PlayerController.State.Dead`)에서는 정지 버튼 프롬프트가 나오지 않고 E가 먹지 않는다. (일시정지 메뉴는 이미 죽은 상태에서 열리지 않게 막혀 있다: `PauseMenu.cs:63`)

## 실제 결과
- `StopButton`의 근접 판정은 거리와 `GameFlow.InputLocked`만 본다. 죽음 여부를 보지 않는다.
  - `Assets/Scripts/Level/StopButton.cs:40-44`
    ```csharp
    bool near = player != null && !GameFlow.InputLocked &&
                Vector2.Distance(player.transform.position, transform.position) <= interactRange;
    ...
    if (near && !lockedLastFrame && kb.eKey.wasPressedThisFrame) OpenChoice();
    ```
- `RespawnManager.Update`는 `InputLocked`와 상관없이 죽은 뒤 1.5초가 지나면 씬을 다시 불러온다 (`Assets/Scripts/Checkpoint/RespawnManager.cs:60-68`).
- 결과: 선택창이 열리거나 엔딩 대화가 시작된 상태에서 씬이 다시 로드되어 엔딩이 중간에 끊기고 1웨이브부터 다시 시작한다. 클라이맥스 장면이 갑자기 사라져 버그처럼 보인다.
- 같은 이유로 `BroadcastHackUi`도 죽은 상태에서 해킹 다이얼을 열 수 있다(`Assets/Scripts/Broadcast/BroadcastHackUi.cs:61-63`). 이쪽은 어차피 다시 로드되므로 영향은 거의 없다.

## 심각도
낮음 — 죽기 직전 1.5초 안에 버튼 옆에서 E를 눌러야 해서 드물다. 다만 엔딩 직전이라 걸리면 체감이 크다.

## 수정 제안 (코드 담당 참고)
- `StopButton.Start`에서 `PlayerHealth`를 같이 찾고, `near` 조건에 `&& !health.IsDead`(또는 `player.CurrentState != PlayerController.State.Dead`)를 더한다.
