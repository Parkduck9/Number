# 배경 아트 시안 (STG-02 ~ STG-05) — bg-art-drafts

> 2026-11-08 (Claude Opus + Codex). **시안이다.** 사용자가 확인하기 전에는 레벨 빌더에 연결하지 않는다.
> 근거: [DECISIONS.md](../DECISIONS.md) 2-B(배경 6장), 9번 Q1(스테이지당 네온 3색 + 흰 심)·Q5("불 꺼진 해질녘" 밝기), [LIGHTING_DESIGN.md](../LIGHTING_DESIGN.md) 1.3·2.3절, [GDD.md](../GDD.md) 8장·ART-00·ART-10, [ANTI_SLOP.md](../ANTI_SLOP.md).
> STG-00은 STG-01 SEG-A를 낮 조명으로 다시 쓰므로 새 그림이 없다.

## 1. 계획

| 파일 (`Assets/Art/Drafts/Backgrounds/`) | 스테이지·구간 | 크기 (px) | 유닛 (PPU 32) | 넣을 빌더 |
|---|---|---|---|---|
| `bg_stg02_segA_rooftops_draft.png` | STG-02 SEG-A 지붕길 | 1620×540 | 50.625×16.875 | `Stg02Builder` 구간 A |
| `bg_stg02_segB_elevated_rail_draft.png` | STG-02 SEG-B 고가 철로 | 1620×540 | 50.625×16.875 | `Stg02Builder` 구간 B |
| `bg_stg03_hideout_room_draft.png` | STG-03 러다이트 아지트 (방 1개) | 960×540 | 30×16.875 | `Stg03Builder` |
| `bg_stg04_segA_production_line_draft.png` | STG-04 SEG-A 생산 라인 | 1620×540 | 50.625×16.875 | `Stg04Builder` 구간 A |
| `bg_stg04_segB_inspection_hall_draft.png` | STG-04 SEG-B 검사 홀 | 1620×540 | 50.625×16.875 | `Stg04Builder` 구간 B |
| `bg_stg05_boss_room_draft.png` | STG-05 본사 최상층 보스 방 | 960×540 | 30×16.875 | `Stg05Builder` |

- **픽셀 격자:** 그림 1픽셀 = 화면 1픽셀(960×540 기준, PPU 32). 캐릭터 스프라이트(약 64px)와 같은 격자라 픽셀 크기가 섞이지 않는다. (STG-01 시험 그림 `14_bg_stg01_pixel_native.png`는 810×270, 1픽셀 = 화면 2픽셀이라 이번 것보다 픽셀이 두 배 굵다.)
- **팔레트:** 바탕은 남색·검정 계열 + 나무·기와·회벽 중간 어두운 값. 강조색은 Q1 세 가지(단청 주홍 `#FF4A3C` 계열, 청록 `#2EC4A8` 계열, 채도 낮춘 주황 `#C8864A` 계열)만, 그것도 **꺼진 유리관·칠한 무늬**로만. 그림당 32색.
- **밝기:** "불 꺼진 해질녘". 하늘·먼 층은 어두운 남색, 중간·가까운 층 벽·기와·바닥은 40~60% 밝기. 네온·간판·화면은 모두 **꺼진 상태**, 후광·빛번짐 없음 (LIGHTING 2.3-1). 켜진 네온은 나중에 간판별 PNG + `neon_list.csv`로 따로 얹는다.
- **층:** 한 장 안에 먼 층(하늘·궁궐식 고층 실루엣) / 중간 층(지붕·구조물) / 가까운 층(거리·바닥)을 그린다. 시안은 한 장으로 합친 그림이고, 승인 후 패럴랙스가 필요하면 층별로 다시 나눈다.
- **바닥:** 아래 약 32px(= 1유닛, 빌더의 바닥 윗면 y 1.0)를 평평한 바닥 띠로. 발판(지붕·철로·캣워크)은 빌더가 따로 그린다(GDD 배경 파이프라인 1).
- **구간 끝:** 오른쪽 끝은 문·대문처럼 끊기는 그림(STG-02 A 대문, B 아지트 입구, STG-04 A 공장 문, B 본사 입구, STG-03 오른쪽 문).
- **전광판 자리:** STG-02·04는 빌더가 전광판을 따로 놓으므로 하늘 위 가운데를 비워 둔다. STG-05는 빌더의 보스 화면(중심 15,11 / 12×5u = 화면 위 가운데 384×160px) 자리에 **꺼진 큰 화면 틀**을 그려 둔다.

### 제작 방법 (GDD ART-10)
1. **Codex CLI (`gpt-6.1-sol`)의 이미지 생성 도구**로 고해상도 원본 6장 생성. 프롬프트 원문은 아래 3절.
   - Codex 원본 크기: 구간 2172×724, 방 1672×941 (원본은 저장소에 넣지 않음, `Temp/bg_src/`에 임시).
2. Claude가 Python(Pillow) 스크립트로 도트화: 목표 비율로 가운데 자르기 → 5×5 중앙값 필터(AI 질감 잡티·글자처럼 보이는 얼룩 제거) → 목표 크기로 BOX 축소 → 밝기 ×1.4(원본 중간·가까운 층 명도가 약 30%라 규칙 40~60%로) → 32색 미디언 컷 양자화(디더링 없음) → 3×3 최빈값 필터. 알파는 전부 255.
   - 결과 명도(HSV V, 아래 2/3 평균): STG-02 A 44% · B 45% · STG-03 39% · STG-04 A 42% · B 49% · STG-05 40%. 하늘도 같이 밝아짐(33~52%).
3. 원본 크기와 4배 확대로 ANTI_SLOP 2·3절 확인.

## 2. 레벨 빌더 연결 (승인 후, Sonnet 작업)
- 지금 STG-02~05 빌더는 `StageBuildKit.BuildSolidBackground`(단색)를 쓴다. 승인되면 STG-01처럼 그림 배경으로 바꾼다: 위 표의 파일을 `Assets/Art/Backgrounds/`로 옮기고(`_draft` 떼기) 구간 루트 왼쪽 아래에 놓는다.
- 임포트 설정: Sprite(Single), **PPU 32**, Pivot Bottom Left, **Filter Point**, 압축 끔, Mip Map 끔, Max Size 2048. 배율 1.0(1620px ÷ 32 = 50.625u = `SegWidth`, 960px ÷ 32 = 30u = 방 너비) — STG-01 SEG-B처럼 확대하지 않는다.
- 이 폴더의 PNG에는 `.meta`가 없다(에디터를 열지 않았다). 처음 에디터를 열 때 자동 생성된다.
- 바닥선·발판 높이는 그림을 보고 LEVEL_STG02/04 표를 맞춘다.

## 3. 시안에서 본 문제 (승인 때 결정)
- STG-02 A: 먼 층 고층 건물이 층층 누각처럼 보일 수 있음 (ANTI_SLOP 3절 주의).
- STG-02 B: 그림의 철로 상판 높이 약 8u, 빌더 `RAIL-B` 5.0u → 둘 중 하나를 맞춘다.
- STG-04 A·B: 뒤쪽 캣워크(약 7.8u·6.9u)가 발판처럼 보이지만 실제 발판은 3.6u → 플레이어가 헷갈릴 수 있음.
- STG-03: 창살이 도트화로 뭉개짐. 위쪽에 바깥 지붕·하늘이 보이는 단면 구도.
- STG-05: 화면 틀(약 330×165px)이 빌더 화면(384×160px)보다 조금 작음 → 빌더가 덮으면 문제없음.
- 공통: 꺼진 네온 간판 틀이 거의 없다. 승인 뒤 간판 자리를 정해 간판별 PNG + `neon_list.csv`(LIGHTING 2.3)를 만든다.
- 검토 페이지: https://claude.ai/artifact/9FoUegC8CiyL3dLcUcMwBq (비공개)

## 4. Codex 프롬프트 (원문, 영어)

````text
You are the asset role in this repo (read AGENTS.md). Task: generate 6 DRAFT background source images with your image generation tool (image_gen). Do NOT edit any scripts, scenes, prefabs or files under Assets/. Do NOT touch CREDITS.md (Claude records it). Only write the 6 PNG files below.

Save each generated image (highest resolution the tool gives, keep the raw output, no resizing) to this exact path inside the repo:
  Temp/bg_src/<name>.png
Create the folder if needed. If the tool saves elsewhere, copy the file there.

Aspect: images 1-2 and 4-5 are wide side-scroller strips (target 3:1, final 1620x540). If the tool cannot make 3:1, use landscape 1536x1024 and compose so ALL important content sits in the middle horizontal band (top 1/6 and bottom 1/6 will be cropped off). Images 3 and 6 are single rooms (target 16:9, final 960x540); with 1536x1024 keep content inside the middle 84% height band.

=== SHARED STYLE BLOCK (append to every prompt) ===
2D side-scrolling game background, pixel-art look with hard edges, strict flat orthographic side view (no perspective vanishing point, no camera tilt, no fisheye). Setting: a Joseon-dynasty Korean city reimagined with restrained near-future tech. Korean hanok features must read first: gently curved giwa clay-tile roofs with softly upturned eave tips, dancheong painted brackets under the eaves, wooden pillars, hanji paper lattice windows, stone base walls. Lighting state: DUSK WITH ALL LIGHTS OFF. Deep navy sky; middle and near layers (walls, roof tiles, ground) are mid-dark values around 40-60% brightness so every shape still reads. Neon tubes, signs and screens are drawn SWITCHED OFF: dark glass tubes and empty dark frames only. NO glow, NO bloom, NO light rays, NO lens flare, NO reflections, NO lit windows. Accent hues allowed only as dull paint and unlit tube glass: vermilion (#FF4A3C family), teal-cyan (#2EC4A8 family), desaturated orange (#C8864A family). One light direction: faint moonlight from the upper left. Limited palette, stepped cel shading instead of smooth gradients, no airbrush, no film grain, no vignette, no depth-of-field blur. Three depth layers: far (sky and distant silhouettes, darkest, lowest contrast), middle (rooftops / structures), near (street level). The bottom 6% of the image is a flat walkable floor strip running edge to edge with a perfectly straight horizontal top edge. FORBIDDEN: any text, letters, numbers, digits, logos, kanji, hangul or fake writing anywhere (signs stay blank); people, characters, animals, vehicles; pink or purple/magenta neon; Japanese or Chinese architecture (no pagodas, no torii, no multi-tier towers, no Chinese pavilions); generic cyberpunk clutter, holograms, floating text.
=== END SHARED STYLE BLOCK ===

1) name: stg02_segA_rooftops
Rooftop path over a dense hanok neighborhood at dusk. Middle layer: a continuous run of hanok giwa rooftops at three slightly different heights (low, high in the middle, low again) forming a walkable rooftop route; ridge lines clearly readable, chimneys, laundry poles, small rooftop water tanks with cables. Far layer: low silhouettes of palace-style tall buildings with hanok rooflines on top, one or two unlit rectangular billboard frames on their facades. Near: an alley floor strip at the bottom with stone paving. Keep the upper middle of the sky calm and empty (a game display will be placed there). Right edge ends at a closed wooden hanok gate with a tiled roof.

2) name: stg02_segB_elevated_rail
An elevated rail viaduct running horizontally across the whole image at about 35-40% height from the bottom: a straight concrete-and-steel track deck supported by thick piers; the piers have stone bases and wooden hanok bracket details; small giwa tile awnings run along the viaduct edge. Under the viaduct: a narrow street with closed shop shutters and blank unlit sign boxes. Behind: hanok rooftops (middle layer) and dark palace-style towers (far). Left side: a staircase rising from the street to the rail deck. Keep the upper middle of the sky calm and empty. Right edge: the street ends at a low, half-hidden hideout entrance — a heavy old wooden door set into a stone wall under a slightly crooked tiled eave, with a dark lantern hanging off.

3) name: stg03_hideout_room
Interior of a Luddite hideout, one room, side view, about 16:9. A converted old hanok storehouse: exposed wooden beams and rafters, mud-plaster walls with patches, hanji lattice window panels (dark), shelves and workbenches stacked with dismantled old machines, coils of wire, broken boxy terminals, hand tools, a small floor brazier (unlit), a low table with cushions. Mood: hand-made, analog, anti-AI. Right edge: a wooden door (exit). Center-right floor area kept open for two characters to stand and talk. Floor strip: worn wooden floorboards. Lighting state the same: everything unlit, mid-dark values, hues limited to warm wood, plaster grey, dull vermilion and teal accents, desaturated orange.

4) name: stg04_segA_production_line
Inside the Embector factory, side view: an industrial production line with long horizontal conveyor belts, robot arms (switched off, frozen), hanging ducts and cable trays. The factory hall has a hanok-inspired steel roof structure: big curved roof trusses echoing hanok eave curves, giant pillars painted with faded dancheong patterns. Raised steel catwalks along the back wall. Stacked shipping crates on the floor. Background: tall dark windows showing a navy dusk sky. Keep the upper middle area calm. Right edge: a heavy sliding factory door.

5) name: stg04_segB_inspection_hall
Inspection hall of the Embector factory, side view: a large clean hall with a row of empty inspection booths / scanning gates (unlit frames) along the back wall, rows of dormant humanoid-android charging racks shown only as empty metal frames (no figures), overhead rail cranes, a raised catwalk. Hanok cues: curved roof truss, dancheong-pattern bands on beams, lattice-patterned steel window grilles. Cold and orderly. Right edge: a grand entrance to the headquarters — tall double doors under a hanok-style tiled portico.

6) name: stg05_boss_room
Top floor of the Embector headquarters, one room, side view, about 16:9. A throne-hall-like chamber modelled on a Joseon palace main hall interior: tall red-lacquered pillars (dull vermilion), dark coffered ceiling with dancheong patterns, a raised stone platform floor. Dominating the upper center: one huge rectangular screen, switched off (pure dark glass with a thin metal frame), its frame spanning about 40% of the image width, from about 20% to 50% of the image height measured from the top. Behind/around the screen: a folding-screen (byeongpung) motif turned into tech panels and cables feeding into it. Right side near the floor: a small pedestal (empty top). Floor strip: polished dark stone. Everything unlit.

When done, list the 6 saved paths and the pixel size of each file. Final answer only that list.
````

## 5. 크레딧
- 이 그림들은 **AI 생성(Codex CLI / OpenAI GPT 이미지 생성) + Claude 스크립트 도트화**다. 게임에 쓰게 되면 [CREDITS.md](../../../CREDITS.md)의 AI 생성 에셋·AI 공개 항목에 반드시 추가한다 (공식 규칙 7(d)). 시안 단계라 아직 CREDITS.md에는 넣지 않았다.
