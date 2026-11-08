검증: CONFIRMED 케이스 A만 (PlayerController.cs:150,173-181에 InputLocked/정지 가드 없음, 일시정지 시 JumpHeld=false로 점프 반감; 신규). 케이스 B는 공격/패링 우선 설계라 REJECTED

# BUG-COMBAT-A12 — 점프 상승 중 일시정지/대화 잠금 시 강제 점프 컷(`jumpCut`) 발생 및 지상 점프+공격/패링 동시 입력 시 점프 누락

## 근거 파일 및 위치
- `Assets/Scripts/Player/PlayerController.cs:101-151, 158-181, 204, 208`
- `Assets/Scripts/Player/PlayerInputReader.cs:38-43`
- `Assets/Scripts/UI/PauseMenu.cs:79-86`

## 재현 단계
1. **케이스 A (일시정지/대화 진입 시 강제 점프 컷):**
   - `W` 키를 길게 누른 채 최대 높이 점프를 시작하여 상승 중(`rb.linearVelocity.y > 0.1f`)일 때 `Esc` 또는 `P`를 눌러 일시정지 메뉴(`PauseMenu`)를 열거나 대화/구간 전환 트리거(`GameFlow.InputLocked = true`)에 진입한다.
   - `W` 키를 계속 누른 상태에서 일시정지를 해제한다.
2. **케이스 B (지상에서 점프+공격 또는 점프+패링 동시/연속 프레임 입력):**
   - 지상에서 `W`(점프)와 좌클릭(공격) 또는 우클릭(패링)을 같은 프레임에 함께 누르거나(점프 공격 시도), 1프레임 먼저 `W`를 누른 직후 다음 렌더링 프레임(`FixedUpdate` 물리 이동 전)에 좌클릭/우클릭을 누른다.

## 기대 결과
- 일시정지나 대화 잠금(`GameFlow.InputLocked`) 때문에 상승 중인 점프 속도가 임의로 반감(`* 0.5f`)되지 않아야 한다.
- 지상에서 점프와 공격/패링을 동시에 누르거나 직후 프레임에 누를 때 공중 점프 공격이 정상적으로 수행되고 수평 이동 관성이 유지되어야 한다 ([PROTOTYPE_ARCH.md](../design/PROTOTYPE_ARCH.md) 2절·7절 Q-H: "공중 공격·패링·대시 모두 가능", "Attack: 지상이면 x속도 0, 공중이면 유지").

## 실제 결과
1. **일시정지/대화 잠금 중 강제 점프 컷:**
   - `PauseMenu.Open()`(`PauseMenu.cs:81-82`)이나 대화 시작 시 `GameFlow.InputLocked = true`가 되면 `PlayerInputReader.Update()`(`PlayerInputReader.cs:38-43`)가 `JumpHeld = false`로 강제 초기화한다.
   - `PlayerController.Update()`(`PlayerController.cs:101-151`)에는 `Time.timeScale == 0f`나 `GameFlow.InputLocked` 가드가 없기 때문에 일시정지 상태에서도 매 프레임 `UpdateJumpCut()`(`150, 173-181줄`)을 호출한다.
   - 그 결과 플레이어가 `W`를 떼지 않았는데도 일시정지한 순간 `!input.JumpHeld`로 판정되어 **`rb.linearVelocity.y`가 절반으로 깎이고 `jumpCut = true`로 고정**되어 일시정지 해제 후 높은 발판에 올라갈 수 없게 된다.
2. **점프+공격/패링 동시·연속 입력 시 점프 증발 및 수평 속도 소실:**
   - `UpdateLocomotion()`(`PlayerController.cs:158-169`)은 `ParryPressed`(`159줄`)와 `AttackPressed || now <= attackBufferedUntil`(`160줄`)을 점프 판정(`163줄`)보다 먼저 검사하고 즉시 `return`한다.
   - 공격 지속 시간(`0.30초`)과 패링 지속 시간(`0.35초`) 모두 점프 버퍼(`jumpBuffer = 0.10초`)보다 길기 때문에, 지상에서 점프와 공격/패링을 동시에 누르면 점프가 실행되지도 않고 `Locomotion` 복귀 전에 `jumpBufferedUntil`이 만료되어 점프가 완전히 씹힌다.
   - 또한 1프레임 먼저 점프(`rb.linearVelocity.y = 14f`, `165줄`)를 누르고 `FixedUpdate()`가 물리 위치를 올리기 전 다음 `Update()` 프레임에 공격/패링을 누르면, `grounded`가 아직 `true`(`116-119줄`)로 남아 있어 `EnterState(State.Attack/Parry)`(`204, 208줄`)가 점프 공격 중인데도 `rb.linearVelocity.x = 0f`로 수평 속도를 죽여 버린다.

## 심각도
중간
