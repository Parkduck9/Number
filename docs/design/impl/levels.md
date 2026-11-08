# 구현 기록 — 레벨 (STG-00 · 02 · 03 · 04, 스테이지 순서)

> 2026-11-08, 브랜치 `claude/levels-9a1293`. 근거: [DECISIONS.md](../DECISIONS.md) 6-A, 2-B, 3-A. 방향이 바뀌면 빌더 표를 고치고 메뉴를 다시 실행하거나, 빌더 파일째 지우면 된다.
> 씬(`.unity`)은 손으로 고치지 않는다. 항상 빌더(`NUMBER/Build STG-0x` 또는 `NUMBER/Build All Stages`)로 다시 만든다.

## 1. 추천안이 무엇이 되었나

| 결정 | 구현 |
|---|---|
| 6-A STG-00 오프닝 | `Stg00Builder` → `STG00.unity`. STG-01 SEG-A 임시 배경 재사용 + 낮 조명. `OpeningSequence`가 순서대로: 1.5초 → 방송 LINE-01(라디오) → 유도 농담 LINE-02(대화, 입력 잠금) → 전광판 1→0 (`SetAiNumber(0)`) → 시민 6명 굳음 + LINE-03 → 오른쪽 끝 출구 켜짐 → 닿으면 페이드 후 STG-01. 전투·적 없음 |
| 2-B STG-02 | `Stg02Builder` → 2구간 (지붕길: 방송 3 / 고가 철로: 방송 7). 시민 적 7명. 해킹 UI 없음 |
| 2-B STG-03 | `Stg03Builder` → 방 1개(한 화면). 러다이트(임시 사각형)에게 다가가면 대화 → 끝나면 충격파 업그레이드 훅 호출 → 출구 열림 |
| 2-B STG-04 | `Stg04Builder` → 2구간 (생산 라인: 방송 7 / 검사 홀: 방송 0). 시민 6 + 안드로이드 자리 3. **해킹 UI 있음** (전광판 높이 4.5 = 바닥에서 E가 닿음) |
| 3-A 방송 구역 고정 | 구간마다 전광판 1개, 숫자 고정. 구역 범위 `zoneX`는 적 규칙 브랜치의 필드가 있으면 넣는다 |
| 스테이지 순서 | `StageFlow.Stages` = STG00→01→02→03→04→05 (`Assets/Scripts/Core/StageFlow.cs:11`). Build Settings는 `StageBuildKit.SyncBuildSettings()`가 Title(있으면) → STG00~05(있는 것) → 나머지 순으로 맞춘다. STG05는 보스 브랜치 씬을 이름으로만 참조 |

## 2. 조정할 수치 (T)

| 무엇 | 위치 |
|---|---|
| 오프닝 시간: 시작 대기 1.5초 / 방송 뒤 대기 4초 / 굳은 뒤 출구까지 1초 | `Assets/Scripts/Level/OpeningSequence.cs:13-15` |
| 낮 조명 세기 1.15 · 색 · 하늘색 | `Assets/Editor/Stg00Builder.cs:16-18` |
| 오프닝 전광판 위치, 시민 x 위치·색, 출구 범위 | `Assets/Editor/Stg00Builder.cs:23-26` |
| 시민 걷기 범위 1.5u · 속도 1u/s · 굳은 색 | `Assets/Scripts/Level/OpeningCitizen.cs:7-10` |
| STG-02 발판·적·전광판·문 | `Assets/Editor/Stg02Builder.cs:14-51` (문서: [LEVEL_STG02.md](../LEVEL_STG02.md)) |
| STG-03 방 너비·러다이트 위치·대화/출구 범위 | `Assets/Editor/Stg03Builder.cs:12-29` |
| STG-04 발판·적·전광판·문 | `Assets/Editor/Stg04Builder.cs:14-50` (문서: [LEVEL_STG04.md](../LEVEL_STG04.md)) |

## 3. 나중에 바꿔야 할 임시 것 (PLACEHOLDER)

- **대사 4개 파일** (사용자가 씀): `Assets/Data/Dialogue/stg00_line01_placeholder.txt`, `stg00_line02_placeholder.txt`(GDD LINE-02 확정 문장), `stg00_line03_placeholder.txt`, `stg03_luddite_placeholder.txt`. 형식만 지키면 파일 내용만 바꿔도 된다. 화자 `luddite`는 `Speakers.asset`에 없어 회색 이름으로 나온다 → 표에 한 줄 추가 필요.
- **배경**: STG-00은 STG-01 SEG-A 컨셉 그림(밤 그림) 재사용, STG-02·03·04는 단색 + 회색 사각형 발판. 아트가 들어오면 빌더의 배경 부분과 발판 높이를 그림에 맞춘다.
- **충격파 업그레이드 훅**: `Assets/Scripts/Level/LudditeMeeting.cs:35` `GrantUpgrade()`가 업그레이드 브랜치의 `Shockwave.SetUnlocked(true)`를 리플렉션으로 부른다. 병합 후 직접 호출로 바꾼다 (WebGL 코드 스트리핑 위험도 같이 사라짐). 병합 전에는 "TODO(upgrade)" 로그만 찍힌다.
- **러다이트·시민 모습**: 사각형. 스프라이트가 생기면 빌더에서 교체.
- **안드로이드**: STG-04 적 이름에 `(ANDROID)`만 붙어 있다. 적 규칙 브랜치가 `android`/`isAndroid` 필드를 넣으면 빌더를 다시 실행하면 켜진다.
- **전광판 `zoneX`**: 적 규칙 브랜치 병합 후 빌더 재실행.

## 4. 쓴 패턴

- **Observer**: 오프닝 시민(`OpeningCitizen`)이 `IBroadcastListener`로 전광판을 구독한다. 연출 스크립트는 시민을 몰라도 `board.SetAiNumber(0)` 한 줄로 전원을 굳힌다. 나중에 진짜 적(NUM-0 규칙)도 같은 이벤트로 움직인다.
- **State (가벼운 형태)**: `OpeningSequence.Phase` enum (Intro → Broadcast → Joke → Freeze → Escape) + 코루틴 하나. 상태 클래스를 따로 만들 만큼 복잡하지 않아 enum으로 충분하다. Inspector/디버그에서 현재 단계를 볼 수 있다.
- **데이터 표 + 빌더**: 레벨 배치는 빌더 위쪽 표에만 있다. 공통 동작은 `StageBuildKit`.

## 5. 패턴 / 리팩터 제안 (다른 파일에 걸친 것 — 이번엔 손대지 않음)

1. `Stg01Builder`의 private 도우미와 `StageBuildKit`이 거의 같다(복사). 통합 때 `Stg01Builder`가 `StageBuildKit`을 쓰게 바꾸면 약 300줄이 줄어든다.
2. `Stg01Builder.SetBuildScene()`은 실행할 때마다 STG01을 Build Settings 맨 위로 올린다 → STG-01을 다시 빌드한 뒤엔 `NUMBER/Sync Build Settings (stage order)`를 실행할 것. 통합 때 `StageBuildKit.SyncBuildSettings()`로 바꾸면 해결.
3. `LevelTransition`(Goal)에서 다음 씬도 Title도 없으면 화면이 검은 채로 입력이 잠긴다 (지금 이 브랜치의 STG-04 목표가 그렇다: STG05·Title 없음). 보스 브랜치·Title 씬이 병합되면 사라지지만, 안전하게 하려면 실패 시 페이드를 되돌리고 잠금을 푸는 몇 줄이 필요하다 (리뷰 제안).
4. `DialoguePlayer`에 "대화 끝" 이벤트가 없어 `OpeningSequence`·`LudditeMeeting`이 `GameFlow.InputLocked`를 지켜본다. 일시정지 메뉴가 같은 플래그를 풀 수 있어 아주 드물게 대화 중간에 다음 단계로 넘어갈 수 있다. `DialoguePlayer`에 `event Action TalkFinished`를 두면 깔끔해진다.
5. 오프닝 시민은 전용 사각형이다. 적 규칙 브랜치의 NUM-0(시민 굳음)이 들어오면 `TestEnemy` 시민(전투 끔)으로 바꿔 같은 코드를 쓰는 것도 가능.
6. 제출 전 Build Settings에서 `Prototype`, `SampleScene`을 끄기 (용량).

## 6. 바꾸는 방법

- 배치·숫자: 해당 빌더 위쪽 표를 고친다 → Unity 메뉴 `NUMBER/Build STG-0x` (또는 `NUMBER/Build All Stages (00,02,03,04)`).
- 배치 모드: `Unity.exe -batchmode -quit -projectPath <프로젝트> -executeMethod StageBuildKit.BuildAll` (에디터가 이 프로젝트를 열고 있으면 실패 → 메뉴로 실행).
- 스테이지 순서: `StageFlow.cs:11` 배열 → `NUMBER/Sync Build Settings (stage order)`.
- 오프닝을 빼고 싶으면: `StageFlow.Stages`에서 `"STG00"`을 지우고 Sync를 실행 (씬은 남겨도 된다).
