검증: PLAUSIBLE (boss.md:48이 6-A를 미구현이라 적었고 TestEnemy.cs에 Special/Frozen 상태가 있어 문서가 낡았을 가능성 높음. 문서만의 문제)

# BUG-BOSS-C03 boss.md 구현 기록이 실제 코드와 다르다 (안드로이드 부채꼴 탄, 방송 0 굳음)

- 영역: 보스 (STG-05) / 문서
- 확인 방법: 문서와 코드 대조

## 재현 단계
1. `docs/design/impl/boss.md` 4장을 읽는다.
2. `Assets/Scripts/Enemy/TestEnemy.cs`와 STG05 안드로이드 템플릿 설정(`Assets/Editor/Stg05Builder.cs:108-116`)을 본다.

## 기대 결과
- 구현 기록이 현재 코드 상태와 같다. (다른 사람이 문서를 보고 밸런스·작업 범위를 판단한다.)

## 실제 결과
- `boss.md:48`: "결정 6-A(황금 탄 3발)는 enemy-rules 세션 몫이라 아직 들어 있지 않다."
  - 실제: 안드로이드 특수 공격은 이미 들어 있다. 빌더가 `kind = 2`(Android)를 넣고(`Stg05Builder.cs:110`), `TestEnemy`가 Android일 때 `Special` 상태로 황금 탄 부채꼴을 쏜다 (`TestEnemy.cs:257`, `:262-273`). 즉 보스 3웨이브 안드로이드는 부채꼴을 쏜다.
  - 또 결정 번호가 틀렸다: 안드로이드 특수 공격은 DECISIONS.md **7번**(7-A)이고 6번은 STG-00 오프닝이다.
- `boss.md:51`: "방송 0의 적 효과(시민 굳음)는 enemy-rules 세션이 `BroadcastBoard` 리스너로 넣는다."
  - 실제: 이미 구현돼 있다 (`TestEnemy.cs:160-162`, `NumberChanged` 구독 `TestEnemy.cs:95-99`). 3웨이브 시민 3명은 땅에 떨어지자마자 굳는다(`State.Frozen`).

## 심각도
낮음 — 게임 동작 문제는 아니다. 문서만 고치면 된다. (3웨이브 시민이 처음부터 굳어 있어 사실상 무저항이라는 점은 기획 판단 사항으로 함께 알려 둔다. 버그로 보지는 않음.)
