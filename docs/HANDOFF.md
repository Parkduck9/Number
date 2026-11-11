# 인수인계 — 다른 컴퓨터에서 이어 하기 (2026-11-11 갱신)

> 이 파일 하나만 읽으면 다른 PC에서 바로 이어 할 수 있게 정리했다.
> Claude에게 시작할 때: **"docs/HANDOFF.md 읽고 이어서 하자"** 라고 말하면 된다.
> 마감: 개발 **2026-11-13 00:00 KST** · ⛔ 기능 동결 **11/11 24:00** · 제출 **11/14 21:00** (18:00까지 목표)
> 전체 일정: [review/ROADMAP-2026-11-09.md](review/ROADMAP-2026-11-09.md) · 결정 기록: [design/DECISIONS.md](design/DECISIONS.md) (18~33이 최신, 23~33은 파일 아래쪽에 있음)

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

## 2. 지금 main에 있는 것 (커밋 `2550b33` 기준)

**게임 흐름:** Title → STG-00(오프닝) → STG-01(튜토리얼 5단계) → STG-02(러다이트가 주사위를 줌) ~ STG-04 → STG-05(합체 배경 보스) → 기계 부수기 엔딩 1개(DECISIONS 21).

### 11/10에 바뀐 것 (`bbc94bc` 이후) — 아래 표보다 이게 우선
| 기능 | 결정 |
|---|---|
| **주인공 = 도사**(plan B 복장). 옛 주인공은 러다이트 도우미 NPC(`helper_*`)로 STG-01·03에 등장 | 19, 24 |
| **E = 행운의 주사위**(도사 스킬 6개, 1~6 눈). 3번 부적 번개, 4번 제자리 발도(가까운 최대 4명). STG-02 시작에 러다이트가 건넴. 상호작용 근처에선 E가 상호작용 우선. **Space = 점프** | 18, 27, 31, 32, 33 |
| **우클릭 = 일섬**(조준한 적 뒤로 번쩍, 사람은 한 방, 쿨타임 5초 + HUD 칸). 갈고리 줄·부적 던지기를 대체 | 26 |
| 패링: 줄어드는 고리 예고(osu! 식), PARRY! 문구 대신 부적 이펙트, 높이 상·중·하 + 자세 9개, 튕긴 탄은 힘 빠져 포물선으로 떨어짐, 패링 뒤 연출용 반격 모션(피해 없음) | 24, 28, 30 |
| 엘리트 맞부딪힘 대결, 조준선, 이동 속도 10, 저격탄 중계(사람 적이 탄을 쳐서 넘김) | 20, 22, 25 |
| **보스 v2**: 배경과 합쳐진 보스, 홀로그램이 다음 공격을 숫자로 예고, 벌레 팔·칼날 팔·가슴 해치(Codex 파츠, `Resources/BossArt`). 공격 사이 약점 코어(좌클릭 1·우클릭 2), 보스전 주사위 가능. 어렵게 조정: HP 60, 코어 1.2초, 추적 칼날 | 23, 29, 31, 33 |
| 애니메이션 v2 15장 연결 + 4096px 넘는 띠는 격자로 재포장(f640a1b), 달리기 v5(8프레임 20fps), 달리기 기울기·발 먼지 | — |
| 저격수 다시 그림, 탄 스프라이트 4종, 픽셀 UI 키트, 조명·포스트(Bloom 등) 고침, 카메라 픽셀 스냅 | — |
| STG-02·04 구간 끝 ~30u 소품 채움 | — |

### 그 전 상태 (`bbc94bc`, 위 표에 바뀐 줄은 옛 내용) 씬은 전부 **빌더가 생성** — 메뉴 **`NUMBER/Build EVERYTHING (Title + STG-00~05)`** 하나로 전부 다시 만든다. `.unity`를 손으로 고치지 않는다.

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
4. **기계 소품 3종 시안 승인** (랙 다듬은 뒤) — `docs/design/concept/machine_props/`. 남은 Gemini 원본: 체크포인트 표식, 엔딩 일러스트 1장, STG-01 SEG-B 배경 (Claude가 앱 브라우저로 직접 뽑을 수 있음, Gemini 로그인만 필요).
5. **엔딩 대사 다시 쓰기** — 엔딩은 하나(기계 부수기). 지금 대사는 Claude 초안(`Assets/Data/Dialogue/ending_01_placeholder.txt`, DIALOGUE_DRAFT.md).
6. 미추적 쓰레기(`Number/`, `Unity Game Jam.txt`, 옛 콘셉트 샘플)는 그대로 둔다.

### ✅ 플레이 피드백 2026-11-11 — 6개 모두 적용됨 (DECISIONS 34, Opus 구현 → Fable 검수, WebGL 빌드 통과). 플레이로 손맛 확인만 남음
| # | 사용자 말 | 해석·코드 위치 |
|---|---|---|
| 1 | 패링하는 느낌이 안 남. 클릭할 때 애니메이션이 같이 나가야 하는데, 누르면(패링 성공 뒤에) 나가는 형태로 되어 있음 | 좌클릭 순간 바로 공격/패링 모션이 나가야 함. 지금은 성공 뒤 반격 모션(DECISIONS 30, `c7e754b`)만 늦게 나옴. `PlayerController.cs` |
| 2 | 우클릭은 이동기 만들었던 거 부활시키고, Shift에 우클릭 원래 기능 넣을 수 있을까? 1자에서만 쓰게 | **우클릭 = 네온 갈고리 줄 복구**(DECISIONS 16, `30f1287`에서 지워짐 — 그 전 커밋 `2a168af`의 코드 참고). **Shift = 일섬**(지금 우클릭, DECISIONS 26), 가로(같은 높이)로만 쓰게. 지금 Shift 회피+반격(13)은 대체됨 → 확인 필요. DECISIONS에 새 번호로 기록 |
| 3 | W랑 Space 동시에 누르면 점프 안 돼. W키 없애고 Space만 살려 | Input Actions의 `Jump`에서 W 바인딩 제거. `PlayerInputReader.cs` / `.inputactions` |
| 4 | 공격 애니메이션 나오는 중에 점프 안 돼 | 공격 중에도 점프 입력 받게 (모션은 점프로 끊어도 됨). `PlayerController.cs` |
| 5 | 보스전 칼 휘두르기가 칼이 아니라 따로 만든 콜라이더로 판정함. 칼 그림에 충돌을 넣는 게 나을 듯 | 보스 칼날 휩쓸기(B', DECISIONS 29)의 판정이 코드로 그린 띠. 칼날 팔 스프라이트 위치에 맞춘 판정으로. `Boss/BossFight.cs` (`blade sweeps` 부분) |
| 6 | 벌레 나올 때 총알 나오는 거 추가하자. 너무 쉽다 | 보스전 벌레 벽/떼가 나올 때 벌레 탄(원거리 벌레 탄 `bullet_bug`) 추가. `Boss/BossFight.cs` |

> ⛔ 기능 동결이 11/11 24:00이라 2·6은 새 기능에 가깝다. 동결을 넘기면 사용자에게 먼저 확인.

### 🤖 Claude — 11/11 저녁 기준 (이 PC 작업 끝, 커밋 `4922d5d` 이후) ← 여기부터 이어서
**오늘 이 PC에서 끝난 것** (모두 main에 푸시, Play 모드 확인은 아직 — 사용자 플레이 필요):
- 기계 소품 3종 엔딩 연결(`cf09e86`), 체크포인트 부적 기둥 그림(`4dfdd11`), 저격수 레이저 굵게(`4545127`)
- DECISIONS 35: 패링 시 맞은 지점이 빛남(주인공 흰 깜빡임·금색 화면 섬광 제거), Shift = 일섬 가능하면 일섬, 아니면 공격 없는 대시(쿨 0.6초), **S+Space = 얇은 발판 아래로**
- DECISIONS 36: 보스 벌레 팔이 지휘하며 빛남 + 벌레 탄은 해치에서, 칼 휘두르기는 팔 전체가 화면 밖으로 나갔다 들어옴 + 재장착 연출, 칼 꽂힘 = 좌클릭 연타(결투 삭제)
- DECISIONS 37: 주사위 충전 = **적중 7번** (패링 아님), HUD 7칸
- DECISIONS 38: 보스전 우클릭 — 천장 고리 2개(매달려 낮은 공격 피하기), 열린 해치로 날아가 베기(2 피해) + 벌레 비 공격 추가, 벌레 공격 빈도 증가. **보스가 빨리 죽을 수 있음(HP 60) → 플레이 후 조정**
- WebGL 빌드: `94841c0` 기준까지 확인(13.6MB). 그 뒤 커밋은 빌드 안 됨 → 최종 빌드 필요

| 순서 | 할 일 | 메모 |
|---|---|---|
| 1 | **엔딩 일러스트 연결** | 그림은 `Assets/Resources/DraftUi/ending_illustration.png` (1024×576, Gemini 원본 그대로 + 워터마크만 제거, 사용자 선택). 엔딩 대사(`ending_01_placeholder.txt`) 끝난 뒤 몇 초 보여 주고 타이틀로 — `Boss/BossFight.cs` `Ending()`. 픽셀아트가 아닌 고해상도 그림이니 화면에 꽉 차게(UI Image 또는 카메라 앞 스프라이트, 필터 Point/Bilinear는 보고 결정). 다시 그린 시안들(`ending_final_*`, `ending_v5_*`)은 사용자가 거절 |
| 2 | **발판 그림 세션 결과 합치기** | 브랜치 `claude/level-art-adad4e` (세션 "Level platform art and missing map parts"). STG01~05 전체 캡처·임시 막대/빈 구역 목록까지 끝(`0db1f00`, `docs/qa/level_overview/before/`), **아트 계획 사용자 승인 대기**. 승인 → Gemini→Codex 발판 그림 → 빌더 수정 → 씬 재생성. 끝나면 main에 병합(사용자 확인 후) |
| 3 | 보스 난이도·우클릭 손맛 조정 | 사용자 플레이 후 |
| 4 | 최종 WebGL 빌드 + zip | 메뉴 `NUMBER/Build WebGL` 또는 배치 모드 `-executeMethod BuildWebGL.Build`. 빌드 뒤 `Mulmaru SDF.asset` 되돌리기 |

**이 PC 메모:** git은 GitHub Desktop 내장(`C:/Users/User/AppData/Local/GitHubDesktop/app-3.6.7/resources/app/git/cmd/git.exe`), 컴파일 확인은 scratchpad `csc_check2.py`(csproj가 오래돼서 `GrappleRope.cs`를 추가해 검사). Gemini 이미지 수신 서버는 포트 8798. `Assets/Scenes/STG05.unity`·`Sandbox.unity`의 로컬 변경은 커밋 안 함.

### 🤖 Claude — (이전) 11/11 오후 기준, 커밋 `1b8f155` 이후
| 순서 | 할 일 | 메모 |
|---|---|---|
| 1 | **기계 소품 3종 넣기** (보스 엔딩에서 부수는 랙·감시 화면·케이블) | 시안: `docs/design/concept/machine_props/` (`prop_rack.png` 48×96, `prop_screen.png` 64×80, `prop_cables.png` 80×40, Gemini → Codex). **사용자 미승인.** 화면·케이블은 괜찮고 **랙 테두리 조각이 자잘한 점으로 지저분함(AI 티) → Codex로 단순한 반복 무늬로 다듬은 뒤** 사용자 확인 → `Assets/Resources/BossArt/`에 넣고 `Boss/BossArt.cs`의 `Props[0..2]`(RackPix/ScreenPix/CablesPix)가 그림이 있으면 그걸 쓰게 (없으면 코드 그림 유지). PPU 32, 피벗 아래 가운데. CREDITS 기록 |
| 2 | 플레이 피드백(DECISIONS 34) 손맛 확인 | 사용자 플레이 후 조정 |
| 3 | 남은 에셋 (Gemini → Codex, 사용자 요청 시) | 체크포인트 표식(지금 색만 바뀌는 네모), 엔딩 일러스트 1장, STG-01 SEG-B 배경 |
| 4 | 엔딩 대사·일러스트 연결 | 사용자 대사 오면 |
| 5 | 정리(선택) | 안 쓰게 된 Shift 돌진 코드(`State.Dash`·`TryPlanDashBehind` 등)와 "RMB issen" 주석이 남아 있음. 동결 중이라 지우지 않음 |

**에셋 만드는 방법 (AI 티 줄이기, [design/AI_IMAGE_RESEARCH.md](design/AI_IMAGE_RESEARCH.md))**
- Gemini에는 **배경·소품만**, 인물·글자 없이. 광원 하나, 12색 이하, 네온 금지를 프롬프트에 쓴다.
- Codex(`gpt-6.1-sol`, reasoning high)가 원래 픽셀 크기로 다시 그림 + **고정 팔레트**(`Assets/Art/Palette/number_palette.png`) + Gemini 워터마크(오른쪽 아래 ✦)·가짜 글자 제거. 인물은 **실제 게임 스프라이트를 합성**.
- Gemini 이미지 가져오기: 앱 내장 브라우저에서 gemini.google.com(로그인 필요) → 다운로드 버튼은 저장이 안 됨. 대신 페이지에서 canvas로 PNG를 만들어 `http://127.0.0.1:8766/#name=<파일>&data=<base64>`로 넘기고, 로컬 수신 서버(파이썬, scratchpad의 `recv.py` — 새 PC에서는 다시 만들어야 함)가 저장했다. Gemini CLI는 개인 계정 지원이 끝나서 못 씀.
- Codex 호출은 프롬프트를 파일로 만들어 `Get-Content -Raw file | codex exec -m gpt-6.1-sol -c model_reasoning_effort=high -s workspace-write -` (PowerShell에 프롬프트를 바로 넣으면 따옴표가 깨짐).

*끝난 것(11/11): 피드백 6개(DECISIONS 34, Fable 검수 지적 4개 반영), 부적 타이틀 로고, 키아트(타이틀 배경·WebGL 로딩 화면·itch 표지 `docs/design/concept/keyart/itch_cover_630x500.png`), 시민 걷기 6프레임, WebGL 빌드 13.6MB 통과(WEBGL-BUILD-2).*
*끝난 것(11/10): 애니메이션 v2 연결, 4096px 넘는 띠 재포장, STG-02·04 끝 소품, 보스 v2, 주사위, 일섬, 기계 부수기 엔딩(초안 대사).*
*참고: `Assets/Art/Palette/previews/sword-slash_quant4x.png`(2720×4168)는 미리보기용이라 게임에서 안 쓰면 빌드에 안 들어간다.*

### 기능 동결 이후 (11/12~)
최종 WebGL 빌드 → itch.io 업로드(사용자, 표지는 `itch_cover_630x500.png`) → CREDITS·AI 공개 최종 → 제출 체크리스트([qa/SUBMISSION_CHECKLIST.md](qa/SUBMISSION_CHECKLIST.md)).

---

## 4. 열린 질문 (다음에 사용자에게 물을 것)
- 패링 판정(0.3초·탄 0.25초)이 손에 맞는지 (플레이 후)
- 적 돌진 중에는 좌클릭 패링이 안 된다 → 돌진 처치나 Shift로 대응. 괜찮은지
- 세로 맵 이동이 충분히 시원한지(갈고리 줄 없이 일섬·주사위로), 카메라를 더 뺄지
- 보스 난이도(HP 60·추적 칼날)가 너무 어렵지 않은지
- (STG-02~05 배경은 부품 방식으로 정리됨. STG-01 SEG-B만 Gemini #3 대기)
## 5. 작업 규칙 요약 (자세히는 AGENTS.md · CLAUDE.md)
- 커밋 머리말 `[code]` `[review]` `[asset]` `[qa]` `[docs]`.
- Unity 배치 모드는 에디터가 꺼져 있을 때만, 한 번에 하나만.
- 무료 도구·에셋만. 기획서는 `[확정]`만 구현 근거. "추천대로" 정한 것은 DECISIONS.md에 기록.
