검증: CONFIRMED (Stg05Builder.cs:91-92 스폰 y=14, BossFight.cs:114-116가 2D 거리로 비교하므로 플레이어가 y 약 5.5 이하면 항상 8.5 이상이라 4u 조건이 발동하지 않음)

# BUG-BOSS-C06 "플레이어 근처 소환 금지 반경(4u)"이 실제로는 한 번도 작동하지 않아, 구석에 선 플레이어 바로 위로 적이 떨어진다

- 영역: 보스 (STG-05) / 적 소환
- 확인 방법: 코드·빌더 수치 계산 (Play 모드 미실행)

## 재현 단계
1. STG05에서 싸움을 시작한 뒤 왼쪽 구석(x ≈ 2) 또는 오른쪽 구석(x ≈ 28, 정지 버튼 x=26 근처)에 선다.
2. 다음 적이 소환되는 것을 본다.

## 기대 결과
- boss.md 3장 "플레이어 근처 소환 금지 반경 4u", 코드 주석 "never spawn this close to the player" (`Assets/Scripts/Boss/BossFight.cs:28`)대로, 플레이어가 서 있는 쪽 소환 지점은 건너뛰고 반대쪽에서 나온다.

## 실제 결과
- 거리 비교는 소환 지점 자체(천장 쪽)와 플레이어 사이의 2D 거리다 (`Assets/Scripts/Boss/BossFight.cs:114-116`).
- 소환 지점은 y=14다 (`Assets/Editor/Stg05Builder.cs:91-92`). 플레이어 기준점은 바닥에서 y=3이고 (`Stg05Builder.cs:62`), 점프 높이는 약 2.5u다 (`jumpSpeed 14`, `gravityScale 4` → 14² / (2 × 9.81 × 4) ≈ 2.5, `PlayerController.cs:16-17`). 방에는 발판이 없다.
- 따라서 플레이어와 소환 지점 거리는 항상 약 8.5u 이상이고, `< spawnSafeRadius(4)` 조건은 절대 참이 되지 않는다. 적은 플레이어가 구석에 있어도 그 자리 위로 떨어진다(활성 범위 0이라 착지 즉시 0.5초 예비 동작 후 공격).

## 심각도
낮음 — 적이 플레이어와 부딪히지 않고 예비 동작이 있어 바로 맞지는 않는다. 다만 문서·주석의 보호 장치가 없는 것과 같고, 정지 버튼(x=26) 옆이 오른쪽 소환 지점(x=28) 착지점과 겹친다.

## 수정 제안 (코드 담당 참고)
- 거리 대신 가로 거리만 비교한다: `Mathf.Abs(player.transform.position.x - point.position.x) < spawnSafeRadius` (1줄).
