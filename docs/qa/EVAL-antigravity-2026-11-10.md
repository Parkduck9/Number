# EVAL antigravity (agy) — 2026-11-10 — 평가 실패 (답변 없음)

도구: `agy.exe` (Antigravity CLI). 평가 요청서(영문)를 같은 문장으로 넘겼으나 답변을 받지 못했다.

| 시도 | 모델 | 결과 |
|---|---|---|
| 1 | claude-opus-5-5-medium, `--mode plan` | 첫 줄("I'll start by reading the design docs")만 출력하고 종료. 파일 읽기 도구가 막힘 |
| 2 | gemini-3.8-flash-high, `--sandbox` | `jetski: no output produced — a tool required the "command" permission that headless mode cannot prompt for` |
| 3 | 같음 + `--settings`로 읽기 전용 명령 allow 규칙 | 같은 오류 (규칙 형식이 먹지 않음) |
| 4 | 같음 + 문서·코드를 프롬프트에 전부 인라인(약 400KB) | 인자 길이 초과 → stdin으로 전달 → 같은 오류 (모델이 여전히 명령 실행을 시도) |

`--dangerously-skip-permissions`는 저장소 쓰기 권한을 열어 읽기 전용 조건에 어긋나므로 쓰지 않았다.
