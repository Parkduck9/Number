검증: CONFIRMED (PlayerController.cs:103-107 early return, Shockwave.cs:49)
중복: BUG-COMBAT-A08-inputs-dropped-during-parry-hitstop.md

# BUG-COMBAT-C04 — 패링 히트스톱(약 0.06초) 동안 누른 공격·대시·점프·충격파 입력이 사라진다

**영역:** COMBAT (히트스톱, 입력 버퍼)
**관련 규칙:** PROTOTYPE_ARCH 2절 "공격 버퍼 0.15초 / 점프 버퍼 0.10초", GDD 5장 `[확정]` 패링·대시 후 바로 반격하는 흐름

## 재현 단계
1. 근접 적의 공격을 패링한다 (히트스톱 시작).
2. 패링 성공과 거의 동시에(0.06초 안) 좌클릭(공격) 또는 Shift(대시) 또는 W(점프)를 누른다.
3. 히트스톱이 끝난 뒤 행동이 나오는지 본다.

## 기대 결과
누른 입력이 버퍼에 남아 히트스톱이 끝나자마자 실행된다 (공격 버퍼 0.15초, 점프 버퍼 0.10초).

## 실제 결과 (코드 근거)
- `PlayerInputReader`는 매 프레임 `WasPressedThisFrame`을 그 프레임에만 true로 둔다 (`Assets/Scripts/Player/PlayerInputReader.cs:45-50`).
- 히트스톱 중 `PlayerController.Update`는 버퍼 갱신(`:122-124`)보다 **먼저** return 한다 (`Assets/Scripts/Player/PlayerController.cs:103-107`). 그래서 이 3~4프레임 동안의 눌림은 버퍼에도 안 남고 그냥 버려진다.
- `Shockwave.Update`도 `Time.timeScale == 0`이면 return 해서 Space 입력을 버린다 (`Assets/Scripts/Player/Shockwave.cs:49`).
- 특히 대시는 버퍼가 없으므로, 패링 직후 다음 타격을 피하려고 바로 누른 Shift가 무시될 수 있다.

## 심각도
낮음 — 0.06초 창이라 자주는 아니지만 "눌렀는데 안 나감" 손맛 문제.

## 수정 제안 (참고)
히트스톱 분기 안에서도 `if (input.JumpPressed) jumpBufferedUntil = ...; if (input.AttackPressed) attackBufferedUntil = ...;` 를 갱신하거나, 히트스톱 중 눌린 Dash/Parry를 bool로 기억했다가 끝날 때 처리한다. 버퍼 시각은 Time.time이 멈춰 있으므로 그대로 써도 히트스톱 뒤 유효하다.

## 확신도
높음.
