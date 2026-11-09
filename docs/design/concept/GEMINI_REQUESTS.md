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
