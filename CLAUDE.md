@AGENTS.md

## Claude 전용
- 코딩은 Sonnet, 검수는 Opus가 맡는다 (AGENTS.md 역할 표 참고).
  - Opus 세션에서 구현을 맡길 때는 Sonnet 서브에이전트에 맡기고, 결과 diff를 Opus가 직접 검수한다.
- Unity 에디터 GUI는 직접 조작할 수 없다. 씬·프리팹 구성이 필요하면 에디터 스크립트(`Assets/Editor/`)로 자동화하거나 사용자에게 단계를 안내한다.
- git은 GitHub Desktop 내장 git을 쓴다 (시스템 PATH에 git 없음).
