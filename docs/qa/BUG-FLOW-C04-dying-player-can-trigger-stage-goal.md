검증: CONFIRMED (LevelTransition.cs:22-27에 IsDead 검사 없음, Checkpoint.cs:23과 비교)

# BUG-FLOW-C04 — 죽는 타격을 맞고 넉백으로 출구에 들어가면 죽었는데도 스테이지가 클리어된다

## 재현 단계
1. STG-02 SEG-B(또는 STG-01·04 SEG-B)에서 HP를 1로 만든다.
2. 출구(TR-B-GOAL) 바로 앞에서, 출구 반대쪽에 있는 적에게 마지막 타격을 맞는다.
3. 넉백(Hurt 상태)으로 출구 트리거 안에 밀려 들어간다.

## 기대 결과
죽은 플레이어는 출구를 쓸 수 없다. 1.5초 뒤 마지막 체크포인트에서 다시 시작한다.

## 실제 결과 (코드 근거)
- `Assets/Scripts/Level/LevelTransition.cs:22-27` `OnTriggerEnter2D`는 `PlayerController`가 있는지만 보고 HP·사망을 확인하지 않는다. `Checkpoint`는 같은 상황을 막아 두었다 (`Assets/Scripts/Checkpoint/Checkpoint.cs:23` "a killing knockback must not advance the checkpoint").
- 죽는 타격은 먼저 Hurt 상태 + 넉백 속도를 준다 (`Assets/Scripts/Player/PlayerController.cs:276-280`). Dead 상태는 `hurtDuration` 뒤에야 들어간다 (`PlayerController.cs:146`). 그 사이 몸이 출구로 밀려 들어갈 수 있다.
- Goal 모드는 0.3초 페이드 뒤 바로 `StageFlow.LoadNext()`를 부른다 (`LevelTransition.cs:34-39`). `RespawnManager`는 Dead 상태가 된 뒤 1.5초를 기다려 다시 로드하므로 (`Assets/Scripts/Checkpoint/RespawnManager.cs:62-68`) 다음 스테이지 로드가 먼저 일어난다.
- 결과: 죽었는데 다음 스테이지로 넘어간다 (새 씬에서 HP는 다시 가득).
- Gate 모드(대문)에서도 죽은 몸이 SEG-B로 순간이동하지만, 이후 리스폰이 기록(CP-A0)으로 되돌리므로 진행 문제는 없다.

## 수정 제안 (참고)
`LevelTransition.OnTriggerEnter2D`에서 `pc.GetComponent<PlayerHealth>().IsDead`이면 return (Checkpoint와 같은 한 줄).

## 심각도
낮음 — 출구 바로 앞에서 반대쪽 적에게 죽어야 하는 드문 상황이고, 이득을 보는 쪽(클리어)으로만 틀린다.
