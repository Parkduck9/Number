검증: PLAUSIBLE / 중복: BUG-ENEMY-C05 (기절 콜라이더 부분). 비활성 적 방송 수신 주장은 비활성화하는 코드가 없어 REJECTED 성격

# BUG-ENEMY-A19: 비활성화되거나 기절(Stunned)한 적이 방송 이벤트 수신·라벨 재활성화·콜라이더 잔류로 인해 전투 판정을 방해하는 문제

## 재현 단계
1. **기절한 적의 공격 판정 가로채기:** 시민 적(또는 `NUM-0`으로 굳은 시민)을 3번 때려 기절(`State.Stunned`)시킨 뒤, 기절해 누워 있는 적과 같은 위치에 겹쳐 서 있는 다른 살아있는 적(예: `STG-01`/`STG-02`/`STG-04`의 2인 배치 구간 또는 `STG-05` 보스 웨이브)을 좌클릭으로 공격한다.
2. **비활성 적의 방송 이벤트 수신 및 기절 적 라벨 재활성화:** 적 오브젝트가 일시 비활성화(`SetActive(false)` 또는 `enabled = false`)된 상태에서 방송 숫자를 변경하거나, 적이 `State.Stunned`로 기절해 숫자 홀로그램이 꺼진(`EnemyNumber.Hide()`) 뒤 `EnemyNumber`의 갱신 메서드(`Reroll`, `SetPulse`, `SetFlame`)가 호출되는 상황을 확인한다.

## 기대 결과
- `docs/design/GDD.md` 4장 `[확정]`("시민은 쓰러뜨리면 기절한다... 기절하면 머리 위 숫자 홀로그램이 꺼진다")에 따라 기절한 적은 더 이상 전투 판정에 개입하지 않아야 하며(살아있는 적에 대한 플레이어의 공격을 가로막지 않아야 함), 숨겨진 숫자 라벨이나 불꽃 이펙트가 다시 켜지지 않아야 한다.
- 비활성화된 적 역시 비활성 상태에서 방송 이벤트를 처리하지 않아야 한다(`docs/design/ARCHITECTURE_PATTERNS.md:30`).

## 실제 결과
- `Assets/Scripts/Enemy/TestEnemy.cs:372-376`의 `EnterState(State.Stunned)`는 기절 시 `number.Hide()`와 회전만 처리할 뿐, 적의 `BoxCollider2D`를 비활성화하거나 레이어를 `Enemy`(`enemyMask`)에서 변경하지 않는다. 그런데 플레이어의 공격 판정인 `PlayerController.DoAttackHit()`(`Assets/Scripts/Player/PlayerController.cs:237-241`)은 `Physics2D.OverlapBoxAll`이 아니라 단일 콜라이더만 반환하는 `Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask)`를 사용한다. 그 결과 기절한 적 위에 다른 살아있는 적이 겹쳐 있으면 `OverlapBox`가 기절한 적의 콜라이더만 집어 `ReceivePlayerHit`가 `false`(`TestEnemy.cs:390`)를 반환하고 끝나버려, **기절한 적이 방패막이가 되어 뒤에 겹친 살아있는 적을 전혀 때릴 수 없다.**
- `TestEnemy.cs:98-106`은 `Start()`에서 `zoneBoard.NumberChanged`를 구독하고 `OnDestroy()`에서만 해제하므로, 적이 `SetActive(false)` 또는 `enabled = false`로 비활성화된 동안에도 방송 변경 시 `OnBroadcastChanged`가 실행되어 `State.Frozen` 전환 및 `number.Reroll()`이 호출된다.
- 또한 `EnemyNumber.Hide()`(`Assets/Scripts/Enemy/EnemyNumber.cs:84-93`)는 숨김 플래그를 따로 저장하지 않고, `Refresh()`(`127줄`)가 호출될 때마다 무조건 `label.gameObject.SetActive(true)`를 실행하므로(코드 `106줄` 주석: `also re-activates a hidden label`), 기절 후 어떤 경로로든 `Refresh()`가 호출되면 꺼졌던 머리 위 숫자 홀로그램이 다시 켜진다.

## 심각도
높음
