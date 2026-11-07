# 프로토타입 아키텍처 — 11/08 (이동·점프·공격·패링·대시 + 테스트 적 1종)

> 작성: Claude Fable (설계). 구현: Sonnet. 검수: Opus.
> 근거: [GDD.md](GDD.md) 5장 ①개인 숫자, 6장 플레이어 능력, 12·13·14장. `[확정]` 항목만 구현 근거로 썼고, 프로토타입에 필요했던 `[제안]`·`[미정]` 항목은 **7절**에 모아 2026-11-08 사용자 답변으로 확정했다.
> 수치 뒤의 `(T)` = 튜닝 가능한 시작값. 인스펙터 `[SerializeField]`로 노출한다.

## 0. 프로젝트 현재 상태 (확인 완료)

| 항목 | 상태 | 설계에 미치는 영향 |
|---|---|---|
| Unity | 6000.3.28f1, URP 17.3 2D Renderer | `Rigidbody2D.linearVelocity` 사용 (`velocity` 아님). 씬에 Global Light 2D 필요 (없으면 Sprite-Lit 머티리얼이 검게 나옴) |
| 입력 | **Input System만** 활성 (`activeInputHandler: 1`). `Assets/Settings/InputSystem_Actions.inputactions` 기본 템플릿 있음 (Player 맵: Move=WASD/화살표, Jump=Space, Attack=좌클릭, Sprint=LShift, Interact=E). C# 래퍼 생성 꺼짐 | 레거시 `Input.GetKey` 금지. 기존 에셋을 고쳐 쓴다 (1.1절) |
| Physics 2D | 기본값. 중력 -9.81, `queriesHitTriggers=1`, 커스텀 레이어 없음, 태그 없음 | 레이어 3개 추가 (5절) |
| 스크립트 | `Assets/Scripts/`, `Assets/Editor/` 없음 | 전부 신규 |
| TextMeshPro | ugui 2.0.0에 포함, **Essential Resources 미임포트** | 씬 빌더가 임포트 (5절) |
| 플레이스홀더 | `Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png` | 색 사각형으로 사용. `Assets/Art/`는 건드리지 않음 |
| WebGL | threads 끔, 1920×1080 | 코루틴·스레드 없이 `Time.time` 타이머. 파일 IO 없음 |

## 1. 파일·클래스 목록

```
Assets/Scripts/
  Combat/HitInfo.cs           struct HitInfo + enum HitResult (공격자→피격자 전달 데이터)
  Player/PlayerInputReader.cs InputActionAsset에서 액션 5개를 찾아 "이번 프레임 눌림/유지" 값으로 노출
  Player/PlayerHealth.cs      HP 5, 피격 후 무적 시간, 사망 여부
  Player/PlayerController.cs  상태 머신 + 이동 물리 + 공격/패링/대시 + 적 공격 수신(ReceiveEnemyHit)
  Enemy/EnemyNumber.cs        개인 숫자 값(2~5) 보관·감소·재추첨 + 머리 위 TMP 표시(색·예고 펄스)
  Enemy/TestEnemy.cs          테스트 적(세뇌된 시민 자리) 행동 루프 + 플레이어 공격 수신(ReceivePlayerHit)
  Core/PrototypeDebug.cs      OnGUI로 HP·플레이어 상태·적 상태/숫자 표시, R 키로 씬 재시작
Assets/Editor/
  PrototypeSceneBuilder.cs    메뉴 NUMBER/Build Prototype Scene: 레이어·TMP 임포트·씬·프리팹 자동 생성
```

생성물(코드가 만드는 것): `Assets/Scenes/Prototype.unity`, `Assets/Prefabs/Prototype/Player.prefab`, `Assets/Prefabs/Prototype/TestEnemy.prefab`.
수정하는 기존 파일: `Assets/Settings/InputSystem_Actions.inputactions` (1.1절).

**연결 방식 — 이벤트 없음, 전부 직접 호출.** 프로토타입에서 이벤트 시스템은 과하다.

| 호출 방향 | 방법 |
|---|---|
| PlayerController → PlayerInputReader / PlayerHealth | 같은 GameObject, `GetComponent` 캐시 |
| PlayerController → TestEnemy | 공격 순간 `Physics2D.OverlapBox(…, enemyMask)` → `GetComponent<TestEnemy>()` → `ReceivePlayerHit(HitInfo)` |
| TestEnemy → PlayerController | 타격 순간 `Physics2D.OverlapBox(…, playerMask)` → `GetComponent<PlayerController>()` → `ReceiveEnemyHit(HitInfo)` 반환값으로 패링/회피 판정 |
| TestEnemy → 플레이어 위치 | `Start()`에서 `FindFirstObjectByType<PlayerController>()` (플레이어 1명) |
| TestEnemy → EnemyNumber | 같은 GameObject, `GetComponent` |
| PrototypeDebug → 모두 | `FindFirstObjectByType`으로 찾아 OnGUI에서 매 프레임 읽기 (폴링) |

**공유 타입 (Combat/HitInfo.cs)** — 두 작업이 병렬로 갈 수 있도록 시그니처를 고정한다.
```csharp
public struct HitInfo { public int Damage; public Vector2 SourcePosition; public bool IsSpecial; }  // IsSpecial = 체인의 마지막 일격
public enum HitResult { Hit, Parried, Dodged }
// PlayerController:  public HitResult ReceiveEnemyHit(HitInfo info)
// TestEnemy:         public bool      ReceivePlayerHit(HitInfo info)   // true = 피해 들어감
```

### 1.1 입력 에셋 수정 (`InputSystem_Actions.inputactions`, JSON 직접 편집)
- `Jump`에 바인딩 `<Keyboard>/w` 추가하고 **Space 바인딩은 지운다** (Q-G: Space는 나중 스킬·기믹용으로 비워 둠).
- `Sprint` → 이름을 `Dash`로 바꾼다 (바인딩 LShift 유지).
- 액션 `Parry` (type Button) 추가, 바인딩 `<Mouse>/rightButton`, groups는 Attack의 좌클릭 바인딩과 동일하게. id는 새 GUID 아무거나.
- Attack(좌클릭)·Interact(E)·Move는 그대로. Look/Crouch/Previous/Next는 건드리지 않는다.
- 키: 좌클릭 공격 / 우클릭 패링 / LShift 대시 / W 점프 — 확정 (Q-A, Q-G).

`PlayerInputReader`: `[SerializeField] InputActionAsset asset;` → `Awake`에서 `asset.FindActionMap("Player", true)`의 Move/Jump/Attack/Parry/Dash를 캐시, `OnEnable/OnDisable`에서 맵 Enable/Disable. 노출: `float MoveX` (Move.x), `bool JumpPressed`(WasPressedThisFrame), `bool JumpHeld`, `bool AttackPressed`, `bool ParryPressed`, `bool DashPressed`. **Update에서만 읽는다** (FixedUpdate에서 WasPressedThisFrame은 누락/중복).

## 2. 플레이어 상태 머신 (PlayerController)

**물리 구성**: `Rigidbody2D` Dynamic, gravityScale 4 (T), Freeze Rotation Z, Interpolate, Continuous. `BoxCollider2D` 0.9×2.0. 레이어 Player. 단위: **PPU 16, 플레이어 1×2 유닛(16×32px)** — ART-11 `[확정]` 캐릭터 키 28~34px에 맞춤.

**틱 분리**: `Update()` = 입력 읽기 → 버퍼 갱신 → 상태 전이·타이머·히트 판정. `FixedUpdate()` = 현재 상태에 맞게 `linearVelocity` 쓰기. 상태는 `enum State { Locomotion, Attack, Parry, Dash, Hurt, Dead }` 하나 + `stateEnteredAt`(Time.time). 클래스 분리 없음.

| 상태 | 들어갈 때 | 매 프레임 | 나가는 조건 |
|---|---|---|---|
| Locomotion | — | 좌우 이동 8 u/s (T), 지상·공중 동일. 점프 입력 처리. 바라보는 방향 = 마지막 0 아닌 MoveX | Attack/Parry/Dash 입력 → 해당 상태 |
| Attack | 지상이면 x속도 0, 공중이면 유지 | 시작 0.08초(T) 후 **한 번** 히트박스 판정 → 적중 시 `ReceivePlayerHit` | 총 0.30초(T) 경과 → Locomotion. Dash 입력은 공격 중 즉시 취소 가능(회복 구간만, 0.08초 이후) |
| Parry | 지상이면 x속도 0 | 0~0.20초(T) = **성공 창**(`ParryActive`), 0.20~0.35초 = 회복(무방비) | 0.35초(T) 경과 → Locomotion. 패링 성공 시 즉시 Locomotion |
| Dash | 방향 = 바라보는 방향, `dashCooldownUntil = now+2.0`(T) | 속도 24 u/s (T) 고정, y속도 0·중력 무시(gravityScale 0). **전 구간 무적** | 0.15초(T) 경과 → Locomotion, gravityScale 복구 |
| Hurt | HP-1, 넉백 (적 반대 방향 x 5, y 4)(T), 무적 0.8초(T) 시작, 깜빡임 | 입력 무시 | 0.25초(T) 경과 → Locomotion (HP 0이면 Dead) |
| Dead | 속도 0, 입력 무시 | — | PrototypeDebug의 R로 씬 재시작 |

**점프**: 지면 판정 = 발밑 `Physics2D.OverlapBox(feet, (0.8, 0.1), 0, groundMask)` 매 Update. 점프 속도 y=14 (T) (gravityScale 4 → 높이 약 2.5u=40px, 체공 0.36초). **코요테 0.10초(T)**, **점프 버퍼 0.10초(T)** (Update에서 눌림 시각 저장, 조건 만족 시 소비). **가변 점프**: 상승 중 JumpHeld가 풀리면 y속도를 절반으로.
**공격 버퍼 0.15초(T)**: Attack/Parry/Dash 회복 중 누른 공격은 Locomotion 복귀 직후 실행. 대시·패링은 버퍼 없음.
**무적 판정 우선순위** (`ReceiveEnemyHit`): ① Dash 중 → Dodged ② 피격 후 무적 중 → Dodged ③ Parry 성공 창 **이고 공격이 앞쪽에서 옴** → Parried ④ 그 외 → Hurt(Hit).
**패링 방향 조건 (Q-I)**: 바라보는 방향 기준 앞쪽 반원 180°(위 90°·아래 90°)에서 온 공격만 패링된다. 판정: `Mathf.Sign(info.SourcePosition.x - transform.position.x) == facing` (같은 x면 성공 처리). 등 뒤 공격은 패링 창이어도 Hit.
**공중 제한 없음**: 공중 공격·패링·대시 모두 허용, 대시는 쿨다운 2.0초(T)만 (Q-H). 쿨다운은 반드시 "적이 도는 시간 0.35초 + Windup 0.5초"보다 길어야 한다 — 그래야 대시로 등 뒤로 간 뒤 이어지는 공격을 패링으로만 막을 수 있다 (Q-B). 2.0초면 대시 뒤 이어지는 약 2번의 공격을 패링으로 버텨야 한다 (한 번 공격 주기 ≈ 0.85초). 쉬우면 늘리고 답답하면 줄인다.
**Gizmo**: `OnDrawGizmosSelected`로 공격 히트박스·발밑 박스 그리기 (튜닝용).

## 3. 전투 규칙

**히트박스 방식 — 트리거 콜라이더 없음.** 공격자가 "타격 순간"에 한 번 `Physics2D.OverlapBox(center, size, 0, mask)`로 상대 레이어의 콜라이더를 찾아 직접 메서드를 부른다. 허트박스 = 각자의 본체 `BoxCollider2D`. 다중 타격·OnTrigger 순서 문제가 없고 1적 프로토타입엔 충분. (나중에 떼가 나오면 `OverlapBox(…, ContactFilter2D, List<Collider2D>)`로 바꾼다.)
- Player ↔ Enemy 레이어 **물리 충돌 끔** (충돌 매트릭스). 대시 통과가 공짜로 되고, 적에 밀리는 일이 없다. 적은 공격 사거리에서 멈추므로 겹침은 드물다.
- 플레이어 공격: center = pos + facing×0.9, size 1.6×1.4 (T), Damage 1.
- 적 공격: center = pos + facing×1.0, size 1.8×1.4 (T), Damage 1.

**피해**: 플레이어 HP 5 (GDD 5장 `[확정]`, 얼굴 단계는 11/09 이후 HUD로. 프로토타입은 텍스트). 적 HP 3 (T, Q-D) → 0이면 기절(Stunned).

**패링 판정** (적의 타격 순간에 결정):
- 성공 = 적 타격 순간에 플레이어가 `ParryActive`(패링 0~0.20초)이고 앞쪽 180°에서 온 공격. → 플레이어 피해 없음, 즉시 Locomotion. **적에게 경직 등 추가 효과 없음**(평소 Recover 그대로), **숫자 1 감소** (Q-E).
- 실패 = 패링 회복 구간(0.20~0.35초)·등 뒤 공격·패링 안 함 → Hurt. 적 숫자 1 감소.
- 대시 회피 = Dodged. 적 숫자 1 감소 (헛스윙도 공격 1회). **마지막 일격(특수 공격, `IsSpecial`)을 대시로 피하면 적 1초 경직** (Q-E).

**숫자>0일 때 적을 때리면 (Q-B)**: 적을 정면에서 때린 일반 공격은 **튕김**(피해 0, 적 흰색 플래시, `ReceivePlayerHit` false). 예외는 **적이 도는 중(Turn 상태, 0.35초)** 에 등 뒤를 때린 공격 — 피해 들어감.
**대시로 등 뒤로 돌아가기 (Q-B)**: 적 공격을 Shift 대시로 피해 적 뒤로 넘어가면, 적은 Recover 대신 **Turn 0.35초(T)** 동안 돌아선 뒤 체인을 이어간다(다음 Windup). 이때 플레이어 대시는 아직 쿨다운이라 **다음 공격은 패링으로만 막을 수 있다**.

**숫자 → 타격 흐름 (한 번의 공격)**
```
숫자 N 표시(항상 보임)
→ [사거리 진입] Windup 0.5초(T): 숫자 펄스(스케일 1→1.3), 적 색 밝아짐   ← 플레이어가 반응할 시간
→ Strike 순간(1프레임): OverlapBox → ReceiveEnemyHit → 결과(Hit/Parried/Dodged). 숫자 N-1 표시
→ 플레이어가 등 뒤에 있으면 Turn 0.35초(T, 등 뒤 공격에 피해 들어감) / 아니면 Recover 0.35초(T)
→ N-1 > 0 이면 다음 Windup (재접근 없음)
→ 0 이면 Open 1초(T): 숫자 "0" 흰색, 적 정지, 일반 공격이 들어감
   (마지막 일격을 대시로 피했으면 Open 대신 Staggered 1초 — 경직, 공격이 들어감)
→ 숫자 재추첨(가중치, 4절) → Approach
```
숫자 1 → 0 이 되는 마지막 일격이 **특수 공격**이다: Windup 동안 숫자 라벨을 더 크게 펄스(1→1.6)하고 노란색으로 바꿔 구분한다(임시 이펙트, 아트 단계에서 교체).

## 4. 테스트 적 행동 루프 (TestEnemy + EnemyNumber)

세뇌된 시민 자리(근접, 숫자 2~5 빨강 `[확정]`). 특수 적의 "0.5초 늦은 마지막 일격·불꽃"과 7번째 패링 불가는 **이번 범위 아님** (HitInfo에 플래그를 미리 넣지 않는다).

`enum State { Approach, Windup, Strike, Recover, Turn, Staggered, Open, Stunned }` + `stateEnteredAt`. `Rigidbody2D` Dynamic gravityScale 4, Freeze Rotation. 이동은 Approach에서만 `linearVelocity.x` 설정.

| 상태 | 행동 | 전이 |
|---|---|---|
| Approach | 플레이어 쪽으로 3 u/s (T). facing 갱신 | 수평 거리 ≤ 1.8 (T) 이고 \|dy\| ≤ 1.5 → Windup. (숫자가 0이면 들어올 수 없음: Open에서 재추첨 후 옴) |
| Windup | x속도 0. `EnemyNumber.SetPulse(true)`, 스프라이트 색 밝게 | 0.5초(T) → Strike |
| Strike | 1프레임: OverlapBox(playerMask) → 맞으면 `ReceiveEnemyHit(IsSpecial = 숫자가 1)`. `EnemyNumber.Decrement()` | 특수 공격이 Dodged → Staggered. 아니면 플레이어가 등 뒤 → Turn, 앞 → Recover |
| Recover | 정지. 펄스 끔 | 0.35초(T) → 숫자>0 ? Windup : Open |
| Turn | 정지, 0.35초(T) 끝에 facing을 플레이어 쪽으로 | → 숫자>0 ? Windup : Open |
| Staggered | 정지, 회색빛 깜빡임, 숫자 0 (공격이 들어감) | 1초(T) → `EnemyNumber.Reroll()` → Approach (Open을 거치지 않음) |
| Open | 정지, 숫자 0 표시(흰색) | 1초(T) → `EnemyNumber.Reroll()` → Approach |
| Stunned | `EnemyNumber.Hide()`, 스프라이트 회색·90° 눕힘, 콜라이더 유지, 영구 (Q-D) | — |

`ReceivePlayerHit(HitInfo)`: Stunned → false. `number > 0 && state != Turn` → 흰 플래시 0.1초, false (튕김). 그 외 → hp -= Damage, 빨강 플래시, hp ≤ 0 → Stunned, true. **피격이 현재 상태·타이머를 바꾸지 않는다** (체인 계속, Open 창 연장 없음).

`EnemyNumber`: `[SerializeField] TMP_Text label; [SerializeField, Range(0,1)] float progress;` `int Value`, `Reroll()`, `Decrement()`(0 밑으로 안 내려감), `Hide()`, `SetPulse(bool, bool special)`. 색: Value>0 빨강 `#FF2A2A`(ART-06 근접=빨강), 특수 공격 Windup 노랑(임시), 0 → 흰색.
**재추첨 가중치 (Q-C)**: 2~5 중 가중 무작위. `progress`(0=초반, 1=후반)로 두 표를 선형 보간한다 — 초반 {2:40, 3:40, 4:15, 5:5}(T), 후반 {2:5, 3:15, 4:40, 5:40}(T). 프로토타입은 인스펙터에서 `progress`를 직접 바꿔 시험하고, 나중에 스테이지 진행도가 이 값을 넣는다. 처음 등장할 때도 `Reroll()`. 펄스는 `Update`에서 `Mathf.PingPong`으로 라벨 스케일 1→1.3. 라벨 위치: 머리 위 (0, 1.6) — 최종은 얼굴 앞 홀로그램(ART-06), 아트 들어올 때 옮긴다.

**멈춤 스프라이트 플래시**: `SpriteRenderer.color`를 바꾸고 `flashUntil` 타이머로 복구. 코루틴 없음.

## 5. 테스트 씬 구성 (Assets/Editor/PrototypeSceneBuilder.cs, 메뉴 `NUMBER/Build Prototype Scene`)

에디터가 열려 있으면 메뉴 실행, 아니면 Unity MCP `execute_menu_item`. 모두 멱등(다시 실행하면 덮어씀).

1. **레이어** (`TagManager.asset`을 `SerializedObject`로): `Ground`=6, `Player`=7, `Enemy`=8. 태그는 필요 없음. 소팅 레이어 Default만 사용(order로 구분).
2. **충돌 매트릭스**: `Physics2D.IgnoreLayerCollision(7, 8, true)` (Player×Enemy 끔). 저장이 안 되면 Project Settings > Physics 2D에서 수동 — 설계 문서에 남긴다.
3. **TMP**: `Assets/TextMesh Pro/Resources/TMP Settings.asset` 없으면 `TMP_PackageResourceImporter.ImportResources(true, false, false)`. (Assets/TextMesh Pro 폴더가 생김 — 커밋 대상, LFS 아님.)
4. **씬** `Assets/Scenes/Prototype.unity` 새로 생성:
   - Main Camera: Orthographic, size **8.4375** (=270/16/2, 480×270 기준 뷰), 배경 `#0B0F1E`, 위치 (0,0,-10). Pixel Perfect Camera는 이번에 안 붙임 (아트 들어올 때).
   - Global Light 2D (Light2D, Global, intensity 1) — URP 2D 템플릿과 동일.
   - `Ground`: Square 스프라이트 색 `#3A3F4B`, scale (60, 2), 위치 (0, -7), BoxCollider2D, 레이어 Ground.
   - `Player` (-5, -5): 루트 = Rigidbody2D + BoxCollider2D(0.9×2) + PlayerInputReader(asset 연결) + PlayerHealth + PlayerController(groundMask=Ground, enemyMask=Enemy), 레이어 Player. 자식 `Visual` = Square 시안 `#4CE0FF`, scale (1, 2), order 10. → 프리팹 저장.
   - `TestEnemy` (6, -5): 루트 = Rigidbody2D + BoxCollider2D(0.9×2) + EnemyNumber + TestEnemy(playerMask=Player), 레이어 Enemy. 자식 `Visual` = Square 주황 `#FF8C42`, scale (1,2), order 10. 자식 `Number` (0, 1.6): `TextMeshPro`(월드) 텍스트 "3", fontSize 6, 가운데 정렬, order 20, EnemyNumber.label에 연결. → 프리팹 저장.
   - `Debug`: PrototypeDebug.
5. 씬 저장 후 `EditorBuildSettings.scenes` 맨 앞에 추가 (R 재시작용 `SceneManager.LoadScene(buildIndex)`).

Play 모드 확인용 색 규칙: 플레이어 시안 / 대시 중 흰색 / 피격 깜빡임, 적 주황 / Windup 밝은 주황 / 튕김 흰 플래시 / 피해 빨강 플래시 / 기절 회색.

## 6. 구현 순서 (Sonnet 작업 3개)

파일 소유권을 나눠 **Task A와 B는 병렬 가능**, Task C는 둘 다 끝난 뒤. 같은 파일을 두 작업이 만지지 않는다.

**Task A — 플레이어** (파일: `HitInfo.cs`, `PlayerInputReader.cs`, `PlayerHealth.cs`, `PlayerController.cs`, `PrototypeDebug.cs`, `PrototypeSceneBuilder.cs`(적 제외), `InputSystem_Actions.inputactions`)
1. 입력 에셋 수정(1.1절) + `HitInfo.cs` + `PlayerInputReader` → 확인: 컴파일 OK, 인스펙터에서 Parry/Dash 액션 보임.
2. 씬 빌더(레이어·TMP·카메라·조명·Ground·Player·Debug, 적 부분은 비워 둠) → 확인: 메뉴 실행 후 Prototype.unity 열리고 Play에서 파란 사각형이 바닥에 서 있음.
3. Locomotion + 점프(코요테·버퍼·가변 점프) → 확인: A/D 이동, W 점프 높이 약 2.5칸, 가장자리에서 떨어진 직후 0.1초 안에 점프됨, 짧게 누르면 낮게 뜀.
4. Dash → 확인: Shift에 바라보는 쪽으로 짧게 돌진, 공중에서 y가 안 떨어짐, 0.35초 안에 연타해도 한 번만.
5. Attack + Parry 상태 (히트 판정은 enemyMask에 아무것도 없어도 OK) → 확인: Gizmo로 히트박스 보임, 공격 중 이동 안 됨, 회복 중 누른 공격이 이어서 나감, 패링 0.35초 후 움직임.
6. `ReceiveEnemyHit` + Hurt/Dead + PlayerHealth + PrototypeDebug(HP·상태 표시, R 재시작) → 확인: Debug에 "HP 5/5 / State Locomotion" 표시, R로 씬 재시작. (피격 자체는 Task C에서.)

**Task B — 적** (파일: `EnemyNumber.cs`, `TestEnemy.cs`) — `HitInfo.cs`와 `PlayerController.ReceiveEnemyHit` 시그니처(1절)가 커밋된 뒤 시작. 씬 빌더·PlayerController는 건드리지 않는다.
1. `EnemyNumber` (값·감소·재추첨·색·펄스·숨김) → 확인: 임시 GameObject에 붙여 인스펙터에서 Value 바꾸면 TMP 라벨이 따라감.
2. `TestEnemy` 상태 루프(Approach→Windup→Strike→Recover→Open→재추첨) + `ReceivePlayerHit` + Gizmo → 확인: 컴파일 OK. (씬 연결은 Task C.)

**Task C — 통합·튜닝** (파일: `PrototypeSceneBuilder.cs` 적 섹션, 필요 시 `TestEnemy.cs`/`PlayerController.cs` 수치)
1. 빌더에 TestEnemy 프리팹·배치 추가, 재실행 → 확인: 적이 플레이어에게 걸어와 사거리에서 멈추고 숫자가 펄스함.
2. 피격 → 확인: 가만히 있으면 HP가 숫자만큼 깎이고(3이면 3번), 숫자가 3→2→1→0으로 줄고, 마지막 일격 Windup은 노란색 큰 펄스, 0에서 1초 멈췄다가 새 숫자로 다시 옴. `progress`를 0과 1로 바꿔 가며 20번쯤 재추첨하면 각각 2~3 / 4~5가 많이 나옴.
3. 패링 → 확인: 적을 바라보고 Windup 끝 직전 우클릭하면 HP 안 깎이고 숫자만 줄어듦(적 경직 없음). 등을 보인 채 패링하면 맞음. 너무 일찍 누르면(0.35초 전) 맞음.
4. 대시 회피·등 뒤 → 확인: Strike 순간 대시로 적을 통과하면 HP 그대로, 적이 0.35초 돌아서는 동안 등을 때리면 빨강 플래시(피해). 돌아선 뒤 다음 공격 때 Shift가 쿨다운이라 패링만 됨. 숫자>0인 적을 정면에서 때리면 흰 플래시(피해 0).
5. 특수 공격 회피 → 확인: 마지막 일격(노란색)을 대시로 피하면 적이 1초 굳고, 바로 새 숫자로 다시 옴.
6. 반격 → 확인: 숫자 0에서 3번 때리면 적이 회색으로 눕고 숫자가 꺼짐(영구).
7. 손맛 튜닝: `(T)` 값만 조정. 느낌 기준: Windup 0.5초가 "읽고 반응"하기 넉넉한지 — 넉넉하면 0.35초로 줄여 본다.

Task C 뒤 Opus 검수 → `[code]` 커밋. 커밋에 `Assets/TextMesh Pro/` 포함.

## 7. 확정된 결정 (2026-11-08 사용자 답변)

| ID | 질문 | 결정 |
|---|---|---|
| Q-A | 공격·패링 키 | **확정**: 좌클릭 공격 / 우클릭 패링 |
| Q-B | 숫자>0일 때 공격·대시 | 정면 일반 공격은 피해 없이 튕김. 적 공격을 Shift로 피해 등 뒤로 가면 적이 0.35초 돌아선 뒤 체인을 이어감. 그때는 대시 쿨다운 중이라 패링만 가능 |
| Q-C | 0 이후 반격 창·다음 숫자 | 1초 → 2~5 재추첨. 초반은 2~3, 후반으로 갈수록 4~5에 가중치 |
| Q-D | 시민 적 체력 | 3타, 영구 기절 |
| Q-E | 패링 성공 효과 | 효과 없음, 숫자만 1 감소. 마지막 일격(특수 공격)을 피하면 적 1초 경직 |
| Q-F | 대시가 적에게 피해? | 통과만 |
| Q-G | Space 점프 | 뺀다 (나중 스킬·기믹용) |
| Q-H | 공중 공격·패링·대시 | 모두 가능 |
| Q-I | 패링 방향 조건 | 있음: 바라보는 방향 180°(위 90°·아래 90°) |
| Q-J | HUD 체력 | 일단 텍스트, UI/UX는 나중에 바뀔 수 있음 |

**추가 확정 2 (같은 날, 통합 후)**:
- **돌격기 (Charge)**: 적이 공격할 때 플레이어가 반대쪽(적에게서 멀어지는 방향)으로 대시해 공격 범위를 완전히 벗어나면(Strike 순간 플레이어가 적 앞쪽 & 수평 거리 > 공격 사거리), 숫자 > 0이면 적은 Recover 대신 **Charge**: 플레이어 쪽으로 빠르게 돌진(14 u/s(T), 최대 0.6초(T))해 사거리에 들어오는 즉시 Strike. 돌진 중 숫자 펄스로 예고. 돌격도 공격 1회(숫자 1 감소). 플레이어 대시는 쿨다운(2초) 중이라 패링으로만 막을 수 있다. 숫자가 0이 된 뒤에는 돌격하지 않는다(Open). 등 뒤로 넘어가는 경우는 기존대로 Turn 0.35초. **대시뿐 아니라 걸어서·넉백으로 사거리를 벗어나도 돌격한다** (사용자 확정).
- **패링 성공 시 둘 다 밀리지 않는다** (넉백 없음). 임시 이펙트: 히트스톱 약 0.06초(T, `Time.timeScale` 0 → 복구, 실시간 타이머) + 두 캐릭터 흰색 번쩍임 + 접점에 픽셀 사각형 불꽃 몇 개(코드로 생성, 0.2초). 최종 이펙트 그림은 아트 단계에서 시안 확인 후 교체. (구현: `ParryFx.cs`, PlayerController 히트스톱. 돌격은 접촉 없이 빗나간 공격 + 사거리 밖일 때만 발동, 돌격 중 등 뒤로 넘어가면 Turn.)

**추가 확정 (같은 날)**: 연속 공격 도중 등 뒤로 넘어가면 돌아서는 0.35초가 체인에 추가되고, 그동안 등 뒤 공격은 피해가 들어간다 / 대시 쿨다운 2.0초(T, 1.0초는 너무 쉬움) / 특수 공격 회피 시 1초 경직만 (뒤에 0 상태가 따로 붙지 않음).

## 8. 위험

- **TMP 임포트**가 에디터 상호작용을 요구하면(대화창) 사용자에게 Window > TextMeshPro > Import TMP Essential Resources 한 번 요청. 그동안 라벨은 비워 두고 Debug 텍스트로 숫자 확인.
- `Physics2D.IgnoreLayerCollision`이 에디터 스크립트에서 ProjectSettings에 저장되지 않을 수 있음 → 수동 설정 1회 (5절 2).
- 입력 JSON을 손으로 고치다 깨지면 임포트 실패 → 에디터에서 에셋 열어 Parry 액션 추가로 대체 가능.
- 대시 중 gravityScale 0 ↔ 4 전환을 Hurt로 끊기면 0이 남을 수 있음 → 모든 상태 진입에서 gravityScale을 4로 되돌리는 한 줄을 `EnterState`에 둔다.
- WebGL: OnGUI·Input System·OverlapBox 모두 문제 없음. 11/09에 UI를 uGUI/TMP로 옮길 때 PrototypeDebug만 교체.
