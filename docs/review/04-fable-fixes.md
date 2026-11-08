# 리뷰 04 — Fable 전체 검토·수정 (2026-11-08)

> 기준 커밋 `c5e7f8a` (main). `Assets/Scripts`, `Assets/Editor`, `ProjectSettings`, `Assets/Plugins/WebGL`, 씬 빌더와 생성된 씬 YAML을 읽고 확인된 문제만 고쳤다. 리뷰 01(Opus)의 코드 항목은 `01-#N`으로 표시했다.
> Unity Play·WebGL 빌드는 하지 않았다(정적 분석). 배치 모드 컴파일: `error CS` **0건** (`scratchpad/unity_compile1.log`). 커밋은 하지 않았다(부모 세션 몫).
> 씬은 다시 만들지 않았다: 고친 빌더는 `Stg01Builder`뿐이고(SEG-B 컨셉 이미지 제거), 지시대로 STG-01 아트는 건드리지 않기 위해 재생성하지 않았다. 다른 빌더는 바뀌지 않았으므로 생성된 씬과 코드가 일치한다.

## 수정·판정 표

| # | 문제 | 근거 (file:line, 수정 전 기준) | 수정 내용 | 상태 |
|---|---|---|---|---|
| 1 (01-#1) | **히트스톱 중 입력이 사라진다.** 타격·처치·패링마다 timeScale 0인 2~5프레임 동안 `Update`가 일찍 끝나 그 프레임의 `WasPressedThisFrame`가 버려진다. 충격파도 같음 | `PlayerController.cs:116-119`, `Shockwave.cs:49` | 히트스톱 분기에서 `BufferDuringHitStop()`: 점프·공격은 기존 버퍼(`jumpBufferedUntil`·`attackBufferedUntil`, Time.time이 멈춰 있으므로 끝난 뒤부터 센다)에 기록, 패링·대시는 `pendingParry/pendingDash`로 들고 있다가 히트스톱이 끝나는 프레임에 `ParryPressed/DashPressed`로 합쳐 처리(그 프레임에 누른 것과 같은 의미, 프레임 끝에 비움). InputLocked면 버림. `Shockwave`는 `pendingPress`로 같은 방식 | 수정됨 |
| 2 | **NUM-7 도핑 시계가 씬 시작 때 깨짐.** `BroadcastBoard.Awake`가 `NumberChangedAt`을 0으로 두어, 처음부터 7을 방송하는 구역(STG-02 SEG-B, STG-04 SEG-A)에서 `Num7Phase = Time.time - 0`이 됨 → 전 씬에서 넘어온 Time.time(수 분)에 따라 도핑/탈진 위상이 제멋대로(도착하자마자 탈진 등) | `BroadcastBoard.cs:59`, `TestEnemy.cs:78-80`, `:177-190` | `Awake`에서 `NumberChangedAt = Time.time` | 수정됨 |
| 3 (01-#17) | 에디터 Domain Reload 꺼짐(`EditorSettings.asset:27-28`)인데 `GameFlow.InputLocked`·`EscConsumedFrame`을 초기화하지 않음 → 이전 Play의 잠금이 남을 수 있음 (RespawnManager 없는 STG00 직접 재생) | `GameFlow.cs:7-9` | `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics()` 추가 (Shockwave·RespawnManager와 같은 규칙) | 수정됨 |
| 4 (01-#14) | 적 탄이 긴 프레임(로딩 히치·탭 전환)에 플레이어·바닥을 뚫음 — 이동 후 끝점만 OverlapBox | `EnemyProjectile.cs:43-52` | 이동 전→후를 `Physics2D.BoxCast`로 스윕 (시작 겹침도 잡으므로 기존 동작 포함) | 수정됨 |
| 5 (01-#12) | 언어 전환 시 정지 버튼 프롬프트·선택지 문구가 안 바뀜 | `StopButton.cs:111`, `:122` | `promptText/choiceText` 보관, `Loc.Changed += RefreshTexts` (BroadcastHackUi와 같은 방식). 표시 중 대사는 다음 줄부터 반영(변경 없음) | 수정됨 |
| 6 (01-#13) | 다이얼 0 경고가 영어만 | `BroadcastHackUi.cs:170` | `T("EVERYONE STOPS BREATHING", "모두 숨을 멈춘다")` | 수정됨 |
| 7 (01-#18) | 리스폰(씬 리로드) 뒤 체크포인트 표식이 비활성 색으로 돌아감 | `Checkpoint.cs:15` | `RespawnManager.IsRecordedAt(pos)` 추가, `Awake`에서 기록 위치와 같으면 활성 색. Opus 검수 지적 반영: 리스폰 리로드일 때만 켜지도록 `respawnPending` 조건 추가 (다른 경로로 리로드하면 시작점으로 돌아가는데 표식만 켜져 있던 문제) | 수정됨 |
| 8 (01-#8) | 릴리스 빌드에 충격파 해금·쿨다운 표시가 없음 (OnGUI는 에디터/개발 빌드 전용) | `Shockwave.cs:80-90` | OnGUI 제거 → 런타임 코드 생성 HUD(`UiKit.CreateCanvas` 정렬 40, 왼쪽 아래 TMP 한 줄): 해금되면 보이고 `READY`(시안색) / `3.2s`(흐림). 0.1초 단위로 바뀔 때만 문자열 갱신, `Loc.Changed`로 언어 반영. 씬·프리팹 변경 없음 | 수정됨 |
| 9 (01-#9) | `Stg01Builder`를 다시 돌리면 SEG-B에 컨셉 시안 `13_style_frame_v2.jpg`(가짜 HUD·적이 그려진 AI 이미지)가 배경으로 들어감 | `Stg01Builder.cs:40-42`, `:193-219` | SEG-B `srcImage = null`, `ImportBackground`가 null이면 건너뜀. STG-01 씬·`Assets/Art/Placeholder`는 건드리지 않음 (`PLACEHOLDER_stg01_segB_bg.jpg`는 그대로 남아 있음 → 에셋 담당이 지울지 결정) | 수정됨(빌더만) |
| 10 (01-#20) | GDD 12장 "공격하면 하나씩 줄어듦"이 5장 `[확정]`(숫자는 줄지 않음)과 충돌 | `docs/design/GDD.md:271` vs `:103` | 12장 문구 정정 + 18장 변경 기록 1줄 | 수정됨(문서) |
| 11 (01-#19) | 임시 스프라이트가 어느 쪽을 보고 그려졌는지 미확인 (`flipX = Facing < 0` 전제) | `PlayerController.cs:325`, `TestEnemy.cs:443` | `Assets/Art/Drafts/Sprites/*_preview4x.png`가 Resources 스트립의 정확한 4배 확대(픽셀 비교 불일치 0)임을 확인하고 보니 **둘 다 오른쪽을 본다** → 현행 flipX가 맞음. 변경 없음 | 확인됨-변경 없음 |
| 12 | 충격파 쿨다운(`readyAt`)이 대화(InputLocked) 중에도 흐름. 패턴 규칙(1.3 타이머)과 다르지만 플레이어에게 유리한 쪽이라 동작 문제는 아님 | `Shockwave.cs:33`, `:64` | 없음 (1줄로 고칠 수 있음: InputLocked면 `readyAt += Time.deltaTime`) | 보고만-사소 |
| 13 | `StageBuildKit.BuildEnemy`: 안드로이드에 `fixedNumber > 0`을 주면 Todo 메시지가 "TestEnemy.kind missing"으로 잘못 찍힘 (현재 데이터에는 해당 없음) | `StageBuildKit.cs:254-255` | 없음 (에디터 로그 문구만) | 보고만-사소 |
| 14 | QA P1/P2(f834116) 반영 여부: 적끼리 충돌 끔, OverlapBoxAll, 패링 유지, 탄 진행 방향, STG-04 안드로이드 값, 보스 x 거리, 죽은 상태 차단, 잠금 중 타이머 정지, NUM-7 공유 시계, NUM-3 1.5초, 새 게임 초기화, 일시정지 중 히트스톱 — 코드와 씬 YAML에서 모두 확인 | (리뷰 01 "이미 고쳐진 항목"과 동일) | 없음 | 확인됨 |
| 15 | 한 방 처치 + 0.5초 뒤 사라짐: `Stunned` → `SetActive(false)`라 보스 클리어 판정·방송 구독·충격파(`ForceStagger`의 Stunned 가드)가 모두 안전. Android 제외 규칙도 DECISIONS와 일치 | `TestEnemy.cs:124-129`, `:406-427`, `BossFight.cs:125-130` | 없음 | 확인됨 |
| 16 | 상태 기계·timeScale·일시정지·리스폰 흐름: 히트스톱은 `OnDisable`/`PauseMenu.LateUpdate`가 복구, 일시정지는 `InputLocked` 중 열리지 않음, 대화 Esc/P는 `EscConsumedFrame`+실행 순서 1000으로 분리, 리로드·타이틀·`StageFlow.Load`가 잠금을 풀어 줌. 누수 경로를 찾지 못함 | `PauseMenu.cs`, `RespawnManager.cs`, `StopButton.cs`, `LevelTransition.cs` | 없음 | 확인됨 |
| 17 | WebGL: 스레드 끔, 파일 IO 없음, PlayerPrefs는 언어 1키, 런타임 리플렉션 없음(`LudditeMeeting`은 직접 호출), Resources 동기 로드만, jslib 1개(우클릭 차단). 설정(Brotli·Fallback·960×540·예외 2·스트리핑 Medium)이 `ProjectSettings.asset`에 반영됨 | `ProjectSettings.asset:47-48, 563-600`, `BuildWebGL.cs` | 없음 | 확인됨 |

## 열린 설계 질문 (사용자 결정 전까지 코드 변경 안 함)
1. (01-#5, QA FLOW C02/C05) 해킹 가능 시점: GDD "STG-03 이후"인데 STG-01에 해킹 UI가 있고 STG-02에는 방송 HUD조차 없음. HUD만/다이얼까지를 스테이지별로 어떻게 할지.
2. (01-#6, QA BOSS) 보스 3웨이브 **예고 2초 중** 해킹하면 적 소환 없이 정지 버튼이 열림 — 허용인지.
3. (01-#7) GDD 6장 `[확정]` "대시가 경로의 적·장애물을 부숨"이 구현되지 않음(대시는 회피만). 구현할지 문구를 고칠지.
4. (01-#11, QA) 체크포인트 재진입 무한 회복 / 충격파가 반격 창(1.5초)을 1초로 줄임 / 마지막 일격 대시 경직이 히트박스 겹침 요구.
5. (01-#15) 게임패드: 패링·E(대화·해킹·정지 버튼)가 패드에 없음 — "패드 미지원" 명시 또는 바인딩 추가.
6. (새로 발견) NUM-7 구역 시계의 시작점: 이번 수정(#2)으로 **씬이 열리는 순간**부터 7초/2초 주기가 돈다. 플레이어가 SEG-B에 1분쯤 뒤에 도착하면 주기 위상이 임의 지점이다. "플레이어가 구역에 들어갈 때 시계를 다시 시작"(차임과 함께)할지 결정 필요 — 결정되면 `BroadcastHackUi.UpdateHud`의 펀치 시점에 한 줄.
7. (01-#2·#3·#10·#16) 오디오 브랜치 병합, CREDITS·SUBMISSION_PAGE 문구, 미사용 패키지 — 사용자/문서 담당 몫. 코드 변경 없음.

## 에셋 필요 사항
- STG-00 오프닝 시민(`OpeningCitizen`)은 아직 사각형이다. 시민 임시 스프라이트(`citizen_attack_3f`)를 쓰려면 `Stg00Builder.cs:47`의 `AddSquare`를 스프라이트로 바꾸는 코드 1줄이 더 필요(`DraftSprites.Setup` 재사용 가능). 확정 아트 뒤에 할지 결정.
- `Assets/Art/Placeholder/PLACEHOLDER_stg01_segB_bg.jpg`(컨셉 시안 복사본)는 빌더에서 더 이상 참조하지 않는다. 지울지는 에셋 담당 결정 (이 세션은 Art 폴더를 건드리지 않음).
- 러다이트(STG-03)·전광판·발판·벌레·안드로이드는 여전히 사각형 (로드맵 그대로).
- 충격파 HUD는 글자 한 줄이다. 아이콘이 생기면 `Shockwave.BuildHud`에서 TMP 대신 Image로 바꾸면 된다.

## 죽은 코드 (지우지 않음, 보고만)
- `Assets/Scripts/Level/StageGoal.cs`: 모든 스테이지가 `LevelTransition`(Goal)을 쓴다. 참조는 `Assets/Editor/FlowTestSetup.cs`뿐.
- `Assets/Scripts/Core/PrototypeDebug.cs`, `CameraTestMover.cs`: Prototype/Test 씬 전용 (빌드 목록에서 꺼져 있음).
- `EnemyNumber.SetProgress()`: 호출 0건. `BossFight.WaveCleared`, `Shockwave.Used`: 발행만 하고 구독자 없음(효과음 자리). `BroadcastBoard.AiNumber` getter: 사용 0건.
- `InputSystem_Actions` Player 맵의 `Look / Interact / Crouch / Previous / Next` 액션: 코드가 읽지 않음 (E는 `Keyboard.current`로 직접 읽음).
- `Stg01Builder`의 private 도우미는 `StageBuildKit`과 중복 (ARCHITECTURE_PATTERNS L1, 잼 뒤).

## Play 모드 확인 체크리스트
| 무엇을 하면 | 무엇이 보여야 하나 |
|---|---|
| STG-01에서 적을 때리는 **순간** 우클릭(패링)·Shift(대시)·좌클릭·W를 누른다 | 히트스톱(0.04~0.08초)이 끝나자마자 그 동작이 나간다 (씹히지 않음). 패링/대시는 히트스톱 중 한 번만 눌러도 됨 |
| 적을 때리는 순간 Space(충격파, F9로 해금) | 히트스톱 뒤 충격파 발동, 왼쪽 아래 HUD가 `5.0s`부터 줄어 `READY` |
| STG-03 러다이트 대화 뒤 | 왼쪽 아래에 `SPACE  Shockwave: READY`가 나타남. 일시정지 → 언어 전환 → 한국어 `SPACE  충격파: 준비` |
| STG-02 SEG-B(방송 7) 도착 | 적이 빨라지고, **도착 후 9초 주기 안의 어느 시점에** 2초 탈진(Open)이 옴. 씬을 몇 번 다시 열어도 "들어가자마자 탈진" 같은 극단이 매번 반복되지는 않음 |
| 적 탄(STG-05 2웨이브 벌레 대역)을 정면에서 패링 / 탭을 잠깐 바꿨다 돌아옴 | 탄이 플레이어를 뚫고 지나가지 않음 |
| STG-05 정지 버튼 근처에서 일시정지 → 언어 전환 → 닫기 | `[E] 정지 버튼` / `[E] STOP BUTTON` 프롬프트와 1/2 선택지가 바뀐 언어로 나옴 |
| 해킹 다이얼에서 0 선택 (한국어) | `모두 숨을 멈춘다` 경고 |
| 체크포인트 CP-B0를 찍고 죽는다 | 리스폰 뒤 CP-B0 표식이 **노란(활성)** 색으로 유지 |
| 에디터에서 STG00을 대화 도중 정지했다가 다시 Play | 조작이 잠겨 있지 않음 |
| `NUMBER/Build STG-01`을 다시 실행(선택, 아트 담당과 상의 후) | SEG-B에 컨셉 이미지가 들어가지 않음. (SEG-A `PLACEHOLDER_stg01_segA_bg.png`는 여전히 들어감 — 현재 씬에는 배경 0개이므로 재생성 시 STG-01 모습이 바뀐다는 점 주의) |
