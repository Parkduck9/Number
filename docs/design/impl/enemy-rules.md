# 적 방송 규칙 구현 기록 (enemy-rules)

> 2026-11-08 작성. 근거: [DECISIONS.md](../DECISIONS.md) 1-A · 3-A · 5-B · 7-A (`[확정]`, "일단 추천대로 — 나중에 바꾸거나 지울 수 있음").
> 구현 Claude Sonnet(game-coder) → 검수 Claude Opus. Unity 6000.3.28f1 배치 모드 컴파일 오류 0, `NUMBER/Build STG-01` 배치 실행 성공.
> 줄 번호는 이 커밋 기준이다. 다른 브랜치가 합쳐지면 밀릴 수 있으니 이름(검색어)도 함께 적었다.

## 1. 결정별로 무엇이 되었나

### 1-A 방송 숫자 효과 (NUM-3 · NUM-7 · NUM-0)
- **구역 개념 추가.** 전광판(`BroadcastBoard`)마다 담당 x 범위 `zoneX`가 있다. `(0,0)`이면 씬 전체를 맡는다. 적은 시작할 때 자기 위치의 전광판을 한 번 찾아 `NumberChanged` 이벤트를 구독한다 (매 프레임 확인하지 않음).
- **NUM-3:** 근접 적(원거리 아님)의 숫자를 3으로 고정한다. 연타 간격은 0.15초 그대로(2026-11-08 리뷰 06: 0.10 × NormalTimeMul 0.65 = 0.065초는 3타가 한 덩어리로 보여 0.15로 되돌림), 연타 뒤 반격 창은 1초에서 1.5초로 바뀐다. 원거리 적은 숫자 1을 그대로 쓴다.
  - 숫자는 **연타 도중에는 바뀌지 않는다** (숫자는 체인 중 고정이라는 기존 확정 규칙). 대기(Approach) 중인 적은 바로 3이 되고, 연타 중인 적은 다음 재추첨 때 3이 된다.
- **NUM-7:** 7이 켜지면 7초 동안 도핑된다. 도핑 중에는 이동 속도가 ×1.3이 되고 예고(windup) 시간이 ÷1.3이 된다. 7초가 끝나면 2초 동안 탈진한다. 탈진은 반격 창(Open) 상태와 같아서 이때 때리면 바로 맞는다.
  - **구현하며 정한 것 (확인 필요):**
    - 구역이 계속 7이면 **도핑 7초 → 탈진 2초를 계속 반복**한다.
    - 도핑 도중 해킹으로 7을 다른 숫자로 바꾸면 도핑만 끝나고 **탈진은 없다.**
- **NUM-0:** `kind = Citizen`인 적만 굳는다 (푸른 회색). 굳은 동안에는 숫자가 있어도 공격이 튕기지 않고 그대로 맞아서, 평소처럼 3타에 기절한다. 숫자가 0에서 다른 숫자로 바뀌면 숫자를 재추첨하고 다시 움직인다. `Bug`와 `Android`는 굳지 않는다.
  - **11-09 플레이 메모 "해킹해도 일반인 안 멈춤":** 코드상 해킹 0도 AI 0과 똑같이 시민을 굳힌다(무시하는 곳 없음, 굳음 5초 = `holdSecondsForZero`). 다만 황금 원거리 적은 GDD `[확정]`대로 `kind = Bug`(원거리 벌레)라 0에서도 계속 쏘는데, 지금 초안 그림은 **사람(원거리 시민)** 이라 "일반인이 안 멈춘다"로 보인다. 그림을 벌레로 바꿀지, 원거리도 굳게 할지 결정 필요.
- **적 종류 필드 `kind` 추가** (Citizen / Bug / Android). 기본값은 Citizen이다.
  - **다른 레벨 빌더(STG-02·04, 보스)는 AI 벌레를 `Bug`(1), 안드로이드를 `Android`(2)로 지정해야 한다.** 안 하면 0일 때 같이 굳는다.

### 3-A 방송 주기 · 해킹 시간
- 방송은 구역 경계에서만 바뀐다. 전광판마다 구역이 따로 있어서 이렇게 된다. 코드로 바꿀 때는 기존 `SetAiNumber`를 쓴다.
- 해킹 유지 시간은 기존 그대로 8초이고, 0일 때만 5초다.
- **역해킹 진행 바를 켰다** (`showCounterHackBar` 기본값 true).
- "더 나쁜 숫자" 규칙은 넣지 않았다.

### 5-B STG-01 방송 "3" 시점
- SEG-A에는 전광판이 없다. SEG-B에만 전광판 하나를 둔다 (숫자 3, 구역 = SEG-B 전체 x 60~110.625).
- HUD는 **플레이어가 서 있는 구역의 전광판만** 보여 준다. 그래서 SEG-A에서는 HUD가 비어 있다. 대문을 지나 SEG-B에 도착하면 "((( 3 )))"이 커졌다가 작아지며 나타나고(펀치), 그 순간 SEG-B 적들이 3으로 고정된다.
- 위치: SEG-B 로컬 (6, 3.5), 도착 지점 P-B (2, 1) 바로 옆이라 도착하자마자 E로 해킹할 수 있다.
- **차임 소리는 아직 없다.** 오디오 세션의 Sfx API가 생기면 `BroadcastHackUi.cs:93`의 `// TODO(audio): SFX-01 broadcast chime here` 자리에 한 줄 넣으면 된다.
- `Assets/Scenes/STG01.unity`는 빌더로 다시 만들어 커밋했다. `EditorBuildSettings`가 바뀐 부분은 되돌렸다. 빌드 목록은 levels 세션(StageFlow) 몫이다.

### 7-A 안드로이드 특수 공격
- `kind = Android`인 적만 쓴다. 연타와 연타 사이에 쿨다운이 찼으면 특수 공격(Special)을 쓴다:
  1. 플레이어 쪽으로 돈다.
  2. 몸이 황금색이 되고 숫자가 맥동한다 (0.5초 예고).
  3. 황금 탄 3발을 15° 간격 부채꼴로 쏜다 (속도 8).
  4. 남은 연타를 이어 간다. 연타 횟수는 줄지 않는다.
- 탄은 `EnemyProjectile`을 재사용한다. 패링할 수 있고, 마지막 일격 규칙(IsSpecial)은 적용되지 않는다.
- 쿨다운은 6초다. 첫 연타 사이에는 바로 쓸 수 있다.
- 테스트 씬 `NUMBER/Build Enemy Variants Test Scene`의 "Strong"이 Android다. 이 씬에는 전광판(3)도 추가해서, E로 3·7·0을 해킹해 보며 규칙을 시험할 수 있다.

## 2. 조정값 위치 (전부 (T))

| 값 | 기본 | 위치 |
|---|---|---|
| NUM-3 연타 간격 | 0.15초 (유효 0.0975초, 일반 적 배율 적용) | `Assets/Scripts/Broadcast/BroadcastRules.cs:6` `Num3ChainWindupTime` |
| NUM-3 반격 창 | 1.5초 | `BroadcastRules.cs:7` `Num3OpenTime` |
| NUM-3 고정 숫자 / 발동 방송 숫자 | 3 | `BroadcastRules.cs:8` `Num3Number` |
| NUM-7 도핑 시간 | 7초 | `BroadcastRules.cs:11` `Num7DopeSeconds` |
| NUM-7 속도 배율 | ×1.3 | `BroadcastRules.cs:12` `Num7SpeedMul` |
| NUM-7 탈진 시간 | 2초 | `BroadcastRules.cs:13` `Num7ExhaustSeconds` |
| 안드로이드 부채꼴 쿨다운 | 6초 | `BroadcastRules.cs:16` `AndroidFanCooldown` |
| 부채꼴 예고 | 0.5초 | `BroadcastRules.cs:17` `AndroidFanTell` |
| 탄 수 | 3 | `BroadcastRules.cs:18` `AndroidFanCount` |
| 탄 사이 각도 | 15° | `BroadcastRules.cs:19` `AndroidFanSpreadDeg` |
| 탄 속도 | 8 | `BroadcastRules.cs:20` `AndroidFanSpeed` |
| 굳음 색 / 특수 예고 색 | 푸른 회색 / 황금 | `Assets/Scripts/Enemy/TestEnemy.cs:49-50` `FrozenColor`, `SpecialColor` |
| 적 종류 | Citizen | `TestEnemy.cs:32` `kind` (인스펙터·빌더에서 지정) |
| 전광판 구역 | (0,0) = 씬 전체 | `Assets/Scripts/Broadcast/BroadcastBoard.cs:24` `zoneX` |
| 해킹 유지 | 8초 / 0은 5초 | `BroadcastBoard.cs` `holdSeconds`, `holdSecondsForZero` (기존) |
| 역해킹 진행 바 | 켬 | `Assets/Scripts/Broadcast/BroadcastHackUi.cs:16` `showCounterHackBar` |
| HUD 펀치 크기·시간 | 1.4배 → 1, 0.3초 | `BroadcastHackUi.cs` `UpdateHud` 안 `1.4f`, `0.3f` |
| STG-01 전광판 위치·크기·숫자 | (6, 3.5) / 2.5u / 3 | `Assets/Editor/Stg01Builder.cs:97-99` `BoardSeg`, `BoardNumber`, `BoardSize`, `BoardPos` |

- `BroadcastRules` 값은 `static` 필드라서 코드에서 고치고 다시 컴파일하면 모든 씬에 한꺼번에 적용된다. 씬을 다시 만들 필요가 없다.
- 7은 `TestEnemy.cs`에 숫자 `7`로 직접 적혀 있다 (77, 159, 169줄 근처 `broadcast == 7`).

## 3. 바꾸거나 지우는 법
- **규칙 하나 지우기:** `TestEnemy.cs`의 `OnBroadcastChanged`(147줄~)에서 해당 블록을 지운다. 블록마다 `// NUM-3:`, `// NUM-7:`, `// NUM-0:` 주석이 붙어 있다. 규칙마다 함께 지울 곳:
  - **NUM-3:** `Windup`의 `chainWait`(216줄), `EnterState`의 `openDuration`(362줄)에서 Num3 조건.
  - **NUM-7:** `UpdateDope`(166줄), `Doped`·`Speed`(77-78줄).
  - **NUM-0:** `State.Frozen` 관련 줄 (364줄, `ReceivePlayerHit` 391줄, `ApplyVisual` 418줄).
- **안드로이드 특수 공격 끄기:** `NextAfterChain`(257줄)의 첫 줄을 지운다. 또는 `AndroidFanCooldown`을 아주 크게 준다.
- **방송을 처음부터 켜기 (5-A로 바꾸기):** `Stg01Builder.cs:400`의 `zoneX`를 `(0,0)`(씬 전체)으로 바꾸거나, SEG-A에 전광판을 하나 더 둔다. 그다음 `NUMBER/Build STG-01`을 다시 실행한다.
- **진행 바 끄기:** `showCounterHackBar = false`로 바꾼 뒤 씬 빌더를 다시 실행한다. 씬에 값이 저장되어 있으므로 빌더를 다시 돌려야 바뀐다.
- **구역 안에서 시간마다 방송 바꾸기 (3-B):** 레벨 스크립트에서 `board.SetAiNumber(n)`을 부르면 된다. 적은 이벤트로 바로 반응한다.

## 4. 쓴 패턴
| 패턴 | 어디 | 왜 |
|---|---|---|
| Observer (C# event) | 발행: `BroadcastBoard.cs:51` `NumberChanged` / 구독: `TestEnemy.cs:98`(Start), `OnDestroy`에서 해제 / 처리: `TestEnemy.cs:147` `OnBroadcastChanged` | 방송이 바뀐 순간에만 반응한다. 매 프레임 전광판을 확인하지 않는다 |
| 유한 상태 기계 (enum + switch) | `TestEnemy.cs` `State`에 `Frozen`·`Special` 추가 | 기존 구조를 그대로 확장했다 (아래 제안 참고) |
| 설정값 한곳 모음 (static 설정 클래스) | `BroadcastRules.cs` | 조정과 삭제를 한 파일에서 한다 |
| 구역 조회 (정적 레지스트리) | `BroadcastBoard.At` (`BroadcastBoard.cs:31`), 기존 `BroadcastBoard.All` 재사용 | 적과 HUD가 같은 "어느 구역 방송인가" 규칙을 쓴다 |

## 5. 패턴 / 리팩터 제안 (이번에 하지 않음)
- **TestEnemy를 상태별 클래스(State 패턴)로 나누기.** 상태가 11개가 되어 `UpdateState`·`EnterState` switch가 길어졌다. 이번에 하지 않은 이유는 두 가지다.
  - upgrade 세션이 같은 파일에 `ForceStagger(float)`를 추가하고 있어서, 지금 구조를 바꾸면 병합이 깨진다.
  - 잼 일정에서 동작이 같은 큰 리팩터는 위험하다.
  - 모든 브랜치를 병합한 뒤 Fable 검토에서 정할 일로 남긴다. 나눌 때 가장 먼저 분리할 후보는 `Special`·`Frozen`·NUM-7 탈진이다.
- **방송 규칙을 `IEnemyBroadcastRule` 같은 전략 객체로 분리.** 지금은 `OnBroadcastChanged` 안에 블록 3개가 있다. 보스(4-A)가 웨이브마다 같은 규칙을 쓴다면 그대로 재사용할 수 있으니 분리할 필요가 없다. 숫자가 더 늘어나면 그때 분리를 검토한다.
- **`IBroadcastListener`와 `NumberChanged` 이벤트가 같은 일을 두 번 한다.** 적은 이벤트를 쓴다. 다른 세션(보스·오디오)이 둘 중 무엇을 쓰는지 확인한 뒤 하나로 줄이는 것을 제안한다.
- **차임 소리:** 오디오 세션은 `BroadcastBoard.NumberChanged`를 구독하거나, `BroadcastHackUi.cs:93`의 TODO 자리에 한 줄을 넣으면 된다. 전자는 Observer로 내 파일을 건드리지 않는다. 다만 이 방법은 "구역에 들어갈 때"는 알 수 없으므로 5-B 연출에는 후자가 맞다.
- **레벨 빌더(levels·boss 세션):** 적 생성 시 `kind`를 지정해야 한다 (Bug = 1, Android = 2). 전광판을 둘 때는 `zoneX`를 그 구역 범위로 지정해야 한다. 지정하지 않으면 (0,0) = 씬 전체가 된다.

## 6. Play 모드 확인 목록
1. **STG-01 SEG-A:** 상단 방송 HUD가 없다. 적 E-A1은 2, E-A2는 3(개인 숫자)이다.
2. **SEG-A → SEG-B 대문:** 페이드가 끝나면 "((( 3 )))"이 커졌다 작아지며 뜬다. SEG-B 적 3명의 숫자가 모두 3이다.
3. **SEG-B 연타:** 3연타 간격이 SEG-A보다 빠르고, 3타 뒤 반격 창이 길다 (약 1.5초).
4. **SEG-B 전광판 앞에서 E → 0 해킹:**
   - 시민이 푸른 회색으로 굳고 공격하지 않는다.
   - 굳은 적을 정면에서 때리면 튕기지 않고 맞는다. 3타에 기절한다.
   - HUD 아래 진행 바가 차오른다. 5초 뒤 3으로 돌아오면 굳었던 적이 다시 움직인다.
5. **E → 7 해킹:**
   - 적이 빨라진다.
   - 8초 안에 7초가 지나면 적이 2초 동안 Open(반격 창) 상태가 되어 바로 맞는다.
   - 바가 끝나 3으로 돌아오면 원래 속도로 돌아온다.
6. **테스트 씬 `NUMBER/Build Enemy Variants Test Scene`의 Strong(안드로이드):**
   - 연타 사이에 가끔 몸이 황금색이 된다. 0.5초 뒤 황금 탄 3발이 부채꼴로 날아온다.
   - 정면 패링으로 탄이 사라진다.
   - 6초 안에는 다시 쏘지 않는다.
   - 0을 해킹하면 시민(Citizen, RoofCitizen)만 굳는다. Strong(Android)과 Ranged(Bug)는 계속 움직인다.
