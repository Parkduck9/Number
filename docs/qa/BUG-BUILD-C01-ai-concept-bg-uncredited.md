검증: CONFIRMED (STG00.unity:724 / Stg00Builder.cs:12 확인, CREDITS.md에 해당 항목 없음)

# BUG-BUILD-C01 — AI 생성 컨셉 이미지가 STG00 배경으로 빌드에 들어가지만 크레딧·AI 공개에 없음

## 재현 단계
1. `Assets/Scenes/STG00.unity`를 연다 (Build Settings에서 켜져 있는 씬, 인덱스 1).
2. `Background (PLACEHOLDER concept image)` 오브젝트의 SpriteRenderer를 본다.
3. 스프라이트 출처를 따라간다:
   - `Assets/Scenes/STG00.unity:724` — `m_Sprite: {... guid: 5a93f46d447996d43885937ca0b118ef ...}` = `Assets/Art/Placeholder/PLACEHOLDER_stg01_segA_bg.png`
   - `Assets/Editor/Stg00Builder.cs:12` — `BgPath = "Assets/Art/Placeholder/PLACEHOLDER_stg01_segA_bg.png"`
   - `Assets/Editor/Stg01Builder.cs:40` — 이 파일은 `docs/design/concept/14_bg_stg01_pixel_native.png`을 복사한 것
   - `docs/README.md:9` — "`design/concept/`의 이미지는 AI 생성(Codex) 참고용이다. 게임 에셋으로 쓰면 CREDITS.md에 기록한다."
4. `CREDITS.md`의 "AI-generated assets" 표와 "Generative AI disclosure"를 본다.

## 기대 결과
- 빌드에 들어가는 모든 AI 생성 이미지가 CREDITS.md "AI-generated assets"에 도구 이름과 함께 기록돼 있다.
- AI 공개 문구에 "Codex — 컨셉 아트/배경 이미지 생성"처럼 실제 사용 방식이 적혀 있다 (공식 규칙 7(d), 빠지면 상 후보 제외).
- 또는 제출 전에 최종 아트로 교체돼 PLACEHOLDER 이미지가 빌드에 남지 않는다.

## 실제 결과
- STG00(시작 스테이지)에 AI 생성 컨셉 이미지가 배경으로 그대로 들어간다.
- CREDITS.md "AI-generated assets" 표에는 number-ember 한 줄뿐이고, 이 배경 이미지는 없다.
- AI 공개의 Codex 항목은 "asset creation and asset setup"뿐이라 컨셉 이미지 생성이 드러나지 않는다.
- (참고) `PLACEHOLDER_stg01_segB_bg.jpg`(`docs/design/concept/13_style_frame_v2.jpg` 복사본, `Stg01Builder.cs:42`)는 현재 어떤 씬에도 참조되지 않아 빌드에 들어가지 않는다. 다시 쓰면 같은 문제가 생긴다.

## 수정 제안 (담당: 에셋/문서 — QA는 고치지 않음)
- 최종 배경으로 교체하거나, 남길 경우 CREDITS.md에 `PLACEHOLDER_stg01_segA_bg.png — AI 생성 (Codex 이미지 생성) — STG00 배경` 한 줄 추가 + AI 공개에 컨셉 아트 생성 용도 추가.
- ANTI_SLOP.md 체크리스트로 이 배경을 최종 화면에 남겨도 되는지 사용자 확인.

## 심각도
중간 — 게임은 동작하지만, AI 공개가 실제와 다르면 상 후보 자격에 영향이 있다.
