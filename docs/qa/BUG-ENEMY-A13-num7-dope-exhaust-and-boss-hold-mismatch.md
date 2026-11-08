검증: PLAUSIBLE (시나리오 A는 A08과 중복. 보스전 7 해킹 4초 주장·시나리오 C는 일부만 코드로 확인) / 일부 중복: A08, C07

# BUG-ENEMY-A13: NUM-7 탈진 중 재도핑 시 exhaustUntil 미초기화 및 STG-05 보스전 해킹 유지 시간(4초) 부족으로 탈진이 발동하지 않는 문제

## 재현 단계
1. **시나리오 A (탈진 중 7 재진입):** 전광판을 `7`로 해킹(`holdSeconds = 8초`, `Assets/Scripts/Broadcast/BroadcastBoard.cs:27`)하여 7초 도핑(`Num7DopeSeconds = 7f`)이 끝난 뒤 2초 탈진(`exhaustUntil = Time.time + 2f`, `t = 7~9초`)에 진입시킨다. 탈진 도중인 `t = 8초`에 해킹이 만료되어 원래 숫자로 돌아간 직후(`t = 8.1초`, 아직 `Time.time < exhaustUntil`), 다시 전광판을 `7`로 해킹한다.
2. **시나리오 B (STG-05 보스전 7 해킹):** `STG-05` 보스전 1웨이브(방송 `3`) 또는 3웨이브(방송 `0`)에서 전광판을 `7`로 해킹하고 해킹이 끝날 때까지 기다린다.
3. **시나리오 C (NUM-7 구역 대기 적):** 방송 숫자가 `7`인 구역(`STG-02` SEG-B, `STG-04` SEG-A)에서 플레이어 인식 범위(`activationRange = 8u`) 바깥에 서 있는 적의 머리 위 숫자를 9초마다 관찰한다.

## 기대 결과
- `docs/design/DECISIONS.md` 1-A 및 `docs/design/impl/enemy-rules.md:13-16`에 따라 방송 숫자 `7`이 켜지면 즉시 도핑(`×1.3` 가속)이 적용되고, 7초가 지나면 2초간 탈진(`State.Open`)이 발동해야 한다.
- 아직 전투를 시작하지 않은 대기 중인 적이나 체인 도중이 아닌 적의 개인 숫자는 공격 체인이 끝나기 전에 임의로 바뀌지 않아야 한다(`docs/design/GDD.md` 5장 `[확정]`).

## 실제 결과
- **시나리오 A:** `Assets/Scripts/Enemy/TestEnemy.cs:159`의 `OnBroadcastChanged`는 `if (n == 7) dopedUntil = Time.time + BroadcastRules.Num7DopeSeconds;`만 설정하고 이전 탈진 종료 시각인 `exhaustUntil`을 초기화하지 않는다. 따라서 `Time.time < exhaustUntil`인 상태에서 `7`이 다시 켜지면 `Doped` 프로퍼티(`TestEnemy.cs:77`: `broadcast == 7 && Time.time < dopedUntil && Time.time >= exhaustUntil`)가 남은 `exhaustUntil` 동안 `false`가 되어 도핑이 즉시 적용되지 않는다.
- **시나리오 B:** `Assets/Editor/Stg05Builder.cs:78`에서 보스 전광판의 `holdSeconds`를 `4f`로 설정해 두었기 때문에, 보스전에서 `7`을 해킹하면 `BroadcastRules.Num7DopeSeconds`(`7f`, `Assets/Scripts/Broadcast/BroadcastRules.cs:11`)가 되기도 전인 4초 만에 해킹이 만료된다(`Assets/Scripts/Broadcast/BroadcastBoard.cs:62`). 즉 적에게 4초 동안 속도 버프만 주고 2초 탈진 보상은 절대 발동하지 않는다.
- **시나리오 C:** `Assets/Scripts/Enemy/TestEnemy.cs:169-177`의 `UpdateDope()`는 적이 비활성 대기 상태(`!IsActive()`)이거나 체인을 한 번도 시작하지 않은(`!chainStarted`) 상태여도 7초마다 강제로 `State.Open`에 넣었다가 2초 뒤 `FinishReset()`(`275-278줄`)을 호출해 `number.Reroll()`을 실행하므로, 가만히 서 있는 적들의 행운의 숫자가 9초마다 저절로 바뀐다.

## 심각도
중간
