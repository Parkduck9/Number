검증: PLAUSIBLE (TestEnemy.cs:390 Stunned는 false 반환, PlayerController.cs:237 단일 OverlapBox는 확인. Stunned 콜라이더가 켜진 채인지는 프리팹 미확인)

# BUG-ENEMY-C05 — 기절한 적의 몸이 플레이어 공격을 가로채서 옆에 있는 살아 있는 적이 안 맞음

## 재현 단계
1. 적 두 명이 붙어 있는 곳을 고른다 (STG-05 보스 웨이브 스폰 지점, STG-01 SEG-B처럼 적이 몰리는 곳).
2. 한 명을 기절(Stunned)시킨다. 기절한 적은 그 자리에 남는다.
3. 기절한 적 바로 뒤(또는 겹친 위치)에 있는 살아 있는 적이 반격 창(Open)일 때 공격한다. 공격 판정 상자(1.6 x 1.4)가 두 적을 함께 덮게 선다.

## 기대 결과
- 이미 기절한 적은 공격 대상에서 빠지고, 살아 있는 적이 맞는다 (빈틈에 맞힌 공격은 들어가야 함 — GDD 5장 `[확정]` "숫자만큼 공격이 끝나면 반격 타이밍").

## 실제 결과
- 공격이 기절한 적에게 걸리면 아무 일도 일어나지 않는다 (피해 없음, 번쩍임 없음, 스파크 없음). 살아 있는 적은 맞지 않는다. 어느 쪽이 걸릴지는 매번 달라서 "공격이 가끔 씹힌다"처럼 보인다.

## 원인 (코드 근거)
- 플레이어 공격은 겹친 적 콜라이더 **하나만** 가져온다: `Assets/Scripts/Player/PlayerController.cs:237` `Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask)` → 241줄에서 그 적 하나에만 `ReceivePlayerHit`.
- 기절한 적은 그 하나를 그대로 소모하고 `false`를 돌려준다: `Assets/Scripts/Enemy/TestEnemy.cs:390` `if (state == State.Stunned) return false;`
- 기절해도 콜라이더·레이어가 그대로다: `TestEnemy.cs:372-376` (`number.Hide()`, 속도 0, 그림만 90° 회전). 루트의 `BoxCollider2D`(0.9 x 2, 트리거 아님)와 Enemy 레이어(8)가 남는다 — `Assets/Prefabs/Prototype/TestEnemy.prefab:249-292`, `Assets/Editor/PrototypeSceneBuilder.cs:157,167`.
- 기절한 적은 사라지지 않는다 (`Destroy`나 콜라이더를 끄는 코드 없음). 보스전은 웨이브마다 스폰 지점 2곳에 적이 쌓인다 (`Assets/Scripts/Boss/BossFight.cs:113-119`).
- 덤: 기절한 몸은 세워진 콜라이더 그대로라 다른 적(Enemy-Enemy 충돌)의 길을 막는 벽이 될 수 있다.

## 수정 제안
- 가장 작은 수정 (적 쪽): `EnterState(State.Stunned)`에서 콜라이더를 끄고 Rigidbody를 멈춘다. 예: `GetComponent<Collider2D>().enabled = false; rb.bodyType = RigidbodyType2D.Kinematic;` (땅에 붙어 있게 하려면 Kinematic + 속도 0). 또는 레이어를 Default로 바꾼다.
- 또는 (플레이어 쪽, 영역 밖): `OverlapBoxAll`로 모두 가져와 기절하지 않은 적을 먼저 고른다.

## 심각도
중간 — 적이 몰리는 곳(보스 웨이브)에서 반격 타이밍의 공격이 이유 없이 무시된다. 핵심 전투 루프가 흔들린다.
