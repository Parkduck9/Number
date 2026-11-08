검증: CONFIRMED / 중복: BUG-ENEMY-C03-stg04-androids-miss-strong-enemy-rules.md (StageBuildKit.cs:248은 kind만 설정)

# BUG-ENEMY-A03 — STG-04 안드로이드 적에 강력한 적 규칙(숫자 1~7, 7번째 패링 불가, 마지막 일격 0.5초 지연, 추가 체력) 미적용

## 재현 단계
1. `Assets/Scenes/STG04.unity`(또는 `NUMBER/Build STG-04`로 생성된 씬)에서 안드로이드 적(`E-A4 (ANDROID)`, `E-B2 (ANDROID)`, `E-B4 (ANDROID)`)과 전투한다.
2. 안드로이드의 개인 숫자 범위, 체력(기절까지 필요한 타격 수), 마지막 일격 지연 시간, 숫자 7일 때의 패링 가능 여부를 확인한다.

## 기대 결과
- 기획서(`docs/design/GDD.md:91, 113, 115-116` `[확정]`)에 따라 안드로이드는 다음과 같이 동작해야 한다:
  1. 개인 숫자 범위가 `1 ~ 7`이어야 한다.
  2. 숫자가 `7`이면 마지막(7번째) 공격은 패링할 수 없고 반드시 `Shift` 대시로 피해야 한다(`unparryableNumber = 7`).
  3. 마지막 공격은 일반 연속 공격보다 `0.5초` 늦게 시전되어야 한다(`lastStrikeDelay = 0.5f`).
  4. 시민(3타)보다 많은 추가 체력(`maxHp = 5`)을 가져야 한다.

## 실제 결과
- `TestEnemy`는 `kind = Kind.Android`로 설정하더라도 코드 내부에서 안드로이드용 기본 수치(`maxHp`, `lastStrikeDelay`, `unparryableNumber`, `EnemyNumber`의 `minValue`/`maxValue`)를 자동으로 적용하지 않는다.
- `Stg05Builder.cs`와 `EnemyVariantsTestSetup.cs`는 안드로이드 생성 시 해당 필드들을 개별 주입하지만, 정작 안드로이드가 첫 등장하는 정규 스테이지 STG-04의 빌더(`StageBuildKit.BuildEnemy`)는 `f.android == true`일 때 `SetInt(te, "kind", 2)`만 설정하고 나머지 필드를 전혀 설정하지 않는다.
- 그 결과 STG-04의 모든 안드로이드는 체력이 시민과 같은 `3`(`maxHp = 3`)이고, 개인 숫자가 `2 ~ 5`에서만 추첨되어 `1`, `6`, `7`이 절대 나오지 않으며, `unparryableNumber = 0` 및 `lastStrikeDelay = 0f` 상태로 동작한다.

## 심각도
높음

## 근거 (파일:줄)
- `Assets/Editor/StageBuildKit.cs:220-251` — `BuildEnemy()`에서 `if (f.android)`일 때 `SetInt(te, "kind", 2)`만 호출하고 `maxHp`, `lastStrikeDelay`, `unparryableNumber`, `EnemyNumber.minValue`/`maxValue` 설정을 누락함.
- `Assets/Editor/Stg04Builder.cs:41, 43, 45, 75` — `StageBuildKit.BuildEnemy`를 호출해 STG-04의 안드로이드 3기를 생성함.
- `Assets/Scripts/Enemy/TestEnemy.cs:13, 32, 37-38` & `Assets/Scripts/Enemy/EnemyNumber.cs:16-17` — `kind = Kind.Android`여도 기본값(`maxHp = 3`, `lastStrikeDelay = 0f`, `unparryableNumber = 0`, `minValue = 2`, `maxValue = 5`)이 그대로 유지됨.
- 비교 근거: `Assets/Editor/Stg05Builder.cs:108-116`, `Assets/Editor/EnemyVariantsTestSetup.cs:74-83`에서는 `lastStrikeDelay = 0.5f`, `unparryableNumber = 7`, `maxHp = 5`, `minValue = 1`, `maxValue = 7`을 수동 설정함.
