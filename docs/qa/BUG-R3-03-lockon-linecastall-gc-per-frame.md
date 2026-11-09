작성: Claude Opus (QA 3라운드, 정적 검사)

# BUG-R3-03 조준 커서의 락온 검사가 매 프레임 배열을 새로 만듦 (WebGL GC)

- 관련: f829688 "no lock-on without line of sight"
- 근거 코드:
  - `Assets/Scripts/UI/MouseCrosshair.cs:62-63` LateUpdate마다 `player.FindLungeTarget(...)` 호출
  - `Assets/Scripts/Player/PlayerController.cs:454` 범위 안 적마다 `ClearLine` 호출
  - `Assets/Scripts/Player/PlayerController.cs:465` `Physics2D.LinecastAll(...)` = 호출마다 새 `RaycastHit2D[]` 할당

## 재현 단계
1. STG-02 SEG-B 높은 철로(E-B7·E-B8·E-B9 + 스폰 장치)처럼 적이 여럿 모인 곳에서 마우스를 적 쪽으로 둔다.
2. WebGL 빌드에서 Profiler(또는 F3 FrameStats)로 GC Alloc을 본다.

## 기대 결과
- 락온 검사는 할당 없이 돈다 (다른 검사들은 `enemyFilter` + 고정 배열 `enemyHits`, `grappleHits`로 이미 할당을 없앴다).

## 실제 결과
- 콘 안·범위 안의 적 수만큼 매 프레임 `LinecastAll` 배열이 새로 생긴다 (적 3명 × 60fps ≈ 초당 180개). WebGL은 GC가 프레임 끝에서만 돌아 주기적인 끊김(hitch) 원인이 될 수 있다.

## 심각도
낮음 (성능, 적이 몰린 장면에서만 체감)

## 참고 (수정 방향)
- `Physics2D.Linecast(from, to, groundFilter, 고정배열)` 비할당 오버로드로 바꾸고, `usedByEffector`가 아닌 것이 하나라도 있으면 false. 같은 패턴: `WallDistance`의 `BoxCastAll`(`:549`, 돌진 시작 때만이라 영향 작음), `FindTargetAhead`의 `OverlapBoxAll`(`:417`, Shift 때만).
