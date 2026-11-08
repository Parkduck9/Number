# 아키텍처 패턴 정리 (병합 후, 2026-11-08)

> 네 브랜치(enemy-rules · boss · upgrade · levels) 병합 직후 Fable이 코드를 읽고 정리했다. 줄 번호는 main `70ce8e8` 기준.
> 원칙: 잼 7일짜리 코드다. MonoBehaviour + static 서비스 + ScriptableObject 하나로 충분하며, 프레임워크·인터페이스 추상화는 쓰지 않는다. 새 시스템을 넣을 때는 아래 패턴 중 하나를 그대로 따른다.

## 1. 쓰고 있는 패턴

### 1.1 상태 기계 = `enum State` + `switch` (클래스로 나누지 않음)
| 어디 | 줄 |
|---|---|
| `TestEnemy` (11개 상태: Approach…Frozen, Special) | `Assets/Scripts/Enemy/TestEnemy.cs:7` enum, `:203` UpdateState, `:341` EnterState |
| `PlayerController` (6개) | `Assets/Scripts/Player/PlayerController.cs:8`, `:126`, `:191` |
| `BossFight` (Idle→Announce→Spawning→Fighting→Unlocked) | `Assets/Scripts/Boss/BossFight.cs:43`, `:61`, `:92` |
| `StopButton.Phase`, `OpeningSequence.Phase` (코루틴 + enum) | `StopButton.cs:24`, `OpeningSequence.cs:8` |

- **왜:** 한 파일에서 전이가 다 보인다. 상태마다 클래스를 만들면 잼에서는 파일만 늘고 디버그가 어려워진다.
- **규칙:** 전이는 반드시 `EnterState()`로 (타이머 `stateEnteredAt` 리셋 + 진입 효과). 상태별 시간은 `Time.time - stateEnteredAt`.
- **확장:** 상태 추가 = enum 한 줄 + `UpdateState` case + `EnterState` case + (색이 있으면) `ApplyVisual` case. 다른 세션이 같은 파일에 상태를 넣을 때 충돌이 나는 곳은 이 네 군데뿐이다.
- **외부에서 상태를 밀어 넣는 공개 메서드**는 `ForceStagger(float)`(`TestEnemy.cs:436`)처럼 "기존 상태로 진입시키는 한 줄 래퍼"로만 둔다. 새 상태를 외부용으로 만들지 않는다.

### 1.2 Observer = C# `event` (매 프레임 폴링 대신)
| 발행 | 구독 |
|---|---|
| `BroadcastBoard.NumberChanged` (`Broadcast/BroadcastBoard.cs:51`, 발생 `:95`) | `TestEnemy.cs:98` (Start 구독, `:105` OnDestroy 해제), `BossBoardView.cs:16` |
| `IBroadcastListener` 인터페이스 (`BroadcastBoard.cs:8`, 호출 `:96-97`) | `OpeningCitizen.cs:23` (같은 일을 하는 두 번째 통로, 아래 제안 E3) |
| `BossFight.WaveStarted / WaveCleared / Unlocked` (`BossFight.cs:37-39`) | `BossBoardView.cs:15` |
| `Shockwave.UnlockChanged`(static) / `Used` (`Player/Shockwave.cs:20`, `:32`) | 아직 없음 (HUD·SFX 자리) |
| `Loc.Changed` (`UI/Loc.cs:9`) | `LocalizedText.cs:39` |

- **왜:** 방송이 바뀌는 순간에만 반응하고, 뷰(전광판 문구·HUD)가 로직을 모른다.
- **규칙:** 구독은 `Start/OnEnable`, 해제는 `OnDestroy/OnDisable`에서 짝을 맞춘다. static 이벤트는 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`에서 `null`로 비운다 (`Shockwave.cs:29-30`, Domain Reload 끔 대비).
- **예외(의도된 폴링):** `BossFight.NewHackThisFrame` (`BossFight.cs:134`) — 같은 숫자로 해킹하면 `NumberChanged`가 안 나서 폴링한다. `BossFight.AllStunned` (`:125`) — 적 5마리라 폴링이 더 단순하다.
- **확장:** 소리·연출은 기존 이벤트를 구독해서 붙인다 (방송 차임은 `BroadcastHackUi.cs:93`의 TODO 자리, 충격파 소리는 `Shockwave.Used`). 발행 쪽 파일을 고치지 않는다.

### 1.3 static 서비스 (씬 오브젝트 없는 전역)
| 무엇 | 어디 | 역할 |
|---|---|---|
| `GameFlow.InputLocked / EscConsumed` | `Core/GameFlow.cs:5` | 대화·전환 중 입력·AI 잠금. **`Time.timeScale`과 분리** (timeScale은 패리 히트스톱·일시정지 전용) |
| `StageFlow.Stages / LoadNext / LoadTitle` | `Core/StageFlow.cs:8` | 스테이지 순서 한 곳. 없는 씬은 건너뜀 |
| `BroadcastRules` | `Broadcast/BroadcastRules.cs:3` | 방송 규칙 조정값 (static 필드 → 씬 재생성 없이 바뀜) |
| `BroadcastBoard.All / At(pos)` | `BroadcastBoard.cs:18`, `:31` | 구역 레지스트리. 적·HUD가 같은 "어느 구역" 규칙을 씀 |
| `SpriteFx`, `ParryFx`, `UiKit`, `Loc`, `DialogueLanguage` | `Combat/SpriteFx.cs:6`, `Combat/ParryFx.cs:4`, `UI/UiKit.cs:9`, `UI/Loc.cs:6`, `Dialogue/DialogueData.cs:5` | 이펙트·UI 생성·언어 유틸 |
| `Shockwave.Unlocked` (static 진행 상태) | `Shockwave.cs:19` | 업그레이드 1개뿐이라 매니저 없이 static |
| `RespawnManager` static 기록 | `Checkpoint/RespawnManager.cs:11-14` | 씬 리로드를 넘어 체크포인트 유지 |
| 씬 싱글톤 | `DialoguePlayer.Instance` (`:11,44`), `PauseMenu` 부트스트랩 (`PauseMenu.cs:24-34`, DontDestroyOnLoad) | |

- **규칙:** static 상태에는 반드시 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)] ResetStatics()`를 둔다 (`Shockwave.cs:29`, `RespawnManager.cs:20`, `PauseMenu.cs:24`).
- **타이머 규칙 (일시정지·히트스톱 호환):** 게임플레이 타이머는 `Time.time`(적·전광판·충격파 쿨다운·보스 웨이브). `InputLocked` 동안은 `deltaTime`만큼 밀어서 멈춘 것처럼 만든다 (`TestEnemy.cs:110-119`, `EnemyProjectile.cs:41`). 연출·UI만 `unscaled` (`DialoguePlayer`, `LevelTransition.Fade:59`, `StageGoal:17`, HUD 펀치 `BroadcastHackUi.cs:93`). 히트스톱은 `PlayerController.cs:268-272`에서 timeScale 0, `PauseMenu.LateUpdate:75`가 일시정지 중 다시 0으로 되돌린다.

### 1.4 데이터 = ScriptableObject 1개 + 텍스트 파일 + 빌더 표
- `UiTheme` (`UI/UiTheme.cs:7`, `Resources/UiTheme.asset`, 없으면 기본값 `:25-35`), `DialogueSpeakers` (`Dialogue/DialogueSpeakers.cs:6`).
- 대사는 `Assets/Data/Dialogue/*.txt` (파서 `DialogueData.cs:23`). 사용자가 내용만 바꾼다.
- 레벨 배치는 **빌더 파일 위쪽 `static readonly` 표**가 유일한 원본 (`Stg02Builder.cs:19-51`, `Stg04Builder.cs:19-54`, `BossFight.cs:30-35` 웨이브 표). 씬은 손으로 고치지 않고 메뉴 `NUMBER/Build …`로 다시 만든다.

### 1.5 씬 빌더 (Editor) = 데이터 표 → `SerializedObject`로 필드 주입
- 공용 도우미 `StageBuildKit` (`Assets/Editor/StageBuildKit.cs:16`): 콜라이더·적·전광판·카메라·HUD·Build Settings 동기화(`:121`).
- 필드 주입은 `SetInt/SetFloat/SetObj/SetVector2` (`:421-425`). 다른 세션의 필드가 아직 없을 때는 `HasProp` + `Todo` 로그로 넘어간다 (`:419`, `:244-249`).
- 보스는 **비활성 템플릿 + 런타임 복제** (`Stg05Builder.cs:96-114` BuildTemplate, `BossFight.cs:119-120` Instantiate). 전용 프리팹이 생기면 템플릿 세 줄만 바꾼다.
- 플레이어 전용 컴포넌트는 프리팹을 안 고치고 런타임 `AddComponent` (`PlayerController.cs:89` Shockwave).
- **주의:** `Stg01Builder`는 `StageBuildKit` 이전에 만든 복사본이라 도우미가 중복돼 있다 (제안 L1).

### 1.6 코루틴 연출 (상태 기계가 아닌 일회성 순서)
`OpeningSequence.Start` (`:19`), `LevelTransition.Run` (`:30`), `LudditeMeeting.Finish` (`:25`). "대화 끝"은 이벤트가 없어 `while (GameFlow.InputLocked) yield return null`로 기다린다. 한 번 흐르고 끝나는 것만 코루틴, 되돌아가는 것은 enum 상태 기계.

### 1.7 코드로 만드는 UI
`BroadcastHackUi.BuildUi` (`:160`), `StopButton.BuildUi` (`:96`), `PauseMenu.Build` (`:100`), `TitleScreen.Start` (`:10`), `UiKit`. 스타일 상수는 파일 위쪽 `static readonly` 또는 `UiTheme`. 프리팹 UI 없음 → 씬 병합 충돌이 없다.

### 1.8 다른 세션 코드에 대한 느슨한 결합 (리플렉션)
`StageBuildKit.AddByName / FindType` (`:400-417`, Editor 전용 — 괜찮음), `LudditeMeeting.GrantUpgrade` (`:35`, **런타임 리플렉션 — WebGL 코드 스트리핑 위험, 아래 U4/병합 후 직접 호출로 바꿈**). 병합이 끝났으니 런타임 리플렉션은 더 쓰지 않는다.

## 2. 리팩터 제안 통합 순위

네 기록의 "패턴 / 리팩터 제안"을 합쳐 매겼다. **ADOPT NOW** = 가치 높고 위험 낮고 1시간 안, 11/13 전. **LATER** = 잼 뒤 또는 여유 생기면. **DROP** = 안 함.

### ADOPT NOW (위에서부터)
| # | 제안 (출처) | 범위 | 한 줄 이유 |
|---|---|---|---|
| A1 | 레벨·보스 빌더가 적 `kind`(Bug=1, Android=2)와 전광판 `zoneX`를 지정하게 (enemy-rules E5) | `StageBuildKit.cs:244-249` (`android` bool → `kind` enum 2), `Stg05Builder.cs:98-114` (bug=1, android=2) → `Build All Stages` + `Build STG-05` 재실행 | 지금 STG-04·보스의 안드로이드·벌레가 전부 시민이라 0 방송에 같이 굳는다 (확정 규칙 위반) |
| A2 | `LudditeMeeting.GrantUpgrade` 리플렉션 → `Shockwave.SetUnlocked(true)` 직접 호출 (levels·upgrade 둘 다 제안) | `LudditeMeeting.cs:29,35-46` | WebGL IL2CPP 스트리핑이 미사용 public static을 지울 수 있어 업그레이드가 조용히 안 열릴 수 있다. 1줄 |
| A3 | 새 게임 시작 때 진행 상태 초기화 (upgrade U4) — `GameFlow.ResetRun()` 대신 `TitleScreen.Start`에 `Shockwave.SetUnlocked(false)` 한 줄 | `TitleScreen.cs:12` | 진행 상태가 1개뿐이라 메서드 하나 더 만들 이유가 없다 |
| A4 | `Stg01Builder.SetBuildScene` → `StageBuildKit.SyncBuildSettings()` (levels L2) | `Stg01Builder.cs:166,324-330` | STG-01 재빌드 때마다 Build Settings 0번이 STG01이 돼 WebGL이 오프닝·타이틀을 건너뛴다 |
| A5 | Title 씬 생성 + Build Settings 동기화 (levels L3의 근본 해결) | 메뉴 `NUMBER/Build Title Scene` → `NUMBER/Sync Build Settings` | `StageFlow.LoadTitle`이 가는 곳이 없어 엔딩·일시정지 "타이틀로"가 제자리에 멈춘다 |
| A6 | 제출 전 `Prototype`·`SampleScene`을 Build Settings에서 끄기 (levels L6) | `ProjectSettings/EditorBuildSettings.asset` | 용량. `SyncBuildSettings`가 "기존 항목 유지"라 자동으로 빠지지 않는다 |
| A7 | `TestEnemy.ForceStagger`에 `Frozen` 가드 (검토에서 발견) | `TestEnemy.cs:438` | 굳은 시민에 충격파를 쓰면 경직 뒤 `FinishReset`→Approach로 풀려 0 방송 중에 공격한다 |

### LATER
| # | 제안 (출처) | 이유 |
|---|---|---|
| L1 | `Stg01Builder` → `StageBuildKit` 사용 (-300줄) (levels) | 동작 이득 없음. STG01 재생성·재검증이 필요해 마감 뒤 |
| E1 | `TestEnemy` 상태별 클래스 분리 (enemy-rules) | 444줄·11상태로 아직 한 파일에서 읽힌다. 동작 같은 큰 리팩터는 잼 뒤 |
| E3 | `IBroadcastListener`와 `NumberChanged` 하나로 (enemy-rules) | 지금은 `OpeningCitizen`만 인터페이스를 쓴다. 잼 뒤 인터페이스 제거 |
| B1 | `BroadcastBoard.Hacked` 이벤트 → `BossFight` 폴링 제거 (boss) | 폴링이 검증돼 동작 중. 이벤트로 바꾸면 "...귀엽군" 문구도 같은 숫자 해킹에 반응하는 작은 이득 |
| B2 | `SetAiNumber(n, force)` (boss) | 웨이브 시작 때 플레이어 해킹을 AI가 덮을지는 기획 판단. 지금은 유지 시간 4/3초로 완화됨 |
| B5 | 에디터 공용 `EnemyVariants.Apply` (boss) | A1을 하면 같은 설정이 세 파일에 생긴다. 전용 프리팹(벌레·안드로이드)이 생기면 그걸로 대체하는 게 더 깔끔 |
| U3 | 충격파 쿨다운 OnGUI → uGUI HUD (upgrade) | HUD 세션 몫 |
| L3 | `LevelTransition` Goal에서 다음 씬도 Title도 없으면 페이드 복구·잠금 해제 (levels) | A5(Title 생성)로 사라진다. 11/13까지 Title이 안 생기면 그때 4줄 |
| E4 | 방송 차임 (enemy-rules) | 오디오 세션이 `BroadcastHackUi.cs:93` TODO에 한 줄 |

### DROP
| # | 제안 (출처) | 이유 |
|---|---|---|
| E2 | `IEnemyBroadcastRule` 전략 객체 (enemy-rules) | 규칙 3개·숫자 더 늘릴 계획 없음. 블록 3개가 가장 읽기 쉽다 |
| B3 | `TestEnemy.Stunned` 이벤트 (boss) | 적 5마리 폴링은 비용 0. 이벤트는 해제 누락 위험만 추가 |
| B4 | `StopButton.Unlock()` / 잠금 플래그 (boss) | `SetActive`가 이미 그 역할 |
| B6 | `RespawnManager` 재시작/새로시작 공개 (boss) | 보스방 중간 체크포인트를 안 하기로 함 |
| U1 | `IStaggerable` 인터페이스 (upgrade) | 보스가 몸 없는 전광판(4-A)이라 구현체가 `TestEnemy` 하나뿐 |
| U2 | `staggerUntil` 필드 (upgrade) | `stateEnteredAt` 이동 한 줄이 이미 동작. 취향 변경 |
| U5 | Shockwave 액션을 `.inputactions` 에셋으로 (upgrade) | 리바인딩 UI 계획 없음 |
| L4 | `DialoguePlayer.TalkFinished` 이벤트 (levels) | 우려한 경쟁(일시정지가 대화 중 잠금 해제)은 `PauseMenu.CanOpen`(`:63`)이 잠금 중 열리지 않아 일어나지 않는다 |
| L5 | 오프닝 시민을 `TestEnemy`로 교체 (levels) | 47줄 플레이스홀더가 동작하고, `TestEnemy`에 "전투 끔"이 없어 오히려 코드가 는다 |

## 3. 새 코드를 넣을 때 체크
1. 상태가 되돌아가면 enum 상태 기계, 한 번 흐르면 코루틴.
2. 다른 시스템에 알릴 일은 `event`. 매 프레임 폴링은 "같은 값 재발생"처럼 이벤트가 못 잡는 경우에만, 주석으로 이유를 적는다.
3. 타이머는 `Time.time` + `InputLocked` 밀기. 연출만 unscaled.
4. 조정값은 `[SerializeField]`(T) 또는 static 설정 클래스. 씬에 저장되는 값은 빌더를 다시 돌려야 바뀐다는 점을 기록에 남긴다.
5. 씬·프리팹은 빌더로만. 다른 세션 필드는 `HasProp`로 넘기되, 병합 뒤엔 직접 참조로 바꾼다.
6. 런타임 리플렉션 금지 (WebGL 스트리핑).
