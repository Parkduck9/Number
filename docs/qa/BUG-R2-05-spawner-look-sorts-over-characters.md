작성: Claude Sonnet (Antigravity 할당량 소진으로 대체)

# BUG-R2-05: 스폰 장치 그림이 런타임에 Default 소팅 레이어로 만들어져 플레이어·적보다 앞에 그려진다

## 심각도
**낮음** (보이는 문제. 장치를 때릴 때 플레이어 몸을 가린다)

## 재현 단계
1. STG-01/02/04 SEG-B에서 스폰 장치에 붙어 선다 (스윙이 닿으려면 장치 중심 2.75u 안).
2. 장치 그림(1.6 × 1.2u)이 플레이어 스프라이트와 겹치는 위치(플레이어 중심이 장치 중심에서 약 1.25u 안)에 서 본다.

## 기대 결과
- 장치는 월드 오브젝트라 World 레이어, 플레이어·적(Characters)보다 뒤에 그려진다 (DECISIONS 9 Q6: Background / World / Characters / FX).

## 실제 결과
- 장치의 `Look` 스프라이트는 `Awake`에서 새로 만들고 `sortingOrder = 5`만 준다. `sortingLayerName`을 정하지 않아 Default 레이어다. TagManager 순서는 Background → World → Characters → **Default** → FX 이므로 Default가 Characters 위에 그려진다.
- 빌더의 `AssignSortingLayers`는 빌드 시점에 있는 렌더러만 처리하므로 런타임에 생기는 이 스프라이트는 놓친다.
- (조명은 FX 글로벌 라이트가 Default도 비추므로 어둡지는 않다. `LightingSetup.cs:119` 및 FX 라이트 생성부)

## 근거
- `Assets/Scripts/Level/EnemySpawner.cs:42-49` (`sr = new GameObject("Look")...`, `sr.sortingOrder = 5`, 레이어 지정 없음).
- `ProjectSettings/TagManager.asset` `m_SortingLayers` 순서: Background, World, Characters, Default, FX.
- `Assets/Editor/StageBuildKit.cs:374-382` `AssignSortingLayers`는 씬에 이미 있는 SpriteRenderer만 대상.

## 제안
- `Look` 생성 직후 `sr.sortingLayerName = "World"` (레이어가 없으면 기본값 유지). 레이어 이름 사용은 `SpriteFx`의 `FxLayer`처럼 `SortingLayer.NameToID`로 안전하게.
