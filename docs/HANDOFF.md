# 인수인계 — 다른 컴퓨터에서 이어 하기 (2026-11-09 새벽 갱신)

> 이 파일 하나만 읽으면 다른 PC에서 바로 이어 할 수 있게 정리했다.
> Claude에게 시작할 때: **"docs/HANDOFF.md 읽고 이어서 하자"** 라고 말하면 된다.
> 마감: 개발 **2026-11-13 00:00 KST** · ⛔ 기능 동결 **11/11 24:00** · 제출 **11/14 21:00** (18:00까지 목표)
> 전체 일정: [review/ROADMAP-2026-11-09.md](review/ROADMAP-2026-11-09.md) · 결정 기록: [design/DECISIONS.md](design/DECISIONS.md) (11~14가 최신)

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

## 2. 지금 main에 있는 것 (커밋 `6f18f41` 이후)

**게임 흐름:** Title → STG-00(오프닝) → STG-01~04 → STG-05(보스 3웨이브) → 정지 버튼 엔딩 2개. 씬은 전부 **빌더가 생성** — 메뉴 **`NUMBER/Build EVERYTHING (Title + STG-00~05)`** 하나로 전부 다시 만든다. `.unity`를 손으로 고치지 않는다. **씬은 11/09 00:13에 새로 만들어 커밋돼 있다.**

| 기능 | 결정 | 구현 기록 |
|---|---|---|
| 좌클릭: 마우스 조준(조준점, 적이 잡히면 하늘색+꺾쇠). 사거리 5u·±30° 안 적 → 적 뒤로 관통 돌진하며 처치(경로의 일반 적 전부), 없으면 제자리 공격 | DECISIONS 12 | impl/dash-attack.md |
| 좌클릭 패링: 탄이 3u 안 / 적 공격이 **0.3초 안에 닿을 때만**. 우선순위 패링 > 돌진 > 제자리 | 12 | dash-attack.md |
| Shift: 4u 안 적 → 무적 대시로 적 뒤 → **뒤돌며 반격**(일반 적 한 방). 없으면 정거리 후 뒤돌아봄. 쿨타임 3.5초. 마지막 일격·안드로이드 7번째 회피 인정 | 13 | movement-assist.md |
| 적: 7u 안이면 바로 쫓아옴, 0.3초 예고 후 **돌진 공격**, 템포 빠르게. 지붕 끝에서 멈춤 | 12 | enemy-rules.md |
| 원거리 시민(후드, 0이면 굳음) / 원거리 벌레(점 떼, STG-02부터, 0이어도 움직임) | 14 | enemy-rules.md, bug-swarm.md |
| STG-01·02·04 가로 1.5배(구간 75.9u, 배경 왼쪽은 좌우 반전 복사), 중간 발판, 적 약 2배 | 12 | levels.md, LEVEL_*.md |
| Space 충격파 설명: 러다이트 대사, 화면 힌트, HUD "SPACE 충격파", 퍼지는 막대 이펙트 | — | upgrade.md |
| 오디오(효과음 12·BGM 2), HUD 그림(얼굴 체력·방송 숫자·홀로그램 창), 배경 STG-02~05, 조작 힌트, 보스 화면 | — | audio.md, draft-sprites.md, bg-art.md |
| 캐릭터 스프라이트 전부 정면 기준 그림으로 다시 그림 (주인공·근접/원거리 시민·안드로이드) | — | draft-sprites.md |
| 속도감 FX(발밑 먼지·속도선·대시 시작), 히트스톱 연속 방지(0.25초 간격, 패링은 예외), FX 재사용(풀) | — | juice.md |
| **F3** = FPS·프레임 시간 표시 (에디터·개발 빌드만). `spikes`가 오르면 진짜 렉, 안 오르면 히트스톱 연출 | — | Core/FrameStats.cs |
| 네온 조명(Card H) 코드·테스트 씬 — **스테이지엔 아직 안 붙임** | 9 | LIGHTING_DESIGN.md |

---

## 3. 해야 할 것 (우선순위 순)

### 👤 사용자
1. **플레이 (씬은 이미 새로 만들어져 있음)** — Title에서 Play. 렉이 느껴지면 F3. 메모는 [qa/PLAYTEST-2026-11-09.md](qa/PLAYTEST-2026-11-09.md)에 이어서, 체크 표는 [qa/PLAYTEST-GUIDE.md](qa/PLAYTEST-GUIDE.md).
   - 볼 것: 맵 확대 후 속도감, 좌클릭 돌진·패링 손맛, Shift 반격, 0 해킹 때 시민(근접·원거리) 굳음, 적 수, 렉(F3).
2. **WebGL 빌드를 브라우저에서 확인** — 에디터가 꺼져 있으면 Claude에게 "WebGL 빌드해서 브라우저로 열어줘". 에디터 렉과 실제 렉을 구분하는 가장 확실한 방법.
3. **팀원 이름·역할·만 19세 이상 확인 (D9)** — CREDITS.md Team, SUBMISSION_PAGE.md. **없으면 제출 불가.** 기한 11/13 정오.
4. **STG-01 SEG-B 배경(Card G) 승인** — `docs/design/concept/stg01/B_lit_composite_preview.png`
5. **대사 확인** — [design/DIALOGUE_DRAFT.md](design/DIALOGUE_DRAFT.md) (안 고치면 초안 그대로)
6. 이전 PC의 `Number/` 폴더, `Unity Game Jam.txt`는 쓰레기 — 새 PC엔 없다.

### 🤖 Claude (Opus 구현 → Fable 검수)
| 할 일 | 메모 |
|---|---|
| 플레이 피드백 반영 | 가장 먼저 |
| **안드로이드 과부하 그림 다시** (`android_stunned_1f`가 뭉개져서 안 읽힘) | Codex, 정면 기준 그림 `enemy_android_stand_big.png` 기준 |
| 주인공 대기 1·2프레임 방향 차이, 걷기 3번째 프레임 소매 패치가 글자처럼 보임 | 거슬리면 Codex로 수정 |
| STG-01 SEG-B 배경 연결 (승인 후) | 검수 결과: 그림에 없는 발판 CRATE-B 제거, 계단 충돌 추가 또는 골 위치 이동, 가짜 글자 같은 네온 2개(neon_B_03·11) 정리, 잎 사각 경계. 단계: Opus 리뷰 보고(이전 세션)대로 `Assets/Art/Backgrounds/bg_stg01_segB_street.png` + `Neon/stg01_segB/` |
| 네온 조명 스테이지 연결 | 정렬 레이어 이름 결정(DECISIONS 9 Q6) 필요. 연결 단계: LightingSetup.BuildInScene을 각 빌더에서 호출, 배경을 Background/World 레이어로 |
| WebGL 빌드 + 브라우저 확인 (Chrome·Firefox): 한글 폰트, 우클릭 막힘, 포커스, 첫 클릭 전 소리, 용량 | 에디터 꺼져 있을 때 `NUMBER/Build WebGL` 배치 실행 |
| 벌레 점 떼가 매 프레임 텍스처를 다시 그림 | F3에서 렉이 보이면 갱신 주기 줄이기 |
| 남은 "우클릭 패링" 문구 정리 | PrototypeDebug.cs, PROTOTYPE_ARCH.md, bug-swarm.md, SUBMISSION_CHECKLIST.md |
| 대사 넣기, 이펙트 미세 조정 | 사용자 확인 후 |

### 기능 동결 이후 (11/12~)
최종 WebGL 빌드 → itch.io 업로드(사용자) → CREDITS·AI 공개 최종 → 제출 체크리스트([qa/SUBMISSION_CHECKLIST.md](qa/SUBMISSION_CHECKLIST.md)).

---

## 4. 열린 질문 (다음에 사용자에게 물을 것)
- 패링 판정 0.3초가 손에 맞는지 (플레이 후)
- 적 돌진 중에는 좌클릭 패링이 안 된다 → 돌진 처치나 Shift로 대응. 괜찮은지
- 맵이 1.5배로도 작게 느껴지면: 더 늘릴지, 카메라를 멀리 뺄지

## 5. 작업 규칙 요약 (자세히는 AGENTS.md · CLAUDE.md)
- 커밋 머리말 `[code]` `[review]` `[asset]` `[qa]` `[docs]`.
- Unity 배치 모드는 에디터가 꺼져 있을 때만, 한 번에 하나만.
- 무료 도구·에셋만. 기획서는 `[확정]`만 구현 근거. "추천대로" 정한 것은 DECISIONS.md에 기록.
