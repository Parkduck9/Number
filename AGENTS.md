# AGENTS.md — AI 작업 규칙 (모든 AI 공통)

이 저장소에서 작업하는 모든 AI(Claude, Codex, Antigravity)는 작업 전에 이 파일을 읽는다.

## 프로젝트
- Unity Game Jam 2026 출품작. 테마: **행운의 숫자 (Lucky Number)**
- 엔진: **Unity 6000.3.25f1** (다른 버전으로 프로젝트를 열거나 업그레이드하지 말 것)
- 타깃: **WebGL** (itch.io 브라우저 플레이). WebGL에서 안 되는 기능(스레드, 파일 시스템 직접 접근 등)은 쓰지 말 것.
- 개발 마감: **2026-11-13 00:00 KST**, 제출 마감: **2026-11-14 21:00 KST**
- 규칙 전체: [JAM_RULES.md](JAM_RULES.md) — 특히 금지 콘텐츠(차별·혐오, 술·담배 홍보)와 에셋 라이선스 조건을 지킬 것.
- 게임 내 텍스트는 영어 (가능하면 한국어도 함께).

## 역할 분담

| 역할 | 담당 | 하는 일 | 주로 건드리는 곳 |
|---|---|---|---|
| 코딩 | **Claude Sonnet** (Claude Code) | 게임 로직, 시스템, UI 코드 구현 | `Assets/Scripts/`, 씬, 프리팹 |
| 코드 검수 | **Claude Opus** (Claude Code) | Sonnet이 작성한 변경사항 리뷰, 버그·구조 문제 지적·수정 | 리뷰 대상 파일 (필요할 때만 수정) |
| 에셋 | **Codex CLI** (OpenAI) | 스프라이트, 사운드, 머티리얼·셰이더, 폰트, 에셋 임포트 설정, 데이터(ScriptableObject 등) | `Assets/Art/`, `Assets/Audio/`, `Assets/Materials/`, `Assets/Data/` |
| QA | **Antigravity CLI** (Google) | 플레이 테스트 시나리오, 버그 찾기·재현, 테스트 코드, 빌드 점검, 제출 체크리스트 확인 | `Assets/Tests/`, `docs/qa/` |

- **자기 영역 밖의 파일은 고치지 않는다.** 다른 영역에 수정이 필요하면 고치지 말고 기록으로 남긴다.
  - QA(Antigravity)는 게임 코드를 직접 고치지 않고 `docs/qa/`에 버그 리포트를 쓴다. Sonnet이 고치고 Opus가 검수한다.
  - 에셋(Codex)은 스크립트를 고치지 않는다. 코드 연결이 필요하면 `docs/qa/` 또는 커밋 메시지에 적는다.
- 코드 변경은 **Sonnet 작성 → Opus 검수 → 커밋** 순서를 따른다.

## 공동 작업 규칙
- 작업 전 `git pull`(GitHub Desktop: Fetch/Pull), 작업 단위마다 커밋.
- **씬(`.unity`)과 프리팹(`.prefab`) 파일은 한 번에 한 AI만 수정한다.** 병합 충돌이 나기 가장 쉬운 파일이다.
- 커밋 메시지 앞에 담당을 붙인다: `[code]`, `[review]`, `[asset]`, `[qa]`
- `Library/`, `Temp/`, `Build/` 등 Unity 생성 폴더는 커밋하지 않는다 (.gitignore 참고).
- 큰 바이너리 파일(이미지, 사운드, 모델, 폰트)은 Git LFS로 관리된다 (.gitattributes 참고).

## 에셋 규칙 (Codex)
- 외부 에셋을 쓰면 **출처와 라이선스를 [CREDITS.md](CREDITS.md)에 바로 기록**한다. 출처를 알 수 없는 에셋은 쓰지 않는다.
- AI로 만든 에셋도 CREDITS.md에 "AI 생성 (도구 이름)"으로 기록한다. 제출 페이지 AI 공개에 필요하다.
- WebGL 용량을 고려해 텍스처는 필요한 만큼만 크게, 오디오는 압축 포맷을 쓴다.

## QA 규칙 (Antigravity)
- 버그 리포트는 `docs/qa/BUG-<번호>-<짧은제목>.md`에 다음 형식으로 쓴다: 재현 단계 / 기대 결과 / 실제 결과 / 심각도(높음·중간·낮음)
- 제출 전에 [JAM_RULES.md](JAM_RULES.md)의 체크리스트를 하나씩 확인한다.

## 생성형 AI 사용 기록
제출 페이지에 사용한 AI 도구와 사용 방식을 공개해야 한다(공식 규칙 7(d)). 역할이 바뀌거나 새 도구를 쓰면 위 역할 표를 갱신한다.
