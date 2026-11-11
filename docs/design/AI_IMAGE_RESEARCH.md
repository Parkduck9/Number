# AI 이미지 조사 (Gemini 원본 → Codex 픽셀 변환)

> 2026-11-11 웹 조사. 기존 [ANTI_SLOP.md](ANTI_SLOP.md)와 겹치는 내용은 뺐다. 출처는 대부분 포럼·블로그라서 "경향"으로만 읽는다.
> 우리는 AI 사용을 숨기지 않는다. 목표는 공개하되 "성의 없는 슬롭"으로 보이지 않게 하는 것.

## 1. 사람들 반응

- itch.io 댓글에는 AI 사용을 솔직히 공개한 개발자에게 적대적인 반응이 쏟아진 사례가 있다. "임시로만 썼다"고 해도 마찬가지. 이후 직접 그린 아트로 바꿔도 사이트 태그가 전부-아니면-전무라 "AI 생성" 표시가 남았다는 불만도 있다. [itch.io 글 1](https://itch.io/post/16543754) · [itch.io 글 2](https://itch.io/post/16633238)
- 반대편도 있다. "공개하면 걸러 볼 수 있게 해 주면 되지, 망신 줄 일이 아니다"라는 의견. 즉 공개 자체보다 **결과물의 질과 의도**가 반응을 가른다. (위 같은 글타래)
- 잼마다 규칙이 갈린다. 공개·출처 표기를 조건으로 허용하는 잼도 있고, 사람 제작자 전용이라 AI 아트·오디오 사용 시 실격시킨 잼도 있다. 우리 잼 규칙은 공개를 요구하니 공개 + 최소화 방향이 안전하다. [OGA 봄 잼 공지](https://itch.io/jam/opengameartorg-spring-game-jam-2025/topic/4762321/a-clarification-on-the-use-of-ai-generated-art)
- itch.io는 2024-11부터 AI 공개를 필수로 요구하고 집행한다. [GamingOnLinux](https://gamingonlinux.com/2024/11/itchio-store-now-requires-ai-generated-content-disclosures-for-assets/page=1)
- 의심만 해도 안 사는 게이머가 많다. 그래서 "AI 아님"을 증명하려 애쓰기보다 AI 티가 나는 부분을 없애는 쪽이 낫다. [Creative Bloq](https://www.creativebloq.com/3d/video-game-design/how-should-game-developers-respond-to-ai-art-accusations)
- r/PixelArt에서는 "픽셀이 격자에 안 맞는다", "앨리어싱이 의도가 아니라 노이즈 같다"가 AI 의심의 근거로 쓰인다. 다만 초보자 실수와 구분이 안 되어 오해도 생긴다. [r/PixelArt 스레드](https://redlib.ssps.io/r/PixelArt/comments/1k26vck/i_dont_use_ai/mnseit0/?context=3)

## 2. 티 나는 부분 (ANTI_SLOP에 없는 것 위주)

- **믹셀(mixel, 섞인 픽셀 크기):** 가장 강한 신호. 외곽선에서 가장 잘 보인다. [Oreate](https://discover.oreateai.com/discover/the-grid-never-lies-how-to-spot-ai-in-pixel-art-and-digital-sprites)
- **해상도가 애매함:** "16비트 픽셀 아트"인데 1024×1024에 격자 구조가 없으면 의심받는다. (같은 글)
- **글자:** 픽셀 글자가 글자처럼 생긴 블록 덩어리로 나온다. [Oreate](https://discover.oreateai.com/discover/the-grid-never-lies-how-to-spot-ai-in-pixel-art-and-digital-sprites) 포스터류에서도 번들거리는 금색 3D 글자, 네온 그라데이션, 반짝이, 위계 없는 빽빽한 배치가 슬롭의 대표. [Mika Reyes](https://mikareyes.com/ai/how-to-fix-ai-looking-graphic-design-and-posters)
- **광원 미지정 시 평평한 "스튜디오" 조명**, 과한 HDR식 채도. [Penchan 가이드](https://penchan.co/en/ai/creative/ai-image-prompt-guide/)
- **디테일이 균일하게 빽빽함:** 모델은 부드러운 그라데이션으로 생각하고 나중에 격자로 줄이기 때문에 픽셀 경계가 "제안"일 뿐이다. 비슷한 갈색 3개가 가죽끈 하나에 쓰이는 식. [Dev.to jenissimo](https://dev.to/jenissimo/how-to-tame-your-ai-pixel-art-3pk5) · [Sorceress](https://sorceress.games/blog/how-to-convert-an-image-to-pixel-art-free-game-ready)
- (추정, 출처 약함) 완벽한 좌우 대칭, "뒷모습 + 도시 전경" 같은 흔한 구도, 손·소품 뭉개짐. 구도는 아래 프롬프트에서 직접 정해 피한다.

## 3. 우리 파이프라인에서 할 일

### Gemini 프롬프트 체크리스트
- [ ] 글자는 요청하지 않는다. "no text, no letters, no signage, no logo" 명시. 로고는 손으로 올린다. [Penchan](https://penchan.co/en/ai/creative/ai-image-prompt-guide/)
- [ ] 광원 한 개를 방향까지 지정한다 (예: 오른쪽 위 달빛 + 아래 붉은 등불 반사). 안 하면 평평해진다. (같은 글)
- [ ] 팔레트를 말로 못 박는다: "muted palette, crushed shadows, max 12 colors, no oversaturation, no HDR". 네온 금지. [Penchan](https://penchan.co/en/ai/creative/ai-image-prompt-guide/)
- [ ] "no perfect symmetry", 비대칭 구도, 화면비(예: 16:9)·매체("flat-shaded pixel illustration, large pixel blocks")를 명시한다. 평균으로 돌아가 밋밋해지는 것을 막는다. [PromptSpace 앨범커버 가이드](https://www.promptspace.in/prompts/gemini/album-covers)
- [ ] 우리 팔레트 스와치/기존 스프라이트를 참고 이미지로 첨부한다. 참고 이미지가 있으면 오류율이 눈에 띄게 줄어든다는 보고가 있다. (Penchan)
- [ ] 큰 픽셀을 요구해도 격자는 안 맞는다. "프롬프트로 완벽한 스냅을 얻으려 싸우지 말고 후처리"가 통설. [Sorceress](https://sorceress.games/blog/snap-an-image-to-pixel-art-ai-browser-sprite-ready)
- [ ] 캐릭터는 그리게 하지 않는다. 배경·소품·분위기만 받는다 (아래 합성 참고).

### Codex 변환 단계 체크리스트
1. **배경/크로마키 제거는 원본 해상도에서 먼저** (후광이 팔레트로 번지는 것을 막는다). [Sorceress](https://sorceress.games/blog/how-to-convert-an-image-to-pixel-art-free-game-ready)
2. **격자 검출 → 네이티브 해상도로 다운스케일 → 팔레트 축소 → 정수 배율 Point 확대** 순서. 팔레트부터 줄이면 "흐릿한 색이 적게" 남을 뿐이다. [Dev.to jenissimo](https://dev.to/jenissimo/how-to-tame-your-ai-pixel-art-3pk5) · [Sorceress](https://sorceress.games/blog/snap-an-image-to-pixel-art-ai-browser-sprite-ready)
3. 단순 최근접 다운스케일은 가장자리가 지저분하다. 블록별 **최빈색(dominant) 방식**을 쓴다. [fal Image2Pixel](https://fal.ai/models/fal-ai/image2pixel/llms.txt) (파라미터 `downscale_method: dominant`, `snap_grid`, `max_colors`) 도구 후보: [Pixel Snapper](https://help.scenario.com/articles/1753861053-pixel-snapper-the-essentials), [Phaser tidy pixel art](https://phaser.io/tools/tidy-pixel-art). 입력에 복원 가능한 격자가 없으면 스냅이 안 맞으니 그때는 아래 4번처럼 새로 다시 찍는다.
4. **고정 프로젝트 팔레트로 강제 매핑**한다 (에셋별 임의 추출 금지). 단일 캐릭터는 16~32색이 보통. 다운스케일 뒤 새로 생긴 색은 한 번 더 정리, 비슷한 색은 손으로 병합. [Sorceress](https://sorceress.games/blog/how-to-convert-an-image-to-pixel-art-free-game-ready)
5. **손 정리 패스:** 외곽선 알파 0/255, 외곽선 규칙 통일(예: 어두운 단색 + 안쪽만 selout), 고립 픽셀 제거, 디테일을 덜어 내기. 크게 어긋나면 변환물 위에 직접 다시 찍는다.
6. **게임 화면에 놓고 4배 확대로 확인** (ANTI_SLOP 5절). 믹셀 검사: 외곽선 픽셀 폭이 전부 같은지.

### 키아트(타이틀/커버) 추가 규칙
- **실제 게임 스프라이트를 합성한다.** 주인공·적·보스는 AI가 그린 캐릭터 대신 인게임 스프라이트를 확대(정수 배율)해서 올린다. AI는 배경·하늘·한옥 실루엣 같은 분위기만 맡는다. 이러면 캐릭터가 게임과 달라 보이는 문제와 믹셀이 같이 해결된다. (조사 근거는 약함. 위 "프롬프트로 스냅 불가" 통설에서 도출한 우리 판단)
- **로고와 글자는 손으로** 우리 픽셀 폰트/로고 파일을 올린다.
- 한 이미지 안에서 픽셀 크기를 하나로 통일한다. 배경만 네이티브로 줄이고 캐릭터만 크게 확대하면 믹셀이 된다. 같은 격자에서 합성한다.

## 4. Gemini 프롬프트 예시 (타이틀 키아트, 배경 전용)

> 영어로 쓴다. 캐릭터·글자 없음 — 스프라이트와 로고는 나중에 합성.

```
Wide 16:9 background illustration for a pixel-art action game, Joseon-dynasty
Korea reimagined as a quiet cyberpunk city at night. Seen from a low street-level
angle, not a back view and not a sweeping city panorama. Foreground: a worn hanok
eave with curved tile roof and dancheong beams cutting diagonally across the upper
left, hanging paper talismans and a cracked stone lantern at lower right. Midground:
a narrow alley of tile-roofed houses, one slightly leaning, with wiring and a few
dull bronze signal boxes. Background: faint tall tower shapes in fog.
Asymmetric composition, large empty dark area in the center for a game logo.
Single light source: cold moonlight from upper right, plus one small warm red
lantern glow at lower left. Muted palette, indigo and charcoal with ochre and
dried-blood red accents, strictly under 12 colors, crushed shadows, no neon,
no oversaturation, no HDR, no glowing eyes, no gradient sky.
Flat-shaded pixel illustration with large chunky pixel blocks, hard edges,
no anti-aliasing, no soft blur, no film grain.
No text, no letters, no signage, no logo, no characters, no people.
```

## 5. 남은 한계
- "AI 사용자 반응" 근거는 포럼 일화 위주다. 정량 조사는 찾지 못했다.
- 변환 도구(Pixel Snapper 등)는 벤더 페이지 설명이라 실제 결과를 우리 스프라이트로 먼저 시험해 보고 쓴다.
