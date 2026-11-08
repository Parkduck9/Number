검증: PLAUSIBLE (시나리오 A는 C04와 중복. 나머지 B·C·D는 일부만 코드로 확인, 플레이 확인 필요) / 일부 중복: BUG-ENEMY-C04, C07

# BUG-ENEMY-A08 — NUM-7 도핑·탈진 타이머와 충격파(ForceStagger)·재해킹·특수 적 지연·STG-05 유지 시간 간 충돌

## 재현 단계
1. **시나리오 A (탈진 중 충격파):** 방송 숫자가 `7`(NUM-7)인 구역에서 7초 도핑이 끝나 적이 2초 탈진(`State.Open`, `openDuration = 2.0f`)에 들어간 직후 `Space` 충격파(`Shockwave`, `staggerSeconds = 1.0f`)를 사용한다.
2. **시나리오 B (탈진 중 7 재방송):** 적이 2초 탈진(`Time.time < exhaustUntil`) 중인 상태에서 전광판이 다른 숫자로 바뀌었다가 다시 `7`로 방송되게 한다.
3. **시나리오 C (특수 적 마지막 일격 지연):** 안드로이드나 원거리 적(`lastStrikeDelay = 0.5f`)이 NUM-7 도핑 상태일 때 마지막 일격의 예고 시간을 확인한다.
4. **시나리오 D (STG-05에서 7 해킹):** STG-05 보스전에서 전광판을 `7`로 해킹한 뒤 7초 도핑 후 탈진이 발생하는지 기다린다.

## 기대 결과
- 확정 결정(`docs/design/DECISIONS.md:24, 54` 1-A·3-A 확정, `docs/design/impl/enemy-rules.md:13-16, 23`)에 따라:
  1. NUM-7 탈진은 2초 동안 유지되어야 하며, 플레이어의 충격파 스킬이 적의 무방비 시간을 오히려 단축시키지 않아야 한다.
  2. 방송이 새로 `7`이 되면 즉시 7초 도핑(`Doped == true`)이 시작되어야 한다.
  3. NUM-7 도핑 중에는 모든 예고(`Windup`) 시간이 `÷1.3`(`Num7SpeedMul`) 빨라져야 한다.
  4. 전광판을 `7`로 해킹하면 기본 유지 시간(8초) 안에 7초 도핑이 끝나 2초 탈진 기회를 얻을 수 있어야 한다.

## 실제 결과
1. **탈진 중 충격파 사용 시 조기 해제:** 2초 탈진(`State.Open`, `openDuration = 2f`, `exhaustUntil = now + 2f`) 직후 `ForceStagger(1f)`가 호출되면 `State.Staggered`(1초)로 덮어써져 **1초 만에 `FinishReset()` → `State.Approach`로 깨어난다.** 게다가 남은 1초 동안은 `Time.time < exhaustUntil`이라 `Doped`가 `false`여서 일반 속도로 다가오다가 `exhaustUntil`이 지나는 순간 갑자기 도핑 속도(`×1.3`)로 바뀐다.
2. **탈진 중 7 재진입 시 도핑 누락:** `OnBroadcastChanged`에서 `if (n == 7) dopedUntil = Time.time + BroadcastRules.Num7DopeSeconds;`만 갱신하고 `exhaustUntil`을 초기화하지 않아, 이전 탈진 타이머(`Time.time < exhaustUntil`)가 남아 있는 동안 새 7 방송이 시작되어도 `Doped`(`Time.time >= exhaustUntil`)가 `false`가 된다.
3. **마지막 일격 지연(`lastStrikeDelay`)에 도핑 배율 미적용:** `State.Windup`에서 `if (Doped) wait /= BroadcastRules.Num7SpeedMul;`을 먼저 계산한 뒤에 `if (strikesLeft == 1) wait += lastStrikeDelay;`를 더하기 때문에, `lastStrikeDelay`(0.5초) 부분은 도핑 속도 배율(`÷1.3`)이 적용되지 않는다.
4. **STG-05 전광판 해킹 시 NUM-7 탈진 불가능:** `Stg05Builder.cs`에서 보스 전광판의 `holdSeconds`를 확정값 `8초` 대신 `4초`로 덮어써서, 플레이어가 전광판을 `7`로 해킹하면 7초 도핑(`Num7DopeSeconds = 7f`)이 끝나기 전인 4초 만에 역해킹당해 적만 4초간 강화시키고 탈진 기회는 절대 얻을 수 없다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Enemy/TestEnemy.cs:77, 158-159, 168-177` — `Doped` 프로퍼티가 `Time.time >= exhaustUntil`을 조건으로 검사하지만 `OnBroadcastChanged`에서 `n == 7`일 때 `exhaustUntil = 0f` 초기화가 없음.
- `Assets/Scripts/Enemy/TestEnemy.cs:436-443` — `ForceStagger(float seconds)`가 현재 남은 `openDuration`(NUM-7 탈진 2초)보다 짧은 `seconds`(1초)로 상태를 덮어씀.
- `Assets/Scripts/Enemy/TestEnemy.cs:218-219` — `if (Doped) wait /= BroadcastRules.Num7SpeedMul;` 이후에 `if (strikesLeft == 1) wait += lastStrikeDelay;`를 수행함.
- `Assets/Editor/Stg05Builder.cs:78-79` & `Assets/Scripts/Broadcast/BroadcastRules.cs:11` — `Stg05Builder`가 `holdSeconds = 4f`로 설정하여 `Num7DopeSeconds = 7f`보다 짧음.
