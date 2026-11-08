# COMBAT QA 요약

- 범위: `Assets/Scripts/Player/*`, `Assets/Scripts/Combat/*`, 히트스톱 vs 일시정지
- 방법: Claude(Opus 검수 에이전트 1회) + Antigravity(agy) 2패스. 모든 리포트는 Claude가 코드를 읽고 검증함. 플레이(Unity 실행)는 하지 않음.
- 발견자: C = Claude, A = Antigravity. 중복은 각 파일 상단 "중복:" 줄 참고.

| 파일 | 발견자 | 심각도(원본) | 검증 | 한 줄 요약 | 중복 |
|---|---|---|---|---|---|
| C01 fan-shots-unparryable | Claude | 높음 | CONFIRMED | 패링 직후 즉시 Locomotion이라 부채꼴 탄·동시 타격의 나머지가 피격 | A04 |
| A04 parry-immediately-drops-guard | Antigravity | 높음 | CONFIRMED | 위와 동일 | C01 |
| C02 attack-hits-only-one-collider | Claude | 중간 | CONFIRMED | 공격이 단일 OverlapBox라 쓰러진 적이 뒤 적을 가림 | A01 |
| A01 single-target-overlapbox | Antigravity | (중간) | CONFIRMED | 위와 동일 | C02 |
| C03 player-facing-not-shown | Claude | 중간 | CONFIRMED | 플레이어 flipX 미설정, 패링 방향이 화면에 안 보임 | A09 일부 |
| A09 attack-dash-cancel-ignores-move-direction | Antigravity | 중간 | CONFIRMED | 공격 대시 취소가 누른 방향 무시 + flipX 없음 | C06, C03 |
| C06 dash-cancel-ignores-held-direction | Claude | 낮음 | CONFIRMED (설계 확인 필요) | 위와 동일 | A09 |
| C04 inputs-lost-during-hitstop | Claude | 낮음 | CONFIRMED | 히트스톱 0.06초 동안 입력 증발 | A08 |
| A08 inputs-dropped-during-parry-hitstop | Antigravity | 높음 | CONFIRMED (낮음 권장) | 위와 동일 | C04 |
| C05 projectile-tunnels | Claude | 낮음 | PLAUSIBLE | 프레임 히치 시 탄 터널링·등 뒤 판정 | |
| A02 inputlocked-does-not-freeze-player | Antigravity | 높음 | CONFIRMED (중간 권장) | 대화 중 대시·쿨다운·무적 타이머가 계속 흐름 | |
| A10 air-dash-into-attack | Antigravity | 높음 | PLAUSIBLE | 공중 대시 직후 공격/패링에서 x속도 24 유지 가능 | |
| A11 close-range-projectile-parry | Antigravity | 높음 | PLAUSIBLE | 0.8u 이내 발사 탄이 등 뒤 판정 | C05 관련 |
| A12 pause-triggers-jump-cut | Antigravity | 중간 | CONFIRMED (케이스 A만) | 점프 중 일시정지하면 점프가 반감됨 | |
| A13 death-flow / static leak | Antigravity | 높음 | CONFIRMED 일부 (중간 권장) | 사망 넉백으로 출구에 닿으면 다음 스테이지 로드 (FLOW와 겹침) | |
| A05 nudge-visual-offset | Antigravity | 중간 | PLAUSIBLE | 대시 취소 후 0.08초 비주얼 어긋남(시각만) | |
| A06 dash-afterimage-frame | Antigravity | 낮음 | CONFIRMED | 잔상이 진입 프레임 누락·종료 프레임 중복 | |
| A07 zero-dx-knockback | Antigravity | 낮음 | CONFIRMED | dx==0이면 항상 오른쪽 넉백 | |
| A14 post-hurt-invulnerability | Antigravity | 중간 | 부분 PLAUSIBLE | 케이스 A는 설계(REJECTED), Hurt 중 점프 버퍼만 유효 | |
| A03 hitstop-unscales-while-paused | Antigravity | 높음 | REJECTED | 영구 고정 근거 없음, PauseMenu.LateUpdate가 방어 | |

## 집계 (중복 제거, 검증 후 권장 심각도)
- 고유 이슈 약 15건. CONFIRMED 10건 안팎, PLAUSIBLE 4건, REJECTED 1건(A03).
- 높음: C01/A04(패링 후 후속 탄 피격) · A13 일부는 중간으로 조정 → 사실상 높음 1건.
- 중간: C02/A01, C03/A09, A02, A12, A13, A10(PLAUSIBLE), A11(PLAUSIBLE)
- 낮음: C04/A08, C06, C05, A05, A06, A07, A14(B)

## 메모
- Claude 리뷰가 "문제 없음"으로 확인한 것: 일시정지 중 히트스톱(PauseMenu.cs:75 방어), 씬 리로드 시 timeScale(OnDisable), 대시+패링 같은 프레임(대시 우선), 충격파 정적 상태 리셋.
- Antigravity는 높음으로 매기는 경향이 있어 심각도를 일부 낮췄다. 실제 재현은 Play 모드 확인이 필요하다.
