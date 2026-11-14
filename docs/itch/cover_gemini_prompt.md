# itch 커버 이미지 다시 만들기 — Gemini 프롬프트

최종: 630×500 PNG (itch 목록에서는 315×250으로 작게 보임 → 작게 봐도 한눈에 읽혀야 함).
흐름: 사용자가 Gemini로 원본 생성 → Codex(gpt-6.1-sol high)가 픽셀 정리 + 실제 로고(logo_number.png) 합성 → 시안 확인 → 업로드.

## Gemini에 첨부할 참고 이미지
1. `docs/design/concept/portraits/yudo_gemini.png` — 주인공 얼굴·옷 (청록 고글, 검은 머리, 남색 한복풍 옷)
2. `Assets/WebGLTemplates/Number/TemplateData/keyart.png` — 게임 분위기·배경 (기와지붕, 네온 숫자 간판, 남보라 밤하늘)

로고는 Gemini에게 그리게 하지 않는다 (글자가 깨짐). 위쪽을 비워 두고 나중에 실제 로고를 얹는다.

## 프롬프트 (그대로 붙여넣기)

```
Make a game cover illustration in 16-bit pixel art style, landscape, aspect ratio 5:4 (for example 1260x1000).

Use the attached images as reference: the first is the main character (keep his face, black messy hair, cyan glowing goggles, dark navy hanbok-style jacket with a white collar and a belt). The second shows the game's world and colors (night city that mixes Joseon-era Korean roof tiles with neon cyberpunk signs, deep navy and violet sky, pink and cyan neon, warm yellow accents).

Composition:
- The hero is in the lower-middle, large (about half the image height), in a dynamic action pose: mid-leap or dashing forward, swinging a short blade, with a bright white-yellow slash arc.
- Behind him, a huge glowing pink neon number "7" on a holographic screen, slightly tilted, lighting the scene.
- A small red six-sided die with white pips spins in the air near his free hand, glowing.
- Background: a rooftop of curved Korean tiled roofs at night, distant towers with neon signs, a few red paper lanterns, light rain.
- Keep the TOP 30% of the image calm and fairly empty (dark sky with soft glow) so a title logo can be placed there later.
- Strong contrast and a clear silhouette so it still reads when shrunk to a small thumbnail.

Style rules: crisp pixel art with visible square pixels, limited palette (about 24 colors), 1-pixel dark outlines on the character, no blur, no gradients that look painted, no soft airbrush glow, no text, no letters, no watermark, no signature, no UI.
```

## (선택) 배너 + 배경용 파노라마 — 두 번째 요청

itch Edit theme의 Banner(페이지 폭 960px, 높이 약 250~300px)와 Background(페이지 뒤 큰 그림)에 같이 쓴다.
주인공은 빼고 도시 풍경만 넓게 뽑는다. 배너에는 가운데에 실제 로고를 얹고, 배경은 어둡게 눌러서 글자가 잘 읽히게 한다.

```
Using the same world as before (second attached image), make a very wide panoramic pixel art scene, aspect ratio 4:1 (for example 1920x480).
A Joseon-era Korean rooftop skyline at night mixed with cyberpunk neon: curved tiled roofs in the foreground, distant towers with pink and cyan neon number signs, red paper lanterns, light rain, deep navy and violet sky.
Keep the center area calm and darker so a title logo can sit there. No characters, no text, no letters, no watermark.
Crisp 16-bit pixel art with visible square pixels, limited palette (about 24 colors), no blur, no painted gradients.
```

## 결과가 마음에 안 들 때 고쳐 말하기 (예시)
- 주인공이 작음 → "Make the hero bigger, filling about 60% of the image height."
- 그림이 매끈한 일러스트처럼 나옴 → "Redo it as true low-resolution pixel art, as if drawn at 320x256 and scaled up with no smoothing."
- 위쪽이 복잡함 → "Clear the top third: only dark sky, no buildings or signs there."
- 글자가 들어감 → "Remove all text and letters."
