검증: PLAUSIBLE (Charge 종료 시 SetPulse(false) 누락은 확인, 단 다음 Windup이 곧 다시 켜므로 영향은 시각적. 타수 미차감 주장은 이후 DoStrike가 차감하므로 과장 — 심각도 낮음으로 봄)

# BUG-ENEMY-A06 — 돌격(State.Charge)이 시간 만료·회전으로 종료될 때 숫자 맥동이 꺼지지 않고 타수가 차감되지 않아 반복 돌격함

## 재현 단계
1. 근접 적(`TestEnemy`, 숫자 3 이상)이 첫 타격을 휘두르는 순간 사거리(`attackRangeX = 1.8f`) 밖 앞쪽에 위치하거나 발판 위로 올라가 적의 돌격(`State.Charge`)을 유발한다.
2. 적이 `chargeMaxTime = 0.6f` 동안 플레이어에게 닿지 못해 `State.Recover`로 전환되거나, 돌격 중 플레이어가 적을 뛰어넘어 `State.Turn`으로 전환될 때 적 머리 위 숫자의 맥동(`pulsing`)과 이후 남은 공격 횟수(`strikesLeft`)를 관찰한다.

## 기대 결과
- 기획서(`docs/design/PROTOTYPE_ARCH.md:209` 추가 확정 2)에 따라:
  1. 숫자 펄스(`SetPulse(true)`)는 돌진·공격 예고 중에만 켜지고 `Turn`/`Recover` 상태에서는 꺼져야 한다.
  2. "돌격도 공격 1회(숫자 1 감소)" 규칙에 따라 돌격이 시간 만료(`chargeMaxTime`)로 끝나더라도 공격 1회가 소모되어야 한다.

## 실제 결과
1. `EnterState(State.Charge)`에서 `number.SetPulse(true)`를 켠 뒤, 돌격이 `State.Strike`가 아닌 `State.Turn`(`cd.x * facing < 0f`)이나 `State.Recover`(`t >= chargeMaxTime`)로 끝나면 `number.SetPulse(false)`가 호출되지 않아 `Turn`/`Recover` 동안에도 숫자가 계속 맥동한다.
2. 돌격이 `t >= chargeMaxTime`(0.6초) 만료로 끝날 때 `strikesLeft`를 차감하지 않고 `EnterState(State.Recover)`로 넘어가기 때문에, 직후 `NextAfterChain()` → `State.Windup` → `DoStrike()`가 다시 실행되어 허공에 타격을 휘두르고, 여전히 `strikesLeft > 0`이고 플레이어가 앞쪽 사거리 밖에 있으면 `DoStrike()`에서 또다시 `State.Charge`에 진입하여 한 체인 안에서 돌격을 여러 번 반복한다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:356-358` — `EnterState(State.Charge)`에서 `number.SetPulse(true)` 호출.
- `Assets/Scripts/Enemy/TestEnemy.cs:228-234` — `State.Charge`에서 `else if ((cd.x * facing < 0f)) EnterState(State.Turn);` 및 `else if (t >= chargeMaxTime) EnterState(State.Recover);`로 전이할 때 `number.SetPulse(false)`와 `strikesLeft--` 처리가 없음.
- `Assets/Scripts/Enemy/TestEnemy.cs:332-336` — `DoStrike()`에서 `!ranged && !playerBehind && col == null && dx > attackRangeX && strikesLeft > 0`이면 다시 `EnterState(State.Charge)`로 진입함.
