# 최종 보스 (STG-05) 구현 기록 — 결정 4-A

> 2026-11-08. 근거: [DECISIONS.md](../DECISIONS.md) 4번 추천안 A `[확정]`. 사용자는 "일단 추천대로, 나중에 바꿀 수 있음"이라고 했으므로 숫자는 쉽게 바꾸고 통째로 뺄 수 있게 만들었다.
> 작업 흐름: Fable 설계 → Sonnet 구현 → Opus 검수(버그 1건 수정) → 배치 모드 컴파일·빌더 실행 확인(`error CS` 0건).

## 1. 4-A가 무엇이 됐나
- **방 하나, 화면 하나** (30 × 16.875u, 카메라 고정). 가운데 위에 큰 전광판(12×5u, 사각형 스프라이트 임시 그림)이 있고, 몸 없는 AI "EMBECTOR / 엠벡터"는 전광판의 글자로만 나온다.
- 플레이어가 전광판에서 가로 8u 안으로 들어오면 싸움이 시작된다.
- 웨이브마다: AI가 방송을 바꿈(`BroadcastBoard.SetAiNumber`) → 전광판에 웨이브 제목 2초 → 적이 천장 양쪽 구석에서 0.6초 간격으로 떨어짐 → 모두 기절(`TestEnemy.State.Stunned`)하면 클리어 → 1.5초 뒤 다음 웨이브.

| 웨이브 | 방송 | 적 |
|---|---|---|
| 1 | 3 | 시민 3 + AI 벌레 1 (대역 = 황금 원거리; 리뷰 06 #6, 클릭 4번으로 끝나지 않게) |
| 2 | 7 | 시민 4 + AI 벌레 2 (대역) |
| 3 | 0 | 안드로이드 2 (대역) + 시민 3 |

- **3웨이브가 시작된 뒤** 플레이어가 전광판을 해킹하면(어떤 숫자든, 화면과 같은 0이어도) 오른쪽(x=26)에 빨간 **정지 버튼**이 나타난다. 3웨이브 적을 다 쓰러뜨리지 않아도 된다. 한 번 열리면 AI가 숫자를 되돌려도 계속 열려 있다.
- 1·2웨이브 중 해킹은 승리 조건과 상관없다(방송 숫자만 바뀐다). 2웨이브에 걸어 둔 해킹이 3웨이브까지 남아 있어도 그것으로는 열리지 않고, 3웨이브 중에 새로 해킹해야 한다.
- 정지 버튼은 기존 `StopButton`을 그대로 쓴다 → E → 엔딩 1/2 선택 → `DialoguePlayer`로 엔딩 대화 → 타이틀.
- 죽으면 `RespawnManager`가 씬을 다시 불러오므로 **1웨이브부터 다시** 한다(보스방 중간 체크포인트 없음).
- 새 공격 코드는 없다. 적은 기존 `TestEnemy` 프리팹을 복제해서 쓴다.

## 2. 파일
- [BossFight.cs](../../../Assets/Scripts/Boss/BossFight.cs) — 웨이브 표, 상태 머신, 적 소환, 해금. 이벤트를 낸다.
- [BossBoardView.cs](../../../Assets/Scripts/Boss/BossBoardView.cs) — 전광판 메시지 줄만 그린다(로직 없음). 숫자 자체는 `BroadcastBoard.label`이 그린다.
- [Stg05Builder.cs](../../../Assets/Editor/Stg05Builder.cs) — 메뉴 `NUMBER/Build STG-05`로 `Assets/Scenes/STG05.unity` 생성. 씬은 손으로 고치지 말고 빌더를 고쳐서 다시 만든다. Build Settings 목록 끝에 STG05를 추가한다(이미 있으면 그대로).

## 3. 조정 값 (T)
| 값 | 기본 | 위치 |
|---|---|---|
| 시작 거리(전광판과 가로 거리) | 8u | [BossFight.cs:25](../../../Assets/Scripts/Boss/BossFight.cs#L25) |
| 웨이브 제목 표시 시간 | 2초 | [BossFight.cs:26](../../../Assets/Scripts/Boss/BossFight.cs#L26) |
| 소환 간격 | 0.6초 | [BossFight.cs:27](../../../Assets/Scripts/Boss/BossFight.cs#L27) |
| 플레이어 근처 소환 금지 반경 | 4u | [BossFight.cs:28](../../../Assets/Scripts/Boss/BossFight.cs#L28) |
| 웨이브 사이 쉬는 시간 | 1.5초 | [BossFight.cs:29](../../../Assets/Scripts/Boss/BossFight.cs#L29) |
| **웨이브 표**(방송 숫자, 시민·벌레·안드로이드 수, 제목 EN/KO) | 위 표 | [BossFight.cs:30-35](../../../Assets/Scripts/Boss/BossFight.cs#L30) |
| 전광판 역해킹 유지 시간 / 0일 때 | 4초 / 3초 (다른 스테이지 기본 8/5보다 짧게: 2웨이브 해킹이 3웨이브 시작을 오래 막지 않게) | [Stg05Builder.cs:78-79](../../../Assets/Editor/Stg05Builder.cs#L78) |
| AI 벌레 대역 능력치 | 원거리, 사거리 9, 마지막 타격 +0.5초, 숫자 1 고정, 금색 | [Stg05Builder.cs:98-105](../../../Assets/Editor/Stg05Builder.cs#L98) |
| 안드로이드 대역 능력치 | 숫자 1~7, 마지막 타격 +0.5초, 7타 패링 불가, 체력 5 | [Stg05Builder.cs:107-114](../../../Assets/Editor/Stg05Builder.cs#L107) |
| 소환 지점 / 플레이어 시작 / 전광판 / 정지 버튼 위치 | (2,14)·(28,14) / 발 (4,2) / 판 기준점 (15,3.5), 화면 중심 (15,11) / (26,2.5) | [Stg05Builder.cs:62-92](../../../Assets/Editor/Stg05Builder.cs#L62) |
| 전광판 대사(엠벡터 / ...귀엽군. / 정지 버튼 열림) | — | [BossBoardView.cs:17-41](../../../Assets/Scripts/Boss/BossBoardView.cs#L17) |

- 웨이브 표와 타이밍은 씬의 `BossFight` 인스펙터에서도 바꿀 수 있지만, 빌더로 씬을 다시 만들면 코드 기본값으로 돌아간다. **오래 쓸 값은 코드 기본값을 고친다.**
- 웨이브 수를 늘리거나 줄여도 된다. "마지막 웨이브"는 표의 마지막 줄이다.

## 4. 대역(stand-in)
- **AI 벌레**: 전용 적 타입이 없어 **원거리 적**(금색 1, `EnemyVariantsTestSetup`의 Ranged와 같은 설정)을 쓴다.
- **안드로이드**: 전용 타입이 없어 **강한 적**(1~7, 7타 패링 불가) 설정에 체력 5를 줬다. 결정 6-A(황금 탄 3발)는 enemy-rules 세션 몫이라 아직 들어 있지 않다.
- 대역 설정은 빌더 안에서 `TestEnemy`/`EnemyNumber` 직렬화 필드를 바꾸는 방식이라, 전용 프리팹이 생기면 `Stg05Builder`의 `BuildTemplate` 세 줄만 그 프리팹으로 바꾸면 된다.
- 전광판·바닥·정지 버튼은 사각형 스프라이트 임시 그림이다. 보스 화면 아트는 별도 작업.
- 방송 0의 적 효과(시민 굳음)는 enemy-rules 세션이 `BroadcastBoard` 리스너로 넣는다. 여기서는 숫자만 바꾼다.

## 5. 바꾸거나 빼는 법
- **보스를 빼기**: Build Settings에서 STG05를 빼면 `StageFlow`가 STG04 다음에 갈 곳이 없어 멈춘다. 이 경우 STG04 끝에 정지 버튼을 두는 등 엔딩 연결을 따로 해야 한다. 코드는 `Assets/Scripts/Boss/`, `Assets/Editor/Stg05Builder.cs`, `Assets/Scenes/STG05.unity`만 지우면 된다(다른 파일이 참조하지 않음).
- **B안(안드로이드 몸 보스)으로 바꾸기**: 웨이브 표를 `{0, 0, 0, 1}` 한 줄로 줄이고 안드로이드 템플릿 체력을 크게 하면 비슷한 시험을 바로 해 볼 수 있다. 해금 조건은 "마지막 웨이브 중 해킹"이므로 필요하면 `BossFight.Update`의 해금 줄([BossFight.cs:59](../../../Assets/Scripts/Boss/BossFight.cs#L59))을 "마지막 웨이브 클리어 시"로 바꾼다.
- **해금 조건을 "3웨이브 적 전멸 후 해킹"으로 바꾸기**: 같은 줄에 `&& clearedFired` 조건을 더한다.

## 6. 사용한 패턴
- **State 패턴(enum + switch)**: `Idle → Announce → Spawning → Fighting → (다음 Announce | Unlocked)`. 상태 enum [BossFight.cs:43](../../../Assets/Scripts/Boss/BossFight.cs#L43), switch [BossFight.cs:61-89](../../../Assets/Scripts/Boss/BossFight.cs#L61), 전환 `Enter` [BossFight.cs:92](../../../Assets/Scripts/Boss/BossFight.cs#L92). 상태가 5개뿐이라 상태마다 클래스를 만들지 않았다(기존 `TestEnemy`와 같은 방식).
- **Observer(C# event)**: `WaveStarted`, `WaveCleared`, `Unlocked` 선언 [BossFight.cs:37-39](../../../Assets/Scripts/Boss/BossFight.cs#L37). 구독은 `BossBoardView`([BossBoardView.cs:15-16](../../../Assets/Scripts/Boss/BossBoardView.cs#L15), 해제 22-23)가 하며 `BroadcastBoard.NumberChanged`도 함께 듣는다. `WaveCleared`는 아직 구독자가 없다(효과음·연출용 자리).
- 해킹 감지는 이벤트가 아니라 **폴링**이다([BossFight.cs:132-143](../../../Assets/Scripts/Boss/BossFight.cs#L132)). `BroadcastBoard`는 화면과 같은 숫자로 해킹하면 `NumberChanged`를 내지 않아서, 3웨이브(0)에서 다이얼을 열고 바로 E를 누르면 해금이 안 되는 버그가 검수에서 나왔기 때문이다.

## 7. 패턴 / 리팩터 제안 (남의 파일이라 고치지 않음)
- **`BroadcastBoard`에 `Hacked` 이벤트 추가**(`event Action<BroadcastBoard, int> Hacked`, `Hack()`에서 숫자가 같아도 발생): 있으면 `BossFight`의 폴링(`NewHackThisFrame`)을 지우고 Observer로 바꿀 수 있다. `BossBoardView`의 "...귀엽군." 대사도 같은 숫자 해킹에 반응하게 된다.
- **`BroadcastBoard.SetAiNumber(n, force)`**: 지금은 해킹 중이면 AI 숫자 변경이 미뤄진다. 2웨이브 해킹이 남아 있으면 3웨이브 시작 때 0이 최대 4초 늦게 뜬다(그래서 이 방만 유지 시간을 4/3초로 줄였다).
- **`TestEnemy`에 `event Action<TestEnemy> Stunned`**: 웨이브 클리어를 매 프레임 폴링하지 않고 이벤트로 셀 수 있다(지금은 적 5마리라 비용은 무시할 만함).
- **`StopButton.Unlock()` 또는 잠금 플래그**: 지금은 GameObject를 꺼 두었다가 켜는 방식이다.
- **에디터 공용 `EnemyVariants.Apply(SerializedObject, Kind)`**: 원거리·강한 적 설정이 `EnemyVariantsTestSetup`과 `Stg05Builder`에 중복돼 있다. 전용 프리팹(AI 벌레·안드로이드)이 생기면 그걸로 대체하는 게 가장 깔끔하다.
- `RespawnManager`가 "재시작 vs 새로 시작"을 공개하지 않아 보스방 중간 체크포인트는 만들 수 없다(지금은 필요 없음).

## 8. Play 모드 확인 목록
1. `NUMBER/Build STG-05` 실행(또는 이미 생성된 `STG05.unity` 열기) → Play.
2. 오른쪽으로 걸어가면 전광판에 "WAVE 1 - LUCKY 3", 상단 방송 3, 시민 4명이 천장에서 떨어진다.
3. 모두 기절시키면 1.5초 뒤 "WAVE 2 - LUCKY 7", 시민 3 + 금색 원거리 2.
4. 모두 기절시키면 "WAVE 3 - LUCKY 0", 강한 적 2 + 시민 3.
5. 3웨이브 중 전광판 아래에서 E → 다이얼에서 **바로 E**(0 그대로) → 전광판 "STOP BUTTON UNLOCKED", 오른쪽에 빨간 버튼.
6. 버튼에서 E → 1/2 선택 → 엔딩 대화 → 타이틀.
7. 2웨이브 중 해킹하고 그대로 3웨이브로 넘어가면 버튼이 **열리지 않아야** 한다(새로 해킹하면 열림).
8. 싸움 중 죽으면 씬이 다시 시작되고 1웨이브부터.
9. L키로 언어를 바꾼 뒤 전광판 문구가 한국어로 나오는지(다음 문구부터 바뀜).
