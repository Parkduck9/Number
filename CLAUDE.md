@AGENTS.md

## Claude 전용

### Claude 모델 라우팅
> **2026-11-08 밤 사용자 변경:** Fable은 **검수 전용**이다. 구현은 Opus가 파일이 겹치지 않게 나눠 맡고(병렬 가능), 단순 작업은 Sonnet. 아래 표보다 이 줄이 우선한다.
> **2026-11-09 오후 사용자 변경:** Fable 사용량이 얼마 남지 않았다. **중요한 코드**(전투·상태 로직 핵심, 위험한 여러 파일 변경)만 Opus 구현 → **Fable 검수**. **중요하지 않은 코드**(스프라이트·머티리얼 연결, 작은 수정, 문서)는 **Sonnet 구현 → Opus 검수**이고 Fable은 쓰지 않는다.

작업 난이도에 따라 모델을 고른다. 서브에이전트(`.claude/agents/`)를 쓸 때나 **병렬 세션(카드)을 여러 개 만들 때 모두** 적용한다.

| 등급 | 모델 | 서브에이전트 | 맡는 일 |
|---|---|---|---|
| 최상 | **Fable** | `game-architect` | 아키텍처·핵심 시스템 설계, 여러 파일에 걸친 리팩터링, 한 번 시도해도 원인을 모르는 버그 |
| 상 | **Opus** | `code-reviewer` | 코드 검수, 기획 판단, 중간 난이도 설계 |
| 중 | **Sonnet** | `game-coder` | 일반 구현 (기본값) |

- 코드 흐름: (필요하면 Fable 설계) → **Sonnet 구현 → Opus 검수** → 커밋.
- 병렬 세션을 만들 때는 각 카드의 작업 난이도에 맞춰 `model`을 지정한다. 서로 같은 파일을 건드리는 작업은 병렬로 돌리지 않는다.
- **모델이 부족하면 한 단계 낮춘다:** 사용량 한도, 오류, 사용 불가일 때 Fable → Opus → Sonnet → Haiku 순서로 내려간다. 낮춘 사실은 사용자에게 알린다.

### 토큰 절약 규칙
- **AI끼리 주고받는 글은 영어로 쓴다:** 서브에이전트·병렬 세션에 보내는 프롬프트, 세션 간 메시지, 공유 노트, Codex 호출 프롬프트가 여기에 해당한다. 사용자에게 하는 보고와 문서(GDD·docs/)는 한국어로 쓴다.
- **effort는 high까지만 쓴다.** xhigh·max·ultracode·Workflow(다중 에이전트 오케스트레이션)는 쓰지 않는다. 사용자가 그 단어를 직접 지시할 때만 예외다. 사용자 설정(`~/.claude/settings.json`)의 `maxEffortLevel: high`, `workflowKeywordTriggerEnabled: false`가 이를 강제한다.
- 서브에이전트는 꼭 필요할 때 한 번에 하나씩 쓴다. 토큰이 갑자기 많이 줄면 의도하지 않은 다중 에이전트·Workflow 호출부터 확인한다.
- 오토컴팩트는 60만 토큰에서 한다 (`autoCompactWindow: 600000`).

### GPT(Codex) 호출
- GPT는 **Claude가 `codex` 스킬로 호출할 때만** 쓴다. 주로 에셋 작업에 쓴다.
- 난이도별로 라우팅한다: 어려움 `gpt-6-astra` / 보통 `gpt-6.1-sol` (기본값) / 쉬움 `gpt-6-luna`. 실패하면 한 단계씩 낮춘다.
- 자세한 방법: `.claude/skills/codex/SKILL.md`

### 기타
- Unity 에디터 GUI는 직접 조작할 수 없다. 씬·프리팹 구성이 필요하면 에디터 스크립트(`Assets/Editor/`)로 자동화하거나 사용자에게 단계를 안내한다.
- **Unity MCP**(MCP for Unity, CoplayDev, 무료 MIT): 에디터가 켜져 있고 `Window → MCP for Unity`에서 서버가 실행 중이면 `.mcp.json`의 `unityMCP`(http://127.0.0.1:8080/mcp)로 에디터를 조작할 수 있다. 씬·프리팹은 한 번에 한 AI만 수정 규칙을 그대로 지킨다.
- **Unity CLI**: 에디터 `C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe`를 `-batchmode -quit -projectPath`로 실행해 컴파일 확인·에디터 스크립트 실행·빌드를 한다. 에디터가 이 프로젝트를 열고 있으면 배치 모드는 실패하므로 MCP를 쓴다.
- **pixelfx 스킬**(patch-gg/pixelfx, MIT): 픽셀 이펙트 제작용. 체크아웃은 저장소 밖 `D:\Git\pixelfx`에 두고 거기서 `node tools/*.mjs`를 실행한다 (Node 18+ 필요, 설치됨: `C:\Program Files\nodejs\node.exe` — PATH에 없으면 전체 경로로 실행). Unity에는 JS 코드를 넣지 않고 `--sheet`로 내보낸 PNG 스프라이트 시트만 `Assets/Art/`에 넣는다 (Point 필터, 압축 끔). 이펙트도 보이는 것이므로 시안을 먼저 보여 준다. 쓴 이펙트는 CREDITS.md에 pixelfx(MIT, Patch / Hugo Duprez)로 기록한다.
- git은 GitHub Desktop 내장 git을 쓴다 (시스템 PATH에 git 없음):
  `C:\Users\qwer0\AppData\Local\GitHubDesktop\app-<버전>\resources\app\git\cmd\git.exe`
