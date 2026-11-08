검증: CONFIRMED (DialoguePlayer.cs:80,98 unscaled 시간 사용 확인, 영향 낮음)

# BUG-FLOW-C03 — 대사(DialoguePlayer)가 일시정지 중에도 진행되고, 드물게 대화 도중 입력 잠금이 풀린다

## 재현 단계
A. 라디오 자막 (항상 재현)
1. STG-00 시작 → 1.5초 뒤 라디오 대사(LINE-01)가 왼쪽 위에 뜬다.
2. 바로 Esc(또는 P)로 일시정지하고 몇 초 기다린다.
3. 계속하기.

B. 대화 도중 잠금 풀림 (타이밍 의존, 드묾)
1. STG-00에서 LINE-01이 뜬 뒤 정확히 4초(broadcastWait)가 끝나는 프레임에 Esc를 누른다.
2. 일시정지 메뉴 아래에서 LINE-02 대화 상자가 열린다.
3. 메뉴의 "Resume"을 마우스로 누르거나 Esc로 닫는다.

## 기대 결과
- 일시정지 중에는 자막·대화가 멈춘다.
- 대화(talk)가 화면에 있는 동안은 플레이어가 움직일 수 없고, 오프닝은 대화가 끝난 뒤에 다음 단계(굳음)로 간다.

## 실제 결과
- A: 라디오 자막이 일시정지 중에도 흘러가 사라진다. 다시 시작하면 대사를 놓친다. `Assets/Scripts/Dialogue/DialoguePlayer.cs:80` `radioTime += Time.unscaledDeltaTime` (일시정지는 timeScale 0만 쓴다, `Assets/Scripts/UI/PauseMenu.cs:83`).
- B: 일시정지가 열린 프레임에 `OpeningSequence`의 `WaitForSeconds`가 끝나면(같은 프레임의 코루틴은 Update 뒤에 실행됨) `Play(line02Talk)`가 대화를 큐에 넣는다 (`Assets/Scripts/Level/OpeningSequence.cs:25-28`). 다음 프레임 `DialoguePlayer.UpdateTalk()`는 일시정지를 확인하지 않고 대화를 시작한다 (`DialoguePlayer.cs:88-96`). 마우스 클릭도 그대로 대사 넘김으로 읽는다 (`DialoguePlayer.cs:103-104`).
  이어서 메뉴를 닫으면 `Close(unlock: true)`가 `GameFlow.InputLocked = false`로 만든다 (`PauseMenu.cs:95`). 대화 상자는 아직 떠 있는데:
  - 플레이어가 움직일 수 있다 (`Assets/Scripts/Player/PlayerInputReader.cs:38`이 잠금만 본다).
  - `OpeningSequence`가 `while (GameFlow.InputLocked)`(`OpeningSequence.cs:30`)를 빠져나가 대화가 끝나기 전에 굳음·출구 열림으로 넘어간다.
- 같은 위험이 `docs/design/impl/levels.md:49`(리팩터 제안 4)에 이미 적혀 있다. 코드는 아직 그대로다.
- 참고: `LudditeMeeting`(STG-03)은 트리거가 물리 단계에서 불리고 같은 프레임 `DialoguePlayer.Update`가 일시정지(실행 순서 1000)보다 먼저 잠그므로 이 경쟁이 생기지 않는다.

## 수정 제안 (참고)
- `DialoguePlayer.Update()` 맨 앞에서 `Time.timeScale == 0f`이고 일시정지 중이면 진행을 건너뛴다 (또는 PauseMenu에 `IsOpen` static을 두고 확인).
- 대화 끝을 `InputLocked` 대신 `TalkFinished` 이벤트로 알린다 (levels.md 제안 4).

## 심각도
낮음 — A는 대사를 놓치는 정도, B는 1프레임 타이밍이 필요해 드물다. 진행이 막히지는 않는다.
