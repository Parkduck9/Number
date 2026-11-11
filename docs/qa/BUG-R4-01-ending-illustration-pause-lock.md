작성: Codex (QA 4차, 정적 점검 — Antigravity 사용량 소진으로 대체)

# BUG-R4-01 엔딩 일러스트 뒤에 일시정지가 열리고, 정지 중에도 타이틀로 돌아감

- 기준: `25a974e` / 도입: `e11351f`
- 확인 방법: 정적 호출·상태 전이 추적. 아래 Play 재현은 실행하지 않음.

## 재현 단계
1. STG-05 보스를 처치하고 기계 소품을 모두 부순다.
2. 엔딩 대사의 마지막 줄을 넘긴다.
3. 일러스트가 나타나기 시작하는 1초 페이드 인 도중 P를 누른다. WebGL에서는 이때 다른 탭으로 전환하는 경우도 확인한다.
4. 아무 입력 없이 기다린다.

## 기대 결과
엔딩 연출이 입력 잠금을 이어받아 일시정지 메뉴가 열리지 않거나, 일시정지를 지원한다면 메뉴가 보이고 연출 시간도 멈춘다.

## 실제 결과
대화가 해제한 `InputLocked`를 일러스트 시작 전에 다시 설정하지 않는다. P로 일시정지를 열 수 있지만, 메뉴의 Canvas 순서 200보다 일러스트의 300이 높아 메뉴가 그림 뒤에 가려진다. 그림은 `unscaledDeltaTime`으로 계속 재생되어 정지 중에도 타이틀로 이동한다. WebGL 포커스 이탈도 같은 숨은 일시정지를 열 수 있다.

## 심각도
중간 — 마지막 연출에서 일시정지 상태와 보이는 화면이 어긋나고, 사용자가 정지했다고 생각하는 동안 엔딩이 종료된다. 영구 소프트락으로 판정한 것은 아니다.

## 코드 근거
- `Assets/Scripts/Dialogue/DialoguePlayer.cs:143–145`: 마지막 대사를 닫으며 `GameFlow.InputLocked = false`.
- `Assets/Scripts/Boss/BossFight.cs:1178–1184`: 잠금 해제를 기다린 뒤 곧바로 `EndingIllustration()` 호출, 완료 후 타이틀 로드. 사이에 잠금 획득이 없음.
- `Assets/Scripts/Boss/BossFight.cs:1192`: 일러스트 Canvas 순서 300.
- `Assets/Scripts/Boss/BossFight.cs:1199–1208`: 페이드·유지 시간이 모두 `Time.unscaledDeltaTime`; PauseMenu 상태를 확인하지 않음.
- `Assets/Scripts/UI/PauseMenu.cs:68–82`: 잠금이 풀린 살아 있는 플레이어가 있으면 P/Esc 및 WebGL 포커스 이탈로 메뉴를 열 수 있음.
- `Assets/Scripts/UI/PauseMenu.cs:129`: 일시정지 Canvas 순서 200.

## 수정 힌트 (Sonnet 구현 → Opus 검수)
대화 종료를 확인한 다음 일러스트 호출 직전에 엔딩의 `InputLocked`/`ownLock`을 다시 획득하고, 기존 타이틀 복귀·OnDestroy 해제 경로를 사용한다. 그림 누락 분기에서도 잠금이 남지 않는지 확인한다. 스킵 키 검사는 입력 잠금과 무관한 원시 입력이라 유지할 수 있다.

수정: BossFight.Ending()이 대사 종료 후 일러스트 직전에 InputLocked/ownLock을 다시 잡아 타이틀 로드 때까지 유지 (일시정지 차단).
