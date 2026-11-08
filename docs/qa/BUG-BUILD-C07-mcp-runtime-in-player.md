검증: PLAUSIBLE (패키지 asmdef 플랫폼 설정 직접 확인 못함)

# BUG-BUILD-C07 — 개발용 MCP for Unity 패키지의 런타임 어셈블리가 WebGL 빌드에 포함된다

## 재현 단계
1. `Packages/manifest.json` — `"com.coplaydev.unity-mcp": "10.3.0"` (에디터 조작용 개발 도구).
2. `Library/PackageCache/com.coplaydev.unity-mcp@*/Runtime/MCPForUnity.Runtime.asmdef` — `"includePlatforms": []`, `"autoReferenced": true`
   → 에디터 전용이 아니라 모든 플랫폼(WebGL 포함) 플레이어 빌드에 컴파일된다.
3. 이 어셈블리는 `System.IO`(`Runtime/Helpers/ScreenshotUtility.cs:3`)와 리플렉션(`UnityAssembliesCompat.cs`, `UnityPhysicsCompat.cs`, `UnityTypeConverters.cs`)을 쓴다.

## 기대 결과
- 개발 도구 코드는 게임 빌드에 들어가지 않는다 (WebGL 다운로드 용량 최소화, 위험 API 없음).

## 실제 결과
- 게임 코드에서는 쓰지 않지만 어셈블리가 빌드에 들어간다. 자동 실행 훅(`RuntimeInitializeOnLoadMethod`)은 없어 동작 문제는 확인되지 않았고,
  영향은 주로 용량과, 패키지 업데이트 시 WebGL 컴파일·스트리핑 오류 가능성이다.

## 수정 제안 (담당: 사용자/코드)
- 제출 빌드 직전에 빌드 크기 보고서(Editor.log의 Build Report)에서 `MCPForUnity.Runtime`·Newtonsoft 포함 여부를 확인한다.
- 포함되면 제출 빌드 동안 패키지를 제거(Package Manager → Remove)하고 빌드 후 되돌린다. 마감 후에는 게임 파일 수정 금지이므로 업로드 전에 결정.

## 심각도
낮음
