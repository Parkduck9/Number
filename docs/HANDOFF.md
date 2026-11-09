# 인수인계 — 다른 컴퓨터에서 이어 하기 (2026-11-09 갱신)

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
- 흐름: Opus 구현(여러 명) → **Fable 검수** → 커밋·푸시.
- **사용자에게는 한국어**, AI끼리(서브에이전트 프롬프트·세션 메시지)는 **영어**.
- 보이는 에셋은 **시안 먼저 → 사용자 확인 → 본작업**. AI 생성은 CREDITS.md에 기록.
- 사용자가 열어 둔 에디터는 건드리지 않는다. **에디터가 꺼져 있으면** Claude가 배치 모드로 씬을 직접 만든다:
  `Unity.exe -batchmode -quit -projectPath <repo> -executeMethod StageBuildKit.BuildEverything -logFile <log>`
  (실행 뒤 `Mulmaru SDF.asset`, `LiberationSans SDF.asset` 변경은 `git checkout --`로 되돌린다)
- 에디터가 열려 있을 때 컴파일 확인: Roslyn 스크립트(이전 PC scratchpad의 `compile.ps1`, Unity 설치 폴더의 csc.dll + csproj 참조). 새 PC에서는 필요하면 다시 만든다.

---

## 2. 지금 main에 있는 것 (커밋 `7f1d37b` 이후)

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

---

## 3. 해야 할 것 (우선순위 순)

### 👤 사용자
1. **플레이** — Title에서 Play. 렉이 느껴지면 F3. 메모는 [qa/PLAYTEST-2026-11-09.md](qa/PLAYTEST-2026-11-09.md)에, 체크 표는 [qa/PLAYTEST-GUIDE.md](qa/PLAYTEST-GUIDE.md).
   - 볼 것: 세로 맵 동선, 갈고리 줄 손맛, 저격수·안드로이드 막기, 스포너·전멸 클리어, 층 분리 배경, 좌클릭 돌진·패링, 렉(F3).
2. **팀원 이름·역할·만 19세 이상 확인 (D9)** — CREDITS.md Team, SUBMISSION_PAGE.md. **없으면 제출 불가.** 기한 11/13 정오.
3. **대사 확인** — [design/DIALOGUE_DRAFT.md](design/DIALOGUE_DRAFT.md) (안 고치면 초안 그대로)
4. **WebGL 브라우저 확인** — 지금 Claude가 하는 중. 결과를 받으면 보기.
5. STG-01 SEG-B 배경: 17의 층 분리 방식이 SEG-A·STG-00만 다룬다. SEG-B는 아직 배경이 없으니 같은 방식으로 갈지 정하기.
6. 이전 PC의 `Number/` 폴더, `Unity Game Jam.txt`는 쓰레기 — 새 PC엔 없다.

### 🤖 Claude (Opus 구현 → Fable 검수)
| 할 일 | 메모 |
|---|---|
| 플레이 피드백 반영 | 가장 먼저 |
| **QA 3차** | 진행 중 ([qa/SUMMARY-R2.md](qa/SUMMARY-R2.md) 다음) |
| **CREDITS·AI 공개 점검** | 진행 중 |
| **STG-02~05 배경 자르기·루프** | Gemini 원본 4묶음(먼 배경·건물·소품·실내)은 들어옴. Codex 지시서도 있음. Codex가 지금 안 돼서 대기 |
| pixelfx 순간 이펙트 시안 9개 연결 | 만들어만 놨고 안 붙임 |
| 주인공 대기 1·2프레임 방향 차이, 걷기 3번째 프레임 소매 패치 | 거슬리면 Codex로 수정 |
| 대사 넣기, 이펙트 미세 조정 | 사용자 확인 후 |

### 기능 동결 이후 (11/12~)
최종 WebGL 빌드 → itch.io 업로드(사용자) → CREDITS·AI 공개 최종 → 제출 체크리스트([qa/SUBMISSION_CHECKLIST.md](qa/SUBMISSION_CHECKLIST.md)).

---

## 4. 열린 질문 (다음에 사용자에게 물을 것)
- 패링 판정(0.3초·탄 0.25초)이 손에 맞는지 (플레이 후)
- 적 돌진 중에는 좌클릭 패링이 안 된다 → 돌진 처치나 Shift로 대응. 괜찮은지
- 세로 맵 + 갈고리 줄로 이동이 충분히 시원한지, 카메라를 더 뺄지
- STG-02~05 배경을 STG-01처럼 층 분리·움직임까지 갈지, 정지 배경으로 끝낼지 (시간 대비)
## 5. 작업 규칙 요약 (자세히는 AGENTS.md · CLAUDE.md)
- 커밋 머리말 `[code]` `[review]` `[asset]` `[qa]` `[docs]`.
- Unity 배치 모드는 에디터가 꺼져 있을 때만, 한 번에 하나만.
- 무료 도구·에셋만. 기획서는 `[확정]`만 구현 근거. "추천대로" 정한 것은 DECISIONS.md에 기록.
