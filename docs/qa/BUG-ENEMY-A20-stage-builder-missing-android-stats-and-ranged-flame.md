검증: PLAUSIBLE / 일부 중복: BUG-ENEMY-C03 (안드로이드 스탯 누락은 CONFIRMED). STG-02 벌레 미배치·원거리 불꽃 주장은 미검증

# BUG-ENEMY-A20: 스테이지 빌더(StageBuildKit)의 STG-04 안드로이드 핵심 스탯 누락, STG-02 AI 벌레 미배치, 원거리 적의 붉은 불꽃 오작동 문제

## 재현 단계
1. `Assets/Editor/StageBuildKit.cs:220-251`(`BuildEnemy`)로 생성되는 `STG-04`의 안드로이드(`E-A4`, `E-B2`, `E-B4`, `Assets/Editor/Stg04Builder.cs:41, 43, 45`)의 인스펙터 필드(`maxHp`, `minValue`, `maxValue`, `unparryableNumber`, `lastStrikeDelay`, 스프라이트 색상)를 `STG-05`(`Assets/Editor/Stg05Builder.cs:108-116`) 및 `Test_Enemies`(`Assets/Editor/EnemyVariantsTestSetup.cs:74-83`)의 안드로이드와 비교한다.
2. `Assets/Editor/Stg02Builder.cs:35-44`에 배치된 적 7명(`E-A1`~`E-B4`)의 `kind` 및 `ranged` 설정을 확인한다.
3. `STG-05` 또는 `Test_Enemies`에서 원거리 AI 벌레(`ranged = true`, 숫자 `1`, 황금색 `#FFC93C`)가 사격 예고(`State.Windup`)에 들어갈 때 머리 위 숫자의 색상, 불꽃 이펙트, 예고 시간을 확인한다.

## 기대 결과
- `docs/design/GDD.md` 4장·5장 `[확정]`에 따라:
  1. `STG-04`에 등장하는 안드로이드는 추가 체력(`maxHp = 5`), 숫자 범위 `1 ~ 7`, 숫자 `7`일 때 마지막 공격 패링 불가(`unparryableNumber = 7`), 마지막 공격 `0.5초` 지연(`lastStrikeDelay = 0.5f`)을 가져야 한다.
  2. `STG-02`에는 첫 등장 적 종류인 AI 벌레(`Kind.Bug`)가 배치되어야 한다(`GDD.md:90`).
  3. 원거리 적(숫자 `1`)의 홀로그램 숫자는 황금색(`#FFC93C`)을 유지해야 하며, 연속 공격의 마지막 강타 전용인 빨간 불꽃(`number-ember`)과 붉은 변색이 단발 원거리 사격마다 붙지 않아야 한다(`GDD.md:109-116, 220`).

## 실제 결과
- `Assets/Editor/StageBuildKit.cs:246-250`의 `BuildEnemy`는 `f.android == true`일 때 `SetInt(te, "kind", 2)`만 설정하고, `maxHp = 5`, `lastStrikeDelay = 0.5f`, `unparryableNumber = 7`, `EnemyNumber`의 `minValue = 1` / `maxValue = 7`, 안드로이드 외형 색상을 모두 누락했다. 그 결과 `STG-04`의 모든 안드로이드는 체력 3, 숫자 2~5, 주황색 시민 외형으로 생성되어 숫자 7 패링 불가와 마지막 공격 0.5초 지연 규칙이 전혀 작동하지 않는다.
- `StageBuildKit.Foe` 구조체(`Assets/Editor/StageBuildKit.cs:48-52`)에는 `Bug`나 `ranged`를 지정하는 필드가 아예 없으며, `Assets/Editor/Stg02Builder.cs:35-44`의 적 7명이 전원 기본값인 근접 시민(`Kind.Citizen`)으로만 생성되어 `STG-02`에 AI 벌레가 한 마리도 등장하지 않는다.
- 또한 원거리 AI 벌레(`ranged = true`, `Value = 1`)는 항상 `strikesLeft == 1`인데, `EnterState(State.Windup)`(`Assets/Scripts/Enemy/TestEnemy.cs:354`: `number.SetFlame(strikesLeft == 1);`)과 `Windup` 대기 시간(`TestEnemy.cs:219`: `if (strikesLeft == 1) wait += lastStrikeDelay;`)이 `!ranged`(또는 다연타 체인 여부)를 구분하지 않는다. 이 때문에 원거리 AI 벌레가 사격할 때마다 황금색이어야 할 숫자 `1`이 `EnemyNumber.Refresh()`(`Assets/Scripts/Enemy/EnemyNumber.cs:129`: `Color.Lerp(numberColor, Red, 0.6f)`)에 의해 붉게 변색되고 빨간 불꽃이 붙으며, `Stg05Builder.cs:103`에서 지정된 `lastStrikeDelay = 0.5f`가 기본 `windupTime = 0.5f`에 더해져 단발 사격 예고 시간이 매번 `1.0초`로 늘어난다.

## 심각도
높음
