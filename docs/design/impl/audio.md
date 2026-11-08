# 오디오 구현 기록 (결정 10-A)

> 2026-11-08 · audio 세션 (Claude Opus). 근거: [DECISIONS.md](../DECISIONS.md) 10-A `[확정]`.
> 브랜치 `claude/audio-4c66f5`. 다른 스크립트에는 호출을 넣지 않았다. 연결할 곳은 아래 "훅 목록"에 있다.

## 1. 만든 것

| 파일 | 내용 |
|---|---|
| `Assets/Audio/Resources/Sfx/sfx_*.wav` | 효과음 12개 (32 kHz 모노 WAV, Unity가 Vorbis로 인코딩) |
| `Assets/Audio/Resources/Bgm/bgm_field.wav`, `bgm_boss.wav` | BGM 2곡, 끊김 없이 반복되게 만듦 |
| `Assets/Audio/Source/gen-audio.mjs.txt` | 위 소리를 모두 만드는 생성기 원본 (Node 18+, 의존성 없음). `.txt`는 Unity가 스크립트로 읽지 않게 붙인 것 |
| `Assets/Scripts/Audio/SfxId.cs` | `SfxId`, `BgmId` 열거형. 이름 = 파일 이름 (`EnemyTell` → `sfx_enemy_tell`) |
| `Assets/Scripts/Audio/GameAudio.cs` | `Sfx.Play`, `Bgm.Play/Stop`, `GameAudio.MuteAll`, `GameAudio.BreathStop` |
| `Assets/Scripts/Audio/Editor/AudioImportRules.cs` | `Assets/Audio/` 아래 소리의 임포트 설정 자동 적용 (Vorbis, 모노) |

### 출처
- **외부 음원 없음.** 모든 소리는 `gen-audio.mjs`가 수학적으로 합성한다 (sfxr식 발진기·노이즈, 종소리 모드 합성, Karplus-Strong 뜯는 소리). 팀 소유, CC0로 공개.
- 결정 10-A는 "BGM은 CC0 곡"이었지만, OpenGameArt에서 찾은 CC0 후보(아래)는 국악·한옥 분위기와 맞지 않아 같은 생성기로 BGM도 만들었다. 사용자가 청취 페이지에서 고른다.
  - Boss Fight 2 — ansimuz, CC0 — https://opengameart.org/content/boss-fight-2
  - NotThatEast / IntoTheStars — KiluaBoy, CC0 — https://opengameart.org/content/sci-fi-adventure-eastern-quiet-piano-loop
  - Fool's Philosophy — iamoneabe, CC0 — https://opengameart.org/content/fools-philosophy
- 청취 페이지(승인용): https://claude.ai/artifact/2RCARCepYagWREGz2i6PdF

### 소리별 설계
| SfxId | 길이 | 국악 요소 |
|---|---|---|
| Swing 휘두르기 | 0.20s | — (바람 소리) |
| Hit 타격 | 0.28s | 박(拍) 같은 나무 딱 소리 |
| Parry 패링 | 0.70s | 꽹과리 같은 쇳소리 |
| Dash 대시 | 0.16s | — |
| Hurt 피격 | 0.30s | — (디지털 잡음) |
| EnemyTell 적 공격 예고 | 0.20s | — (경고음) |
| LastTell 마지막 타 예고 | 0.50s | 북 |
| Stunned 적 기절 | 0.75s | 편종 같은 작은 종 |
| Broadcast 방송음 SFX-01 | 1.90s | 편종 3음(솔·도·라, 5음계) + 높낮이 없는 기계 음성(톱니파 118 Hz + 모음 포먼트 2개) |
| HackOk 해킹 성공 | 1.00s | 가야금 같은 뜯는 소리, 5음계 상행 |
| Shockwave 충격파 | 1.80s | 징 같은 긴 울림 |
| UiSelect UI 선택 | 0.12s | 나무 딸깍 + 종 |
| BGM Field | 27.4s 반복 | 12/8 굿거리 느낌(점4분 70), 가야금 선율, 장구 덩·덕, 낮은 신스 |
| BGM Boss | 19.2s 반복 | 12/8 빠르게(점4분 100), 꽹과리, 찌그러진 베이스 |

### 용량
- 원본 WAV 합계 약 3.4 MB (저장소). 빌드에서는 Vorbis(WebGL은 AAC로 다시 인코딩) 품질 0.45~0.6이라 전체 1 MB 안팎 예상 → 목표 5 MB 이하.

## 2. API

```csharp
Sfx.Play(SfxId.Hit);            // 한 번 재생. 10채널 순환, 같은 소리 0.04초 안 중복은 건너뜀, 전투음은 음높이 ±5%
Bgm.Play(BgmId.Field);          // 반복. 이전 곡을 0.6초 줄이고 새 곡을 0.6초 키움. 같은 곡이면 무시
Bgm.Stop();
GameAudio.MuteAll(true);        // SFX-02 숨 멈춤: AudioListener.volume = 0 (게임은 계속 돈다)
GameAudio.BreathStop(1.5f);     // N초 동안 모두 끊었다가 돌아옴
```
- 씬에 아무것도 놓지 않아도 된다. `RuntimeInitializeOnLoadMethod`로 `GameAudio` 오브젝트를 만들고 `DontDestroyOnLoad`.
- 클립은 `Resources.Load`로 읽는다 (`Assets/Audio/Resources/`).
- 시간은 모두 `Time.unscaledTime` — 일시정지(timeScale 0)와 패링 히트스톱 중에도 소리·페이드가 멈추지 않는다.
- **WebGL 첫 입력 문제:** 브라우저는 사용자가 한 번 누르기 전까지 소리를 막는다. WebGL에서는 첫 키·클릭·터치(`InputSystem.onAnyButtonPress`) 전까지 효과음은 버리고, 요청된 BGM은 기억했다가 첫 입력에 시작한다. 타이틀 화면에서 "Start"를 누르는 순간 풀리므로 실제로 놓치는 소리는 거의 없다.

### 자동으로 되는 것 (관찰자 패턴)
`GameAudio`가 `BroadcastBoard.All`을 매 프레임 확인해 새 전광판의 `NumberChanged` 이벤트를 구독한다. 그래서 다음은 **훅 없이** 동작한다.
- AI가 숫자를 바꿈 / 역해킹으로 되돌림 → `Broadcast` (SFX-01)
- 플레이어 해킹 확정 → `HackOk` (`board.IsHacked`로 구분)
- 숫자가 0이 됨 → 차임(또는 해킹음)이 끝난 뒤 `BreathStop(1.5초)` (SFX-02). `GameAudio.BreathStopOnZero = false`로 끌 수 있다.

### 조정값 (T) — `GameAudio.cs` 위쪽
| 이름 | 값 | 뜻 |
|---|---|---|
| `Voices` | 10 | 동시에 나는 효과음 수 |
| `SfxVolume` / `BgmVolume` | 0.8 / 0.45 | 전체 음량 |
| `BgmFade` | 0.6초 | 곡 전환 페이드 |
| `SameSfxCooldown` | 0.04초 | 같은 소리 중복 방지 |
| `PitchJitter` | ±5% | 전투음 반복 피로 줄이기 |
| `BreathStopSeconds` | 1.5초 | 숫자 0 정적 길이 |
| 임포트 품질 | SFX 0.6 / BGM 0.45 | `AudioImportRules.cs` |

소리 자체를 바꾸려면 `gen-audio.mjs.txt`를 `.mjs`로 복사해 고친 뒤 `node gen-audio.mjs <출력폴더>` → WAV를 `Assets/Audio/Resources/`에 덮어쓴다.

## 3. 훅 목록 (다른 세션·메인 세션이 넣을 것)
줄 번호는 `8bf1990` 기준. 한 줄씩만 추가하면 된다.

| 이벤트 | 호출 | 넣을 곳 |
|---|---|---|
| 플레이어 공격 휘두름 | `Sfx.Play(SfxId.Swing);` | `PlayerController.DoAttackHit()` 첫 줄 근처 — `Assets/Scripts/Player/PlayerController.cs:234` (ParryFx.Slash 옆) |
| 공격 적중 | `Sfx.Play(SfxId.Hit);` | `PlayerController.cs:240` `if (damaged)` 블록 (PlayHitSpark 옆) |
| 패링 성공 | `Sfx.Play(SfxId.Parry);` | `PlayerController.ReceiveEnemyHit` — `PlayerController.cs:265` (SpriteFx "parry" 옆). 히트스톱 중에도 소리는 난다 |
| 대시 시작 | `Sfx.Play(SfxId.Dash);` | `PlayerController.EnterState` `case State.Dash:` — `PlayerController.cs:208` |
| 플레이어 피격 | `Sfx.Play(SfxId.Hurt);` | `PlayerController.cs:274` `health.TakeDamage` 다음 줄 |
| 적 공격 예고 | `Sfx.Play(strikesLeft == 1 ? SfxId.LastTell : SfxId.EnemyTell);` | `TestEnemy.EnterState` `case State.Windup:` — `Assets/Scripts/Enemy/TestEnemy.cs:264` (SetFlame 다음) |
| 적 기절(쓰러짐) | `Sfx.Play(SfxId.Stunned);` | `TestEnemy.EnterState` `case State.Stunned:` — `TestEnemy.cs:275` |
| 반격 창 열림 (선택) | `Sfx.Play(SfxId.Stunned, 0.6f);` | `TestEnemy.EnterState` `case State.Open:` — `TestEnemy.cs:271` (Staggered와 case를 같이 쓰므로 `if (next == State.Open)`으로 감쌀 것) |
| 해킹 다이얼 돌림 | `Sfx.Play(SfxId.UiSelect);` | `BroadcastHackUi.UpdateDial()` 숫자가 바뀌는 곳 — `Assets/Scripts/Broadcast/BroadcastHackUi.cs:126~133` (selected가 바뀐 프레임에 한 번) |
| 해킹 확정 | (자동) | `BroadcastBoard.Hack` → NumberChanged 구독으로 처리됨 |
| 메뉴 이동 | `Sfx.Play(SfxId.UiSelect);` | `ButtonHighlight.OnSelect` — `Assets/Scripts/UI/UiKit.cs:114` |
| 충격파 (업그레이드) | `Sfx.Play(SfxId.Shockwave);` | upgrade 브랜치 `claude/upgrade-shockwave-4d96cf`의 `Shockwave` 컴포넌트 발동 지점 (병합 후 넣기) |
| 필드 BGM | `Bgm.Play(BgmId.Field);` | 각 스테이지 시작 — `Assets/Scripts/Core/StageFlow.cs:34` `Load()` 또는 스테이지 씬의 시작 스크립트. 타이틀은 `Bgm.Stop()` 또는 Field 유지 |
| 보스 BGM | `Bgm.Play(BgmId.Boss);` | boss 세션의 STG-05 보스전 시작 지점 |
| 숨 멈춤 (다른 때 쓰고 싶으면) | `GameAudio.BreathStop(초);` | 컷신·오프닝(LINE-01 "zero" 뒤) 등 |

## 4. 쓴 패턴
- **관찰자(Observer):** 전광판 `NumberChanged` 이벤트를 오디오가 구독. 전광판 코드는 오디오를 모른다.
- **파사드(Facade):** `Sfx` / `Bgm` 정적 클래스가 `GameAudio` 내부(풀, 페이드, WebGL 잠금)를 감춘다. 호출하는 쪽은 한 줄.
- **오브젝트 풀:** AudioSource 10개를 순환 사용 (매번 생성하지 않음).
- 싱글턴은 `GameAudio.Hub` 하나만. 씬 배치 없이 자동 생성(기존 `PauseMenu.Bootstrap`과 같은 방식).

## 5. 패턴 / 리팩터 제안 (내 파일 밖이라 손대지 않음)
1. **전투 이벤트 버스:** `PlayerController`, `TestEnemy`에 `public static event Action<...> Attacked / Parried / Hurt / Stunned` 같은 정적 이벤트를 두면, 오디오·FX·카메라 흔들림이 각자 구독할 수 있다. 지금은 `ParryFx`/`SpriteFx` 호출이 전투 코드 안에 직접 박혀 있고, 위 훅 목록대로 하면 `Sfx.Play`도 같은 자리에 늘어난다. 잼 일정상 지금은 훅 한 줄씩이 더 싸다 — 마감 후 정리 후보.
2. **`BroadcastBoard`에 정적 이벤트 `AnyNumberChanged`:** 지금은 오디오가 `All` 목록을 매 프레임 훑어 새 전광판을 찾는다 (전광판 수가 적어 비용은 무시할 만함). 정적 이벤트가 있으면 폴링이 필요 없다.
3. **숫자 0 = 숨 멈춤 연출 일원화:** 시민 굳음(enemy-rules 세션)과 소리 정적(여기)이 따로 0을 감지한다. 나중에 "Zero 상태 시작/끝" 이벤트 하나로 묶으면 타이밍이 어긋나지 않는다.
4. **음량 설정:** 일시정지 메뉴에 음량 슬라이더를 넣으려면 `GameAudio`에 `SetVolume(sfx, bgm)` 하나만 추가하면 된다 (지금은 요청이 없어 안 만듦).
