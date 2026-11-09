# Credits

제출 페이지 크레딧과 AI 공개 문구의 원본. 에셋을 추가할 때마다 바로 기록한다.

## Team
- (팀원 이름 / 역할)

## Third-party assets
| 에셋 | 제작자 | 출처 URL | 라이선스 | 사용 위치 |
|---|---|---|---|---|
| Galmuri9 / Galmuri11 / Galmuri14 (pixel font, EN+KO) v2.40.4 | quiple | https://github.com/quiple/galmuri | SIL OFL 1.1 | UI text (Assets/Fonts/Galmuri/) |
| Mulmaru (물마루, pixel font, EN+KO) v1.0 | mushsooni | https://github.com/mushsooni/mulmaru | SIL OFL 1.1 | UI text (Assets/Fonts/Mulmaru/) |
| pixelfx v0.3.0 — sword-slash (recolored cyan), parry, hit-spark sprite sheets | Patch / Hugo Duprez | https://github.com/patch-gg/pixelfx | MIT | Combat FX (Assets/Resources/FX/) |

## AI-generated assets
| 에셋 | 도구 | 사용 위치 |
|---|---|---|
| STG-00 / STG-01 SEG-A background (PLACEHOLDER_stg01_segA_bg.png, copy of concept image 14_bg_stg01_pixel_native.png) — temporary, to be replaced or kept by user decision | Codex (OpenAI GPT image generation) | Assets/Art/Placeholder/, STG00 / STG01 scenes |
| Hero walk / Citizen attack DRAFT sprites, 3 frames each (hero_walk_3f.png, citizen_attack_3f.png) — Citizen attack regenerated from the approved front-facing enemy reference samples/enemy_citizen_stand_big.png as right-facing action poses; unchanged hero follows samples/hero_stand_big_b.png | AI 생성 (Codex, OpenAI GPT image generation), pixelized to 64x64 frames and quantized to number_palette without dithering; enemy scale compared with hero_idle_2f | Assets/Art/Drafts/Sprites/, Assets/Resources/DraftSprites/ |
| Hero idle / attack / dash / hurt DRAFT sprites (hero_idle_2f, hero_attack_3f, hero_dash_2f, hero_hurt_1f) — regenerated from the original concept lineup reference and primary samples/hero_stand_big_b.png, with selected candidate B idle references | AI 생성 (Codex, OpenAI GPT image generation), pixelized to 64x64 frames and quantized to number_palette without dithering | Assets/Art/Drafts/Sprites/; existing in-game drafts in Assets/Resources/DraftSprites/ |
| Android attack / Citizen B ranged / Luddite NPC idle DRAFT sprites (android_attack_3f, citizen_b_ranged_3f, luddite_npc_idle_2f) — Android and Citizen B regenerated from the approved front-facing enemy references samples/enemy_android_stand_big.png and samples/enemy_citizen_b_ranged_stand_big.png as right-facing action poses, preserving their exact armor and hooded outfit; Luddite retained unchanged | AI 생성 (Codex, OpenAI GPT image generation for regenerated sprites), pixelized to 64x64 frames and quantized to number_palette without dithering; enemy scale compared with hero_idle_2f; original Luddite by Codex gpt-6.1-sol | Assets/Art/Drafts/Sprites/; existing in-game drafts in Assets/Resources/DraftSprites/ |
| Android overload (stunned) redraw DRAFTS A/B (android_overload_A/B.png, 64x64, palette-quantized) — drafts, not in game, user chooses | Codex (gpt-6.1-sol, then palette-quantized with a Python script) | Assets/Art/Drafts/Sprites/ |
| number-ember (last-strike red flame on enemy numbers) — new effect written by Claude on pixelfx (MIT, Patch / Hugo Duprez; flame-tongue code adapted from Sprite Fusion fire, MIT, Hugo Duprez). Source: Assets/Art/FX/Source/number-ember.js.txt | Claude Opus (Claude Code) + pixelfx | Assets/Resources/FX/number-ember.png |
| run-dust, speed-lines, dash-burst (movement speed FX) — new effects authored by Claude on pixelfx core (MIT, Patch / Hugo Duprez); palette-quantized, alpha 0/255. Docs: docs/design/concept/fx_speed/ | Claude (Claude Code) + pixelfx | Assets/Resources/FX/run-dust.png, speed-lines.png, dash-burst.png |
| Control-hint stencil icons (8 blank-key / mouse DRAFTs), strip and 4x previews — 손으로 설계한 픽셀 배열, Python 표준 라이브러리 생성 | AI 생성 (Codex gpt-6.1-sol) | Assets/Art/Drafts/UI/ctrl_*.png, in game via Assets/Resources/DraftUi/; source: Assets/Art/Drafts/UI/_gen/gen_ui_icons.py |
| Shockwave HUD ready / cooldown DRAFT icons and 4x preview — 링 버스트, 팔레트 제한 | AI 생성 (Codex gpt-6.1-sol) | Assets/Art/Drafts/UI/shockwave_*.png; source: Assets/Art/Drafts/UI/_gen/gen_ui_icons.py |
| Embector AI broadcast-screen idle / angry DRAFTs and 4x preview — 기하학 단일 눈, 텍스트·로고 없음 | AI 생성 (Codex gpt-6.1-sol) | Assets/Art/Drafts/UI/boss_embector_*.png; source: Assets/Art/Drafts/UI/_gen/gen_ui_icons.py |
| Sound effects ×12 + BGM ×2 (field, boss) — synthesized from scratch by a generator script Claude wrote (oscillators/noise, modal bells, Karplus-Strong plucks; no samples). Released CC0 by the team. Source: Assets/Audio/Source/gen-audio.mjs.txt | Claude Opus (Claude Code) | Assets/Audio/Resources/Sfx/, Assets/Audio/Resources/Bgm/ |
| HUD 시안: 얼굴 체력 6단계, 방송 숫자, 홀로그램 숫자 창 (게임에 연결됨, 테스트용) | Codex CLI (GPT-6.1 Sol)가 쓴 Python 스크립트로 픽셀을 직접 찍음, Claude Opus 검토 | Assets/Art/Drafts/HUD/, in game via Assets/Resources/DraftHud/ |
| Missing-state DRAFT sprites (hero_jump_2f, hero_parry_2f, citizen_walk_2f, citizen_stunned_1f, android_walk_2f, android_stunned_1f, citizen_b_walk_2f) — enemy walks and stunned poses regenerated from the approved front-facing enemy references samples/enemy_citizen_stand_big.png, samples/enemy_android_stand_big.png and samples/enemy_citizen_b_ranged_stand_big.png; citizen sits dazed and android kneels overloaded; unchanged hero follows samples/hero_stand_big_b.png | AI 생성 (Codex, OpenAI GPT image generation), pixelized to 64x64 frames and quantized to number_palette without dithering; enemy scale compared with hero_idle_2f | Assets/Art/Drafts/Sprites/; existing in-game drafts in Assets/Resources/DraftSprites/ |
| Backgrounds STG-02 to STG-05 (bg_stg02_segA_rooftops, bg_stg02_segB_elevated_rail, bg_stg03_hideout_room, bg_stg04_segA_production_line, bg_stg04_segB_inspection_hall, bg_stg05_boss_room; 32-color pixel art) — in game | Codex (gpt-6.1-sol image generation) + Claude script pixelization (median filter, box downscale, 32-color quantize) | Assets/Art/Backgrounds/, STG02-STG05 scenes |

## Generative AI disclosure (draft)
- Claude Fable (Anthropic, via Claude Code) — architecture and system design
- Claude Sonnet (Anthropic, via Claude Code) — gameplay code implementation
- Claude Opus (Anthropic, via Claude Code) — code review and planning
- Codex CLI (OpenAI GPT-6 Astra / GPT-6.1 Sol / GPT-6 Luna) — concept art and background image generation (the STG-00/STG-01 background in the build is AI-generated), asset creation and asset setup
- Antigravity CLI (Google) — QA, testing and bug reports
