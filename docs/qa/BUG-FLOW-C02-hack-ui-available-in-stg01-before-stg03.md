검증: PLAUSIBLE (BuildHud(true) 빌더 확인, 설계 의도는 기획 확인 필요)

# BUG-FLOW-C02 — 해킹 UI가 STG-01에 있고 STG-02·03엔 없다 (확정 규칙: STG-03 이후)

## 재현 단계
1. 새 게임 → STG-00 → STG-01.
2. STG-01 SEG-B에 도착해 도착 지점 옆 전광판(숫자 3) 근처에서 E를 누른다.
3. 해킹 다이얼이 열리고 숫자를 바꿀 수 있다.
4. STG-02로 넘어가 전광판(3, 7) 근처에서 E를 누른다.

## 기대 결과
GDD `[확정]`: 전광판 해킹은 "STG-03 이후" (`docs/design/GDD.md:158`). 해킹 도구는 STG-03에서 러다이트에게 받는다 (`docs/design/LEVEL_STG02.md:50`, `docs/design/impl/levels.md:11,13`).
→ STG-01·02·03에서는 해킹이 안 되고, STG-04부터 된다.

## 실제 결과
- STG-01에서 해킹이 된다. `Assets/Editor/Stg01Builder.cs:361-362` `BuildHud()`가 무조건 `BroadcastHackUi`를 만든다. 씬에도 들어 있다: `Assets/Scenes/STG01.unity:574` (`Assembly-CSharp::BroadcastHackUi`).
- STG-02·03에서는 해킹이 안 된다 (`Assets/Editor/Stg02Builder.cs:89` `BuildHud(false)`, `Assets/Editor/Stg03Builder.cs:61` `BuildHud(false)`; 두 씬에 BroadcastHackUi 없음).
- STG-04에서 다시 된다 (`Assets/Editor/Stg04Builder.cs:92` `BuildHud(true)`, `Assets/Scenes/STG04.unity:3973`).
- 결과: 플레이어가 STG-01에서 배운 해킹을 STG-02에서 이유 없이 잃는다. STG-03에서 "도구를 받는" 흐름(`LudditeMeeting`)도 해킹과 연결되지 않는다 (충격파만 켬, `Assets/Scripts/Level/LudditeMeeting.cs:28`).

## 참고
`Stg01Builder.cs:99` 주석("so it can be hacked at once")을 보면 STG-01 해킹이 의도였을 수도 있다. 그렇다면 GDD 158행과 LEVEL_STG02.md 50행을 고쳐야 하고, STG-02에도 해킹 UI가 있어야 흐름이 맞는다. 어느 쪽이 맞는지 기획 확인이 필요하다.

## 수정 제안 (참고)
`Stg01Builder.BuildHud()`에서 `BroadcastHackUi` 생성을 빼고 STG-01을 다시 빌드한다 (GDD 기준일 때). 또는 기획을 바꾼다면 STG-02·03도 `BuildHud(true)`.

## 심각도
중간 — 막히지는 않지만 `[확정]` 규칙과 다르고, 능력을 얻었다가 잃는 진행 오류다.
