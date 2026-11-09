# Gemini 원본 요청 (2026-11-09)

> 순서: **사용자가 Gemini로 원본 생성 → 아래 폴더에 저장 → Claude에게 "넣었어" → Codex가 게임용으로 변환 → 시안 확인 → 연결.**
> 프롬프트는 영어 그대로 복사해 쓰면 된다. 참고 이미지가 있으면 같이 첨부한다.
> 공통 금지: 글자·로고·워터마크, 실존 브랜드, 술·담배, 부드러운 그라데이션 광택(AI 티). ANTI_SLOP.md 참고.

---

## 1. 저격수 전용 스프라이트 원본
- **저장:** `docs/design/concept/enemies/sniper_gemini.png`
- **첨부할 참고:** `docs/design/concept/portraits/citizen_gemini.png` (시민 얼굴·옷 계열), 이전에 적 만들 때 쓴 정면 레퍼런스 사진
- **게임 쪽:** Codex가 원거리 시민과 같은 크기의 대기 2·조준 1·발사 2 프레임으로 변환 (`Assets/Resources/DraftSprites/sniper_*`)

```
Full-body character design sheet, front-facing view plus a side view aiming, of a sniper in a Joseon-dynasty cyberpunk city.
A civilian controlled by an AI broadcast: hanbok-inspired long coat in steel blue and dark indigo, a gat-style wide brim hat with a thin cyan visor line,
expressionless face, holding a long slim rail rifle with a small red laser emitter at the muzzle.
Pixel-art friendly design: clear silhouette, flat colors, limited palette (indigo, steel blue, bone white, small red accent), hard edges, no soft gradients.
Plain solid dark navy background (#0B1026), no text, no logo, no extra characters.
```

---

## 2. 엔딩 일러스트 2장
- **저장:** `docs/design/concept/endings/end01_gemini.png`, `end02_gemini.png` (16:9)
- **첨부할 참고:** `docs/design/concept/portraits/yudo_gemini.png` (주인공), `broadcast_gemini.png` (방송 화면)
- **게임 쪽:** Codex가 480×270 픽셀 일러스트로 변환, 엔딩 대사 뒤에 한 장씩 띄움 (대사는 아직 임시라 장면은 넓게 잡음)

END-01 — 정지 버튼을 눌렀다 (행운의 숫자는 처음부터 없었다):
```
Wide 16:9 illustration for a pixel-art game ending. Joseon-dynasty cyberpunk city at night, seen from a rooftop.
The protagonist (see reference) stands with his back to us, his hand still on a large red stop button.
Every giant broadcast screen across the city has gone dark and black; the crowds below have stopped and are looking up, confused.
Mood: quiet, empty, a little sad. Palette: deep indigo, cold blue, a few dim amber lanterns, no magenta.
Flat colors, hard pixel-art-friendly edges, no text, no logo, no numbers.
```

END-02 — 숫자를 내가 정했다:
```
Wide 16:9 illustration for a pixel-art game ending. Same Joseon-dynasty cyberpunk city at night, same rooftop.
The protagonist (see reference) faces the city in three-quarter view, holding up one hand; every giant broadcast screen now glows cyan
with one large blank number frame (leave the frame empty, no digit), and the citizens below are moving again, some looking up hopefully.
Mood: hopeful, defiant. Palette: indigo night, bright cyan screens, warm amber lanterns.
Flat colors, hard pixel-art-friendly edges, no text, no logo.
```
(숫자는 플레이어가 고른 값을 게임이 그려 넣는다 → 그림에는 빈 틀만)

---

## A. 타이틀 키아트 (= itch 표지·로딩 화면 배경) — 우선
- **저장:** `docs/design/concept/keyart/title_keyart_gemini.png` (16:9, 가능한 한 크게)
- **첨부할 참고:** `portraits/yudo_gemini.png`, `portraits/broadcast_gemini.png`, STG-01 배경 원본(`bg_layers/stg01_bg_far_gemini.png`)
- **게임 쪽:** Codex가 ① 타이틀 배경(960×540 픽셀), ② itch 표지 630×500, ③ WebGL 로딩 화면 배경으로 자름. **로고 글자 "NUMBER"는 Gemini에 넣지 않고** Codex가 픽셀 폰트로 따로 만든다 (AI 글자는 깨지기 쉬움).

```
Wide 16:9 key art for a pixel-art action game, no text at all.
Night view of a Joseon-dynasty cyberpunk city: layered giwa rooftops, paper lanterns, cyan and magenta neon,
and one gigantic broadcast screen in the sky showing a single huge glowing magenta digit "0" made of thick pixel blocks.
In the foreground, the protagonist (see reference) stands on a rooftop ridge seen from behind and slightly to the side,
coat and hair moving in the wind, holding his blade low, looking up at the screen.
Leave the upper-left third fairly calm and dark (space for a logo). Strong silhouette, limited palette
(deep indigo, navy, cyan, magenta, a few amber lanterns), flat colors, hard pixel-art-friendly edges, no soft gradients, no text, no watermark.
```

---

## B. 정지 버튼 (엔딩 직전 소품)
- **저장:** `docs/design/concept/props/stop_button_gemini.png`
- **게임 쪽:** Codex가 월드 소품(약 64×64)과 근접 화면용 큰 버전으로 변환. 지금은 글자 "[E] STOP BUTTON"뿐.

```
Pixel-art friendly prop design, front view and three-quarter view: the AI company's emergency STOP button
in a Joseon cyberpunk palace server room. A big red mushroom push button under a raised hinged glass cover,
set into a carved dark-wood and brass pedestal with dancheong patterns, cables running into the floor,
a small cyan status light. Flat colors, hard edges, limited palette (red, brass, dark wood, cyan accent).
Plain solid dark navy background (#0B1026), no text, no logo.
```

---

## C. 체크포인트 = 러다이트 표식
- **저장:** `docs/design/concept/props/checkpoint_mark_gemini.png`
- **게임 쪽:** Codex가 꺼짐(회색)·켜짐(금색) 2상태 소품(약 32×64)으로 변환. 지금은 색만 바뀌는 네모.

```
Pixel-art friendly prop design, front view: a small hidden shrine-post used as a secret sign by the Luddite resistance
in a Joseon cyberpunk city. A short wooden jangseung-like post wrapped in rope and paper talismans, with a hand-carved
gear symbol (no electronics, no screens) and a small oil lamp on top. Show two versions side by side: unlit (grey, cold)
and lit (warm gold lamp glow). Flat colors, hard edges, limited palette. Plain solid dark navy background (#0B1026), no text.
```

---

## 0. 러다이트 게임 스프라이트 다시 (우선)
- **저장:** `docs/design/concept/npc/luddite_full_gemini.png`
- **첨부할 참고:** `docs/design/concept/portraits/luddite_gemini.png` (얼굴·옷·색을 이것과 똑같이), 주인공 스프라이트 만들 때 쓴 정면 레퍼런스 사진
- **게임 쪽:** Codex가 64×64 대기 4프레임(`luddite_npc_idle_4f`)으로 변환, 주인공·적과 같은 크기·외곽선·팔레트

```
Full-body character design sheet of the same old man as the attached portrait: front-facing standing pose, plus a three-quarter view.
Elderly leader of the Luddite resistance in a Joseon-dynasty cyberpunk city: grey hair in a sangtu topknot with a small metal topknot cap,
grey goatee, calm stern face, white hanbok jeogori under a worn brown leather vest, dark baji trousers, straw-and-metal shoes.
His right arm is a crude hand-made mechanical prosthetic (riveted steel plates, exposed gears, no screens, no glowing AI parts),
with only a thin cyan rim light on one side like the portrait. Slightly stooped but strong, hands relaxed at his sides.
Pixel-art friendly design: clear silhouette, flat colors, limited palette matching the portrait (greys, skin, brown, off-white, small cyan accent),
hard edges, no soft gradients. Plain solid dark navy background (#0B1026), no text, no logo, no extra characters.
```

---

## 3. STG-01 SEG-B 배경 (층 분리)
- **저장:** `docs/design/concept/bg_layers/stg01B_far_gemini.png`, `stg01B_mid_gemini.png` (가로 3:1)
- **첨부할 참고:** `docs/design/concept/bg_layers/` 안의 SEG-A 원본(`stg01_bg_far_gemini.png`, `stg01_bg_mid_gemini.png`) — 같은 화풍·같은 밤이어야 한다
- **게임 쪽:** Codex가 SEG-A와 같은 방식(810×268, 하늘·스카이라인·중경·움직이는 등/창문)으로 자름. SEG-B는 거리 → 지그재그로 올라가 높은 지붕·탑에서 끝나는 구간이라 위쪽까지 덮게 세로로 잇는다.

먼 배경(far):
```
Wide 3:1 background layer for a 2D side-scrolling pixel-art game, matching the attached reference exactly in style and palette.
Far distance only: night sky over a Joseon-dynasty cyberpunk city, a tall pagoda-like broadcast tower on the right horizon,
distant layered giwa rooftops and thin antenna masts, tiny window lights, faint haze. No foreground, no characters.
Flat colors, hard edges, limited indigo/blue palette with tiny amber and cyan lights. No text, no logo.
```

중경(mid):
```
Wide 3:1 background layer for a 2D side-scrolling pixel-art game, matching the attached reference exactly in style and palette.
Middle distance: a dense market street climbing uphill toward high rooftops, stacked hanok shops with awnings, wooden bridges between roofs,
hanging paper lanterns, cables, a few cyan neon panels and small magenta accents. Darker and lower contrast than gameplay,
leave the bottom 15% mostly dark for the ground. No characters, no text, no logo. Flat colors, hard edges.
```
