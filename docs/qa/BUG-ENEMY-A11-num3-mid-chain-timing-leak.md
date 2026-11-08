검증: CONFIRMED (TestEnemy.cs Windup가 매 프레임 broadcast==3을 읽어 연타 도중 방송이 바뀌면 간격이 즉시 변함 — OnBroadcastChanged는 숫자 재추첨만 보호. 영향 낮음~중간)

# BUG-ENEMY-A11: 방송 숫자 3(NUM-3)이 연타 도중 변경될 때 진행 중인 체인의 공격 속도와 반격 창 시간이 즉시 바뀌는 문제

## 재현 단계
1. 근접 적(개인 숫자 4~5인 시민 또는 숫자 4~7인 안드로이드)이 `State.Windup`을 마치고 첫 타격(`DoStrike`)을 실행해 연타 체인이 시작된 상태(`chainStarted == true`, `strikesLeft > 0`)를 만든다.
2. 체인이 끝나기 전에 구역 전광판을 해킹하여 방송 숫자를 `3`으로 바꾸거나, 반대로 방송 숫자 `3`에서 시작한 3연타의 마지막 타격 후 `State.Recover` 구간(`0.15초`) 동안 해킹 유지 시간이 만료되어 방송 숫자가 `3`이 아닌 값으로 바뀌게 한다.
3. 남은 연타의 예고 시간(`Windup`)과 체인 종료 시 반격 창(`State.Open`) 유지 시간을 확인한다.

## 기대 결과
- `docs/design/DECISIONS.md` 1-A 및 `docs/design/impl/enemy-rules.md:11-12`에 따라 개인 숫자와 체인 규칙은 연타 도중 바뀌지 않으며, `NUM-3` 이전에 시작된 4~7연타는 기존 연타 간격(`0.15초`)을 유지해야 한다.
- 마찬가지로 `NUM-3` 상태에서 빠른 3연타(`0.10초` 간격)를 시작한 적은 체인 도중 방송이 바뀌더라도 3타 종료 후 약속된 긴 반격 창(`1.5초`, `Num3OpenTime`)을 제공해야 한다.

## 실제 결과
- `Assets/Scripts/Enemy/TestEnemy.cs:150-157`의 `OnBroadcastChanged`는 체인 도중 숫자 재추첨만 건너뛸 뿐 `broadcast = n;`(`150줄`)은 즉시 갱신한다.
- 그런데 `State.Windup`의 연타 간격 계산(`Assets/Scripts/Enemy/TestEnemy.cs:216`: `float chainWait = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3ChainWindupTime : chainWindupTime;`)과 `EnterState(State.Open)`의 반격 창 시간 설정(`Assets/Scripts/Enemy/TestEnemy.cs:362`: `openDuration = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3OpenTime : openTime;`)은 체인 시작 시점의 규칙이 아니라 매 순간의 `broadcast` 값을 직접 참조한다.
- 이로 인해 ① 체인 시작 후 방송이 `3`으로 바뀌면 4~7연타의 남은 타격들이 전부 `0.10초`(`Num3ChainWindupTime`) 초고속 연타로 가속되고, ② `NUM-3`의 빠른 3연타를 버텼더라도 `State.Open` 진입 직전에 방송 `3`이 해제되면 반격 창이 `1.5초` 대신 기본 `1.0초`로 줄어든다.

## 심각도
중간
