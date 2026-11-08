# BOSS 영역 QA 요약

범위: `Assets/Scripts/Boss/*`, Stg05Builder, STG05.unity, StopButton·엔딩, UI(Title/Pause/Loc). 코드만 읽고 판정했으며 Unity는 실행하지 않았다.
찾은 사람: **C** = Claude(Opus 리뷰어 2회), **A** = Antigravity(2회). 검증은 이 세션이 직접 코드를 읽어 했다.
참고: A06, A07 번호가 A 1차·2차 파일에 겹쳐 있어 파일명 전체로 구분한다(삭제·개명 안 함).

## 한눈에
- 높음 0건, 중간 약 2건, 낮음 위주. soft-lock·웨이브 미클리어·엔딩 후 타이틀 복귀 실패는 못 찾았다.
- 중복을 합친 고유 버그: 확정 6건, 추정 5건, 기각 4건.

## 고유 버그 표

| # | 제목 | 심각도(검증 후) | 판정 | 찾은 사람 | 파일 |
|---|---|---|---|---|---|
| 1 | 3웨이브 예고(Announce) 중 해킹하면 적이 소환되지 않고 정지 버튼이 열림 (기획 판단 필요) | 중간 | CONFIRMED | C, A | C05, A10(A부분) |
| 2 | 스폰 안전 반경이 2D 거리라 영구 불발 (스폰 y=14) | 낮음~중간 | CONFIRMED | C, A | C06, A12(A부분) |
| 3 | 죽은 플레이어도 정지 버튼 선택/엔딩 시작, 1.5초 뒤 리로드로 끊김 | 낮음 | CONFIRMED | C, A | C01, A07-stopbutton, A06-death |
| 4 | 죽은 상태에서 해킹 다이얼을 열고 확정 가능 | 낮음 | CONFIRMED | C, A | C04, A06-death |
| 5 | 언어 전환 시 코드로 만든 UI(해킹/정지 버튼/전광판)와 표시 중 대사가 갱신 안 됨 | 낮음 | CONFIRMED | C, A | C02, A03, A11 |
| 6 | 해킹 다이얼 중 P로 일시정지하면 다이얼이 닫히고, 라디오 자막이 일시정지 중에도 흘러감 | 낮음 | CONFIRMED | A | A02, A08(B) |
| 7 | boss.md 구현 기록이 코드와 다름 (안드로이드 황금 탄, NUM-0 굳음, 번호 6-A/7-A) | 낮음(문서) | PLAUSIBLE | C | C03 |
| 8 | STG05 단독 시작 시 충격파 잠김 (정상 진행은 STG-03에서 해금) | 낮음(개발·QA) | PLAUSIBLE | A | A06-shockwave, A09(B) |
| 9 | 엔딩 선택창/대화 중 BroadcastBoard 역해킹 타이머가 멈추지 않음 | 낮음 | PLAUSIBLE | A | A07-counter(B) |
| 10 | 충격파가 Open(반격 창) 상태 적의 무방비 시간을 1초로 줄임 | 낮음 | PLAUSIBLE | A | A09(B) |
| 11 | Esc가 near!=dialBoard 프레임에 EscConsumed를 못 남겨 일시정지가 같이 열릴 수 있음 (재현 경로 약함) | 낮음 | PLAUSIBLE | A | A08(A) |
| 12 | 배경 클릭 시 EventSystem 선택이 풀려 키보드 메뉴 조작 불가 (WebGL 실측 필요) | 낮음 | PLAUSIBLE | C | C07 |

## 기각 (REJECTED)
| 파일 | 이유 |
|---|---|
| A01 | PauseMenu의 lockedAtFrameStart가 같은 프레임 오픈을 막음 (PauseMenu.cs:63,76) |
| A04 | 다이얼 확정 키는 E, Enter 공격은 의도된 바인딩, 정지 버튼은 전광판과 범위가 겹치지 않음 |
| A05 | 해킹 중 AI 숫자 변경 지연과 3웨이브 시작 후 해킹 해금은 boss.md:17,65에 문서화된 의도 (Announce 중 해킹은 #1로 분리) |
| A07-counter A, A12 B | 일시정지 중 히트스톱은 LateUpdate가 되돌림. 정적 초기화 누락은 에디터 Domain Reload 비활성 한정 |

## 확인했지만 문제 없음
웨이브 클리어(기절·null·동결 처리), 마지막 웨이브 해킹 해금(같은 숫자 0, 재해킹 포함), 엔딩 두 번 선택 불가, 두 엔딩 모두 타이틀 복귀, 언어 설정 저장, 빌드 순서(Title 첫째, STG05 마지막), 장면 리로드 시 InputLocked/timeScale 복구.

## 우선 처리 제안
1. #1 기획 확정(Announce 중 해킹 인정 여부) 후 수정, #2 x거리만 비교하도록 수정.
2. #3·#4 사망 확인을 `near` 조건에 추가 (한 줄씩).
3. #5는 LocalizedText 사용, #7 문서 갱신.
