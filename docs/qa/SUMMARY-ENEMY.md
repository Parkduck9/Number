# SUMMARY-ENEMY — 적·방송 영역 QA 요약

범위: `Assets/Scripts/Enemy/*`, `Assets/Scripts/Broadcast/*`. 코드 추적만 했고 Unity는 실행하지 않았다. 코드·씬·프리팹은 수정하지 않았다.
발견: Claude(Opus) 2패스 = C01~C07, Antigravity 2패스 = A01~A20. 검증은 Claude가 코드를 읽어 했다 (CONFIRMED = 코드로 확인, PLAUSIBLE = 코드 일부만 확인·플레이 확인 필요).
Antigravity 심각도는 과대한 경우가 있어 아래 표에는 Claude 검증 후 심각도를 적었다 (원문 심각도는 각 파일 참고).

## 고유 버그 표 (중복 병합)
| 대표 ID | 제목 | 심각도 | 발견 | 중복 | 검증 |
|---|---|---|---|---|---|
| C01 | 근접 시 정면 탄 패링 실패 (탄 현재 위치로 앞/뒤 판정, 머즐 0.8u) | 중간 | Claude, A | A16 | CONFIRMED |
| C03 | STG-04 안드로이드가 강력한 적 규칙(1~7, 7번째 대시 전용, 0.5s 지연) 누락 | 중간 | Claude, A | A03, A20 일부 | CONFIRMED |
| C07 | NUM-7 도핑 시계가 적마다 따로 시작 (보스 웨이브 동시 탈진 불가) | 중간 | Claude, A | A08·A13 일부 | CONFIRMED |
| C05 | 기절한 적 콜라이더가 단일 OverlapBox를 가로채 다른 적 공격 무효 | 중간 | Claude, A | A09, A19 일부 | PLAUSIBLE |
| C06 | 부채꼴 3발 중 한 발만 패링되고 나머지에 피격 | 중간 | Claude | - | PLAUSIBLE |
| A07 | NUM-3 중 Staggered가 1.5초 대신 1.0초 | 낮음~중간 | Antigravity | - | CONFIRMED |
| A05 | 안드로이드 Special 후 방향·사거리 재확인 없이 연타 재개 | 중간 | Antigravity | A14 일부 | CONFIRMED |
| A17 | 사망 후에도 해킹 다이얼 조작 가능 | 낮음~중간 | Antigravity | - | CONFIRMED |
| A11 | NUM-3 연타 도중 방송이 바뀌면 간격이 즉시 변함 | 낮음~중간 | Antigravity | - | CONFIRMED |
| A04 | 안드로이드 Special 중 마지막 일격 불꽃이 켜진 채 | 낮음 | Antigravity | A14 일부 | CONFIRMED |
| C02 | 대화 잠금 중 해킹 타이머만 진행 | 낮음 | Claude, A | A10(A) | CONFIRMED |
| C04 | 충격파가 Open(NUM-3/7 반격 창)을 1초로 단축 | 낮음 | Claude, A | A08(A) | CONFIRMED |
| A12 | NUM-3→0 전환 시 굳는 시민 숫자가 재추첨됨 | 낮음 | Antigravity | - | CONFIRMED |
| A01 | 대시로 적 통과 시 Turn 대신 Recover (타이밍 의존) | 중간 | Antigravity | - | PLAUSIBLE |
| A02 | 마지막 일격 대시 회피 경직이 히트박스 겹침을 요구 (코드 주석상 의도일 수 있음, 기획 확인 필요) | 중간 | Antigravity | - | PLAUSIBLE |
| A06 | 돌격 종료 시 숫자 펄스 안 꺼짐 (타수 미차감 주장은 과장) | 낮음 | Antigravity | A15 | PLAUSIBLE |
| A10 | STG-01/02 해킹 해금·HUD 분리·언어 전환 미갱신 (일부 C02와 중복) | 중간 | Antigravity | - | PLAUSIBLE |
| A18 | 다중 전광판 HUD 펀치 누락·구역 경계 불일치 | 중간 | Antigravity | - | PLAUSIBLE |
| A20 | STG-02 AI 벌레 미배치·원거리 불꽃 (안드로이드 스탯은 C03 중복) | 중간 | Antigravity | C03 일부 | PLAUSIBLE |

집계(고유 19건, 높음 0): 중간 10 / 낮음~중간 3 / 낮음 6. CONFIRMED 12, PLAUSIBLE 7, REJECTED 0 (A19의 "비활성 적 방송 수신" 주장만 REJECTED 성격 — 비활성화하는 코드가 없음).
중복 쌍: A03=C03, A09=C05, A14=A04+A05, A15=A06, A16≈C01, A19≈C05.

## 우선 고칠 것
1. **C01** 근접 정면 탄 패링 실패 — 전투 핵심. 발사 방향(속도 부호) 기준으로 판정하면 해결.
2. **C03** STG-04 안드로이드 스탯 — Editor 빌더(`StageBuildKit.cs:248`)에 Stg05Builder 값 복사.
3. **C05** 기절 콜라이더 — 기절 시 콜라이더 끄기 또는 OverlapBoxAll.
그 다음 C07(보스 NUM-7 규칙), C06(부채꼴 패링).

## 확인했고 문제 없던 것 (Claude)
방송 변경 중 숫자 유지, 7 해킹 만료 후에도 탈진 실행, 얼어붙은 적 3타 기절, Frozen이 ForceStagger 무시, 화염 정리, 해킹 다이얼/Esc 순서, 정적 상태 초기화.
