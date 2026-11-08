# 인수인계 — 다른 컴퓨터에서 이어 하기 (2026-11-08 밤)

> 이 파일 하나만 읽으면 다른 PC에서 바로 이어 할 수 있게 정리했다.
> Claude에게 시작할 때: **"docs/HANDOFF.md 읽고 이어서 하자"** 라고 말하면 된다.
> 마감: 개발 **2026-11-13 00:00 KST** · ⛔ 기능 동결 **11/11 24:00** · 제출 **11/14 21:00** (18:00까지 목표)

---

## 1. 새 PC 준비 (처음 한 번)

| 순서 | 할 일 | 메모 |
|---|---|---|
| 1 | **Git + Git LFS** 설치 → `git lfs install` | png 등 큰 파일이 LFS. LFS 없으면 이미지가 텍스트 포인터로 받아진다 |
| 2 | `git clone https://github.com/Parkduck9/Number.git` | 저장소는 **비공개 유지** (Asset Store 에셋 재배포 금지) |
| 3 | **Unity 6000.3.28f1** (Unity Hub) + WebGL Build Support 모듈 | 다른 버전으로 열거나 업그레이드 금지. 첫 열기는 Library 생성으로 오래 걸림 |
| 4 | **Node 18+** | pixelfx 이펙트 도구용 |
| 5 | pixelfx: `git clone https://github.com/patch-gg/pixelfx` (저장소 **밖**, 예: `D:\Git\pixelfx`) | 경로가 다르면 CLAUDE.md의 pixelfx 줄을 고칠 것 |
| 6 | **Claude Code** (데스크톱 앱 Code 탭) | `.claude/agents`(game-architect=Fable, code-reviewer=Opus, game-coder=Sonnet)와 `.claude/skills`는 저장소에 들어 있다 |
| 7 | **Codex CLI** (`npm i -g @openai/codex`, 로그인) | 에셋 작업. Claude가 `codex` 스킬로 호출 |
| 8 | (선택) Antigravity CLI `agy` — QA용, Google 로그인은 직접 | |
| 9 | (선택) Unity MCP: 에디터 `Window → MCP for Unity` 서버 실행 | `.mcp.json`에 설정 있음 |
| 10 | 사용자 설정 `~/.claude/settings.json`: `maxEffortLevel: high`, `workflowKeywordTriggerEnabled: false`, `autoCompactWindow: 600000` | 이전 PC와 같게 (CLAUDE.md 토큰 규칙) |

**경로가 바뀌는 곳:** CLAUDE.md의 Unity 에디터 경로, git 경로(이전 PC는 GitHub Desktop 내장 git), pixelfx 경로. 새 PC에 맞게 고쳐서 커밋.

### Claude 기억 (이전 PC 로컬에만 있던 것 — 여기 옮김)
- 병렬 세션은 **최대 5개** (11/08 저녁에만 임시로 10개 허용했음). 한도 안에서는 적극적으로 병렬.
- **코드 검수는 Fable**(game-architect)로, 새 일은 **쉬고 있는 세션에 먼저** 맡긴다. Fable 한도가 부족하면 Opus → Sonnet 순서로 낮추고 알린다.
- 디자인 패턴(State·Observer 등)은 각 세션이 자기 파일 안에서만 적용하고, 파일을 넘는 아이디어는 기록 → Fable이 종합 검토.
- "추천대로" 정한 결정은 전부 [design/DECISIONS.md](design/DECISIONS.md)에 기록해서 나중에 바꿀 수 있게 한다.

---

## 2. 브랜치 상태

| 브랜치 | 내용 | 할 일 |
|---|---|---|
| `main` | 모든 확정 작업 (아래 3장) | 여기서 작업 |
| `claude/audio-4c66f5` | 효과음 12개·BGM 2곡(직접 합성, CC0)·생성기 원본·Sfx API (미병합 2커밋, WAV는 LFS) | **사용자 청취 승인** 후 main에 병합 + 훅 3곳 연결 ([design/impl/](design/impl/) audio 노트) |
| `claude/hud-art-drafts-a2d1e9` | HUD 시안: 얼굴 체력 6단계·방송 숫자·홀로그램 창 (`Assets/Art/Drafts/HUD/`) | **사용자 승인** 후 main에 병합 + 연결 |
| `bg-art-drafts` | STG-02~05 배경 시안 6장 + 기록 | **사용자 승인** 후 병합 + 빌더 연결 (배경 6→4장으로 줄이기로 함) |
| `webgl-test` | 첫 WebGL 빌드 결과 문서 `docs/qa/WEBGL-BUILD-1.md` (코드 변경 없음) | 문서만 main으로 가져오면 됨 |

브랜치를 main에 합칠 때는 사용자가 "합쳐"라고 할 때만 한다.

---

## 3. 지금 main에 있는 것 (11/08 기준)

**게임:** Title → STG-00(오프닝) → STG-01~04 → STG-05(보스 3웨이브) → 정지 버튼 엔딩 2개. 씬은 전부 **에디터 빌더가 생성** (`StageBuildKit.BuildAll`, `Stg0XBuilder`, `Stg05Builder.Build`, `TitleSceneBuilder.Build`). `.unity` 파일을 손으로 고치지 않는다.

| 기능 | 기록 |
|---|---|
| 플레이어: 이동·점프·공격·패링·대시·충격파(Space, STG-03 이후) | `PlayerController.cs`, impl/upgrade.md |
| 적: 시민·AI 벌레(1픽셀 점 떼 외형)·안드로이드, 숫자 = 연속 공격 횟수(줄지 않음) | impl/enemy-rules.md, impl/bug-swarm.md |
| **일반 적 한 방 처치** + 0.5초 뒤 사라짐, 일반 적 템포 빠르게 | impl/juice.md |
| **안드로이드 과부하**: 마지막 일격 패링(7은 대시 통과) → 1.5초 과부하 → 한 대 처치. 그 외엔 막음 | impl/android-gimmick.md |
| 방송 숫자 3·7·0, 해킹 다이얼은 **STG-03 이후만**(STG-01·02는 방송 HUD만) | impl/enemy-rules.md |
| 보스 3웨이브, 예고 중 해킹 무시 | impl/boss.md |
| 타격감: 히트스톱·카메라 셰이크·숫자 팝·피격 틴트 (히트스톱 중 입력 버퍼) | impl/juice.md |
| STG-00 "숨 0번" 연출: 정전·정적·숫자 0만 빛남·시민 순차 굳음 (소리 끊김은 훅만, 오디오 병합 후 연결) | impl/opening-breath-zero.md |
| 적 배치 원칙: 1:1 평지 금지, 묶음·원거리 엄호·발판, STG-01 첫 적 = 원거리 1 | impl/levels.md, LEVEL_*.md |
| 타이틀: 금색 `0` 하나 + NUMB-ER 로고 | impl/title.md |
| 엔딩 END-02에 숫자 다이얼 재사용 | impl/ending-dial.md |
| 주인공 걷기·시민 공격 **임시 스프라이트** (`DraftSprites.Use`로 끔) | impl/draft-sprites.md |
| 24색 고정 팔레트 시안 + `NUMBER/Quantize Selected To Palette` 도구 | design/PALETTE.md |
| WebGL 설정 960×540, 우클릭 차단, 포커스 잃으면 일시정지 | qa/SUMMARY-BUILD.md |

**첫 WebGL 빌드 성공** (11.9 MB, 오류 0) — `webgl-test` 브랜치 `docs/qa/WEBGL-BUILD-1.md`. 브라우저 확인은 아직.

**검수:** Fable 전체 검수·수정(`f9164ff`), 이후 커밋은 Sonnet 구현 → Fable 검수, 마지막으로 Opus 패스 2회(`ec53346`, `e30b523` — 확정 버그 없음). 기록: `docs/review/04-fable-fixes.md`, `05-opus-pass.md`.

**아무도 Play 모드로 처음부터 끝까지 해 보지 않았다.** 모든 확인은 컴파일·정적 분석뿐이다. → 새 PC에서 가장 먼저 할 일.

---

## 4. 바로 다음에 할 일 (순서대로)

1. 👤 **Unity에서 Title → 엔딩까지 플레이** (20분). 메모는 `docs/qa/PLAYTEST-2026-11-09.md`에 3가지만: 막힌 곳 / 지루한 곳 / 숫자를 보고 행동했나.
   - 확인 목록은 각 impl 문서의 "Play 모드 확인" 절. 특히: 첫 원거리 적 탄이 닿는지, 안드로이드 과부하, 벌레 점 떼 모양, 오프닝 정전, 타이틀·엔딩 다이얼.
2. 👤 **WebGL 빌드를 브라우저에서 확인**: Unity에서 `NUMBER/Build WebGL` → `Build/WebGL`을 로컬 서버로 열기(Chrome·Firefox). 타이틀→STG-01, 우클릭 막힘, 전체화면 Esc, 한글 폰트. 그다음 itch.io 비공개 페이지 업로드.
3. 👤 **결정 대기 (막는 순서)** — [review/REVIEW.md](review/REVIEW.md) 2장
   - **D1 주인공 스프라이트 확정 — 11/09 정오** (못 하면 임시안으로 확정). 확정되면 팔레트도 확정.
   - **D2 오디오 승인** — [청취 페이지](https://claude.ai/artifact/2RCARCepYagWREGz2i6PdF)
   - **D3 HUD 시안 승인** — [검토 페이지](https://claude.ai/artifact/9bYK8AFpoTSt4VhdMLyVfZ)
   - **D4 배경 시안 승인** — [검토 페이지](https://claude.ai/artifact/9FoUegC8CiyL3dLcUcMwBq)
   - **D6 대사**: [design/DIALOGUE_DRAFT.md](design/DIALOGUE_DRAFT.md) 초안을 읽고 고치기 → Claude가 `Assets/Data/Dialogue/`에 넣음
   - **D9 팀원 이름·역할·만 19세 이상·자격** → CREDITS.md, SUBMISSION_PAGE.md (없으면 **제출 불가**)
4. 🎨 **Codex 에셋 시안 확인** (main에 커밋됨, 게임에는 미연결, 각 `*_preview4x.png`로 보기). 승인한 것만 게임에 연결.
   - `Assets/Art/Drafts/Sprites/`: 주인공 대기 2·공격 3·대시 2·피격 1프레임 (`ad5e0a4`, **대시 프레임 품질이 약함**), 안드로이드 공격 3·원거리 시민 3·러다이트 NPC 대기 2 (`987da1c`)
   - `Assets/Art/Drafts/UI/`: 조작 힌트 낙서 아이콘(마우스 좌·우, Shift, E, Space, W·A·D — 키 글자는 게임에서 폰트로 얹음), 충격파 HUD 아이콘, 보스 Embector 화면 얼굴 2프레임 (`e230c02`)
   - 모두 24색 팔레트(`Assets/Art/Palette/`)로 양자화됨.
5. 승인 결과대로 연결 작업 → 11/11 2차 QA → **11/11 24:00 기능 동결** → 11/12 최종 WebGL 빌드 → 11/13 페이지·스크린샷 → 11/14 제출.

전체 계획·자를 목록: [review/REVIEW.md](review/REVIEW.md) (HTML: [review/REVIEW.html](review/REVIEW.html)), 결정 기록: [design/DECISIONS.md](design/DECISIONS.md), 남은 작업: [TODO.md](TODO.md).

---

## 5. 작업 규칙 요약 (자세히는 AGENTS.md · CLAUDE.md)
- 코드: Sonnet 구현 → **Fable 검수** → 커밋. 커밋 머리말 `[code]` `[review]` `[asset]` `[qa]` `[docs]`.
- Unity 배치 모드는 한 번에 하나만 (에디터가 프로젝트를 열고 있으면 실패 → MCP 사용).
- 배치 실행 뒤 `Assets/Fonts/Mulmaru/Mulmaru SDF.asset`, `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`가 바뀌면 `git checkout --`로 되돌린다.
- 보이는 에셋은 **시안 먼저 → 사용자 확인 → 본작업**, AI 생성은 CREDITS.md에 기록 ([design/ANTI_SLOP.md](design/ANTI_SLOP.md)).
- 무료 도구·에셋만. 기획서는 `[확정]`만 구현 근거.
