검증: PARTIAL — 압축 0은 Brotli(enum Brotli=0)라 "압축 비활성화" 주장은 틀림(REJECTED). 폴백 0(:578), 해상도 960x600(:47-48), 1920x1080 캔버스 문제는 CONFIRMED. 중복: BUG-BUILD-C03, BUG-BUILD-C04

# BUG-BUILD-A01 — WebGL PlayerSettings 미적용(`ProjectSettings.asset`) 및 고정 1920×1080 캔버스 크기 이슈

## 심각도
중간 (표준 빌드 창 사용 시 높음)

## 관련 파일 및 근거 (`file:line`)
- `Assets/Editor/BuildWebGL.cs:18-42` (`ApplySettings()`에서 `Brotli`, `decompressionFallback = true`, `1920x1080`, `WasmCodeOptimization.DiskSize`, `ManagedStrippingLevel.Medium` 설정 코드 정의)
- `ProjectSettings/ProjectSettings.asset:47-48` (`defaultScreenWidthWeb: 960`, `defaultScreenHeightWeb: 600` — 16:9가 아닌 16:10 해상도로 저장됨)
- `ProjectSettings/ProjectSettings.asset:571` (`webGLTemplate: APPLICATION:Default`)
- `ProjectSettings/ProjectSettings.asset:574` (`webGLCompressionFormat: 0` — 압축 비활성화 상태)
- `ProjectSettings/ProjectSettings.asset:578` (`webGLDecompressionFallback: 0` — 압축 해제 폴백 비활성화 상태)
- `ProjectSettings/ProjectSettings.asset:599` (`managedStrippingLevel: {}` — WebGL 타깃 오버라이드 미저장)
- `docs/design/GDD.md:42, 214` (기준 해상도 16:9 `960×540` ×2 = `1920×1080`)

## 재현 단계
1. 저장소의 `ProjectSettings/ProjectSettings.asset` 파일을 열어 WebGL 관련 설정 값(`defaultScreenWidthWeb`, `defaultScreenHeightWeb`, `webGLCompressionFormat`, `webGLDecompressionFallback`, `managedStrippingLevel`)을 확인한다.
2. 커스텀 메뉴(`NUMBER/Apply WebGL Settings` 또는 `NUMBER/Build WebGL`)를 거치지 않고 Unity 기본 `File > Build Profiles (Build Settings) > Build`로 WebGL 빌드를 실행하거나, `NUMBER/Apply WebGL Settings` 실행 후 `APPLICATION:Default` 템플릿에서 생성되는 고정 픽셀 캔버스 크기(`1920×1080`)를 일반 브라우저 창/itch.io 임베드 환경에서 확인한다.

## 기대 결과
1. `ProjectSettings/ProjectSettings.asset`에 WebGL 필수 설정(`webGLCompressionFormat: 1` (Brotli), `webGLDecompressionFallback: 1`, 16:9 해상도, `managedStrippingLevel` WebGL `Medium`)이 이미 적용·커밋되어 있어 어떤 빌드 경로로 빌드하더라도 동일한 압축·폴백·비율 설정이 보장되어야 한다.
2. itch.io 브라우저 임베드 화면(전체화면 전환 전 기본 뷰포트)에서도 캔버스가 잘리지 않고 16:9 비율(`960×540` 또는 반응형 스케일링)로 한눈에 표시되어야 한다.

## 실제 결과
1. `BuildWebGL.ApplySettings()`가 아직 실행·커밋되지 않아 `ProjectSettings/ProjectSettings.asset`에는 여전히 기본값(`defaultScreenWidthWeb: 960`, `defaultScreenHeightWeb: 600` [16:10 비율], `webGLCompressionFormat: 0` [압축 없음], `webGLDecompressionFallback: 0`, `managedStrippingLevel: {}`)이 저장되어 있다. 또한 `BuildWebGL.cs:35`의 `UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSize`는 `UserSettings/`(Git 추적 제외)에만 저장되므로 다른 작업자 PC에 공유되지 않는다.
2. `BuildWebGL.cs:28-31`은 고정 픽셀 크기를 사용하는 `APPLICATION:Default` 템플릿 상태에서 캔버스 크기를 `1920×1080`px로 설정한다. 1080p 이하 모니터의 일반 브라우저 창이나 itch.io 기본 페이지 레이아웃에서는 전체화면 버튼을 누르기 전까지 캔버스 우측·하단이 잘려 보일 수 있다.
