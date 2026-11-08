검증: PLAUSIBLE (dashedThrough가 col!=null일 때만 설정되는 것은 확인. 다만 코드 주석이 '실제 대시 통과만 경직'을 의도로 적어 둠 — 기획 의도와 맞는지 사용자 판단 필요)

# BUG-ENEMY-A02 — 마지막 일격을 대시로 회피할 때 타격 프레임에 전방 히트박스와 겹쳐 있어야만 1초 경직(Staggered) 발동

## 재현 단계
1. 적(`TestEnemy`)의 연속 공격 중 마지막 일격(`strikesLeft == 1`, 숫자에 빨간 불꽃 예고 표시)이 시작될 때까지 기다린다.
2. 불꽃 예고(`State.Windup`, 일반 적 0.15~0.5초 / 안드로이드 등 특수 적 0.65~1.0초)를 보고 `Shift` 대시를 눌러 적의 뒤쪽(`dx < -0.35u`)이나 앞쪽 사거리 밖(`dx > +2.35u`)으로 회피한다.
3. 적의 마지막 타격(`DoStrike()`) 직후 적이 `State.Staggered`(점멸하는 1초 경직)에 들어가는지 확인한다.

## 기대 결과
- 기획서(`docs/design/GDD.md:115, 369` `[확정]`, `docs/design/PROTOTYPE_ARCH.md:99, 180` Q-E 확정)에 따라, 적의 마지막 일격(특히 패링이 불가능해 반드시 `Shift` 대시로 피해야 하는 7번째 일격 포함)을 대시로 피하면 적이 1초 동안 경직(`State.Staggered`)되어야 한다.

## 실제 결과
- `DoStrike()`가 실행되는 단일 프레임에 플레이어의 콜라이더가 적의 전방 히트박스(`attackOffset.x = 1.0f`, `attackSize.x = 1.8f`, 즉 적 앞쪽 `[+0.1u, +1.9u]`) 안에 겹쳐 있으면서 동시에 그 프레임에 아직 `PlayerController.State.Dash`(0.15초)가 끝나지 않은 상태여야만 `dashedThrough = true`가 된다.
- 플레이어가 마지막 일격의 불꽃 예고(특수 적은 `lastStrikeDelay = 0.5f`가 추가되어 총 `0.65초` 예고)를 보고 대시(`0.15초`, 이동 거리 `3.6u`)로 적을 통과해 이미 등 뒤로 빠져나갔거나 뒤로 거리를 벌려 히트박스를 벗어나면, `DoStrike()` 시점에는 `col == null`이거나 이미 대시가 끝난 상태가 되어 `dashedThrough == false`가 된다.
- 그 결과 마지막 일격을 대시로 정상 회피했음에도 `State.Staggered`가 발동하지 않고 `State.Turn`(0.35초) 또는 `State.Recover`(0.15초) 후 `State.Open`으로 넘어간다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:23-24, 303-315` — `DoStrike()`에서 `col = Physics2D.OverlapBox(center, attackSize, 0f, playerMask)`가 검출된 경우에만 `dashedThrough = pc.CurrentState == PlayerController.State.Dash;`를 설정함.
- `Assets/Scripts/Enemy/TestEnemy.cs:323-327` — `if (special && dashedThrough && result == HitResult.Dodged)` 조건으로만 `EnterState(State.Staggered)`를 호출함.
- `Assets/Scripts/Enemy/TestEnemy.cs:216-220` & `Assets/Scripts/Player/PlayerController.cs:36-37` — 마지막 일격의 `Windup` 시간(특수 적 기준 `0.65초`~`1.0초`)이 대시 지속 시간(`0.15초`)보다 훨씬 길어, 예고를 보고 대시하면 타격 프레임 이전에 히트박스 영역(`[-0.35u, +2.35u]`)을 벗어나거나 대시 상태가 종료됨.
