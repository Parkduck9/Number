검증: CONFIRMED (TestEnemy.cs:110-119 는 InputLocked 동안 dopedUntil을 밀지만 BroadcastBoard.cs Update의 해킹 타이머는 그대로 진행 — 코드로 확인, 영향 낮음)

# BUG-ENEMY-C02 — 대화(InputLocked) 중에도 해킹 시간은 흐르고 적 타이머는 멈춰서 NUM-7 탈진이 사라짐

## 재현 단계
1. 전광판 구역(AI 숫자 3)에서 E → 7로 해킹한다 (유지 8초).
2. 해킹 직후 입력을 잠그는 대화(talk 모드, `DialoguePlayer.cs:94` `GameFlow.InputLocked = true`)를 2초 이상 진행한다.
3. 대화를 닫고 적을 지켜본다.

## 기대 결과
- 문서 `docs/design/impl/enemy-rules.md` 6장 5번: "8초 안에 7초가 지나면 적이 2초 동안 Open(반격 창)이 되어 바로 맞는다."
- 전광판과 적 타이머가 같은 시간을 쓴다 (`BroadcastBoard.cs:15` 주석 "Game time (Time.time) is used, so it matches enemy timers").

## 실제 결과
- 대화 중에 적은 `dopedUntil`을 계속 뒤로 미루지만 전광판은 해킹 시간을 그대로 센다. 대화가 1초 넘게 걸리면 도핑 끝(7초+대화시간)보다 해킹 만료(8초)가 먼저 와서, 방송이 3으로 돌아가며 `broadcast != 7`이 되고 **탈진이 한 번도 일어나지 않는다.**
- 역해킹 진행 바도 대화 중에 계속 차오른다 (플레이어가 조작할 수 없는 동안 해킹 시간이 소모됨). 0 해킹(5초)도 대화 중에 풀릴 수 있다.

## 원인 (코드 근거)
- 적: 잠금 중 모든 타이머를 미룬다 — `Assets/Scripts/Enemy/TestEnemy.cs:110-119` (115줄 `dopedUntil += Time.deltaTime;`, 116줄 `exhaustUntil += ...`).
- 전광판: 잠금을 보지 않는다 — `Assets/Scripts/Broadcast/BroadcastBoard.cs:60-66` `if (IsHacked && Time.time - hackedAt >= hackDuration)`, 49줄 `CounterHackProgress`도 같은 식.
- 탈진 조건은 방송이 7일 때만 — `TestEnemy.cs:169` `if (broadcast == 7 && Time.time >= dopedUntil ...)`.

## 수정 제안
- `BroadcastBoard.Update` 첫 줄에 `if (GameFlow.InputLocked) { if (IsHacked) hackedAt += Time.deltaTime; return; }` 를 넣어 적과 같은 방식으로 시간을 멈춘다.

## 심각도
낮음 — 해킹 직후 입력을 잠그는 대화가 있을 때만 일어난다. 다만 그때는 확정 규칙(1-A NUM-7 탈진)이 통째로 빠진다.
