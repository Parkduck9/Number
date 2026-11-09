# LIGHTING_DESIGN — 네온 조명·낮밤 시스템 설계

> 작성 2026-11-08 (Claude Fable, 설계만). 구현: Sonnet → 검수: Opus. 코드·씬 변경은 아직 없음.
> 사용자 요청: "어두운 장면을 주로 네온 간판이 비춘다. 반사광(스필)은 네온 주변에 몰려 있다. 낮에는 네온이 전부 꺼진다."
> 근거: GDD 8장(STG-00 아침, 나머지 밤·실내), 11장 ART-01·06·10·11, ANTI_SLOP.md, 컨셉 13번. 수치 뒤 `(T)` = 시작값, 인스펙터에서 튜닝.
> 상태 표시는 GDD 규칙: `[확정]` 근거가 있는 것 / `[제안]` 이 문서가 새로 정하는 것(사용자 확인 전 구현 금지인 항목은 따로 표시).

---

## 0. 프로젝트 현재 상태 (파일로 확인, 2026-11-08)

| 항목 | 값 | 설계에 미치는 영향 |
|---|---|---|
| URP | 17.3.0, `Assets/Settings/UniversalRP.asset` (품질 6단계 전부 같은 에셋, WebGL 기본 품질 = High) | 품질 단계별 분기 없음 |
| 2D Renderer | `Assets/Settings/Renderer2D.asset`: HDR Emulation Scale **1(끔)**, Light RT Scale **0.5**, 최대 Light RT 16, 블렌드 스타일 4개(Multiply / Additive / …with Mask), **PostProcessData 연결됨**, 기본 스프라이트 머티리얼 = **Lit** | 2D 포스트프로세싱 가능. 조명 텍스처는 480×270로 계산(싸고 부드럽게 뭉개짐 — 픽셀 아트에 OK). 모든 일반 스프라이트는 Global Light 없으면 검게 나옴 |
| 색 공간 | **Linear** (`m_ActiveColorSpace: 1`) | 블룸 임계값은 리니어 값. 아래 색상 hex는 전부 인스펙터(sRGB) 기준 |
| 포스트 프로세싱 | `DefaultVolumeProfile.asset`에 Bloom(intensity 0, threshold 0.9) 등 전부 "꺼진 값"으로 들어 있음. 씬에 Volume 없음. 프로토타입 카메라에 `UniversalAdditionalCameraData` 없음(= renderPostProcessing 꺼짐) | 씬 Volume + 카메라 `renderPostProcessing = true` 추가 필요 |
| 카메라 | 카메라 카드가 `CameraSetup.Configure()`(Pixel Perfect 960×540, PPU 32, **Upscale Render Texture**, Stretch Fill)와 `CameraFollow2D`, `ParallaxLayer` 추가 중 (디스크에 있음, 미커밋) | 960×540으로 그린 뒤 ×2 포인트 확대 → 블룸도 960×540 격자에서 계산됨(아래 6절 위험 2) |
| WebGL | 그래픽 API 자동(= WebGL2), 스레드 끔, 1920×1080, `BuildWebGL.cs` 있음 | WebGL2 전제. 컴퓨트·스레드·파일 IO 없음 |
| 기존 코드 | `EnemyNumber`(TMP 라벨, 빨강 `#FF2A2A`), `BroadcastBoard`(전광판 TMP 라벨), `GameFlow.InputLocked`, `DialoguePlayer` | TMP 셰이더는 **Unlit** → 숫자는 조명 영향 없이 항상 원색. 전광판 화면은 네온 간판 1개로 취급(4절) |
| 소팅 레이어 | Default만 | 캐릭터만 더 밝게 비추려면 소팅 레이어 분리 필요(1.2절) |

---

## 1. 밤 룩 (Night)

### 1.1 원칙 — "밝기 서열"
화면에서 밝은 순서가 항상 다음을 지켜야 한다(ART-06·ANTI_SLOP 3절):

```
적 숫자 홀로그램(1.0, Unlit, 블룸 큼) > 네온 간판(0.95, Unlit, 블룸 작음) > 네온 근처의 조명된 바닥·캐릭터(≤0.8) > 그 밖의 배경(≈0.4~0.5)
```

- **HDR에 기대지 않는다.** HDR Emulation Scale은 1(끔) 유지, URP HDR 설정도 손대지 않음. 조명 결과는 항상 ≤ 1.0(알베도 그대로가 최대)이므로 "조명된 것은 절대 블룸 임계값을 못 넘고, Unlit인 숫자·네온만 넘는다"가 보장된다. WebGL2의 float RT 지원 여부와 무관하게 같은 그림이 나온다.
- 숫자가 네온보다 더 빛나는 건 **임계값 초과분 차이**로 만든다: 임계값 0.8(T, 리니어) 기준으로 숫자(1.0)는 0.2 초과, 네온(sRGB 0.95 ≈ 리니어 0.89)은 0.09 초과 → 숫자 블룸이 약 2배. 셰이더·HDR 없이 끝.

### 1.2 Global Light 2D 두 개
| 이름 | 대상 소팅 레이어 | 밤 값 (T) | 낮 값 (T) | 역할 |
|---|---|---|---|---|
| `Global Light (World)` | 전부 | 색 `#B4BCE6`(차가운 남청), 세기 **0.7** → 실효 약 (0.49, 0.52, 0.63) | 색 `#FFF4E0`, 세기 1.0 | 장면 전체 어둡게 + 남색 틴트 (ART-01) |
| `Global Light (Characters)` | **Characters**만 | 색 흰색, 세기 **+0.2** | 0 | 캐릭터 가독성. 배경은 0.5대, 캐릭터는 0.7대로 읽힘 |

- 소팅 레이어 추가 `[제안]`: `Background`(먼·중간 배경), `World`(가까운 층·발판·바닥, 기본값), `Characters`(플레이어·적·군중), `FX`(이펙트·패링 섬광). TMP 숫자는 Unlit이라 어느 레이어든 무관(order 20 유지). 레벨·카메라 카드와 이름을 맞춰야 한다 → 열린 질문 Q6.
- `Light2D.targetSortingLayers` / `AddTargetSortingLayer(int)` 가 URP 17.3에 존재함(리플렉션으로 확인). 빌더에서 `SortingLayer.layers`로 ID를 찾아 Characters 라이트에만 넣는다. 안 되면 `SerializedObject`의 `m_ApplyToSortingLayers` 배열에 ID를 직접 쓴다.

### 1.3 배경 원화 밝기 규칙 (Codex가 지켜야 할 것)
조명이 전체를 **약 0.5배** 더 어둡게 만든다. 그러므로 밤 배경 원화는 "칠흑"이 아니라 **"불 꺼진 해질녘"** 값으로 그린다: 하늘·먼 건물은 어두운 남색이어도 되지만, 중간·가까운 층의 벽·기와·바닥은 중간 어두운 값(대략 40~60% 밝기). 검증: 원화를 50% 밝기로 내려 보고 형태가 읽히면 통과. 이래야 네온 포인트 라이트가 **1.0까지 끌어올릴 여유(헤드룸)**가 생겨 "네온 주변만 밝다"가 보인다.

### 1.4 블룸 (URP Volume)
- 씬에 전역 `Volume` 1개, 프로필 `Assets/Data/Lighting/NightPostFx.asset` (빌더가 생성). 오버라이드는 **Bloom 하나만**: threshold **0.8**(T), intensity **0.6**(T), scatter **0.45**(T), High Quality Filtering 끔, Downscale Half, Max Iterations 4(T, 960×540이면 충분). 다른 효과(비네트·색보정·안티앨리어싱) 없음 — 픽셀 아트라 FXAA/SMAA는 금지.
- 낮/밤 전환은 프로필을 바꾸지 않고 **`Volume.weight`**(밤 1 → 낮 0)만 바꾼다. 프로필 자산을 런타임에 수정하지 않아도 되고 페이드도 공짜.
- 카메라: `cam.GetUniversalAdditionalCameraData().renderPostProcessing = true`, `antialiasing = None`. (Pixel Perfect 카메라 카드와 독립적으로 `LightingSetup`이 설정한다.)
- 2D Renderer의 포스트프로세싱 지원: Renderer2D.asset에 PostProcessData가 이미 연결되어 있어 셰이더가 빌드에 포함된다. 비용: 960×540에서 블룸 4단 미프 — 내장 GPU에서도 1ms 미만 예상.
- **블룸은 보조 효과**다. 네온의 주된 "후광"은 Codex가 네온 레이어에 **픽셀로(2~3단계 디더)** 그린다(ANTI_SLOP: 부드러운 그라데이션 금지). 블룸은 그 위에 얹는 얇은 광택이고 숫자를 돋보이게 하는 용도. 블룸이 WebGL에서 문제를 일으키면 끄기만 해도 그림이 성립해야 한다.

---

## 2. 네온 간판 — 발광 픽셀을 만드는 방법

### 2.1 선택: **별도 네온 스프라이트(Unlit) + 정점 색으로 세기 조절. 커스텀 셰이더 없음.**

검토한 안:
| 안 | 내용 | 판단 |
|---|---|---|
| A. 배경 레이어당 발광 마스크 텍스처 + 커스텀 Sprite-Lit 셰이더 | 마스크 픽셀은 조명 무시하고 발광 | Sprite-Lit HLSL 복제(라이트 텍스처 결합 코드 포함)가 필요하고, 레이어 크기(1620×540) 마스크를 레이어마다 하나씩 더 실어야 함(대부분 투명 픽셀인데 RGBA32 3.5MB). 잼에 과함 |
| B. 네온만 잘라낸 별도 스프라이트, `Sprite-Unlit-Default` 머티리얼 | 간판별로 작은 PNG, 조명 영향 없음, 세기는 `SpriteRenderer.color`(정점 색)로 | **채택.** 셰이더 0개, 추가 드로우 = 간판 수(화면당 수 개), 간판마다 깜빡임·라이트·켜고 끄기를 따로 제어 가능. 전광판도 같은 방식 |
| C. 글로벌 `_NeonIntensity` 셰이더 | B에 HDR 부스트(>1.0) 추가 가능 | 1.1절대로 HDR이 필요 없어서 보류. 나중에 "네온이 더 타올라야" 하면 Shader Graph(Sprite Unlit, 전역 Float 프로퍼티) 하나로 B 위에 얹을 수 있다 |

### 2.2 `NeonSign` 컴포넌트 (`Assets/Scripts/Lighting/NeonSign.cs`)
```csharp
public class NeonSign : MonoBehaviour
{
    public static readonly List<NeonSign> All = new();     // BroadcastBoard.All과 같은 방식
    [SerializeField] SpriteRenderer sprite;               // Sprite-Unlit-Default 머티리얼
    [SerializeField] Light2D light;                       // 자식 Point Light (없어도 됨)
    [SerializeField, Range(0f, 1f)] float flicker = 0f;    // 0 = 안정, 0.3 = 가끔 떨림 (T)
    [SerializeField] float lightIntensityOn = 0.4f;        // 켜졌을 때 라이트 세기 (T)
    public bool On { get; private set; } = true;           // 개별 끄기 (전광판 꺼진 상태, 고장난 간판)
    public void SetOn(bool on);                            // 개별
    public void SetIntensity(float k);                     // LightingController가 호출. 0 = 꺼짐
}
```
- `SetIntensity(k)`: `sprite.color = new Color(k, k, k, k)` (정점 색 RGB·알파 모두 k → k=0이면 완전히 사라져 바탕 레이어의 "꺼진 유리관" 그림이 보임), `light.intensity = lightIntensityOn * k`, `light.enabled = k > 0.01f`. `On == false`면 k를 0으로 취급.
- 깜빡임: `flicker > 0`인 간판만 `Update`에서 `EnemyNumber.UpdateFlame`과 같은 계단식 해시(초당 12단계)로 `k × (1 − flicker × h)`를 다시 적용. 부드러운 사인파 금지(픽셀 느낌). 대부분 간판은 flicker 0이라 Update 비용 없음(`enabled = flicker > 0`).
- 머티리얼: `Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat` (Renderer2D의 기본 Unlit). 빌더가 `AssetDatabase.LoadAssetAtPath`로 연결.
- 정렬: 바탕 배경 레이어와 **같은 `ParallaxLayer` 부모** 아래, 같은 소팅 레이어, `sortingOrder = 바탕 + 1`. (다른 부모에 두면 패럴랙스로 어긋난다.)

### 2.3 Codex 납품물 (배경 1장 = 구간 1개 × 층 1개 기준)
1. **바탕 레이어 PNG**(ART-10 그대로): 네온은 **꺼진 상태**(어두운 유리관·간판 틀)로 그린다. 바탕에 후광·빛번짐을 그리지 않는다. 밝기 값은 1.3절 규칙.
2. **간판별 네온 PNG**: 간판의 켜진 픽셀 + 그 주변 **픽셀 후광**만 담고 바운딩 박스로 자른 작은 PNG. 규칙: 관 픽셀은 채널 하나 이상 255(채도 높은 원색), 관 가운데 1px 밝은 심(`#FFF8E8`류) 허용, 후광은 같은 색상의 어두운 단계 2~3개를 **디더링**으로, 알파는 0 또는 255만. Point 필터, 압축 끔, PPU 32.
3. **`neon_list.csv`**(구간당 1개): `layer,file,x_px,y_px,light_hex,light_radius_px,flicker` — x·y는 그 층 이미지의 **왼쪽 위 기준 픽셀 오프셋**, light_hex는 라이트 색(보통 관 색보다 채도 낮게), radius는 포인트 라이트 바깥 반지름(px), flicker 0~1. 레벨 빌더가 이 표로 `NeonSign` 오브젝트를 놓는다.
4. (선택, Q3) 가까운 층 바탕의 큰 간판 아래 바닥에 **젖은 돌·물웅덩이 픽셀**(바닥보다 조금 밝고 차가운 색 몇 덩어리). 라이트가 닿으면 반사처럼 보인다.
5. 네온 색 팔레트는 스테이지당 **3색 + 강조 1색** 이내 `[제안]` — 예: 단청 주홍 `#FF4A3C`, 단청 청록 `#2EC4A8`, 한지 등불 황금 `#FFC84A`, 강조 흰 심. 핑크+시안 범벅 금지(ANTI_SLOP 3절). 황금은 원거리 적 숫자 색과 겹치므로 간판에는 **채도를 낮춘 주황빛 황금**만 쓴다 → Q1.

---

## 3. 반사광 — "네온 주변에만" 빛이 몰리게

### 3.1 포인트 라이트 1개/간판
- `NeonSign`의 자식 `Light2D` **Point** 타입. 위치 = 간판 중심(큰 간판은 아래쪽 1/3 지점 — 빛이 바닥으로 떨어지는 느낌). 값(T): 색 = CSV `light_hex`, `intensity 0.4`, `pointLightInnerRadius 0.5u`, `pointLightOuterRadius = radius_px/32` (보통 4~7u), `falloffIntensity 0.6`, `blendStyleIndex 0`(Multiply), 그림자·볼류메트릭 끔, `normalMapQuality = Disabled`.
- 대상 소팅 레이어: **World + Characters**(Background 제외 — 먼 층은 간판 스필이 닿을 거리가 아니다. 먼 층의 작은 창문 불빛은 네온 PNG만, 라이트 없음).
- 라이트 세기 합 규칙: `Global World 0.7 + Characters 0.2 + Point 0.4 ≈ 1.0` 안팎 → 간판 바로 아래 캐릭터는 알베도 원색으로 보이고, 2~3u 떨어지면 0.5~0.6으로 가라앉는다. 라이트 텍스처가 1.0에서 잘리므로(HDR 끔) 겹치는 간판이 있어도 과노출되지 않는다.
- 큰 간판(전광판 등)은 포인트 대신 **Sprite 타입 Light2D** 하나로 간판 모양 그대로의 빛을 줄 수 있다(`lightCookieSprite`에 Codex의 64×64 소프트 블롭 스프라이트). 선택 사항, 기본은 포인트.

### 3.2 젖은 바닥 하이라이트 (선택, Q3)
- 간판 아래 바닥에 Point Light 1개 더: 트랜스폼 스케일 (3, 0.5, 1)로 **납작한 타원**, 세기 0.25(T), 대상 레이어 **World만**, 색 = 간판 색. 추가 그림 없이도 Codex가 그린 물웅덩이 픽셀이 올라와 반사처럼 읽힌다. 구간당 2~3개의 주요 간판에만.
- 가산(Additive) 블렌드 스타일·가산 스프라이트는 쓰지 않는다 — 어두운 바닥에 가산을 올리면 뿌연 "AI 글로우"가 된다.

### 3.3 노멀 맵 — 쓰지 않음
픽셀 아트 64px 캐릭터에 노멀 맵은 그릴 비용 대비 티가 거의 없고, 텍스처가 2배가 되며 라이트 비용이 오른다. `normalMapQuality = Disabled` 고정. 입체감은 픽셀 음영(ANTI_SLOP: 광원 방향 하나로 통일)이 담당.

### 3.4 라이트 예산 `[제안]`
| 범위 | 최대 | 비고 |
|---|---|---|
| 화면(960×540) 안에 보이는 Light2D | **10** = Global 2 + 네온 Point 6 + 바닥 타원 2 | URP는 카메라 밖 Light2D를 컬링하므로 구간 전체 개수는 더 많아도 됨 |
| 구간(1620px) 당 네온 라이트 | **≤ 14** | 간판은 더 많아도 되나(라이트 없는 `NeonSign`), 라이트 달린 건 14개까지 |
| 블렌드 스타일 | **0번(Multiply)만** | 스타일 하나 = 라이트 RT 1장. Additive·Mask 스타일 사용 금지 |
| 그림자 캐스터·볼류메트릭·노멀맵 | **0** | |
| Light RT Scale | **0.5 유지** | 480×270. 더 올리지 않음 |

---

## 4. 낮/밤 — `LightingProfile` + `LightingController`

### 4.1 전제
낮/밤은 **같은 그림을 런타임에 바꾸는 게 아니다.** STG-00(아침)은 자체 아침 배경을 쓰고 이 시스템은 "네온 전부 꺼짐 + 밝은 전역광 + 블룸 0"으로만 작동한다. 밤 스테이지는 밤 배경 + 밤 프로필. 한 씬 안에서 아침→밤으로 넘어가는 연출은 **없다**(Q7).

### 4.2 `LightingProfile` (ScriptableObject, `Assets/Scripts/Lighting/LightingProfile.cs`)
```csharp
[CreateAssetMenu(menuName = "NUMBER/Lighting Profile")]
public class LightingProfile : ScriptableObject
{
    public Color worldLightColor = new Color32(0xB4, 0xBC, 0xE6, 0xFF);
    [Range(0f, 1.5f)] public float worldLightIntensity = 0.7f;
    [Range(0f, 1f)]   public float characterLightIntensity = 0.2f; // 흰색, Characters 레이어만
    [Range(0f, 1f)]   public float neonIntensity = 0.95f;          // 0 = 모든 간판·라이트 끔
    [Range(0f, 1f)]   public float postFxWeight = 1f;               // Volume.weight (블룸)
}
```
필드 5개. 에셋 2개로 시작: `Assets/Data/Lighting/Night.asset`(위 기본값), `Day.asset`(`#FFF4E0`, 1.0, 0, 0, 0). 실내(STG-03~05)는 Night를 복제해 색만 바꾼다(필요할 때).

### 4.3 `LightingController` (`Assets/Scripts/Lighting/LightingController.cs`)
- 씬당 1개, `[ExecuteAlways]`. 참조: `worldLight`, `characterLight`(Light2D), `volume`, `profile`(현재), 디버그용 `dayProfile`/`nightProfile`.
- `public static LightingController Current;` (OnEnable/OnDisable에서 등록 — 스테이지 스크립트·트리거가 `LightingController.Current.Apply(profile, 0.5f)`로 호출).
- `Apply(LightingProfile p, float seconds = 0f)`: seconds 0이면 즉시, 아니면 `Update`에서 5개 값을 `Time.deltaTime`로 선형 보간(코루틴 없음, PROTOTYPE_ARCH 방식). 매 프레임 적용 내용: 두 Global Light의 color/intensity, `volume.weight`, `NeonSign.All` 전부에 `SetIntensity(neon)`.
- 편집 모드: `OnEnable`·`OnValidate`에서 `Apply(profile)` 1회 → 씬 뷰·게임 뷰에서 Play 없이 밤 룩이 보인다(검증 수단). 편집 모드에서는 매 프레임 적용하지 않는다(씬 더티 방지). 깜빡임은 Play 중에만.
- 디버그 키 **N**(Input System `Keyboard.current.nKey.wasPressedThisFrame`, PrototypeDebug와 같은 방식): day/night 프로필 토글, 페이드 0.5초. `dayProfile`·`nightProfile`가 비어 있으면 무시. (프로토타입 전용, 빌드에서도 켜 두어도 무해하나 제출 빌드에서는 끈다.)
- `Blackout(float seconds)` `[제안, 선택]`: 방송 숫자 0 순간(GDD LINE-03 "도시가 숨을 멈춘다") 연출. 네온 세기를 0으로 떨어뜨렸다가 `seconds` 후 원래 값으로 복구하는 2단 타이머. 호출은 `BroadcastBoard`의 `NumberChanged`를 구독하는 스테이지 스크립트가 한다 — **이번 구현 범위에 넣을지는 Q4**.

### 4.4 전광판과의 관계
`BroadcastBoard`의 화면 패널 스프라이트를 `NeonSign`으로 만들고 TMP 라벨은 그대로(Unlit, 블룸 자동). LEVEL_STG01의 "꺼진 전광판/켜진 전광판 그림 2장"은 필요 없어진다: SEG-A는 `SetOn(false)`, SEG-C 방송 트리거에서 `SetOn(true)`. 이 연결은 레벨 카드 몫이며 이 문서는 수단만 제공한다.

---

## 5. 프로토타입 씬 테스트 구성

### 5.1 파일 소유
- 신규 `Assets/Editor/LightingSetup.cs` — `DialogueSetup`과 같은 패턴(별도 파일이라 다른 카드와 충돌 없음).
  - `EnsureSortingLayers()`: `TagManager.asset`의 `m_SortingLayers`에 `Background`, `World`, `Characters`, `FX` 추가(이름이 이미 있으면 건너뜀. `uniqueID`는 0 아닌 임의 값).
  - `EnsureProfiles()`: `Assets/Data/Lighting/Night.asset`, `Day.asset`, `NightPostFx.asset`(VolumeProfile + Bloom) 없으면 생성.
  - `BuildInScene()`: 아래 5.2.
  - 메뉴 `NUMBER/Lighting/Toggle Day-Night (Edit Mode)` — 편집 모드에서 현재 씬의 `LightingController.profile`을 Day↔Night로 바꾸고 `Apply`. Play 없이 확인용.
- 수정 `Assets/Editor/PrototypeSceneBuilder.cs`: **대화 카드가 커밋된 뒤에** `BuildLight()` 호출을 `LightingSetup.BuildInScene()`으로 바꾸고 `BuildLight()` 삭제(2줄). 캐릭터 `Visual`·숫자에 소팅 레이어 `Characters`, Ground에 `World` 지정(각 1줄).

### 5.2 `BuildInScene()`이 만드는 것 (전부 플레이스홀더, Square 스프라이트)
```
Lighting
 ├ Global Light (World)        Light2D Global, Night 값
 ├ Global Light (Characters)   Light2D Global, targetSortingLayers = {Characters}, 0.2
 ├ PostFx Volume               Volume(global, weight 1, NightPostFx)
 └ LightingController          profile=Night, dayProfile=Day, nightProfile=Night, 참조 연결
Backdrop                        Square #2A2F4A, 60×17u, order -5, 레이어 World (스필이 보일 벽)
Neon_Red  (x -8, y -2)          NeonSign: Square 3×0.75u 색 #FF4A3C, Unlit 머티리얼, order 5, World
                                 └ Light2D Point, 색 #E0604A, outer 5u, 0.4
Neon_Teal (x  2, y  0)          위와 같음, 색 #2EC4A8 / 라이트 #3AA895, flicker 0.3
Neon_Gold (x 11, y -1)          색 #FFC84A / 라이트 #D9A64A, outer 6u, + 바닥 타원 라이트 (3,0.5) 0.25 World만
```
- 카메라: `GetUniversalAdditionalCameraData().renderPostProcessing = true`. (카메라 카드의 `CameraSetup.Configure`가 같은 카메라에 붙어도 충돌 없음.)
- 플레이어·적 `Visual`은 이미 Lit 머티리얼이라 어두워지고, 네온 근처에서 색이 물든다. TMP 숫자는 Unlit이라 그대로 빨강 1.0 → 가장 밝다.

### 5.3 확인 항목 (Play 없이 — 사용자 에디터에서 Play 금지 규칙)
1. 메뉴 `NUMBER/Build Prototype Scene` 실행 → 콘솔 에러 0, 하이어라키에 `Lighting` 루트.
2. **게임 뷰(편집 모드)**: 배경 벽이 남색으로 어둡고, 간판 3개는 원색, 간판 주변 벽·바닥만 그 색으로 물듦, 그 밖은 어두움. 적 숫자 "3"이 화면에서 가장 밝고 블룸이 가장 큼. 캐릭터 사각형은 배경보다 밝게(0.7대) 읽힘.
3. 메뉴 `NUMBER/Lighting/Toggle Day-Night` → 간판 3개 사라짐(바탕색만), 라이트 꺼짐, 전체 밝고 따뜻한 톤, 블룸 없음. 다시 토글 → 밤 복귀.
4. 씬 뷰 스크린샷: MCP 연결 시 `manage_camera` 스크린샷, 아니면 사용자가 게임 뷰 캡처를 보내 준다. 캡처를 1.1절 서열과 대조.
5. (사용자 수동, Play) N 키로 0.5초 페이드 전환, Teal 간판이 계단식으로 깜빡임, R 재시작 후에도 밤 유지.
6. WebGL: `NUMBER/Build WebGL` 후 Chrome·Firefox에서 2·3항과 같은 그림(블룸 보임, 분홍/검정 화면 없음). 에디터 사용 중엔 빌드 금지(BuildWebGL.cs 주석).

---

## 6. 구현 순서 (Sonnet) — 단계 → 확인

1. `LightingProfile.cs`, `NeonSign.cs`, `LightingController.cs` 작성 → 확인: 컴파일 OK(콘솔), `Create > NUMBER > Lighting Profile` 메뉴 보임.
2. `LightingSetup.cs`(소팅 레이어·에셋·BuildInScene·토글 메뉴) → 확인: 메뉴 단독 실행 시 `Assets/Data/Lighting/` 3개 에셋 생성, TagManager에 소팅 레이어 4개.
3. 대화 카드 커밋 확인 후 `PrototypeSceneBuilder.cs` 연결(5.1) → 확인: 5.3의 1~3.
4. 스크린샷 검토 → 값(T) 튜닝: 전역광 0.6~0.8, 포인트 0.3~0.5, 블룸 threshold 0.75~0.85 사이에서 서열이 지켜지는 값 고정.
5. Opus 검수 → `[code]` 커밋(`Assets/Data/Lighting/` 포함).
6. (아트 단계, 별도 카드) Codex 네온 PNG + `neon_list.csv` → 레벨 빌더가 CSV로 `NeonSign` 배치. 이 문서 범위 밖.

파일 목록 정리:
```
Assets/Scripts/Lighting/LightingProfile.cs      신규
Assets/Scripts/Lighting/LightingController.cs   신규
Assets/Scripts/Lighting/NeonSign.cs             신규
Assets/Editor/LightingSetup.cs                  신규
Assets/Editor/PrototypeSceneBuilder.cs          수정 (BuildLight → LightingSetup.BuildInScene, 소팅 레이어 3줄)
Assets/Data/Lighting/{Night,Day,NightPostFx}.asset   빌더가 생성
ProjectSettings/TagManager.asset                빌더가 소팅 레이어 추가
Assets/Settings/Renderer2D.asset, UniversalRP.asset  변경 없음
```

---

## 7. 위험 (WebGL 포함)

| # | 위험 | 대응 |
|---|---|---|
| 1 | WebGL2에서 HDR 렌더 타깃(half float) 미지원 환경 | 설계가 HDR에 기대지 않음(1.1절). URP가 LDR로 폴백해도 같은 그림. 필요하면 `UniversalRP.asset`의 HDR을 꺼서 대역폭도 아낄 수 있음(선택) |
| 2 | Pixel Perfect **Upscale Render Texture**와 포스트프로세싱 순서: URP 17 2D Renderer는 포스트프로세싱 뒤에 업스케일 패스를 두는 것으로 알고 있으나(블룸이 960×540 격자로 나옴) 미확인 | 5.3-2 스크린샷에서 블룸이 1080p의 부드러운 번짐으로 나오면: Bloom Downscale Half + scatter를 낮춰 격자 느낌 유지, 그래도 어색하면 블룸을 끄고 픽셀 후광만 쓴다(1.4절 마지막 줄). 그림은 블룸 없이도 성립하게 설계함 |
| 3 | 블룸 임계값은 리니어 공간 값 — sRGB 0.95 ≈ 리니어 0.89 | 문서 수치는 이 차이를 반영. 튜닝 시 "숫자 > 네온 > 조명된 것" 서열만 지키면 됨 |
| 4 | 조명 합이 1.0을 넘는 지점에서 밝은 알베도(흰 한복) 픽셀이 임계값을 넘어 블룸 | HDR Emulation 끔이라 조명은 1.0에서 잘림 → 알베도 ≥ 0.8(리니어)인 픽셀만 위험. 캐릭터 팔레트에 순백(`#FFFFFF`)을 쓰지 않는다(ANTI_SLOP 팔레트 규칙에 추가 요청). 그래도 보이면 포인트 0.4 → 0.3 |
| 5 | 라이트 개수·필레이트 | 3.4절 예산. Light RT 0.5 스케일, 블렌드 스타일 1개, 그림자 0. 내장 GPU 기준 라이트 10개 = 480×270 쿼드 10장, 무시해도 되는 비용 |
| 6 | `[ExecuteAlways]` 컨트롤러가 편집 모드에서 씬을 더티로 만듦 | OnEnable/OnValidate에서만 적용, Update는 Play 중에만 |
| 7 | 소팅 레이어 추가가 다른 카드(레벨·카메라·이펙트)와 이름 충돌 | Q6에서 이름 확정 후 구현. `EnsureSortingLayers`는 있는 이름을 덮어쓰지 않음 |
| 8 | `Light2D.AddTargetSortingLayer`가 에디터 전용으로 막혀 있을 가능성 | 빌더(에디터 코드)에서만 호출. 안 되면 `m_ApplyToSortingLayers` SerializedProperty로 대체 |
| 9 | 네온 PNG가 많아지면 RGBA32 비압축 텍스처 용량 | 간판 단위 크롭(2.3절) + 스프라이트 아틀라스는 아트 단계에서 검토. 레이어 크기 마스크(안 A)를 피한 이유 |
| 10 | TMP 라벨이 Unlit이라 밤에도 조명 영향 없음 — 숫자 외의 TMP(대사 창 등)도 똑같이 "최대 밝기" | 대사 UI는 화면 공간 Canvas(Overlay)라 조명·블룸 영향 없음. 월드 TMP는 숫자·전광판만 쓴다 |

---

## 8. 열린 질문 (사용자)

- **Q1 네온 팔레트**: 스테이지당 3색(단청 주홍·청록·등불 황금) + 흰 심으로 갈까? 황금은 원거리 적 숫자 색과 겹치므로 간판 황금은 채도를 낮춘 주황빛으로 — OK?
- **Q2 셰이더 없는 방식(2.1 B)**: 네온 세기를 정점 색으로만 조절(최대 1.0, HDR 부스트 없음). 숫자가 더 빛나는 건 임계값 차이로. 이 정도 블룸이면 충분한가, 아니면 처음부터 HDR 부스트 셰이더(안 C)까지 넣을까?
- **Q3 젖은 바닥**: 바닥 타원 라이트 + Codex 물웅덩이 픽셀을 넣을까(간판당 라이트 1개 추가, 구간당 2~3개)? 아니면 1차는 포인트 라이트만?
- **Q4 숫자 0 블랙아웃**: `Blackout()`을 이번에 구현할까, 연출 단계로 미룰까?
- **Q5 배경 밝기 규칙(1.3절)**: Codex 배경을 "불 꺼진 해질녘" 값으로 그리고 조명이 어둡게 만드는 방식 — AI 원화 생성 프롬프트에 영향. 확인 부탁.
- **Q6 소팅 레이어 이름**: `Background / World / Characters / FX` — **확정 (2026-11-09, DECISIONS 9)**.
- **Q7 낮/밤**: 한 씬 안에서 시간이 바뀌는 연출은 없고 스테이지마다 프로필 하나(STG-00만 Day) — 맞는지.
