검증: CONFIRMED (팀원 정보 placeholder, TMP 번들 에셋·Codex 배경 미기재). 중복: BUG-BUILD-C01, BUG-BUILD-C08, SUBMISSION_CHECKLIST

# BUG-BUILD-A06 — `CREDITS.md` 및 `README.md`의 잼 제출 필수 항목(팀원, 외부·AI 에셋 출처, 생성형 AI 공개, 조작법) 누락·미완성

## 심각도
높음 (미기재 시 `JAM_RULES.md`에 따라 상 후보 제외 가능)

## 관련 파일 및 근거 (`file:line`)
- `CREDITS.md:5-6` (`## Team` 항목이 `- (팀원 이름 / 역할)` 플레이스홀더 상태)
- `CREDITS.md:8-13` (`## Third-party assets`에 `Galmuri`, `Mulmaru`, `pixelfx`만 기재됨)
- `CREDITS.md:15-18` (`## AI-generated assets`에 `number-ember.png` 1건만 기재되고 Codex로 생성한 배경 에셋 등 누락)
- `CREDITS.md:20-25` (`## Generative AI disclosure (draft)` 제목이 초안 상태이며 `pixelfx` 도구 결합 사용 내역 등이 요약에 반영되지 않음)
- `Assets/TextMesh Pro/Resources/TMP Settings.asset:27, 39` (`Resources` 폴더 내에 위치하여 WebGL 빌드에 자동 포함되는 기본 폰트 `LiberationSans SDF` 및 기본 스프라이트 `EmojiOne` 참조)
- `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt:1-7, 34` (SIL Open Font License 1.1 저작권 및 라이선스 고지 의무)
- `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt:1-3` (EmojiOne 출처 표기 안내)
- `Assets/Editor/Stg01Builder.cs:40-42` 및 `docs/design/GDD.md:230-235` (현재 `STG00`, `STG01` 씬에 포함된 `PLACEHOLDER_stg01_segA_bg.png`, `PLACEHOLDER_stg01_segB_bg.jpg`는 Codex [OpenAI GPT]로 생성된 AI 이미지)
- `README.md:18-21` 및 `JAM_RULES.md:35-36, 47-55` (제출 페이지 필수 항목: 제목·설명, 조작법, 모든 외부 에셋 크레딧, 팀원 이름, 생성형 AI 공개)

## 재현 단계
1. `JAM_RULES.md:47-55` 및 `AGENTS.md:69-70, 77-78`의 제출 페이지 필수 요건과 현재 저장소의 `CREDITS.md`, `README.md`, 그리고 실제 빌드 포함 에셋(`Assets/`)을 대조한다.

## 기대 결과
1. `CREDITS.md`의 `## Team`에 실제 참가 팀원 이름과 역할이 기입되어 있어야 한다.
2. 빌드에 포함되는 모든 서드파티 에셋(TextMesh Pro `Resources/TMP Settings.asset`이 참조하여 빌드에 묶이는 `Liberation Sans` [SIL OFL 1.1] 및 `EmojiOne` [CC-BY 4.0 / Ranks.com] 포함)이 `CREDITS.md`의 `## Third-party assets`에 기록되거나 빌드에서 제외되어야 한다.
3. 빌드에 포함된 모든 AI 생성 에셋(현재 `STG00`·`STG01`에서 사용 중인 Codex 생성 배경 `PLACEHOLDER_stg01_segA_bg.png`, `PLACEHOLDER_stg01_segB_bg.jpg` 및 `Assets/Resources/FX/` 이펙트)과 도구별 역할(Claude Fable/Sonnet/Opus, Codex CLI [OpenAI GPT], Antigravity CLI, pixelfx)이 `(draft)` 표시 없이 최종본으로 `CREDITS.md`에 정확히 기재되어야 한다.
4. 제출 페이지에 바로 복사해 쓸 수 있도록 실제 코드에 구현된 조작법(`A/D`·방향키 이동, `W` 점프, 좌클릭/`Enter` 공격, 우클릭 패링, `Left Shift` 대시, `Space` 충격파, `E` 대화·해킹·상호작용, `0~9`·위/아래·마우스 휠 다이얼 조작, `1/2` 엔딩 선택, `Esc/P` 일시정지)이 `README.md` 또는 `CREDITS.md`에 영문·국문으로 정리되어 있어야 한다.

## 실제 결과
1. `CREDITS.md:6`의 팀원 이름이 `- (팀원 이름 / 역할)` 빈칸으로 남아 있다.
2. `Assets/TextMesh Pro/Resources/TMP Settings.asset`에 의해 빌드에 자동 포함되는 `Liberation Sans` 폰트와 `EmojiOne` 스프라이트가 `CREDITS.md`에 빠져 있다.
3. `CREDITS.md:15-18`의 `## AI-generated assets` 표에 Codex(OpenAI GPT)로 생성되어 현재 `STG00`·`STG01`에 쓰인 배경 에셋이 누락되어 있고, `CREDITS.md:20`의 `## Generative AI disclosure (draft)`가 초안 상태이며 `pixelfx`를 통한 이펙트 생성 내역이 Generative AI disclosure 목록에 통합되어 있지 않다.
4. `README.md`에 제출 필수 항목인 조작법(Controls) 안내가 작성되어 있지 않다(`GDD.md:38, 151-152`에도 공격·패링 키가 `[미정]`으로 남아 있어 실제 구현된 좌클릭/우클릭·`Space`·`P` 조작과 다르다).
