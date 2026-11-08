검증: CONFIRMED / 중복: BUG-COMBAT-C06-dash-cancel-ignores-held-direction.md, BUG-COMBAT-C03-player-facing-not-shown.md (심각도 '낮음~중간')

# BUG-COMBAT-A09 — 공격 후딜레이 대시 캔슬 및 공격/패링 중 충격파 사용 시 반대 방향 입력(`MoveX`)이 무시됨

## 근거 파일 및 위치
- `Assets/Scripts/Player/PlayerController.cs:129-133, 156, 183-189, 210-213, 284-295`
- `Assets/Scripts/Player/Shockwave.cs:57-65, 78`
- `Assets/Scripts/Combat/SpriteFx.cs:85-95`

## 재현 단계
1. 오른쪽(`Facing = 1`)에 있는 적을 향해 좌클릭으로 공격(`State.Attack`)한다.
2. 공격 판정(0.08초) 직후 회복 구간(0.08~0.30초)에 적이 반격을 준비할 때, 왼쪽 이동 키(`A`, `MoveX = -1`)를 누른 상태에서 `Shift`(대시)를 눌러 공격을 대시로 캔슬한다 (`PlayerController.cs:131`).
3. 또는 오른쪽(`Facing = 1`)으로 공격/패링 중인 상태에서 왼쪽 이동 키(`A`, `MoveX = -1`)를 누르며 `Space`(충격파)를 사용한다 (`Shockwave.cs:57-65`).

## 기대 결과
- 공격 회복 구간에서 반대 방향 키(`A`/`D`)를 누른 채 대시 캔슬을 하면 현재 입력 중인 이동 방향(`input.MoveX`)으로 `Facing`이 갱신되어 그쪽으로 대시(회피)해야 한다.
- 공격/패링 중 반대 방향 키를 누른 채 충격파를 쓸 때도 의도한 방향이나 갱신된 바라보는 방향으로 발동해야 하며, 캐릭터 시각 요소(`visual.flipX`) 및 대시 잔상(`SpriteFx.Afterimage`)도 `Facing` 방향을 반영해야 한다.

## 실제 결과
1. `PlayerController.Facing`은 오직 `UpdateLocomotion()`(`PlayerController.cs:156`) 안에서만 `input.MoveX`로 갱신된다.
2. `State.Attack` 회복 구간에서 호출되는 `TryDash(now)`(`PlayerController.cs:131, 183-189`)는 `Facing`을 갱신하지 않고 곧바로 `EnterState(State.Dash)`(`210-213줄`: `rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f)`)를 실행한다. 그 결과 적에게서 멀어지려고 반대 방향 키를 누른 채 대시 캔슬을 해도 **이전 `Facing` 방향(적 쪽)으로 돌진**하게 된다.
3. `Shockwave` 역시 `Attack`·`Parry` 상태에서 발동이 허용(`Shockwave.cs:57-59`)되어 있으나 `Facing`이 `Locomotion` 복귀 전까지 고정되어 반대 방향 키를 누른 채 써도 기존 방향으로만 나가며, `PlayerController.UpdateVisual()`(`PlayerController.cs:284-295`)은 `TestEnemy.ApplyVisual()`(`TestEnemy.cs:411`)과 달리 `visual.flipX = Facing < 0`을 전혀 설정하지 않아 스프라이트 및 대시 잔상(`SpriteFx.cs:92`)의 좌우 반전이 이루어지지 않는다.

## 심각도
중간
