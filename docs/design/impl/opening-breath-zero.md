# STG-00 "숨 0번(BREATH ZERO)" 연출 구현 메모

근거: [03-strategy.md](../../review/03-strategy.md) 2-1, [DECISIONS.md](../DECISIONS.md) 6-A, [LIGHTING_DESIGN.md](../LIGHTING_DESIGN.md) 4.3 `Blackout` (Q4 확정, 2단 타이머), GDD LINE-01~03.
런타임 코드만 바꿨다. 씬·빌더·프리팹은 손대지 않았다 (STG00.unity의 기존 OpeningSequence는 새 필드를 기본값으로 쓴다).

## 파일
| 파일 | 상태 | 역할 |
|---|---|---|
| `Assets/Scripts/Level/OpeningSequence.cs` | 수정 | 타임라인. 전광판 0 표시 시점을 방송 단계로 당김, 정적 0.3초, 컷 순간에 `BreathZero` 추가 + 시민 순차 정지 |
| `Assets/Scripts/Level/OpeningCitizen.cs` | 수정 | 전광판 리스너 제거 → `Freeze(delay)`로 시퀀스가 직접 멈춤 (방송 0이 떠도 바로 굳지 않게) |
| `Assets/Scripts/Level/BreathZero.cs` | 신규 | 조명 정전(모든 `Light2D` + 카메라 배경색) 2단 타이머, 소리 끊김(`AudioListener.volume`), 오디오 훅 이벤트 |

## 타임라인 (초, 기본값 기준. 모두 게임 시간 → 일시정지 중엔 멈춤)
| 시각 | 단계 | 일어나는 일 |
|---|---|---|
| 0.0 | Intro | 대기 `introDelay` 1.5 |
| 1.5 | Broadcast | **전광판 0 표시** (`board.SetAiNumber(0)`) + LINE-01 라디오 "오늘의 행운의 숫자는 0" |
| 5.5 | Joke | LINE-02 대화창(입력 잠금). 플레이어가 닫을 때까지 대기 |
| 닫은 뒤 +0.3 | (정적) | `silenceDelay` 0.3 — 아무 일도 없음 |
| **컷 = T** | Freeze | (a) `BreathZero.StopRequested(holdSeconds)` 발생 + `AudioListener.volume = 0` (b) 모든 Light2D 세기 × 1 → × 0.08, 카메라 배경(하늘)색도 같은 비율로 0.35초 동안 감소 (c) 전광판 0은 TMP(Unlit)라 조명 영향 없이 그대로 빛남 (d) 시민은 전광판에서 가까운 순으로 0.25초 간격 정지 (`i × freezeStagger`), LINE-03 라디오 |
| T+1.0 | Escape | `freezeWait` → 출구 트리거 켜짐 (오른쪽으로 달려 대시로 STG-01) |
| T+0.35 ~ T+2.85 | | 어둠·정적 유지 (`holdSeconds` 2.5) |
| T+2.85 | | 소리 복구, 조명 1.5초(`restoreSeconds`) 동안 원래 값으로 복구 |
| T+4.35 | | 연출 끝 |

시민 6명이면 마지막 시민은 T+1.25에 굳는다 (0.25 × 5).

## 튜닝 값 (전부 `[SerializeField]`, (T))
| 값 | 위치 | 기본 | 뜻 |
|---|---|---|---|
| `silenceDelay` | OpeningSequence | 0.3 | 대화창 닫힘 → 컷 사이 정적 |
| `freezeStagger` | OpeningSequence | 0.25 | 시민 한 명씩 굳는 간격 (전광판에서 가까운 순) |
| `freezeWait` | OpeningSequence | 1.0 | 컷 → 출구 열림 (기존 값) |
| `breathZero` | OpeningSequence | true | false = 정전·소리 끊김 없음 (시민 정지는 유지) |
| `dimSeconds` | BreathZero | 0.35 | 어두워지는 시간 |
| `dimFactor` | BreathZero | 0.08 | 바닥 조명 세기 배율 (0 = 완전 암전. 0.08이면 실루엣이 간신히 보여 달릴 방향을 잡을 수 있다) |
| `holdSeconds` | BreathZero | 2.5 | 어둠·정적 유지 시간. 오디오 훅에 그대로 전달 |
| `restoreSeconds` | BreathZero | 1.5 | 복구 시간 |
| `muteListener` | BreathZero | true | `AudioListener.volume`으로 소리 끊기. GameAudio가 직접 처리하게 되면 false |

`BreathZero`는 씬에 없고 컷 순간 `OpeningSequence`가 `AddComponent`로 붙이므로, 지금은 인스펙터에서 BreathZero 값을 바꾸려면 코드 기본값을 고쳐야 한다 (아래 빌더 후속).

## 끄기 / 완전히 제거
- **끄기:** STG00 씬의 `OpeningSequence.breathZero = false` (씬 수정) 또는 코드 기본값 false. 시민은 여전히 순차 정지한다. 동시에 굳게 하려면 `freezeStagger = 0`.
- **완전 제거:** `BreathZero.cs` 삭제, `OpeningSequence`에서 `breathZero` 필드와 `if (breathZero) gameObject.AddComponent<BreathZero>().Play();` 한 줄 삭제. `OpeningCitizen`은 그대로 둬도 된다 (시퀀스가 `Freeze`를 부르는 방식은 연출과 무관하게 필요).

## 오디오 훅 (오디오 브랜치 `claude/audio-4c66f5` 병합 뒤)
```csharp
public static event System.Action<float> BreathZero.StopRequested; // 컷 순간, 인자 = holdSeconds
```
연결 한 줄 (GameAudio의 Awake/OnEnable 등에서):
```csharp
BreathZero.StopRequested += GameAudio.BreathStop;   // 정적 메서드면 이대로, 인스턴스면 s => GameAudio.BreathStop(s)
```
- 구독자가 없으면 아무 일도 안 한다 (`?.Invoke`).
- 오디오 측이 직접 소리를 끊고 복구하게 되면 `BreathZero.muteListener`를 false로 두어 `AudioListener.volume`과 이중으로 걸리지 않게 한다. 둘 다 켜져 있어도 깨지진 않는다 (볼륨은 hold 끝/OnDestroy에서 반드시 원래 값으로 돌아온다).
- 복구 보장: `AudioListener.volume`은 hold가 끝날 때, 그리고 `OnDestroy`(씬 전환·R 재시작·에디터 Play 종료)에서 저장해 둔 이전 값으로 돌아간다.

## 설계 메모
- 조명은 "세기 × 배율" 한 값으로만 움직인다. `Light2D`를 종류 구분 없이 전부(`FindObjectsByType<Light2D>`) 잡으므로 나중에 네온 Point Light·`NeonSign`이 STG-00에 들어와도 코드 변경 없이 같이 꺼진다. 네온 스프라이트(Unlit, 정점 색)는 라이트가 아니라 안 꺼진다 → STG-00은 아침이라 네온이 없어서(LIGHTING_DESIGN 4.1) 지금은 문제 없음. 밤 씬에서 쓰려면 `NeonSign.SetIntensity`도 같이 호출해야 한다.
- 하늘은 카메라 clear color라 라이트가 못 건드려서 같은 비율로 검게 만든다.
- TMP 라벨(전광판 0, 적/시민 숫자)은 Unlit이라 자동으로 "0만 빛남"이 된다. STG-00 시민 플레이스홀더에는 아직 숫자 라벨이 없다. 시민 위에 0 홀로그램을 달면(EnemyNumber 또는 TMP) 추가 코드 없이 그대로 빛난다.
- `BroadcastBoard` 리스너(`IBroadcastListener`)는 이제 구현체가 없다. 인터페이스는 남겨 뒀다 (고치지 않은 코드).
- 시간은 전부 scaled(`Time.deltaTime`, `WaitForSeconds`) → 일시정지(`timeScale 0`)하면 연출도 멈춘다. 기존 OpeningSequence 관례와 같다.

## 빌더 후속 (이번에 하지 않음 — Stg00Builder는 다른 세션 소유)
1. `Stg00Builder`에서 `seq.gameObject.AddComponent<BreathZero>()`를 미리 붙이고 `OpeningSequence`가 `GetComponent`로 찾게 하면 BreathZero 값을 씬 인스펙터에서 튜닝할 수 있다. (지금은 `AddComponent` 런타임 생성.)
2. `BuildBoard(..., aiNumber: 1, ...)`의 초기 숫자: 방송 전엔 1이 떠 있다가 1.5초에 0으로 바뀐다. 방송 전에 빈 화면/다른 숫자를 원하면 빌더 값만 바꾸면 된다.
3. 시민 위 0 홀로그램(시민 숫자)을 넣으려면 빌더에서 TMP 라벨을 추가한다. 정전 중 그대로 빛난다.

## Play 모드 확인 항목
1. STG00 Play → 1.5초에 전광판이 **0**으로 바뀌고 라디오 자막이 뜬다. 시민은 계속 걷는다.
2. 대화창(농담)을 닫으면 0.3초 뒤: 화면 전체가 0.35초 동안 거의 검게(하늘 포함) 가라앉고, 전광판 노란 **0**만 그대로 밝다. 소리가 있다면 전부 끊긴다.
3. 시민이 전광판 가까운 쪽부터 0.25초 간격으로 한 명씩 회색이 되어 멈춘다 (동시에 멈추지 않음).
4. 컷 1초 뒤부터 오른쪽으로 달려 출구로 나갈 수 있다. 컷 약 2.85초 뒤 소리가 돌아오고 1.5초에 걸쳐 밝아진다.
5. 정전 중 Esc(일시정지) → 어두움·시민 정지 타이머가 멈추고, 해제하면 이어진다.
6. 정전 중 R 재시작 또는 출구로 씬 전환 → 다음 씬에서 소리가 정상 (AudioListener.volume 복구). 에디터 Play 종료 후에도 볼륨 정상.
