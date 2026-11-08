# BUILD QA 요약 (WebGL 준비·잼 제출)

범위: WebGL 안전성, 빌드 설정, 디버그 잔재, 크레딧·AI 공개, 제출 체크리스트. 코드·설정은 수정하지 않았다 (읽기 전용 검증).
발견 주체: C = Claude(Opus code-reviewer), A = Antigravity CLI. 검증은 직접 코드·설정을 읽어 한 것이다.

## 이슈 표

| 보고서 | 제목 | 심각도 | 발견 | 검증 | 비고 |
|---|---|---|---|---|---|
| BUG-BUILD-C01 | AI 컨셉 이미지가 STG00 배경인데 크레딧·AI 공개에 없음 | 중간 | C | CONFIRMED | A06과 중복 일부 |
| BUG-BUILD-C02 | WebGL 예외 지원 Explicitly Thrown Only | 중간 | C | CONFIRMED (설정) / 영향 PLAUSIBLE | |
| BUG-BUILD-C03 | 고정 1920×1080 캔버스가 itch 임베드에 큼 | 중간 | C | CONFIRMED (설정) / 영향 PLAUSIBLE | A01 중복 |
| BUG-BUILD-C04 | BuildWebGL 설정이 저장소에 미적용 (Decompression Fallback 0) | 낮음 | C | CONFIRMED | A01 중복 |
| BUG-BUILD-C05 | 숨은 L키 언어 전환 | 낮음 | C | CONFIRMED | A04 중복 |
| BUG-BUILD-C06 | 해킹 다이얼·엔딩 선택이 Esc 전용 (브라우저 전체화면) | 낮음 | C | CONFIRMED | A02 중복 |
| BUG-BUILD-C07 | MCP 런타임 어셈블리가 플레이어에 포함 | 낮음 | C | PLAUSIBLE | |
| BUG-BUILD-C08 | TMP 번들 에셋(LiberationSans, EmojiOne) 미기재 | 낮음 | C | PLAUSIBLE | A06 중복 |
| BUG-BUILD-C10 | 게임 내 조작법 안내 없음 | 중간 | C | CONFIRMED | |
| BUG-BUILD-C11 | 게임패드 일부만 바인딩 | 낮음 | C | PLAUSIBLE | |
| BUG-BUILD-C12 | 미사용 Visual Scripting 패키지 | 낮음 | C | PLAUSIBLE | |
| BUG-BUILD-A01 | WebGL 설정 미적용 + 캔버스 크기 | 중간 | A | 부분 (압축 "비활성화" 주장은 오류: 0=Brotli) | C03, C04 중복 |
| BUG-BUILD-A02 | 전체화면 Esc 취소 불가 | 높음→중간~낮음 권장 | A | CONFIRMED (심각도 과대) | C06 중복 |
| BUG-BUILD-A03 | 캔버스 포커스 상실·우클릭 컨텍스트 메뉴 | 중간 | A | PLAUSIBLE | OnApplicationFocus 미구현 확인 |
| BUG-BUILD-A04 | 릴리스에 남은 L키 + Shockwave.OnGUI 프로토타입 HUD | 중간 | A | CONFIRMED | L키는 C05 중복, OnGUI는 신규 |
| BUG-BUILD-A05 | STG01 SEG-B 플레이스홀더 배경 | 낮음 | A | REJECTED | 어느 씬·프리팹도 참조 안 함, 빌드 제외 |
| BUG-BUILD-A06 | 크레딧·AI 공개·팀원 정보 누락 | 중간 | A | CONFIRMED | C01, C08 중복 |

(C09는 결번. agy 2차 패스(A07 이후)는 신규 이슈 파일 없이 응답 대기 상태로 멈춰 있어 반영하지 못했다 — Antigravity 2차는 미완료.)

## 집계 (중복 제외 기준, 고유 이슈 약 13건)
- 중간: 5 (C01, C02, C03, C10, A04의 OnGUI 포함해 A03) — 높음 0건
- 낮음: 약 7
- REJECTED 1건 (A05)

## 통과한 점검
빌드 씬 순서(Title, STG00~05 켜짐, Prototype·SampleScene 꺼짐), 스레드·System.IO·Task 사용 없음, 리플렉션 없음, 스레드 지원 꺼짐, F9는 `DEVELOPMENT_BUILD` 가드, PrototypeDebug는 비활성 씬에만 존재, Quit 버튼 WebGL에서 숨김, 압축은 Brotli, 텍스처 작음, 커서 잠금 없음, PlayerPrefs는 언어만.

## 제출 체크리스트
`SUBMISSION_CHECKLIST.md` 참고.
