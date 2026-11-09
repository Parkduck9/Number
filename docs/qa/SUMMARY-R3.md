# QA 3차 요약 (2026-11-09, 정적 점검, Claude Opus)

> 범위: 73308be..7f1d37b — 갈고리 줄, 저격수·안드로이드 방어, 세로 맵·모두 처치 클리어·적 생성기·화살표, 아래 돌진, 겹친 배경, 타격 FX, 초상화, 조명.
> 진행 막힘(소프트락)·널 참조·도달 불가 적은 찾지 못했다. Unity Play·WebGL 실행 점검은 하지 않았다 (WebGL 빌드 자체는 같은 날 성공·타이틀/STG-00 로드 확인).

| # | 심각도 | 문제 | 근거 |
|---|---|---|---|
| [R3-01](BUG-R3-01-grapple-not-in-ingame-hints.md) | 중간 | 갈고리 줄(우클릭)이 화면 조작 힌트에 없다. 세로 맵의 핵심 이동인데 모르고 지나칠 수 있다 | ControlHints.cs:27-30 |
| [R3-02](BUG-R3-02-rmb-dropped-outside-locomotion.md) | 낮음 | 공격·돌진·패링·대시·피격 중 누른 우클릭이 버려진다 (좌클릭·점프는 버퍼됨). 버퍼할지는 기획 판단 | PlayerController.cs:248, 280 |
| [R3-03](BUG-R3-03-lockon-linecastall-gc-per-frame.md) | 낮음 | 조준점이 매 프레임 LinecastAll로 배열을 만든다 → WebGL GC 끊김 위험 | MouseCrosshair.cs:63, PlayerController.cs:465 |
| [R3-04](BUG-R3-04-radio-portrait-non-integer-scale.md) | 낮음 | 무전 초상화 칸 48px에 100px 그림 → 1080p에서 0.96배 축소로 픽셀 줄이 빠진다 | DialoguePlayer.cs:157, 179 |

## Play 모드에서 확인할 것
- 골 재확인이 `OnTriggerStay2D`에 기대는데, 플레이어 Rigidbody2D가 잠들면 안 불릴 수 있다.
- 갈고리 반동(슬링샷)이 발판 위에 안정적으로 올려 주는지.
- 가로로 걸린 줄도 "위쪽 고정"으로 쳐서 벽마다 반동이 나는데, 의도인지.
- 처치·패링 순간 확대 중 조준이 어긋나는지.
- 화살표가 SEG-A에 있을 때도 골 구간 적을 센다 (지금 맵에선 문제 안 됨).
