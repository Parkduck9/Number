검증: CONFIRMED 동작 / 설계 판단 필요 (Facing은 Locomotion에서만 갱신, PlayerController.cs:156,212)
중복: BUG-COMBAT-A09-attack-dash-cancel-ignores-move-direction.md

# BUG-COMBAT-C06 — 공격 중 대시 취소는 지금 누르는 방향을 무시하고 공격 시작 때 방향으로만 나간다 (불확실: 설계 확인 필요)

**영역:** COMBAT (대시, Facing)
**관련 규칙:** PROTOTYPE_ARCH 2절 "Dash 방향 = 바라보는 방향, 바라보는 방향 = 마지막 0 아닌 MoveX", "Dash 입력은 공격 중 즉시 취소 가능(0.08초 이후)", 7절 추가 확정 2 돌격기(Charge) "반대쪽으로 대시해 공격 범위를 벗어나면"

## 재현 단계
1. 오른쪽 적을 바라보고 좌클릭으로 공격한다.
2. 공격 판정(0.08초) 뒤, 회복 구간에서 A(왼쪽)를 누른 채 Shift를 누른다.

## 기대 결과 (해석에 따라)
"마지막 0 아닌 MoveX"는 왼쪽이므로 왼쪽(적 반대쪽)으로 대시해 빠져나간다.

## 실제 결과 (코드 근거)
- `Facing`은 Locomotion 상태의 `UpdateLocomotion`에서만 갱신된다 (`Assets/Scripts/Player/PlayerController.cs:156`).
- Attack 상태의 대시 취소 `TryDash`(`:131`, `:183-189`)는 Facing 갱신 없이 `Facing * dashSpeed`로 나간다 (`:212`). 그래서 오른쪽(적 쪽)으로 대시해 적을 통과한다.
- 공격 직후 반대로 빠지는 동작(돌격기 유도 등)이 공격 취소로는 불가능하고, 공격이 끝날 때까지(0.30초) 기다려야 한다.

## 심각도
낮음 — 현재 문서 문구("바라보는 방향")대로라면 의도된 동작일 수도 있다. 손맛·의도 확인 필요.

## 수정 제안 (참고)
의도가 "누른 방향으로 취소 대시"라면 `TryDash` 안에서 `if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1;` 를 먼저 한다.

## 확신도
코드 동작: 높음. 버그인지 여부: 낮음~중간 (설계 판단 필요).
