# AI 벌레 떼 비주얼 (BugSwarmVisual)

GDD ART-09 `[확정]`: "픽셀 1개 크기의 벌레 떼 → 모여서 큰 공격형 벌레 / 원거리 벌레". **외형만** 바꾼다. 새 적 종류·게임플레이 변경 없음.

- 코드: `Assets/Scripts/Enemy/BugSwarmVisual.cs` 한 파일 (컴포넌트 `BugSwarmVisual` + 부트스트랩 `BugSwarmBootstrap`).
- 대상: `TestEnemy.kind == Kind.Bug`인 적 전부. `ranged`면 침(스팅어) 형태 + 황금 포인트, 아니면 공격형 딱정벌레 형태 + 빨강 포인트.
- 프리팹·씬·빌더·`TestEnemy.cs`는 건드리지 않는다. 런타임에 자동으로 붙는다.
- 참고 시안: `docs/design/concept/11_character_lineup.jpg`, `15_idle_grid.jpg`.

## 동작 방식

1. **부착**: `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` + `SceneManager.sceneLoaded`에서 씬의 모든 `TestEnemy`(비활성 포함)를 찾아 Bug면 컴포넌트를 붙인다. 보스 씬의 `Template-Bug`는 비활성 템플릿이라 여기서 붙고, `BossFight`가 `Instantiate`한 복제본은 컴포넌트를 그대로 물려받는다. 다른 경로로 런타임에 적을 만들면 `BugSwarmBootstrap.TryAttach(enemy)` 한 줄이면 된다.
2. **상태 읽기**: `TestEnemy.Current`(public)로 상태를, 숨긴 placeholder `SpriteRenderer`의 `flipX`로 방향을, `color`로 플래시(흰색 = 패링, 빨강 = 피격)를 읽는다. `kind`·`ranged`는 private 직렬화 필드라 **리플렉션으로 한 번만** 읽고 캐시한다 (`FieldInfo` 2개). 필드가 없으면 경고 한 번 찍고 아무것도 하지 않는다.
3. **그리기**: 적마다 96×96 `Texture2D`(RGBA32, Point, Clamp) 하나를 만들고, 자식 `Swarm` 오브젝트의 `SpriteRenderer`(원래 Visual과 같은 sortingLayer/Order)에 PPU 32로 붙인다. 텍스처 메모리를 `GetRawTextureData<Color32>()`로 직접 써서, **지난번에 찍은 점만 지우고 새 점을 찍은 뒤** `Apply(false, false)`. 매 프레임이 아니라 **`RedrawHz`(15Hz)마다만** 시뮬레이션·업로드한다. 상태가 바뀌거나(타격 터짐·사망) 플래시(흰색·빨강)가 켜지고 꺼지는 프레임은 바로 다시 그린다. 화면 밖(`isVisible == false`)이면 건너뛰고, 히트스톱·일시정지(`deltaTime 0`)엔 그릴 것이 없어 업로드도 없다. 비활성(사라진 적)은 `LateUpdate` 자체가 돌지 않는다. **프레임당 할당 없음**.
4. **점(Dot)**: 각 점은 실루엣 위 고정 자리 `home`, 현재 위치 `pos`, 스프링으로 `home`을 따라감 + 짧은 주기의 1px 트윗치(날개는 세로 2px, 빠르게; 다리는 느리게; 눈·침 끝은 고정). 스폰 시 반지름 30px 안에 흩어져 있다가 모여든다(= "모여서 벌레가 된다").

| 상태 (`TestEnemy.State`) | 표현 |
|---|---|
| Approach / Recover | 기본 실루엣 + 1px 호버 bob(다리 제외) + 트윗치 |
| Windup | 0.3초에 걸쳐 플레이어 쪽으로 x 1.5배 늘어나고 6px 앞으로. 트윗치는 줄어 긴장. `Charge` 파트(가슴 3점 / 침 끝 2점)가 포인트 색으로 켜짐 |
| Strike | 근접: 앞쪽으로 최대 14px 터짐 → 0.1초 안에 복귀. 원거리: 뒤로 반동. ParryFx의 Visual 넛지 위치도 그대로 따라감 |
| Charge | 늘어남 1.8배 + 9px 앞 |
| Turn | 트윗치 2배 (방향 재정렬 중) |
| Staggered / Open | 4px 처짐, 트윗치 1.8배, 눈·침이 8Hz로 깜빡임 (때릴 수 있는 창) |
| Stunned (사망) | 점들이 무작위 방향(위쪽 치우침)으로 날아가며 중력으로 떨어지고, 0.5초(`Juice.KillVanishDelay`와 같은 값) 동안 점 개수가 줄어 사라짐. 0.1초 빨강 플래시 |
| 패링당함 | Visual 색이 흰색인 동안 모든 점 흰색 |

Frozen·Special·Overload는 Citizen/Android 전용이라 Bug에는 오지 않는다 (기본 실루엣으로 그림).

### 실루엣 (오른쪽 보기 기준, 루트 중심 px, y 위)
- **공격형(melee)**: 배 타원(-9,-9) 20점, 가슴(2,-7) 12점, 머리(10,-6) 8점, 빨간 눈 2 + 송곳니 1, 더듬이 6, 다리 3쌍 9점(-21까지), 날개 타원(-4,-1) 10점, Charge 3점. 약 71점, 약 38×24px, 콜라이더 아래쪽 절반에 웅크림.
- **원거리(ranged)**: 수평 침 몸통(-14..11, y=6) 14+8+8점, 황금 침 끝 3점 + Charge 2점(12, 16), 꼬리 지느러미 6, 뒤로 젖힌 날개 10, 접은 다리 4. 약 55점, 약 36×13px, 루트보다 6px 위에 떠서 총구 위치(+0.8, +0.3u)와 맞춤.

### 팔레트 (변종당 5색)
Dark `#2E2638`, Mid `#4A3F5C`, Light `#8C7FA6` (어두운 회보라 3단계) + 포인트: 근접 빨강 `#FF2A2A`, 원거리 황금 `#FFC93C`(= 숫자 색). 플래시용 흰색. 반투명 없음(알파 0/255만). 소멸은 알파 페이드 대신 점 개수 감소.

## 튜너블 (T) — `BugSwarmVisual.cs` 상단 const
| 이름 | 기본 | 뜻 |
|---|---|---|
| `Size` | 96 | 캔버스 px (한 변). 커지면 업로드 비용 ↑ |
| `MaxDots` | 80 | 점 최대 개수 (배열 크기) |
| `GatherRadius` | 30 | 스폰 시 흩어진 반경 px |
| `Follow` | 18 | 점이 제자리로 모이는 속도 1/s |
| `TwitchMin/Max` | 0.05 / 0.14 | 몸통 점 트윗치 간격 s |
| `WingTwitchMul` / `LegTwitchMul` | 0.35 / 2.5 | 날개(빠름)·다리(느림) 간격 배수 |
| `WindupRamp` | 0.3 | 윈드업 늘어남이 최대가 되는 시간 s |
| `WindupStretch` / `WindupLead` | 0.5 / 6 | 윈드업 x 늘어남 비율 / 앞 이동 px |
| `ChargeStretch` / `ChargeLead` | 0.8 / 9 | 돌진 중 값 |
| `StrikeBurst` / `StrikeDecay` | 14 / 10 | 타격 시 튀는 px / 복귀 속도 1/s |
| `Sag` | 4 | Staggered/Open 처짐 px |
| `DeathFade` | 0.5 | 소멸 시간 s (`Juice.KillVanishDelay`와 맞출 것) |
| `DeathSpeed` / `DeathGravity` | 40 / 90 | 소멸 점 속도 px/s / 중력 px/s² |
| `RedrawHz` | 15 | 시뮬레이션·업로드 빈도 /s. 올리면 부드럽고 비쌈, 12까지 내려도 됨 |
| `MaxStep` | 0.25 | 한 번에 진행하는 최대 시간 s (화면 밖에 오래 있다 돌아올 때) |

실루엣 좌표는 `BuildMelee` / `BuildRanged`, 색은 `Dark/Mid/Light/Red/Gold` static에서 바꾼다.

## WebGL / 성능
- **비용 (2026-11-09 개선, Play 측정 전 추정):** 화면 안 벌레 1마리당
  - 이전: 매 프레임(60Hz) 36KB 버퍼 지우기 + `SetPixels32` 36KB 복사 + GPU 업로드 36KB → 초당 약 6.5MB 메모리 이동 + 업로드 60회.
  - 지금: 15Hz마다 점 ≤80개 시뮬레이션 + 픽셀 쓰기 ≤160개(지우기+찍기) + 업로드 36KB 1회 → 초당 업로드 15회(약 0.54MB), 관리 메모리 복사 0. 나머지 45프레임은 상태·색 비교 몇 줄뿐.
  - 화면에 10마리면 업로드가 초당 600회 → 150회(약 1/4), CPU 쪽 복사는 사라짐. 화면 밖·히트스톱·일시정지는 0회.
- 확인 방법: 에디터/개발 빌드의 F3 FrameStats로 벌레 많은 구간(STG-02 SEG-B, STG-04, 보스 2웨이브)의 프레임 시간 최대값을 이전 커밋과 비교.
- 겉모습: 점이 제자리로 모이는 힘을 프레임 수와 무관한 지수식(`1 - exp(-Follow·dt)`)으로 바꿔 15Hz에서도 60Hz와 같은 속도로 모인다. 움직임이 초당 15번 끊어 보이는 것은 픽셀 아트 애니메이션(보통 8~12fps)과 같은 수준. 날개 떨림(0.02~0.05초 주기)은 15Hz에서 일부 단계를 건너뛰지만 여전히 무작위로 떨린다.
- 할당: `Start`에서 텍스처·점 배열·인덱스 배열 한 번. 이후 프레임 할당 없음(`GetRawTextureData`는 복사 없는 뷰).
- 리플렉션은 `kind`·`ranged` 필드를 **클래스당 한 번** 찾고(`FieldInfo` 캐시), 적마다 `GetValue` 2회(부착 시). 직렬화 필드라 IL2CPP 스트리핑 대상이 아니다.

## 끄기 / 제거
- 완전 제거: `Assets/Scripts/Enemy/BugSwarmVisual.cs`(+ .meta)와 이 문서를 지운다. 다른 파일은 참조하지 않으므로 그걸로 끝. placeholder 사각형이 다시 보인다.
- 임시로 끄기: `BugSwarmBootstrap.Init`의 `[RuntimeInitializeOnLoadMethod]` 줄을 주석 처리.

## TestEnemy에 바라는 것 (선택)
지금은 리플렉션으로 충분하다. `kind`·`ranged` 이름이 바뀌면 경고가 뜨고 벌레는 사각형으로 돌아간다. 그때는 `TestEnemy`에 한 줄씩 추가하고 `BugSwarmBootstrap.IsBug/IsRanged`를 그걸로 바꾸면 된다:
```csharp
public Kind EnemyKind => kind;
public bool Ranged => ranged;
```

## Play 모드 체크리스트
현재 빌더가 만드는 Bug는 전부 **원거리(ranged) 황금 1**이다 (DECISIONS 14: STG02 E-A9·E-B9, STG04 E-A9·E-B9, 보스 1·2웨이브, `Test_Enemies`의 "Ranged"). STG01과 나머지 원거리는 **원거리 시민**(Kind Citizen, 초안 그림)이라 스웜이 붙지 않는다. 원거리 벌레는 2px 느린 호버로 떠 있는 사수처럼 보이게 했다(`RangedHover`). 근접형 실루엣은 Bug + `ranged=false`인 적이 있어야 보인다 (`Test_Enemies`에서 "Ranged"의 `ranged`를 꺼서 확인).

1. STG01 시작 → E-A1(LEDGE-A1 위 황금 1)이 **사각형 대신 점 떼**로 보인다. 처음 0.3초 흩어진 점이 모여 침 모양이 된다. 날개 점이 떨린다.
2. 4배 확대(에디터 Scene 뷰 줌)로 본다: 점이 전부 1px 격자에 맞고, 반투명·번짐이 없다. 황금 침 끝 3점이 가장 밝다.
3. 플레이어가 다가가면(Windup) 침이 플레이어 쪽으로 길어지고 끝의 황금이 2점 더 켜진다. 발사(Strike) 순간 몸통이 뒤로 튕겼다가 돌아온다.
4. 발사체를 패링하면(좌클릭, 탄이 가까울 때) 적 점 전부가 흰색으로 번쩍인다.
5. 벌레를 때리면 0.1초 빨강 → 점들이 사방으로 흩어져 떨어지며 0.5초 안에 사라지고, 기존 소멸 스파크가 그대로 나온다. 숫자 라벨은 기존대로 숨는다.
6. 플레이어가 왼쪽에 있으면 침 끝이 왼쪽을 향한다 (flipX 반영).
7. 보스 씬(STG05): 웨이브에서 나온 Bug 복제본도 점 떼로 보인다 (템플릿에 붙은 컴포넌트를 상속).
8. 시민·안드로이드는 **변화 없음** (스프라이트/사각형 그대로).
9. 체크포인트 사망 → 씬 리로드 후에도 Bug가 점 떼로 보인다 (`sceneLoaded` 재부착).
10. Console에 `BugSwarm:` 경고가 없다.
11. (근접형 확인) `Test_Enemies` "Ranged"의 `ranged` 체크를 끄고 Play: 넓은 딱정벌레 실루엣 + 빨간 눈 2점, Windup 때 가슴 3점이 빨갛게 켜지고 Strike 때 앞으로 터진다.
