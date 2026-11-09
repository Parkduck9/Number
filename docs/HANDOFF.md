# 인수인계 — 다른 컴퓨터에서 이어 하기 (2026-11-10 갱신)

> 이 파일 하나만 읽으면 다른 PC에서 바로 이어 할 수 있게 정리했다.
> Claude에게 시작할 때: **"docs/HANDOFF.md 읽고 이어서 하자"** 라고 말하면 된다.
> 마감: 개발 **2026-11-13 00:00 KST** · ⛔ 기능 동결 **11/11 24:00** · 제출 **11/14 21:00** (18:00까지 목표)
> 전체 일정: [review/ROADMAP-2026-11-09.md](review/ROADMAP-2026-11-09.md) · 결정 기록: [design/DECISIONS.md](design/DECISIONS.md) (15~17이 최신)

---

## 1. 새 PC 준비 (처음 한 번)

| 순서 | 할 일 | 메모 |
|---|---|---|
| 1 | **Git + Git LFS** 설치 → `git lfs install` | png·wav가 LFS. LFS 없으면 이미지가 텍스트 포인터로 받아진다 |
| 2 | `git clone https://github.com/Parkduck9/Number.git` | 저장소는 **비공개 유지** (Asset Store 에셋 재배포 금지) |
| 3 | **Unity 6000.3.28f1** + WebGL Build Support | 다른 버전으로 열거나 업그레이드 금지. 첫 열기는 Library 생성으로 오래 걸림 |
| 4 | **Node 18+**, pixelfx: `git clone https://github.com/patch-gg/pixelfx` (저장소 **밖**, 예: `D:\Git\pixelfx`) | 이펙트 도구 |
| 5 | **Claude Code** (데스크톱 앱 Code 탭), **Codex CLI** (`npm i -g @openai/codex`, 로그인) | `.claude/agents`, `.claude/skills`는 저장소에 있음 |
| 6 | 사용자 설정 `~/.claude/settings.json`: `maxEffortLevel: high`, `workflowKeywordTriggerEnabled: false`, `autoCompactWindow: 600000` | CLAUDE.md 토큰 규칙 |
| 7 | (선택) Unity MCP: 에디터 `Window → MCP for Unity` 서버 실행 | `.mcp.json`에 설정 있음 |

**경로가 바뀌는 곳:** CLAUDE.md의 Unity 에디터 경로, git 경로(이전 PC는 GitHub Desktop 내장 git), pixelfx 경로. 새 PC에 맞게 고쳐서 커밋.

### Claude 작업 규칙 (이전 PC 로컬 기억 → 여기 옮김)
- **Fable = 검수 전용** (11/08 밤부터). 구현은 **Opus**가 파일이 겹치지 않게 나눠 맡고(병렬 가능), 단순 작업·문서는 Sonnet. CLAUDE.md 맨 위 줄에도 있음.
- **11/09 오후 변경:** Fable 사용량이 얼마 안 남았다. **중요한 코드·시각 검수만** Fable. 중요하지 않은 코드(스프라이트·머티리얼 연결, 작은 수정, 문서)는 **Sonnet 구현 → Opus 검수**.
- 흐름: Opus 구현(여러 명) → **Fable 검수** → 커밋·푸시.
- **Codex는 크레딧이 가끔 바닥난다.** 실패하면 사용자가 충전한 뒤 재시도.
- **사용자에게는 한국어**, AI끼리(서브에이전트 프롬프트·세션 메시지)는 **영어**.
- 보이는 에셋은 **시안 먼저 → 사용자 확인 → 본작업**. AI 생성은 CREDITS.md에 기록.
- 사용자가 열어 둔 에디터는 건드리지 않는다. **에디터가 꺼져 있으면** Claude가 배치 모드로 씬을 직접 만든다:
  `Unity.exe -batchmode -quit -projectPath <repo> -executeMethod StageBuildKit.BuildEverything -logFile <log>`
  (실행 뒤 `Mulmaru SDF.asset`, `LiberationSans SDF.asset` 변경은 `git checkout --`로 되돌린다)
- 에디터가 열려 있을 때 컴파일 확인: Roslyn 스크립트(이전 PC scratchpad의 `compile.ps1`, Unity 설치 폴더의 csc.dll + csproj 참조). 새 PC에서는 필요하면 다시 만든다.

---

## 2. 지금 main에 있는 것 (커밋 `bbc94bc` 기준)

**게임 흐름:** Title → STG-00(오프닝) → STG-01~04 → STG-05(보스 3웨이브) → 정지 버튼 엔딩 2개. 씬은 전부 **빌더가 생성** — 메뉴 **`NUMBER/Build EVERYTHING (Title + STG-00~05)`** 하나로 전부 다시 만든다. `.unity`를 손으로 고치지 않는다.

| 기능 | 결정 | 구현 기록 |
|---|---|---|
| 좌클릭: 마우스 조준, 사거리 5u·±30° 안 적 → 관통 돌진 처치. 시선이 막히면 조준 안 됨. 아래로 돌진하면 얇은 발판을 통과 | 12 | impl/dash-attack.md |
| 좌클릭 패링: 탄이 **0.25초 안에 도착**하거나 적 공격이 0.3초 안에 닿을 때. 우클릭 패링은 없음 | 12, 15 | dash-attack.md |
| **우클릭 = 네온 갈고리 줄**: 마우스 방향 9u, 22 u/s로 끌려감, 모서리 걸림, 공중 1회 | 16 | (커밋 dcce69e) |
| Shift: 4u 안 적 → 무적 대시 → 뒤돌며 반격. 쿨타임 3.5초 | 13 | movement-assist.md |
| 적: 7u 안이면 쫓아옴, 0.3초 예고 후 돌진 공격. 원거리 시민(0이면 굳음) / 원거리 벌레(STG-02부터) | 12, 14 | enemy-rules.md, bug-swarm.md |
| **저격수**: 레이저 1.0초 추적, 0.25초 고정, 패링 가능한 한 발. 안드로이드는 정면 막힘, 뒤·위·과부하 때만 한 방 | 15 | (커밋 335ecef) |
| **스테이지 클리어 = 적 전멸 + 골.** 부술 수 있는 스포너(부술 때까지 리스폰), 남은 적 방향 화살표 | 15 | (커밋 dd91c81, 7b8d1bb) |
| **세로 맵:** STG-01·02·04가 지붕 길·수직 통로로 이어짐 (스포너 + 저격수 2) | 15 | levels.md, LEVEL_*.md |
| **STG-01·00 거리 배경 = 층 분리 + 움직임** (하늘·스카이라인·중경 시차, 구름·네온·홀로·드론, 중경 화면에 방송 숫자) | 17 | (커밋 fa4bcc2) |
| **네온 조명이 실제 스테이지에 붙음** (정렬 레이어 Background/World/Characters/FX, STG-00 낮·나머지 밤) | 9 | LIGHTING_DESIGN.md |
| 히트 FX 연결(타격 링·처치 터짐·패링 충돌), 흰색 피격 깜빡임(SpriteFlash), 안드로이드 과부하 글리치, 벌레 스프라이트, 주인공 달리기 6프레임, 발이 땅에 닿게 피벗 조정 | — | juice.md, draft-sprites.md |
| 대화 초상화(주인공·시민·러다이트·방송), HUD 얼굴 체력·보스 화면 Gemini 원본으로 다시 | — | draft-sprites.md |
| 벌레 점 떼 15Hz 갱신(렉 완화), 카메라 펀치 줌, 부활 0.55초 | — | bug-swarm.md, juice.md |
| 오디오(효과음 12·BGM 2), 조작 힌트, 속도감 FX, Space 충격파 설명 | — | audio.md, upgrade.md |
| **F3** = FPS·프레임 시간 표시 (에디터·개발 빌드만) | — | Core/FrameStats.cs |
| **GUI 움직임 (0fecfe2):** 메뉴 버튼 반짝임·눌림·순차 등장(UiKit ButtonHighlight), 일시정지 팝인, **Shift 쿨타임 원형 HUD**(왼쪽 아래, UI/ShiftCooldownHud.cs), 방송 숫자 글리치·스크램블, 얼굴 HUD 흔들림·번쩍, 조작 힌트 토스트 | — | — |
| **pixelfx 순간 이펙트 9개 연결 (Combat/MomentFx.cs):** 착지 먼지·갈고리 적중·스포너 파괴·적 워프인·저격 총구·체크포인트·플레이어 글리치 사망(사망 시 몸 숨김)·해킹 펄스·골 열림 루프 | — | — |
| **STG-02~05 배경 재작성 (bbc94bc):** Codex 부품(`Assets/Art/Backgrounds/STG02_05_Parts`) + `Editor/PartsBackdropKit.cs`, 가로등 글로우 재추가. FX 시트 8개 Codex 복구(원본은 `D:\Git\pixelfx`). 주인공 대기·걷기 수정 + 새 `hero_grapple_1f`. 씬 재생성 | — | — |
| QA 3차 수정(e5c301b, c0b9afa): 인게임 힌트에 갈고리, 우클릭 버퍼, 조준선 비할당 체크, 무릎 벽 매달림 수정(마찰 0 재질), WebGL 빌드 13.8MB 확인, CREDITS·AI 공개 점검 | — | docs/qa/ |

---

## 3. 해야 할 것 (우선순위 순)

### 👤 사용자
1. **플레이** — Title에서 Play. 렉이 느껴지면 F3. 메모는 [qa/PLAYTEST-2026-11-09.md](qa/PLAYTEST-2026-11-09.md)에, 체크 표는 [qa/PLAYTEST-GUIDE.md](qa/PLAYTEST-GUIDE.md).
   - 볼 것: 세로 맵 동선, 갈고리 줄 손맛, 저격수·안드로이드 막기, 스포너·전멸 클리어, 층 분리 배경, 좌클릭 돌진·패링, 렉(F3).
2. **팀원 이름·역할·만 19세 이상 확인 (D9)** — CREDITS.md Team, SUBMISSION_PAGE.md. **없으면 제출 불가.** 기한 11/13 정오.
3. **대사 확인** — [design/DIALOGUE_DRAFT.md](design/DIALOGUE_DRAFT.md) (안 고치면 초안 그대로)
4. **Gemini 원본 요청** — [design/concept/GEMINI_REQUESTS.md](design/concept/GEMINI_REQUESTS.md): **#0 러다이트 게임 스프라이트(다시, 최우선)**, #1 저격수 스프라이트, #2 엔딩 일러스트 2장, #3 STG-01 SEG-B 층 분리 배경.
5. **부드러운 애니메이션 시안 확인** — `docs/design/concept/anim_v2`(주인공 idle4/walk6/attack5/jump4/dash3, 적 walk4/attack5, 15장). Fable 검수 → Codex 수정 후 사용자 승인 대기.
6. **WebGL 브라우저 확인** — 애니메이션 연결 뒤 다시 빌드해서 Claude가 확인.
7. 미추적 쓰레기(`Number/`, `Unity Game Jam.txt`, 옛 콘셉트 샘플)는 그대로 둔다.

### 🤖 Claude (Opus 구현 → Fable 검수)
| 할 일 | 메모 |
|---|---|
| 플레이 피드백 반영 | 가장 먼저 |
| **애니메이션 v2 연결** | 시안 15장 Fable 검수 → 어색한 것 Codex 재작업 → 사용자 승인 → 연결. 코드: `DraftSprites` 이름·프레임 수, WalkFps 8→16, IdleFps 2→4, 타격 타이밍 유지 |
| **배경 띠 3장 재포장** | 4096px 넘는 배경 스트립 3개를 WebGL용으로 다시 자르기 (진행 중) |
| **WebGL 재빌드 + 브라우저 확인** | 위 둘 뒤에 |
| STG-02·04 구간 끝 ~30u 거리 소품 없음 | 채우기 |
| 엔딩 대사 | 아직 자리표시. Gemini 일러스트(#2) 후 연결 |
| 대사 넣기, 이펙트 미세 조정 | 사용자 확인 후 |

*끝난 것(11/09~10): QA 3차, CREDITS·AI 공개 점검, STG-02~05 배경 자르기, pixelfx 순간 이펙트 9개 연결, 주인공 대기·걷기 패치.*

### 기능 동결 이후 (11/12~)
최종 WebGL 빌드 → itch.io 업로드(사용자) → CREDITS·AI 공개 최종 → 제출 체크리스트([qa/SUBMISSION_CHECKLIST.md](qa/SUBMISSION_CHECKLIST.md)).

---

## 4. 열린 질문 (다음에 사용자에게 물을 것)
- 패링 판정(0.3초·탄 0.25초)이 손에 맞는지 (플레이 후)
- 적 돌진 중에는 좌클릭 패링이 안 된다 → 돌진 처치나 Shift로 대응. 괜찮은지
- 세로 맵 + 갈고리 줄로 이동이 충분히 시원한지, 카메라를 더 뺄지
- (STG-02~05 배경은 부품 방식으로 정리됨. STG-01 SEG-B만 Gemini #3 대기)
## 5. 작업 규칙 요약 (자세히는 AGENTS.md · CLAUDE.md)
- 커밋 머리말 `[code]` `[review]` `[asset]` `[qa]` `[docs]`.
- Unity 배치 모드는 에디터가 꺼져 있을 때만, 한 번에 하나만.
- 무료 도구·에셋만. 기획서는 `[확정]`만 구현 근거. "추천대로" 정한 것은 DECISIONS.md에 기록.
