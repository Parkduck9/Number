검증: PLAUSIBLE (TMP 번들 에셋 라이선스는 일반 지식 기반, 크레딧 부재는 확인)

# BUG-BUILD-C08 — 빌드에 들어가는 TextMesh Pro 기본 에셋(LiberationSans, EmojiOne)이 크레딧에 없다

## 재현 단계
1. `Assets/TextMesh Pro/Resources/` 폴더는 `Resources`라서 참조 여부와 상관없이 빌드에 전부 들어간다.
   - `Assets/TextMesh Pro/Resources/TMP Settings.asset` — `m_defaultFontAsset` = LiberationSans SDF (`Assets/TextMesh Pro/Fonts/LiberationSans.ttf` 기반)
   - `Assets/TextMesh Pro/Resources/Sprite Assets/` — EmojiOne 스프라이트 에셋 (`Assets/TextMesh Pro/Sprites/EmojiOne.png`)
2. `CREDITS.md`의 Third-party assets 표를 본다.

## 기대 결과
- JAM_RULES.md "모든 외부(third-party) 출처를 크레딧에 적을 것"에 따라 빌드에 포함된 외부 폰트·이미지가 모두 기록돼 있다.

## 실제 결과
- CREDITS.md에는 Galmuri, Mulmaru, pixelfx만 있다. LiberationSans(SIL OFL 1.1)와 EmojiOne 이미지(Unity TMP에 포함, 원 저작권 EmojiOne)는 없다.
- (참고) 반대로 Galmuri는 크레딧에 있지만 TMP 폰트 에셋이 없어 현재 빌드에는 쓰이지 않는다. 문제는 아니지만 최종 크레딧과 실제 사용을 맞출 것.

## 수정 제안 (담당: 문서)
- CREDITS.md에 "Liberation Sans — SIL OFL 1.1 — TextMesh Pro 기본 폰트(Unity Technologies 배포)" 한 줄, EmojiOne도 사용 안 하면 크레딧 대신
  `Resources/Sprite Assets`에서 빼는 방법도 있다(코드·에셋 담당 결정).

## 심각도
낮음
