검증: CONFIRMED (OnBroadcastChanged에서 NUM-3 해제 재추첨 블록이 Frozen 진입보다 먼저 실행 — 코드 확인. 굳은 동안 숫자만 바뀌는 사소한 문제, 심각도 낮음)

# BUG-ENEMY-A12: 방송 숫자가 3에서 0(NUM-0)으로 바뀔 때 굳는 시민의 머리 위 숫자가 즉시 무작위로 재추첨되는 문제

## 재현 단계
1. 방송 숫자가 `3`인 구역(예: `STG-01` SEG-B 또는 `Test_Enemies` 씬)에서 아직 체인을 시작하지 않은(`state == State.Approach && !chainStarted`) 시민 적(`kind == Kind.Citizen`, 머리 위 숫자 `3`) 앞에 선다.
2. 전광판에서 `E` 키로 해킹 다이얼을 열고 방송 숫자를 `0`으로 바꾼다(`old = 3, n = 0`).
3. 시민이 푸른 회색으로 굳는(`State.Frozen`) 순간 머리 위 숫자를 확인하고, 5초 뒤 역해킹이 완료되어 방송이 다시 `3`으로 돌아올 때 숫자를 확인한다.

## 기대 결과
- `docs/design/GDD.md` 5장 `[확정]`("행운의 숫자는 바뀌지 않는다") 및 `docs/design/DECISIONS.md` 1-A / `docs/design/impl/enemy-rules.md:17`에 따라, 방송이 `0`이 되면 시민은 즉시 숨을 멈추고 굳기만 해야 하며 머리 위 숫자는 `0`이 해제될 때(`old == 0`) 재추첨되어야 한다.

## 실제 결과
- `Assets/Scripts/Enemy/TestEnemy.cs:152-162`의 `OnBroadcastChanged`에서 `NUM-3` 해제 시의 즉시 재추첨 블록(`153-157줄`)이 `NUM-0` 동결 상태 진입(`161줄: if (n == 0 && kind == Kind.Citizen) EnterState(State.Frozen);`)보다 먼저 실행된다.
- 방송이 `3 -> 0`으로 바뀔 때 `153줄` 시점에는 아직 `state`가 `State.Approach`이고 `old == BroadcastRules.Num3Number`(`3`)이므로 `number.Reroll()`(`155줄`)이 먼저 호출된 뒤 `EnterState(State.Frozen)`(`161줄`)으로 들어간다.
- 그 결과 시민이 `0` 방송에 의해 숨을 멈추고 굳는 순간 머리 위 숫자가 `3`에서 `2~5` 무작위 값으로 즉시 바뀌며, 5초 뒤 `0 -> 3`으로 복귀할 때 `162줄`(`FinishReset()`)에서 다시 `3`으로 재추첨되어 숫자가 두 번 바뀐다.

## 심각도
낮음
