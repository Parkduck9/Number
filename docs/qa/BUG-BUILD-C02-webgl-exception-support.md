검증: CONFIRMED (설정값 ProjectSettings.asset:564=1, BuildWebGL.cs:33 확인. 크래시 영향은 PLAUSIBLE)

# BUG-BUILD-C02 — WebGL 예외 지원이 "Explicitly Thrown Only"라 null 참조가 브라우저에서 크래시/오동작이 된다

## 재현 단계
1. `ProjectSettings/ProjectSettings.asset:564` — `webGLExceptionSupport: 1` (= Explicitly Thrown Exceptions Only).
2. `Assets/Editor/BuildWebGL.cs:33` — `ApplySettings()`가 같은 값(`WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly`)으로 강제한다.
3. 순수 C# null 참조(예: 비어 있는 직렬화 필드, 아직 만들어지지 않은 List/배열 접근)가 생기는 상황을 WebGL 빌드에서 만든다.

## 기대 결과
- 에디터처럼 `NullReferenceException`이 콘솔에 찍히고 해당 프레임만 실패한 뒤 게임이 계속 돈다.

## 실제 결과
- 이 모드에서는 IL2CPP가 null 검사와 배열 범위 검사를 넣지 않는다. 에디터에서 로그 한 줄로 끝나는 오류가
  WebGL에서는 "memory access out of bounds" 같은 런타임 오류로 게임 전체가 멈추거나, 주소 0을 읽어 조용히 잘못된 값으로 진행될 수 있다.
- 잼 빌드는 플레이 테스트 시간이 짧아, 에디터에서만 보던 사소한 NRE가 심사자 브라우저에서 치명적 크래시가 될 위험이 있다.

## 수정 제안 (담당: 코드)
- `BuildWebGL.cs:33`의 목표 값을 `WebGLExceptionSupport.FullWithoutStacktrace`로 바꾼다 (용량·속도 비용은 작고, null/범위 예외가 정상적으로 잡힌다).
- 최종 업로드 전 한 번이라도 WebGL 빌드에서 전 스테이지를 통과 플레이해 콘솔 오류를 확인한다.

## 심각도
중간 — 지금 발견된 NRE는 없지만, 생기면 브라우저에서 게임이 멈춘다.
