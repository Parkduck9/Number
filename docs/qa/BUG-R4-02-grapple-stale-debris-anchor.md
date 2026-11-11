작성: Codex (QA 4차, 정적 점검 — Antigravity 사용량 소진으로 대체)

# BUG-R4-02 지붕 조각에 건 갈고리가 움직이는 조각 대신 발사 당시 허공을 당김

- 기준: `25a974e` / 관련: DECISIONS 40의 이동 발판·갈고리 사용
- 확인 방법: 이동 발판과 갈고리 좌표 보관 경로의 정적 대조. 아래 Play 재현은 실행하지 않음.

## 재현 단계
1. STG-05에서 지붕 조각이 흔들림을 끝내고 내려오기 시작할 때까지 기다린다.
2. 약 7–9u 거리의 내려오는 조각을 우클릭해 줄을 건다.
3. 당기는 동안 줄 끝과 조각의 위치를 비교한다.
4. 조각이 부서지는 높이 y=4.3에 가까워졌을 때 다시 시도해, 당기는 도중 조각이 사라지는 경우도 확인한다.

## 기대 결과
줄 끝과 당기는 목표가 조각의 이동을 따라간다. 조각이 부서지면 걸린 줄도 해제된다.

## 실제 결과
갈고리는 Raycast 순간의 월드 좌표만 저장한다. 조각은 초당 1.2u 내려오지만 줄 끝과 당기는 목표는 그대로 남는다. 0.5초면 둘 사이가 약 0.6u 벌어진다. 조각이 비활성화되어도 대상의 생존 여부를 확인할 참조가 없어, 원래 좌표를 향한 당기기와 자동 놓기/상승 반동이 계속 처리된다. 움직이는 발판에 줄을 건다는 새 기능의 연결이 어긋난다.

## 심각도
중간 — 보스전 이동 수단이 보이는 발판과 다른 지점을 향해 작동한다. 널 참조나 영구 진행 막힘은 확인하지 못함.

## 코드 근거
- `Assets/Scripts/Boss/BossDebris.cs:164–168`: 조각 위치를 `FallSpeed * dt`만큼 계속 내리고 y=4.3에서 제거.
- `Assets/Scripts/Boss/BossDebris.cs:183–185`: 조각 제거는 Stage 초기화와 `SetActive(false)`.
- `Assets/Scripts/Player/PlayerController.cs:930–945`: `best.point`를 `grappleAnchor`/`grapplePull`에 복사한 뒤 `best.collider`나 로컬 부착점을 보관하지 않음.
- `Assets/Scripts/Player/PlayerController.cs:528–529`, `995–1015`: 이후 당기기·놓기 조건도 저장된 월드 좌표를 사용.
- `Assets/Scripts/Player/GrappleRope.cs:43–45`, `79`: 그림의 줄 끝도 `Shoot()` 때 복사한 `target`을 사용.

## 수정 힌트 (Sonnet 구현 → Opus 검수)
Ground 갈고리에 부착 대상 Transform/Collider와 로컬 부착점을 보관해 물리 목표와 줄 그림의 끝을 함께 갱신한다. 대상이 없어지거나 비활성화되면 줄을 해제한다. 고정 지형·천장 고리·해치의 기존 경로도 함께 확인한다.

수정: PlayerController가 히트한 Collider의 Transform과 로컬 부착점을 보관해 FixedUpdate마다 anchor/pull/줄 끝(GrappleRope.MoveTarget)을 따라 움직이고, 대상이 비활성/파괴되면 줄을 해제.
