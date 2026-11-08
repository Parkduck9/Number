검증: PLAUSIBLE (TestEnemy.cs DoStrike의 playerBehind 판정이 타격 프레임 한 시점 기준인 것은 확인. 대시 타이밍에 따른 실제 발생 빈도는 플레이 확인 필요)

# BUG-ENEMY-A01 — 대시로 적 통과 후 등 뒤 이동 시 회전(Turn) 상태 누락 및 등 뒤 반격 무효화

## 재현 단계
1. 근접 적(`TestEnemy`, 숫자 2~5) 앞에 서서 적이 첫 번째 공격 예고(`State.Windup`)를 시작할 때까지 기다린다.
2. 적의 타격(`State.Strike`) 직전에 적을 향해 `Shift` 대시를 눌러 적을 통과해 등 뒤로 넘어간다(또는 `Windup` 도중 대시로 적의 등 뒤로 이동한다).
3. 대시가 끝난 직후 적의 등 뒤에서 좌클릭(공격)을 눌러 반격하고, 이어지는 적의 다음 연타 방향을 관찰한다.

## 기대 결과
- 기획서(`docs/design/GDD.md:106, 369` `[확정]`, `docs/design/PROTOTYPE_ARCH.md:100-101` Q-B 확정)에 따라, 남은 타수가 0이 아니어도 `Shift` 대시로 적의 공격을 피해 등 뒤로 넘어가면 적은 `State.Recover` 대신 `State.Turn`(0.35초 회전) 상태가 되어야 한다.
- 적이 도는 동안 등 뒤에서 바로 공격하면 피해가 들어가야 하며, 0.35초 회전이 끝난 뒤에는 플레이어 쪽으로 방향(`facing`)을 돌려 다음 연타를 이어가야 한다.

## 실제 결과
1. 플레이어의 대시는 `0.15초` 동안 `3.6u`를 이동(`PlayerController.cs:36-37`)하는데, 적의 타격 판정 순간(`DoStrike()`)에 플레이어가 대시 중이더라도 플레이어 중심이 아직 적 중심(`transform.position.x`)을 완전히 넘기 전(`dx >= 0f`)이면 `playerBehind`가 `false`로 판정되어 적이 `State.Turn` 대신 `State.Recover`(0.15초)로 진입한다.
2. 수십 ms 뒤 대시가 끝나 플레이어가 적의 등 뒤에 위치하더라도, 적은 `State.Recover` → `State.Windup` 상태이므로 플레이어가 등 뒤를 공격하면 `ReceivePlayerHit`의 `state != State.Turn` 조건에 막혀 흰색 플래시만 뜨고 피해가 0(튕김)으로 처리된다(`Windup` 도중 미리 등 뒤로 대시한 경우에도 `state == State.Windup`이므로 동일하게 튕긴다).
3. 나아가 `State.Recover`, `NextAfterChain()`, `State.Windup` 어디에서도 플레이어가 등 뒤에 있는지 재확인하거나 `FacePlayer()`를 호출하지 않기 때문에, 적은 등 뒤에 있는 플레이어를 무시하고 원래 바라보던 빈 허공으로 다음 연타를 휘두른다.

## 심각도
높음

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:329-338` — `DoStrike()` 실행 프레임 단 한 번만 `float dx = (player.transform.position.x - transform.position.x) * facing; bool playerBehind = dx < 0f;`를 계산하여 `State.Turn` / `State.Recover`를 결정함.
- `Assets/Scripts/Enemy/TestEnemy.cs:215-225`, `255-259` — `State.Recover` 종료 후 `NextAfterChain()` → `State.Windup` 전이 과정에서 등 뒤 판정 및 `FacePlayer()` 호출이 없음.
- `Assets/Scripts/Enemy/TestEnemy.cs:391` — `ReceivePlayerHit(HitInfo info)`에서 실제 피격 방향(등 뒤 여부)을 확인하지 않고 `if (strikesLeft > 0 && state != State.Turn && state != State.Frozen)`으로만 판정하여 `Turn` 상태가 아니면 등 뒤 공격도 모두 튕겨냄.
- `Assets/Scripts/Player/PlayerController.cs:36-37, 257` — 대시는 `dashDuration = 0.15f` 동안 이동하며 전 구간 `HitResult.Dodged`를 반환하므로, 타격 프레임 시점과 대시 종료 시점의 플레이어 전후방 위치가 달라짐.
