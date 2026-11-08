검증: CONFIRMED (StageBuildKit.cs:248 은 kind만 설정, Stg05Builder.cs:108-112 는 lastStrikeDelay/unparryableNumber 설정 — 대조 확인)

# BUG-ENEMY-C03 — STG-02/04 레벨 빌더의 안드로이드는 kind만 바뀌고 "강력한 적" 규칙(1~7, 7번째 대시 전용, 마지막 일격 0.5초 지연)이 빠져 있음

## 재현 단계
1. `Assets/Editor/Stg04Builder.cs`로 STG-04를 만든다 (안드로이드: E-A4, E-B2, E-B4 — 41·43·45줄 `android = true`).
2. 플레이해서 안드로이드의 숫자와 연타를 본다.

## 기대 결과
- GDD 5장 `[확정]`:
  - 표(113줄): 강력한 적(안드로이드 등) 숫자 범위 **1 ~ 7**.
  - 115줄: 숫자가 **7**이면 7번째 공격은 패링 불가, Shift로만 피함.
  - 116줄: 특수 적의 마지막 공격은 **0.5초 늦게**, 숫자에 빨간 불꽃.
- 같은 적을 만드는 다른 빌더는 이 값을 넣는다: `Assets/Editor/Stg05Builder.cs:108-116` (`lastStrikeDelay 0.5`, `unparryableNumber 7`, `SetEnemyNumber(t, 1, 7, null)`), `Assets/Editor/EnemyVariantsTestSetup.cs:79-82`.

## 실제 결과
- STG-04 안드로이드는 `kind = 2`만 설정된다: `Assets/Editor/StageBuildKit.cs:246-250`.
- 그래서 숫자 범위는 프리팹 기본값 2~5 (`Assets/Scripts/Enemy/EnemyNumber.cs:16-17`), `unparryableNumber = 0`, `lastStrikeDelay = 0` (`TestEnemy.cs:37-38` 기본값) 그대로다.
- 결과: STG-04 안드로이드는 숫자가 7이 나오지 않고, 7번째 대시 전용 공격과 0.5초 늦은 마지막 일격이 **절대 일어나지 않는다**. 부채꼴 특수 공격만 다르고 나머지는 시민과 같다.

## 수정 제안
- `StageBuildKit.BuildEnemy`의 `if (f.android)` 블록에 Stg05Builder와 같은 값을 넣는다: `lastStrikeDelay 0.5`, `unparryableNumber 7`, EnemyNumber `minValue 1 / maxValue 7` (필요하면 `maxHp`도 Stg05와 맞춤). 그 뒤 STG-04(·안드로이드가 있으면 STG-02) 씬을 다시 빌드한다.
- (참고: Editor 빌더 파일은 QA 영역 밖이라 고치지 않았다.)

## 심각도
중간 — 확정 규칙 두 개(7번째 대시 전용, 마지막 일격 지연)가 STG-04에서 빠진다. STG-05 보스 웨이브에서만 보인다.
