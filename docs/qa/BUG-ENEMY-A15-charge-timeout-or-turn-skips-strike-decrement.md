검증: PLAUSIBLE / 중복: BUG-ENEMY-A06 (타수 미차감 주장은 이후 DoStrike가 차감하므로 과장, 심각도는 낮음으로 봄)

# BUG-ENEMY-A15: 돌격(State.Charge)이 시간 초과되거나 플레이어가 등 뒤로 넘어가 Turn으로 전환될 때 타수(strikesLeft)가 차감되지 않고 숫자 펄스가 꺼지지 않는 문제

## 재현 단계
1. 개인 숫자가 `2` 이상인 근접 적의 첫 타격(`DoStrike`) 순간 적의 앞쪽 사거리 밖(`dx > attackRangeX`)으로 벗어나 적이 `State.Charge` 돌격을 시작하게 한다(`Assets/Scripts/Enemy/TestEnemy.cs:332-335`).
2. 돌격 중인 적을 점프·대시로 넘어가 적의 등 뒤(`cd.x * facing < 0f`)로 위치하거나, 높은 발판 위/먼 거리로 피해 돌격 최대 시간(`chargeMaxTime = 0.6초`)이 만료되게 한다.
3. 이후 적이 멈추지 않고 추가로 몇 번 더 공격하는지와 머리 위 숫자의 펄스(크기 맥동) 상태를 확인한다.

## 기대 결과
- `docs/design/GDD.md` 5장 `[확정]`(개인 숫자 = 연속 공격 횟수) 및 `docs/design/PROTOTYPE_ARCH.md:209` `[확정]`("돌격도 공격 1회(숫자 1 감소)")에 따라, 돌격이 플레이어에게 닿지 않고 `Turn`이나 시간 초과(`Recover`)로 끝나더라도 돌격 1회분 타수(`strikesLeft`)가 차감되어야 하며, 돌격 종료 시 숫자 펄스(`SetPulse(false)`)도 꺼져야 한다.

## 실제 결과
- `Assets/Scripts/Enemy/TestEnemy.cs:228-234`의 `State.Charge` 처리에서 플레이어에게 근접해 `State.Strike`(`DoStrike`)로 들어갈 때(`230-231줄`)만 `DoStrike()` 내부의 `strikesLeft--`(`317줄`)와 `number.SetPulse(false)`(`319줄`)가 실행된다.
- 반면 돌격 중 플레이어가 등 뒤로 넘어가 `EnterState(State.Turn)`으로 전환되거나(`232줄`), `t >= chargeMaxTime`으로 `EnterState(State.Recover)`로 전환될 때(`233줄`)는 `strikesLeft`를 전혀 감소시키지 않고 `number.SetPulse(false)`도 호출하지 않는다.
- 그 결과:
  1. 적이 이미 돌격 공격을 수행했음에도 타수가 줄지 않아 `Turn`/`Recover` 이후 `NextAfterChain()`(`255-259줄`)에서 또다시 `Windup -> Strike`를 수행하여 예고된 행운의 숫자보다 총 공격 횟수가 1회 이상 많아진다(거리 유지를 반복하면 한 체인 안에서 돌격을 무한정 반복할 수 있음).
  2. `EnterState(State.Turn)` 및 `EnterState(State.Recover)`에 `number.SetPulse(false)`가 없어, 돌격이 빗나간 뒤 `Turn`·`Recover`·`Open`(반격 창) 상태 내내 머리 위 숫자가 멈추지 않고 계속 펄스한다.

## 심각도
높음
