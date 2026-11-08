# 엔딩 선택에 숫자 다이얼 재사용 (END-02) 구현 기록

> 2026-11-08. 근거: [03-strategy.md](../../review/03-strategy.md) 2-4 "엔딩 선택 화면에 숫자 다이얼을 다시 쓴다", [DECISIONS.md](../DECISIONS.md) 검수 Top 5 5번 (END-02 전용 그림은 자름 → 대사 + 다이얼 연출만), GDD 10장 END-01·END-02 `[확정]`.
> 새 아트 없음. 씬·빌더·프리팹은 손대지 않았다 (STG05의 `StopButton`은 그대로 쓴다).

## 1. 흐름
1. 정지 버튼 옆에서 **E** → 기존 선택창 그대로: `[1] 정지 버튼을 누른다` / `[2] 누르되, 행운의 숫자를 직접 정한다` / Esc·P: 돌아가기.
2. **1** → END-01 대화 → 타이틀 (변경 없음).
3. **2** → 선택창이 사라지고 **해킹 다이얼과 같은 판**이 가운데에 뜬다. 제목만 `LUCKY NUMBER / 행운의 숫자`, 나머지(큰 `<  N  >`, 0~9 줄, 0일 때 빨간 경고 "EVERYONE STOPS BREATHING", 아래 조작 안내)는 해킹 다이얼과 동일.
   - 조작도 동일: **0~9** 바로 선택, **↑/↓** 또는 **마우스 휠**로 한 칸씩, **E** 확정, **Esc**(또는 P) → 선택창으로 돌아감.
   - 시작 숫자 = 플레이어가 서 있는 구역의 현재 방송 숫자 (해킹 다이얼과 같은 규칙. 보스방 3웨이브면 0). 구역 전광판이 없으면 0.
4. **E** 확정 → END-02 대화 → 타이틀.

## 2. 고른 숫자의 의미 (판단 필요 — 지금은 가장 단순한 쪽)
- GDD·DECISIONS 어디에도 "어떤 숫자를 고르면 다른 엔딩/다른 대사"라는 항목이 없다. 그래서 **숫자는 상징이다. 어떤 숫자를 골라도 END-02 하나로 간다.** (엔딩은 2개 `[확정]`, END-03 `[보류]`.)
- 다만 고른 숫자가 화면에 다시 나오지 않으면 다이얼이 헛돈 것처럼 보이므로, **엔딩 대사 파일의 `{N}` 토큰을 고른 숫자로 바꿔 준다.** 예: `citizen | 오늘의 행운의 숫자는… {N}. | Today's lucky number is... {N}.` 토큰이 없으면 아무 일도 없다 (지금 placeholder 파일에는 토큰이 없다 → 대사 작성 때 사용자가 넣으면 된다, `Assets/Data/Dialogue/ending_02_placeholder.txt`).
- `[플래그]` 0을 고르면 "모두 숨을 멈춘다" 경고가 그대로 뜬다. 0 = 특별 엔딩으로 갈라야 하는지는 사용자 결정. 지금은 0도 END-02.
- `[플래그]` 확정한 숫자를 보스 전광판에 실제로 띄우는 것(`BroadcastBoard.Hack(n)`)은 넣지 않았다. 보스방의 역해킹 타이머(4초)가 엔딩 대화 중에 숫자를 0으로 되돌려 버려서 오히려 어색하다. 원하면 `StopButton.Play`에서 한 줄로 가능하지만 `BroadcastBoard`의 유지 시간을 함께 손봐야 한다.

## 3. 파일
| 파일 | 상태 | 역할 |
|---|---|---|
| `Assets/Scripts/Broadcast/NumberDial.cs` | 신규 | 다이얼 판(제목·큰 숫자·0~9 줄·경고·조작 안내)을 코드로 만들고 입력을 읽는 공용 클래스 (MonoBehaviour 아님). 스타일 상수(색·크기)는 이 파일 위쪽으로 옮김 |
| `Assets/Scripts/Broadcast/BroadcastHackUi.cs` | 수정 | 자체 다이얼 코드를 지우고 `NumberDial`을 씀. **해킹 동작은 그대로** (시작 숫자 = 전광판 숫자, E = Hack, Esc = 닫기, 멀어지거나 대화·사망이면 닫힘) |
| `Assets/Scripts/Level/StopButton.cs` | 수정 | `Phase.Dial` 추가. 2 → 다이얼, E → `{N}` 치환 후 END-02 재생, Esc/P → 선택창. 선택·다이얼 중 사망이면 닫고 Idle |
| `docs/design/impl/ending-dial.md` | 신규 | 이 문서 |

`NumberDial` 사용법 (두 곳이 같다):
```csharp
dial = new NumberDial(canvasRoot, font, "BROADCAST HACK", "방송 해킹"); // 판 생성 (처음엔 꺼짐)
dial.Open(startNumber);                     // 켜고 숫자 표시
switch (dial.Read()) { case Confirm: ... dial.Selected ...; case Cancel: ...; }  // 매 프레임, 열려 있을 때
dial.RefreshTexts();                        // Loc.Changed 때
```
`Read()`는 Esc를 소비하면 `GameFlow.EscConsumedFrame`을 적어 일시정지 메뉴가 같은 프레임에 열리지 않게 한다 (기존 해킹 다이얼과 같음).

## 4. 지킨 QA 항목
- BUG-BOSS-C01/C04 (사망 중 열림): `StopButton`은 Idle에서 `GameFlow.IsPlayerDead`를 보고, **선택창·다이얼이 열린 채 죽으면 닫고 Idle로** 돌아간다(추가). `BroadcastHackUi`의 사망 검사는 그대로.
- BUG-BOSS-C02 (언어 전환): 모든 문구는 `T(en, ko)`로 만들고 `Loc.Changed`에서 다시 적용한다. 다이얼이 열려 있으면 경고 문구도 즉시 바뀐다.

## 5. 되돌리기 / 빼기
- END-02를 예전처럼 "2 누르면 바로 대화"로: `StopButton.Update`의 `digit2Key` 줄을 `Play(ending02)`로 바꾸고 `Phase.Dial` case·`OpenDial`을 지운다. `NumberDial`은 해킹 UI가 계속 쓰므로 남긴다.

## 6. Play 모드 확인 목록
1. STG05 3웨이브 중 전광판 해킹 → 정지 버튼 옆에서 E → 선택창 (이전과 같음). Esc → 닫힘, 다시 조작 가능.
2. **2** → 선택창 대신 해킹 다이얼과 똑같이 생긴 판, 제목 "LUCKY NUMBER". 시작 숫자는 상단 HUD의 방송 숫자와 같다.
3. 0~9 키로 바로 바뀜, ↑/↓·휠로 한 칸씩, 0이면 빨간 경고.
4. **E** → 판이 닫히고 END-02 대화 → 마지막 줄 뒤 타이틀. (대사 파일에 `{N}`을 넣어 두면 고른 숫자가 그 자리에 나온다.)
5. 다이얼에서 **Esc** → 선택창으로 돌아감 (일시정지 메뉴가 열리지 않아야 함). 선택창에서 Esc → 닫힘.
6. 다이얼이 열린 동안 플레이어는 움직이지 않고 적도 멈춘다 (`InputLocked`).
7. L 키로 언어 전환: 프롬프트·선택창·다이얼 제목/안내/경고가 즉시 바뀐다.
8. 전광판 해킹 다이얼은 이전과 똑같이 동작한다 (시작 숫자·E·Esc·멀어지면 닫힘·죽으면 닫힘·0 경고).
9. 죽은 상태(리스폰 1.5초 안)에서는 정지 버튼 프롬프트가 안 뜨고 E가 먹지 않는다.
