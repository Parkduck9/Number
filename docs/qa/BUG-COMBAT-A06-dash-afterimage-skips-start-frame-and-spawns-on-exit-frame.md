검증: CONFIRMED (PlayerController.cs:137-143 잔상이 전환보다 먼저 실행; 시각적 사소함)

# BUG-COMBAT-A06: 대시 시작 프레임에 첫 잔상이 생성된 후, 이전 대시의 타이머 상태에 따라 대시 잔상 생성 횟수와 시점이 달라짐

## 심각도
**낮음**

## 위반 규칙
- **PROTOTYPE_ARCH.md 2절 / PlayerController.cs:48-50**: 대시 지속 시간 `dashDuration = 0.15f`, 잔상 간격 `afterimageInterval = 0.04f`, 잔상 수명 `afterimageLife = 0.2f`.

## 코드 근거
- `Assets/Scripts/Player/PlayerController.cs:71, 137-144, 210-213`:
  ```csharp
  case State.Dash:
      if (visual != null && now >= nextAfterimageAt)
      {
          nextAfterimageAt = now + afterimageInterval;
          SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife);
      }
      if (elapsed >= dashDuration) EnterState(State.Locomotion);
      break;
  ```
  그리고 `EnterState(State.Dash)`(`210-213`):
  ```csharp
  case State.Dash:
      rb.gravityScale = 0f;
      rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
      break;
  ```
- **문제 1 (`EnterState(State.Dash)`에서 `nextAfterimageAt`을 초기화하지 않음)**:
  - 대시 진입(`EnterState(State.Dash)`)은 `UpdateLocomotion` 또는 `State.Attack` 도중 `TryDash(now)`에서 호출된다.
  - `TryDash(now)`가 호출된 바로 그 프레임에 `switch (state)`에서 `case State.Locomotion:`(또는 `case State.Attack:`)의 `break`로 빠져나오므로, 대시가 시작된 첫 프레임에는 `case State.Dash:`가 실행되지 않는다.
  - 다음 프레임 `Update()`에서 비로소 `case State.Dash:`가 실행될 때 `now >= nextAfterimageAt`이 참이 되어(이전 대시로부터 2초 쿨다운이 지났으므로) 첫 잔상이 생성된다. 즉, **대시 시작 지점(출발 위치)에는 잔상이 남지 않고** 1프레임 이동한 뒤부터 잔상이 찍힌다.
  - 또한 만약 디버그/튜닝 등으로 쿨다운이 줄어들거나 `EnterState(State.Dash)`가 재호출될 때 `nextAfterimageAt`이 `EnterState`에서 리셋되지 않는다.
- **문제 2 (대시 종료 프레임에도 잔상이 생성됨)**:
  - `case State.Dash:`에서 `if (elapsed >= dashDuration) EnterState(State.Locomotion);` 검사보다 **잔상 생성 코드가 앞에** 위치해 있다(`138-143`).
  - 따라서 `elapsed >= 0.15f`가 되어 대시가 끝나는 마지막 프레임에도 플레이어의 현재(정지) 위치에 잔상이 하나 더 생성되어 플레이어 본체 바로 뒤에 0.2초 동안 겹쳐 남는다.

## 재현 단계
1. 플레이어로 제자리에서 Shift를 눌러 대시를 실행한다.
2. 대시 출발 지점과 대시 종료 지점의 잔상(`Afterimage`) 생성 위치를 확인한다.

## 기대 결과
대시 시작 시점부터 종료 전까지 일정한 간격(`afterimageInterval = 0.04f`)으로 이동 경로에 잔상이 남아야 하며, 대시가 끝나는 프레임에는 종료 판정이 먼저 이루어져 정지 위치에 불필요한 잔상이 겹쳐 생성되지 않아야 한다.

## 실제 결과
`EnterState(State.Dash)`에서 첫 잔상을 찍거나 `nextAfterimageAt`을 초기화하지 않아 대시 시작 지점에는 잔상이 생성되지 않고, 반대로 `case State.Dash:`에서 종료 검사(`elapsed >= dashDuration`)보다 잔상 생성이 먼저 실행되어 대시가 끝난 위치에 잔상이 추가로 생성된다.
