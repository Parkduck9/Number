---
name: game-dev-workflow
description: Step-by-step game development workflow (define → architecture → visual/audio standard → assets → first playable → onboarding → playtest → release) adapted for this 7-day Unity WebGL jam. Use when planning what to build next, starting a new phase, or deciding whether the game is ready to move on.
---

# 게임 개발 워크플로 (잼용)

출처: [Glitch-Gaming-Platform/AI-Prompts-For-Game-Development](https://github.com/Glitch-Gaming-Platform/AI-Prompts-For-Game-Development)의 단계 구성을 Unity Game Jam 2026(7일, WebGL, 2D)에 맞게 줄여서 정리했다.
원본 프롬프트는 glitch.fun 사이트에 있다(예: [Unity architecture](https://www.glitch.fun/publishers/tools/ai-game-development-prompts?prompt=unity-game-architecture#prompt-picker)). 필요할 때만 참고한다.

## 원칙
- **프롬프트는 한 번에 하나.** 결과를 확인하고, 동작하는 상태로 커밋한 뒤 다음으로 넘어간다.
- **결정은 문서로 남긴다.** 단계마다 `docs/`의 문서를 만들거나 갱신해서, 다음 작업이 같은 내용을 다시 알아내느라 토큰을 쓰지 않게 한다.
- **작게.** 끝까지 갈 수 있는 경로 하나(성공·실패 상태 포함)가 미완성 시스템 여러 개보다 낫다.

## 단계

| # | 단계 | 산출물 | 잼 일정 (KST) |
|---|---|---|---|
| 1 | **게임 정의**: 플레이어 판타지, 목표, 압박 요소, 핵심 비틀기, 코어 루프. "행운의 숫자"가 코어 루프에 들어가야 한다 (심사 1순위) | `docs/design.md` | 1일차 |
| 2 | **아키텍처**: 폴더 구조, 씬 구성, 핵심 시스템과 의존 방향, 입력, HUD·메뉴, 텍스트 키(영/한) | `docs/architecture.md` | 1일차 |
| 3 | **비주얼·오디오 기준**: 아트 스타일 한 줄 정의, 색상 팔레트, UI 스타일, 필요한 효과음·BGM 목록, WebGL 압축 설정 | `docs/art-audio.md` | 2일차 |
| 4 | **대표 에셋 몇 개**를 먼저 완성하고, 이후 에셋이 따라 할 제작·임포트 규칙을 문서화 (Codex 담당) | `Assets/Art`, `Assets/Audio`, CREDITS.md | 2일차 |
| 5 | **첫 플레이어블 빌드**: 코어 루프 + 성공·실패 + 조작 + HUD + 사운드. **WebGL로 한 번 빌드해 본다** | 플레이 가능한 WebGL 빌드 | 3~4일차 |
| 6 | **온보딩**: 실제 플레이로 하나씩 가르치기, 빠른 첫 성공, 설명 화면 대신 게임 안의 안내 | 튜토리얼 흐름 | 5일차 |
| 7 | **플레이테스트 → 개선**: Antigravity QA 리포트와 실제 플레이 피드백을 근거로 우선순위를 정해 고친다 | `docs/qa/` | 5~6일차 |
| 8 | **마무리**: 폴리싱, 성능·용량 점검, 최종 WebGL 빌드 | 최종 빌드 | 6~7일차 (11/13 00:00 전) |
| 9 | **제출**: itch.io 페이지, 조작법, 크레딧, AI 공개, JAM_RULES.md 체크리스트 | 제출 완료 | 11/13~11/14 21:00 |

## 잼에서 건너뛰는 원본 단계
백엔드, 분석(analytics), 모바일 최적화, AAA 비주얼 패스, 배포 파이프라인은 7일 오프라인 WebGL 잼에는 과하다. 필요해지면 그때 원본을 참고한다.

## 다음 단계로 넘어가는 기준
- 5단계 전: 코어 루프 없이 기능을 추가하지 않는다.
- 6단계 전: 처음 보는 사람이 첫 플레이어블을 끝까지 할 수 있어야 한다.
- 11/13 00:00 이후: 기능 추가 금지. 버그 수정, 빌드, 제출만 한다.
