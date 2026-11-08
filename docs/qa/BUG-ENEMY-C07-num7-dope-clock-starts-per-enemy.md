검증: CONFIRMED (TestEnemy.cs:95-100 Start가 OnBroadcastChanged 호출 → :159 dopedUntil 설정, UpdateDope는 플레이어 근접 여부와 무관하게 순환)

# BUG-ENEMY-C07 — NUM-7 도핑 7초가 "방송이 켜진 때"가 아니라 적마다 생성된 때부터 돌아서, 탈진 시점이 어긋나고 모두 함께 탈진하지 않음

## 재현 단계
A) STG-02
1. STG-02를 시작해 SEG-A(방송 3)에서 30초 이상 싸운다.
2. SEG-B(AI 방송 7)로 넘어간다. HUD에 "((( 7 )))"이 펀치와 함께 뜬다.
3. 도착 순간부터 시간을 잰다.

B) STG-05 보스 2웨이브 (방송 7, 시민 3 + 벌레 2)
1. 2웨이브가 시작되면 적 5명이 0.6초 간격으로 나온다.
2. 7초쯤 뒤 탈진(Open) 상태를 지켜본다.

## 기대 결과
- DECISIONS 1-A: "7초 도핑 … 끝나면 **모든 적** 2초 탈진". GDD 5장 NUM-7: "7초 동안 버텨야 함 → 7초 후 탈진". 플레이어는 방송 7을 본 때부터 7초를 세고, 그 뒤 구역 적 전체가 함께 2초 빈틈을 보여야 한다.

## 실제 결과
- A) SEG-B 적은 **씬을 불러온 순간**부터 도핑 7초 / 탈진 2초를 반복하고 있었다. 플레이어가 도착한 시점의 위상은 아무 때나다 (도착하자마자 탈진이 끝나 다시 7초 도핑일 수도, 1초 뒤 탈진일 수도 있음). 보이지 않는 곳의 적도 9초마다 Open → 재추첨을 반복한다.
- B) 적마다 자기 `Start`에서 7초를 시작하므로 탈진 시작이 0.6초씩 어긋난다. 첫 적의 탈진은 스폰 후 7~9초, 마지막(5번째, +2.4초) 적은 9.4~11.4초 → **다섯 명이 동시에 탈진하는 순간이 없다**.

## 원인 (코드 근거)
- 도핑 시작 시각을 적이 각자 정한다: `Assets/Scripts/Enemy/TestEnemy.cs:159` `if (n == 7) dopedUntil = Time.time + BroadcastRules.Num7DopeSeconds;`
- 이 함수가 적의 `Start`에서도 불린다 (방송이 바뀐 게 아니어도): `TestEnemy.cs:95-100` `OnBroadcastChanged(zoneBoard, zoneBoard.Number, -1)`.
- 탈진 주기는 활성화 거리(`IsActive`)와 상관없이 매 프레임 돈다: `TestEnemy.cs:120`, `166-177`.
- 데이터: STG-02 SEG-B AI 숫자 7 — `Assets/Editor/Stg02Builder.cs:48,76`. 보스 2웨이브 7, 스폰 간격 0.6초 — `Assets/Scripts/Boss/BossFight.cs:27,33,102,108-123`.

## 수정 제안
- 도핑 기준 시각을 전광판이 갖게 한다. 예: `BroadcastBoard`에 `public float NumberSince`(숫자가 바뀐 `Time.time`)를 두고, 적은 `dopedUntil = board.NumberSince + Num7DopeSeconds`로 맞춘다. 늦게 생긴 적도 같은 위상을 쓴다. (주기 반복은 `(Time.time - NumberSince) % (7+2)`로 계산.)
- STG-02처럼 처음부터 7인 구역은 "플레이어가 구역에 처음 들어온 때"를 기준으로 할지 사용자 확인이 필요하다 (enemy-rules.md 1-A "구현하며 정한 것 (확인 필요)"와 함께).

## 심각도
중간 — 확정 규칙(1-A "모든 적 2초 탈진")이 STG-02 SEG-B와 보스 2웨이브, 즉 NUM-7이 나오는 모든 곳에서 의도대로 보이지 않는다.
