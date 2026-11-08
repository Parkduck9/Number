검증: REJECTED (PLACEHOLDER_stg01_segB_bg.jpg는 어떤 씬·프리팹도 참조하지 않아 빌드에 안 들어감. Bilinear 설정 불일치는 해당 안 됨). 관련: BUG-BUILD-C01

# BUG-BUILD-A05 — 빌드 포함 씬(`STG01`) 배경에 가짜 HUD·캐릭터가 구워진 플레이스홀더 텍스처 사용 및 `.meta` 필터 설정 불일치

## 심각도
높음

## 관련 파일 및 근거 (`file:line`)
- `Assets/Editor/Stg01Builder.cs:40-43` (`SEG-A`에 `14_bg_stg01_pixel_native.png` → `PLACEHOLDER_stg01_segA_bg.png`, `SEG-B`에 `13_style_frame_v2.jpg` → `PLACEHOLDER_stg01_segB_bg.jpg`를 복사·배치)
- `Assets/Art/Placeholder/PLACEHOLDER_stg01_segB_bg.jpg` (1600×533 JPEG 이미지 내부에 상단 `((( 3 )))` 방송 숫자, 좌상단 유도의 얼굴 HUD, 지붕 위·바닥의 시민 적 `3`·`2`·`0`, 공중의 AI 벌레 `1`, 대시 중인 주인공 캐릭터가 배경 그림으로 함께 그려져 있음)
- `Assets/Art/Placeholder/PLACEHOLDER_stg01_segB_bg.jpg.meta:40, 54, 78, 100-108` (`filterMode: 1` [Bilinear], `spritePixelsToUnits: 32`, `textureCompression: 0` [Uncompressed], WebGL `overridden: 0`)
- `docs/design/GDD.md:214, 230, 250` (컨셉 이미지는 픽셀 크기가 고르지 않아 게임 에셋으로 직접 쓰지 않으며, 배경은 `Filter Mode = Point` 및 PPU 통일 명시)
- `docs/design/ANTI_SLOP.md` 및 `AGENTS.md:64-66` (AI 슬롭 및 가짜 UI/배경 혼입 금지)

## 재현 단계
1. `Assets/Scenes/STG01.unity` 씬(Build Settings 인덱스 2, `enabled: 1`)을 열고 두 번째 구간(`SEG-B`, 월드 X = 60)으로 이동한다.
2. 배경 스프라이트(`Assets/Art/Placeholder/PLACEHOLDER_stg01_segB_bg.jpg`)와 그 임포트 설정(`PLACEHOLDER_stg01_segB_bg.jpg.meta`)을 확인한다.

## 기대 결과
- 빌드 포함 씬(`STG01`)의 배경 텍스처에는 캐릭터나 UI HUD가 그려져 있지 않은 순수 배경만 들어 있어야 하며, 픽셀 아트 기준(`GDD.md:214, 250`)에 맞춰 `filterMode: 0` (Point)으로 임포트되어야 한다.

## 실제 결과
- `STG01`의 `SEG-B` 구간 배경으로 컨셉 시안 전체 화면(`13_style_frame_v2.jpg`)을 그대로 복사한 `PLACEHOLDER_stg01_segB_bg.jpg`가 연결되어 있어, 실제 게임 플레이 중 배경에 움직이지 않는 가짜 주인공·가짜 적(`3`, `2`, `1`, `0`)·가짜 상단 방송 숫자(`((( 3 )))`)·가짜 좌상단 얼굴 체력 HUD가 겹쳐 보인다.
- 또한 해당 텍스처의 `.meta` 파일(`PLACEHOLDER_stg01_segB_bg.jpg.meta:40`)이 `filterMode: 1`(Bilinear)로 설정되어 있어 `SEG-A`(Point 필터 도트 배경)와 화풍·선명도가 어긋난다.
