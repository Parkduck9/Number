검증: PLAUSIBLE (Stg02Builder.cs:89 BuildHud(false) 확인, 브로드캐스트 표시 의도는 기획 확인 필요)

# BUG-FLOW-C05 — 방송 숫자 HUD가 해킹 UI 안에 있어서, STG-02에는 방송 숫자 표시가 없다

## 재현 단계
1. 새 게임 → STG-01 SEG-B 도착: 화면 위 가운데에 `((( 3 )))` 방송 HUD가 뜬다.
2. STG-02로 넘어간다. SEG-A(방송 3), 대문을 지나 SEG-B(방송 7, 처음 나오는 숫자)로 간다.

## 기대 결과
방송 숫자가 걸린 구역에 들어가면 화면 위에 그 구역의 방송 숫자가 보인다. STG-01·04·05와 같다.
- 근거: DECISIONS 5-B `[확정]` "대문 전환 직후 차임 + 3 방송", 3-A `[확정]` "구역마다 고정, 구역 경계에서만 바뀜" + 역해킹 진행 바 켜기. GDD 12장 "화면 위: 현재 방송 숫자"는 `[제안]`이지만 STG-01·04·05에는 이미 들어가 있다.

## 실제 결과 (코드 근거)
- 방송 HUD(`((( N )))`), 구역 진입 펀치 연출, 차임 TODO 자리, 역해킹 진행 바가 모두 `BroadcastHackUi` 한 컴포넌트에 있다: `Assets/Scripts/Broadcast/BroadcastHackUi.cs:6`, `:52-53`, `:84-104` (차임 TODO `:93`).
- STG-02는 `BuildHud(false)`라 `BroadcastHackUi`가 만들어지지 않는다: `Assets/Editor/Stg02Builder.cs:89`, `Assets/Editor/StageBuildKit.cs:332`. 씬에도 없다 (`Assets/Scenes/STG02.unity`에 `BroadcastHackUi` 없음).
- 결과: STG-02에서는 방송 숫자 HUD·진입 연출·(나중에 붙을) 차임이 전부 없다. NUM-7을 처음 배우는 SEG-B(`Stg02Builder.cs:46-48` "B shows 7 (first time)")에서 화면 신호가 월드 전광판 하나뿐이다.
- 같은 이유로 BUG-FLOW-C02의 수정 제안("STG-01에서 `BroadcastHackUi` 생성을 뺀다")을 그대로 하면 **STG-01의 방송 3 HUD도 같이 사라져 5-B 연출이 깨진다.** C02를 고칠 때 이 결합을 먼저 풀어야 한다.

## 수정 제안 (참고)
- `BroadcastHackUi`에 "해킹 허용" 스위치(`[SerializeField] bool allowHack`)를 두고, 다이얼 열기(`:61-64`)와 프롬프트(`:65`)만 그 값으로 막는다. HUD는 항상 표시.
- 빌더는 STG-01·02·03에 HUD만(`allowHack=false`), STG-04·05에 해킹 허용으로 만든다. (C02의 기획 확인 결과에 맞춰 조정)

## 심각도
중간 — 진행은 되지만, `[확정]` 방송 규칙의 화면 신호가 스테이지마다 있다 없다 한다. C02 수정 때 회귀를 만들 위험이 있다.
