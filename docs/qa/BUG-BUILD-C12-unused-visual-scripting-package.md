검증: PLAUSIBLE (manifest.json:30 확인, 빌드 크기 영향은 빌드 필요)

검증: PLAUSIBLE (패키지가 설치돼 있고 프로젝트에서 안 쓰는 것은 확인. WebGL 빌드 크기 영향은 빌드 리포트로 확인 필요)

# BUG-BUILD-C12 — 쓰지 않는 Visual Scripting 패키지가 설치돼 있어 IL2CPP(WebGL) 빌드에 끼어들 수 있다

## 재현 단계
1. `Packages/manifest.json:30` — `"com.unity.visualscripting": "1.9.12"` (Unity 템플릿 기본 포함).
2. 프로젝트에서 사용 여부 확인: `Assets/` 전체의 `.unity`, `.prefab`, `.asset`, `.cs`에서 `ScriptMachine`, `StateMachine`, `Unity.VisualScripting` 검색 → 0건. Visual Scripting 그래프 에셋 없음.
3. `NUMBER/Build WebGL`(`Assets/Editor/BuildWebGL.cs:45`)로 WebGL(IL2CPP) 빌드.

## 기대 결과
- 게임에서 안 쓰는 런타임 패키지는 빌드에 영향을 주지 않는다 (다운로드 용량·빌드 시간 최소).

## 실제 결과 (확인 필요)
- Visual Scripting은 런타임 어셈블리(`Unity.VisualScripting.Core`, `.Flow`, `.State`)가 자동 참조되고,
  IL2CPP 빌드 전처리에서 AOT 스텁·link.xml을 생성해 자기 어셈블리를 스트리핑에서 보존하는 것으로 알려져 있다.
  그러면 쓰지도 않는 코드가 wasm에 남아 용량과 빌드 시간이 늘고, 패키지 쪽 오류가 빌드를 깨뜨릴 수 있다.
- BUG-BUILD-C07(MCP 런타임)과 같은 종류의 문제다.

## 수정 제안 (담당: 사용자/코드)
- 첫 WebGL 빌드 후 `Editor.log`의 Build Report(또는 `Build/WebGL/Build/*.wasm` 크기)에서 `Unity.VisualScripting.*` 포함 여부를 본다.
- 포함되거나 확인이 번거로우면 Package Manager → Visual Scripting → Remove. 이 프로젝트는 쓰지 않으므로 되돌릴 일이 없다.
  제거는 `Packages/manifest.json`·`packages-lock.json` 변경이라 커밋 한 번으로 끝난다. 11/14 업로드 전에 결정.

## 심각도
낮음 — 동작 문제는 없고 용량·빌드 안정성 문제.
