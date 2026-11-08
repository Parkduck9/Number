검증: CONFIRMED (decompressionFallback 0, ProjectSettings.asset:578 확인)

# BUG-BUILD-C04 — 저장소의 WebGL 플레이어 설정이 BuildWebGL.cs 목표 값과 다르다 (아직 적용 안 됨)

## 재현 단계
1. `ProjectSettings/ProjectSettings.asset`의 현재 값을 본다:
   - `:47-48` `defaultScreenWidthWeb: 960`, `defaultScreenHeightWeb: 600`
   - `:578` `webGLDecompressionFallback: 0`
   - `:574` `webGLCompressionFormat: 0` (Brotli)
   - `:599` `managedStrippingLevel: {}` (WebGL 항목 없음 = 기본값)
2. `Assets/Editor/BuildWebGL.cs:23-38`의 목표 값과 비교한다 (Brotli + Decompression Fallback 켬, 1920×1080, Medium stripping 등).
3. 메뉴 `NUMBER/Build WebGL`이 아니라 `File → Build Profiles → Build`로 빌드한다.

## 기대 결과
- 어떤 경로로 빌드해도 같은 설정이 쓰인다. 특히 Brotli를 쓰면 Decompression Fallback이 켜져 있다.

## 실제 결과
- `NUMBER/Apply WebGL Settings`가 아직 실행·커밋되지 않아 저장소 설정은 목표와 다르다.
- 이 상태로 Build Profiles 창에서 빌드하면 Brotli + Fallback 꺼짐 조합이 된다. 호스트가 `.br` 파일에 `Content-Encoding: br` 헤더를 주지 않으면
  로더가 "Unable to parse ... Build.framework.js.br" 오류로 시작조차 못 한다. 캔버스도 960×600(16:10)이라 16:9 게임 위아래에 여백이 생긴다.

## 수정 제안 (담당: 코드/사용자)
- 에디터에서 `NUMBER/Apply WebGL Settings`를 한 번 실행하고 `ProjectSettings.asset` 변경을 커밋한다.
- 제출 빌드는 반드시 `NUMBER/Build WebGL` 메뉴로 만든다 (README나 체크리스트에 명시).
- C03 결정(캔버스 크기)을 먼저 반영한 뒤 적용.

## 심각도
낮음 — 메뉴로 빌드하면 문제없지만, 다른 경로로 빌드하면 itch.io에서 로딩 실패 가능.
