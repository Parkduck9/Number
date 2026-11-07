# 설치된 스킬

| 스킬 | 출처 | 자동 실행 | 용도 |
|---|---|---|---|
| `codex` | 직접 작성 | ✅ | Claude가 Codex(GPT)를 난이도별 모델로 호출 |
| `game-dev-workflow` | Glitch-Gaming-Platform/AI-Prompts-For-Game-Development (README를 잼용으로 각색) | ✅ | 게임 개발 단계·일정 |
| `grill-me`, `grilling` | mattpocock/skills | grilling ✅ / grill-me는 `/grill-me`로 호출 | 기획을 질문 공세로 다듬기 |
| `antfu*`, `nitro`, `nuxt`, `pinia`, `pnpm`, `unocss`, `vite`, `vitepress`, `vitest`, `vue` | antfu/skills | ❌ `/이름`으로만 | JS/TS 웹 개발용 (Unity와 무관) |
| `analytics-dashboard`, `content-matrix`, `gemini-*`, `graphic-designer`, `hook-generator`, `newsletter-voice`, `niche-research`, `pinned-comment`, `post-*`, `profile-optimizer`, `quote-post`, `reels-scripting`, `voice-builder`, `youtube-thumbnail` | charlie947/social-media-skills | ❌ `/이름`으로만 | SNS 홍보 콘텐츠 |

- "자동 실행 ❌"인 스킬은 Unity 작업 중에 잘못 켜지지 않도록 `disable-model-invocation: true`를 추가했다. 원본에서 바꾼 것은 이 한 줄뿐이다.
- 주의가 필요한 스킬:
  - `antfu-create-pr`: 확인 없이 브랜치를 push하고 PR을 만들며, 동봉 스크립트가 GitHub 토큰을 사용한다.
  - `antfu`: JS 프로젝트에 npm 스킬 자동 설치와 pre-commit 훅을 넣으려 한다. Unity 프로젝트에서는 쓰지 말 것.
  - `reels-scripting`, `post-scorer`: 유료 API(Apify, Gemini)를 쓸 수 있다.
  - `niche-research`: 로그인된 Reddit·X 피드를 탐색한다.
- 라이선스: 모두 MIT. 원문은 `_licenses/`에 있다.
