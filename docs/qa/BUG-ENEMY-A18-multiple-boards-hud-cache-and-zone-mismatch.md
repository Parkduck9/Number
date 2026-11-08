검증: PLAUSIBLE (HUD 펀치·구역 경계 주장은 일부만 확인, 플레이 확인 필요)

# BUG-ENEMY-A18: 다중 전광판/구역 환경에서 구역 이동 시 HUD 펀치 연출 누락, 구역 밖 해킹 가능, 적과 HUD의 전광판 불일치 문제

## 재현 단계
1. **구역 이동 시 HUD 연출 누락:** `STG-02`(SEG-A `3`, SEG-B `7`) 또는 `STG-04`(SEG-A `7`, SEG-B `0`)에서 SEG-A의 전광판을 SEG-B와 같은 숫자로 해킹한 뒤 대문(`LevelTransition` Gate)을 통과해 SEG-B로 진입한다.
2. **구역 밖 전광판 해킹:** 전광판의 `zoneX` 경계선 바로 바깥(전광판과의 거리는 `interactRange = 3u` 이내이지만 `pos.x`가 `zoneX` 밖인 위치)에 서서 상단 HUD와 `[E] 해킹` 프롬프트를 확인한다.
3. **다중 전광판 씬 불일치:** `Test_Broadcast` 씬(`Assets/Editor/BroadcastTestSetup.cs:50-51`, `Board A = 3`, `Board B = 7`, 둘 다 `zoneX = (0,0)`) 또는 두 구역이 인접한 씬에서 적이 처음 생성된 위치에서 플레이어를 쫓아 다른 전광판 근처로 이동한 뒤 전광판을 해킹한다.

## 기대 결과
- `docs/design/DECISIONS.md` 1-A · 5-B 및 `docs/design/impl/enemy-rules.md:10, 29-30`에 따라, 플레이어가 새로운 구역의 전광판 범위로 진입하면 HUD 방송 숫자가 펀치 스케일 연출(및 방송 차임 훅)과 함께 갱신되어야 하며, 적·시민·HUD·해킹 프롬프트가 모두 동일한 구역 규칙(`BroadcastBoard.At`)을 일관되게 따라야 한다.

## 실제 결과
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:84-104`의 `UpdateHud`는 현재 표시 중인 `BroadcastBoard` 참조를 저장하지 않고 `hudWasShown`(`b != null`), `shownNumber`, `shownHacked`만 비교한다(`91줄`). `LevelTransition.Run`이 플레이어를 SEG-A에서 SEG-B로 한 프레임 만에 이동시키므로 `b`가 `null`이 되는 프레임이 없어 `wasShown`이 계속 `true`이고, 두 구역의 숫자와 해킹 여부가 같으면 새 구역에 진입했음에도 `punchAt` 갱신(`93줄`)이 실행되지 않는다.
- `BroadcastHackUi.Nearest(interactRange)`(`Assets/Scripts/Broadcast/BroadcastHackUi.cs:69-81`)는 `BroadcastBoard.At(pos)`(`Assets/Scripts/Broadcast/BroadcastBoard.cs:31-43`)와 달리 `b.zoneX` 범위를 확인하지 않고 거리(`<= 3u`)만 검사하므로, 플레이어가 해당 전광판의 담당 구역(`zoneX`) 바깥에 서 있어 상단 HUD에는 숫자가 뜨지 않거나 다른 구역 숫자가 떠 있는 상태에서도 그 전광판을 해킹할 수 있다.
- `TestEnemy.Start()`(`Assets/Scripts/Enemy/TestEnemy.cs:95`)는 시작 위치에서 단 한 번만 `zoneBoard = BroadcastBoard.At(transform.position)`을 고정하는 반면, `BroadcastHackUi.Update()`(`52줄`)는 매 프레임 플레이어 위치로 `BroadcastBoard.At`을 다시 계산한다. 따라서 `zoneX = (0,0)`인 전광판이 둘 이상이거나 적이 플레이어를 추격해 다른 전광판 쪽으로 이동하면, 상단 HUD에 표시되는 방송 숫자와 눈앞의 적이 적용받는 전광판이 서로 달라진다.
- 또한 `OpeningCitizen.cs`(`Assets/Scripts/Level/OpeningCitizen.cs:23, 42-46`)는 `BroadcastBoard.At`을 사용하지 않고 `BroadcastBoard.All` 전체를 구독하여 어느 구역 전광판이든 마지막으로 바뀐 숫자가 `0`인지 여부로만 동결 상태가 덮어써진다.

## 심각도
중간
