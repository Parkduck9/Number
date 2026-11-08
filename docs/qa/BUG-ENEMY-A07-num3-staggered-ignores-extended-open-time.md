검증: CONFIRMED (EnterState는 Staggered에도 openDuration=1.5를 계산하지만 UpdateState Staggered는 staggerTime 사용 — TestEnemy.cs 대조 확인. 심각도는 낮음~중간)

# BUG-ENEMY-A07 — NUM-3 방송 중 마지막 일격을 대시로 회피해 경직(Staggered)시키면 반격 창이 1.5초 대신 1.0초로 단축됨

## 재현 단계
1. 방송 숫자가 `3`(NUM-3)인 구역(예: STG-01 SEG-B, STG-02 SEG-A)에서 근접 적의 3연타 중 마지막 3번째 타격을 `Shift` 대시로 회피해 적을 `State.Staggered` 상태로 만든다.
2. 적이 반격 허용 상태(`State.Staggered`)에 머무는 시간을 측정하여, 3번째 타격을 패링하거나 맞았을 때의 `State.Open` 유지 시간(1.5초)과 비교한다.

## 기대 결과
- 확정 결정(`docs/design/DECISIONS.md:24` 1-A 확정, `docs/design/impl/enemy-rules.md:11`)에 따라, NUM-3 방송 중에는 근접 적의 3연타 뒤 반격 창이 기본 `1.0초`에서 `1.5초`(`BroadcastRules.Num3OpenTime`)로 길어져야 한다.

## 실제 결과
- `EnterState()`에서는 `case State.Open:`과 `case State.Staggered:`를 함께 묶어 `openDuration = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3OpenTime : openTime;`(1.5초)을 계산한다.
- 그러나 정작 `UpdateState()`의 `case State.Staggered:`에서는 방금 계산한 `openDuration`(1.5초) 대신 고정 필드 `staggerTime`(1.0초)을 검사(`if (t >= staggerTime) FinishReset();`)한다.
- 그 결과 NUM-3 방송 중 3타째를 그냥 맞거나 패링하면 `State.Open`으로 1.5초의 반격 창이 주어지지만, 마지막 일격을 대시로 회피해 `State.Staggered`를 띄우면 오히려 반격 창이 1.0초로 짧아진다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:359-363`:
  ```csharp
  case State.Open:
  case State.Staggered:
      number.SetFlame(false);
      openDuration = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3OpenTime : openTime;
      break;
  ```
- `Assets/Scripts/Enemy/TestEnemy.cs:238-243`:
  ```csharp
  case State.Staggered:
      if (t >= staggerTime) FinishReset();
      break;
  case State.Open:
      if (t >= openDuration) FinishReset();
      break;
  ```
- `Assets/Scripts/Broadcast/BroadcastRules.cs:7` — `public static float Num3OpenTime = 1.5f;`
