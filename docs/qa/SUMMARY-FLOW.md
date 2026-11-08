# FLOW QA 요약 (타이틀 → STG00~05 전체 진행)

범위: Core / Level / Checkpoint / Dialogue 스크립트, 스테이지·타이틀 빌더, 빌드 설정. 정적 코드 분석(Unity 실행 없음).
Antigravity(agy) 패스: **보류** — 아직 결과 파일이 없다 (아래 참고).

| ID | 제목 | 심각도 | 검증 | 발견 |
|---|---|---|---|---|
| C01 | STG00 재시작 후 리스폰 플래그가 새 게임에 새어 나감 | 중간 | CONFIRMED (조건부) | Claude |
| C02 | STG01에서 해킹 UI 사용 가능 (STG03 이전) | 중간 | PLAUSIBLE (기획 확인) | Claude |
| C05 | 방송 번호 표시가 해킹 UI에 묶여 STG02에 없음 | 중간 | PLAUSIBLE (기획 확인) | Claude |
| C03 | 일시정지 중에도 대사가 진행됨 | 낮음 | CONFIRMED | Claude |
| C04 | 죽어가는 플레이어가 StageGoal을 발동 | 낮음 | CONFIRMED | Claude |
| C06 | 체크포인트 재진입 시 무한 회복 | 낮음 | CONFIRMED (설계 판단) | Claude |
| C07 | 스테이지 클리어 후 페이드인 없음 | 낮음 | CONFIRMED | Claude |
| C08 | 에디터 재생 중단 시 InputLocked 잔존 (에디터 한정) | 낮음 | CONFIRMED | Claude |
| C09 | Title 씬에 AudioListener 없음 | 낮음 | CONFIRMED | Claude |
| C10 | levels.md가 옛 업그레이드 훅을 설명 (문서) | 낮음 | PLAUSIBLE | Claude |

집계: 높음 0 / 중간 3 / 낮음 7. 확인됨 7, 타당함 3, 기각 0, 중복 0.
소프트락·입력 잠금 고착(게임 빌드)·씬 순서·참조 누락은 발견되지 않았다.

## Antigravity 상태
1차 실행은 로그인 요구 → 셸 권한 거부로 실패했다. 셸 금지 지시를 넣은 재실행은 FLOW가 아니라 BOSS 영역을 분석해 `BUG-BOSS-01~07`(접두사 없는 파일)을 썼고, `BUG-FLOW-A*`는 만들지 않았다. 이 7개 파일은 FLOW 소관이 아니므로 검증·수정하지 않았다(BOSS 세션이 정리할 것). 따라서 FLOW의 Antigravity 결과는 없음 — Claude 단독 결과다.
