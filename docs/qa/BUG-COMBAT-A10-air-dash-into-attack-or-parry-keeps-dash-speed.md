검증: PLAUSIBLE (PlayerController.cs:143,204,208,220-231: 대시 종료 후 FixedUpdate 전에 Attack/Parry로 전이되면 공중에서 x속도 24 유지 가능. 프레임/물리틱 타이밍 의존, 재현 미확인; 신규 발견)

# BUG-COMBAT-A10 — 공중 대시 직후 공격/패링 진입 시 대시 속도(24 u/s)가 공격/패링 내내 유지됨

## 근거 파일 및 위치
- `Assets/Scripts/Player/PlayerController.cs:123-124, 143, 159-160, 199-213, 220-231`

## 재현 단계
1. 점프하여 공중(`!grounded`)에 뜬 상태에서 `Shift`를 눌러 대시(`State.Dash`, 속도 `dashSpeed = 24f`)한다.
2. 대시 도중 좌클릭(공격)을 미리 눌러 공격 버퍼(`attackBufferedUntil = now + 0.15f`, `PlayerController.cs:123-124`)를 걸어 두거나, 대시가 끝나는 시점(`0.15초`)에 좌클릭(공격) 또는 우클릭(패링)을 입력한다.

## 기대 결과
- 공중 대시(0.15초, 24 u/s)가 끝나고 공격(`State.Attack`, 0.30초)이나 패링(`State.Parry`, 0.35초)으로 이어질 때, 대시의 초고속 수평 속도(24 u/s)는 종료되고 일반 이동 속도(`moveSpeed = 8f` 이하)로 감속되어야 하며, 공격/패링 도중 착지하면 지상 규칙대로 수평 속도가 멈춰야 한다.

## 실제 결과
1. `State.Dash`가 끝날 때(`PlayerController.cs:143`) 호출되는 `EnterState(State.Locomotion)`(`199-200줄`)은 `rb.linearVelocity.x`를 초기화하지 않고, 다음 `FixedUpdate()`(`224-226줄`)가 실행될 때 `input.MoveX * moveSpeed`로 덮어쓰도록 의존하고 있다.
2. 그러나 50Hz 물리 틱보다 높은 일반 프레임레이트(60~144FPS)에서 `FixedUpdate()`가 끼어들기 전에 다음 `Update()`가 실행되면, `UpdateLocomotion()`(`159-160줄`)이 버퍼링된 공격(`now <= attackBufferedUntil`) 또는 공격/패링 입력을 받아 즉시 `EnterState(State.Attack)` 또는 `EnterState(State.Parry)`로 전이한다.
3. `EnterState(State.Attack)`(`204줄`)과 `EnterState(State.Parry)`(`208줄`)는 `if (grounded)`일 때만 `rb.linearVelocity.x = 0f`로 만들며, `FixedUpdate()`(`220-231줄`)에는 `State.Attack`·`State.Parry` 분기가 아예 없다.
4. 그 결과 공중 대시 직후 공격/패링으로 전이하면 **대시 속도(24 u/s, 평소의 3배)가 공격(0.30초) 또는 패링(0.35초) 시간 내내 그대로 유지**되어 최대 7~8유닛을 비정상적으로 날아가며, 공격/패링 도중 지면에 착지해도 `rb.linearVelocity.x`가 0으로 멈추지 않고 미끄러진다.

## 심각도
높음
