---
name: codex
description: Delegate a task to OpenAI Codex CLI (GPT) from Claude, routing to gpt-6-astra / gpt-6.1-sol / gpt-6-luna by difficulty. Use for asset work in this project (sprites, audio, materials, shaders, import settings, ScriptableObject data) or whenever a GPT second opinion is wanted.
---

# Codex(GPT) 호출

이 프로젝트에서 GPT는 **Claude가 직접 호출할 때만** 쓴다. 사용자가 Codex를 따로 실행하지 않는다.
Codex는 저장소의 AGENTS.md를 자동으로 읽기 때문에 에셋 담당 역할과 수정 가능한 폴더를 이미 안다.

## 모델 라우팅 (난이도별)

| 난이도 | 모델 | 예시 |
|---|---|---|
| 어려움 | `gpt-6-astra` | 여러 시스템에 걸친 에셋 파이프라인, 복잡한 셰이더, 원인을 모르는 문제 |
| 보통 (기본값) | `gpt-6.1-sol` | 스프라이트·UI 에셋 제작, 머티리얼 설정, 데이터 에셋 작성 |
| 쉬움 | `gpt-6-luna` | 이름 바꾸기, 임포트 설정 일괄 변경, 단순 확인·요약 |

- 판단이 애매하면 `gpt-6.1-sol`을 쓴다.
- **사용량 한도나 오류로 실패하면 한 단계 낮춘다:** astra → sol → luna. luna도 실패하면 사용자에게 알린다.

## 호출 방법 (PowerShell)

새 셸은 PATH가 오래됐을 수 있으니 먼저 갱신한다.

```powershell
$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
```

읽기만 하는 작업(조사, 리뷰, 질문):
```powershell
codex exec -m gpt-6.1-sol -s read-only "<작업 설명>"
```

파일을 만들거나 고치는 작업(에셋):
```powershell
codex exec -m gpt-6.1-sol -s workspace-write "<작업 설명>"
```

- 작업이 길 수 있으니 timeout을 넉넉히 잡는다 (최대 10분). 더 길 것 같으면 백그라운드로 실행한다.
- 프롬프트에 포함할 것: 목표, 대상 폴더(AGENTS.md의 에셋 영역 안), 완료 기준, "CREDITS.md에 기록할 것", "스크립트는 고치지 말 것".
- 출력의 마지막 부분이 최종 답이다. 실행 로그가 길면 `| Select-Object -Last 40`으로 자른다.

## 호출 후 Claude가 할 일
1. `git status`와 `git diff`로 Codex가 바꾼 파일을 확인한다. 에셋 영역 밖을 건드렸으면 되돌리거나 사용자에게 알린다.
2. CREDITS.md에 기록됐는지 확인한다.
3. 결과를 사용자에게 보고하고, 커밋 메시지에 `[asset]`을 붙여 커밋한다.
