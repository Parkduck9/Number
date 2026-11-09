작성: Claude Sonnet (Antigravity 할당량 소진으로 대체)

# BUG-R2-04: 저격 탄의 LMB 패링 창이 약 0.16초(WebGL 30fps에서 5프레임)이고, 일찍 누르면 헛스윙이 되어 패링 기회를 날린다

## 심각도
**낮음** (밸런스. 쉬프트 회피로 피할 수 있다. 플레이 확인 필요)

## 재현 단계
1. STG-01/02/04에서 저격(S-*)이 레이저로 조준하고 0.25초 고정 뒤 발사하는 것을 본다.
2. 고정 순간(탄이 아직 15u 밖)에 좌클릭을 누른다 → 근처에 위협이 없으므로 `FaceParryThreat`가 false → 돌진/제자리 공격(0.3초)이 나가고 패링은 안 나간다.
3. 탄이 3u 안(발사 후 마지막 0.16초)일 때만 좌클릭이 패링으로 바뀐다.

## 기대 결과
- 레이저 고정(0.25초)이 보이는 동안 누른 좌클릭도 탄이 도착할 때 패링으로 인정되거나, 최소한 창이 사람이 맞출 만큼 넓다.

## 실제 결과
- 코드 주석이 스스로 밝힌 창: `(3 − 0.7) / 14 = 0.164초`. 프레임 30fps(WebGL)이면 약 5프레임, 입력 지연까지 더하면 거의 한 프레임 차이로 성공/실패가 갈린다.
- 일찍 누른 공격은 `attackBuffer`(0.15초)가 스윙 종료 뒤로 밀어 줄 수 있지만, 스윙 0.3초가 끝나기 전에 탄이 도착하면 맞는다.

## 근거
- `Assets/Scripts/Enemy/TestEnemy.cs:35`: `SniperShotSpeed = 14f` (주석: 패링 창 0.16초).
- `Assets/Scripts/Core/DashRules.cs`: `ParryShotRange = 3f`, `ParryShotLateral = 1.3f`.
- `Assets/Scripts/Player/PlayerController.cs:601-640` `FaceParryThreat`: `dist > ParryShotRange`이면 건너뜀. 같은 파일 `UpdateLocomotion`: 위협이 없으면 `EnterState(State.Attack)`.
- 비교: 근접 패링 창은 `ParryMeleeWindow` 0.3초 + 커버 마진 0.1.

## 제안
- 저격 탄에 한해 `ParryShotRange`를 4~5u로 키우거나(창 0.24~0.31초), 레이저 고정 중 누른 좌클릭을 "패링 예약"으로 처리한다. 사용자 플레이 느낌 확인 후 결정.
