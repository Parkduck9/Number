검증: PLAUSIBLE (EnemyProjectile.cs:43-56 스윕 없음; 프레임 히치 재현은 안 됨)

# BUG-COMBAT-C05 — 프레임이 끊기면 탄이 플레이어를 뚫고 지나가거나 "등 뒤" 판정이 나 패링이 실패한다

**영역:** COMBAT (EnemyProjectile 이동·패링 방향)
**관련 규칙:** PROTOTYPE_ARCH 7절 Q-I (앞쪽 180°만 패링), 2절 무적 판정 우선순위

## 재현 단계
1. 원거리 적(황금 숫자 1)의 탄이 날아오는 중에 프레임 끊김을 만든다. 예: WebGL에서 탭을 잠깐 다른 탭으로 바꿨다 돌아오기, 또는 에디터에서 Application.targetFrameRate=8 등으로 저 FPS 재현.
2. 탄을 바라보고 패링 창 안에 있는다 (또는 그냥 서 있는다).

## 기대 결과
탄은 플레이어 앞쪽에서 한 번 판정되어 패링되거나 맞는다.

## 실제 결과 (코드 근거)
- 탄은 `transform.position += velocity * Time.deltaTime` 으로 순간이동하고 그 위치에서만 겹침을 검사한다 (`Assets/Scripts/Combat/EnemyProjectile.cs:43-53`). 스윕(이동 경로) 검사가 없다.
- 탄 0.5 + 플레이어 0.9 → 겹치는 구간 폭 1.4u. 속도 9u/s(`TestEnemy.cs:42`)면 deltaTime이 약 0.155초를 넘는 한 프레임에 **완전히 통과**할 수 있다 (Unity 기본 maximumDeltaTime 0.333초 → 한 프레임 최대 3u 이동).
- 통과까지는 아니어도 탄 중심이 플레이어 중심을 지난 위치에서 처음 겹치면 `info.SourcePosition = pos` (`EnemyProjectile.cs:56`)가 등 뒤가 되어 `fromFront`가 false → 패링 창이어도 Hit (`PlayerController.cs:260-262`).

## 심각도
낮음 — 정상 60fps에서는 한 프레임 0.15u라 문제없음. WebGL 끊김(GC, 탭 전환) 때만 발생.

## 수정 제안 (참고)
- 판정용 SourcePosition을 탄의 현재 위치 대신 "이번 프레임 이동 전 위치"로 쓰거나, 아예 발사한 적 위치/진행 방향의 반대(-velocity)로 앞뒤를 판정.
- 이동 구간을 `Physics2D.BoxCast(prev, size, 0, dir, dist, playerMask)`로 검사.

## 확신도
중간 — 코드 경로는 확실, 실제 발생 빈도는 기기·FPS에 따라 다름 (미재현).
