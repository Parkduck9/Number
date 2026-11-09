# itch.io 제출 페이지 초안 (붙여넣기용)

> 2026-11-08 재작성 (Claude). 페이지 본문은 **영어**(게임 UI·심사 언어) + 짧은 한국어 소개. 이 머리말과 체크리스트는 사용자 메모.
> 근거: [JAM_RULES.md](../JAM_RULES.md) "제출 페이지 필수 항목"·7(d) AI 공개·8(e) 금지 콘텐츠, [CREDITS.md](../CREDITS.md), [design/GDD.md](design/GDD.md), [design/DECISIONS.md](design/DECISIONS.md) 11장, [review/REVIEW.md](review/REVIEW.md).
> `[TODO: ...]`는 제출 전에 사용자가 채우거나 확인해야 하는 곳이다. 본문의 `---` 아래부터 itch 페이지 설명란에 그대로 붙여 넣는다 (itch는 Markdown을 지원한다).

## 제출 전 체크 (사용자 메모)
- [ ] `[TODO: team names/roles]` 채우기. 전원 만 19세 이상·최근 12개월 Unity 수상 없음 확인 (JAM_RULES 참가 자격). 제출은 Team Lead 계정으로.
- [ ] CREDITS.md와 아래 Credits / AI 공개가 **최종 빌드에 실제로 들어간 것**과 같은지 대조. 드래프트 스프라이트·HUD 시안·UI 아이콘이 빌드에 들어갔으면 "DRAFT" 표현을 지우고, 빠졌으면 그 줄을 지운다.
- [ ] REVIEW 지적: TMP 기본 폰트(LiberationSans, EmojiOne)가 빌드에 남아 있으면 CREDITS.md에 추가하고 아래 Credits에도 한 줄 넣기 (이 문서는 CREDITS.md를 고치지 않는다).
- [ ] itch 설정: Kind = HTML, "This file will be played in the browser" 체크, Viewport 960×540, Fullscreen button 켜기, Mobile friendly 끄기.
- [ ] 스크린샷 3~5장 (오프닝 "숨 0번" 장면 포함), 커버 이미지 630×500.
- [ ] 장르 태그: Action, Platformer, Pixel Art, Cyberpunk, Short, Singleplayer. 입력 태그: Keyboard, Mouse.
- [ ] 금지 콘텐츠 문구 점검 완료: 약물·술·담배 연상 표현 없음 ("dopes" → "overclocks"로 교체됨). 본문을 고칠 때 "doping / high / buzz / drunk" 같은 단어를 쓰지 않는다.

---

# NUMBER

**Read the lucky number in front of every enemy. It tells you exactly how many strikes are coming.**

## About

In a Joseon-styled cyberpunk city, everyone carries a hologram of their "lucky number", and the AI of the tech company **Embector** quietly makes those numbers come true. The city has gone numb to numbers. You haven't.

Every enemy shows a number in front of its face. That number is how many times it will strike. Count the strikes, parry, dash, and punish the opening. Then hack the city's broadcast boards to turn the numbers against the AI, reach the stop button, and decide what happens to the numbers.

- **The number is the fight.** No health bars: the hologram number tells you how long each attack chain is.
- **Broadcast numbers change the whole street.** 3 makes every enemy fast and relentless, 7 overclocks them for seven seconds before they collapse exhausted, 0 makes the city hold its breath.
- **Hack the boards.** Take a broadcast back for a few seconds and flip the fight. The AI will hack it back.
- **Two endings.** At the stop button, you decide.

About 15 minutes. Made in 7 days for the Official Unity Game Jam 2026, theme **"Lucky Number"**. Text in English and Korean (switch in the pause menu).

### 한국어 소개

조선풍 사이버펑크 도시. 모든 적의 얼굴 앞에 뜬 행운의 숫자가 **그 적이 몇 번 연속으로 공격하는지** 알려 준다. 횟수를 세며 패링하고 대시로 피한 뒤, 공격이 끝난 빈틈에 반격하라. 거리의 적은 한 방이지만, 안드로이드는 마지막 일격을 패링해야만 무너진다. 방송 전광판을 해킹해 숫자를 뒤집고, AI 정지 버튼에 닿아 숫자의 운명을 정하라. 약 15분, 엔딩 2개. 영어·한국어 지원.

## The number rule (3 lines)

1. **The number in front of an enemy's face = how many strikes its next attack chain has.** It never counts down. You count.
2. **Parry (left click as a strike or bullet arrives) or dash (Shift) through the strikes.** Just before the last one, the number flares red. When the chain ends, the enemy pauses: that is your opening.
3. **Street enemies drop in one hit whenever you connect. Androids only break when you parry their final strike.** If an Android's number is 7, the 7th strike cannot be parried: dash through it.

## Controls (keyboard + mouse only)

| Action | Key |
|---|---|
| Move | A / D |
| Jump | W |
| Aim | Mouse (a crosshair locks onto an enemy in range) |
| Attack: with a locked enemy within ~5 m, lunge through it and break it; otherwise swing in place | Left mouse |
| Parry: left click while a bullet is incoming or you are inside an enemy's attack (parry comes first) | Left mouse |
| Dodge + counter (invulnerable dash behind the nearest enemy within ~4 m, then turn and strike it; with no enemy near, a fixed-distance dash that ends facing back; 3.5 s cooldown) | Shift |
| Interact / talk / hack a broadcast board | E |
| Shockwave (unlocked at the Luddite hideout, stage 3; staggers enemies in front of you; 5 s cooldown) | Space |
| Pause / language | Esc or P |

- Gamepad is not supported.
- Checkpoints (Luddite marks) heal you fully. Falling at 0 health restarts you at the last checkpoint.
- Your health is Yudo's face in the corner: each hit turns one feature into a machine. Five hits and the chip goes in.

## Playing in the browser

- **Click the game once** so it receives keyboard and mouse input.
- Use the **fullscreen button** under the game for the intended 16:9 view. Press Esc to leave fullscreen.
- Tested in **Chrome and Firefox** (desktop). Other browsers may work but are not tested.
- Unity WebGL build. If the page stays black, wait for the loading bar to finish, then click once.

## Team

`[TODO: team names/roles]` — e.g. (Name) — design, direction, art direction

## Credits

Full list with versions and licenses: `CREDITS.md` in the project repository (summary below).

**Third-party assets**
- Galmuri pixel font — quiple, SIL OFL 1.1 — https://github.com/quiple/galmuri
- Mulmaru pixel font — mushsooni, SIL OFL 1.1 — https://github.com/mushsooni/mulmaru
- pixelfx v0.3.0 (sword slash, parry, hit spark effects, recolored; new effects built on it: number ember, run dust, speed lines, dash burst, kill burst, impact ring, parry clash) — Patch / Hugo Duprez, MIT — https://github.com/patch-gg/pixelfx
- Liberation Sans (TextMesh Pro default font) — Red Hat / Ascender Corp., SIL OFL 1.1 — https://github.com/liberationfonts/liberation-fonts
- EmojiOne (TextMesh Pro default emoji sprites) — EmojiOne, `[TODO: confirm license; or remove the TMP emoji sprite asset from the build]` — https://www.emojione.com/

**Made by the team with generative AI tools** (see disclosure below)
- Backgrounds, dialogue portraits, character sprites, HUD (face health, broadcast numbers, hologram window), boss screen faces and UI icons, plus new combat and movement effects (number ember, run dust, speed lines, dash burst, kill burst, impact ring, parry clash)
- All sound effects and music (synthesized, released CC0 by the team)

Made with Unity 6 (6000.3). No paid generation services were used.

## Generative AI disclosure

This game was made with heavy use of generative AI tools. The human team `[TODO: adjust to what the team actually did]` designed the game (concept, world, rules, every design decision), directed each tool, and reviewed and approved all output before it went into the build.

| Tool | Used for |
|---|---|
| **Claude** (Anthropic) via Claude Code — Fable, Opus and Sonnet models | Game code (player, enemies, number and broadcast rules, hacking, boss, levels, UI, editor tooling); architecture and system design (Fable); code review, planning and implementation (Opus); gameplay implementation (Sonnet); design documents and dialogue drafts; the "number ember", run dust, speed lines and dash burst effects written as code on the pixelfx library; the background pixelization scripts; the audio generator script |
| **Codex CLI** (OpenAI GPT models: GPT-6 Astra / GPT-6.1 Sol / GPT-6 Luna) | Image generation and pixel-art scripts for: the city backgrounds (stage 0/1 layered parts, stages 2-5); hero and enemy sprite frames (pixelized and palette-quantized to the game palette); HUD (broadcast number, hologram window) and UI icons (control hints, shockwave icon); grid-snapping and cleanup of Gemini source images; the kill burst, impact ring and parry clash effects built on pixelfx; asset import settings |
| **Google Gemini** | Source images (made by the user from the team's character art) for the dialogue portraits, the HUD face-health set, the Embector boss screen faces and the background layers; Codex then grid-snapped and cleaned them for the game |
| **Antigravity CLI** (Google) | QA: play-test scenarios, bug hunting and bug reports, build and submission checklist. No game code |
| **Claude-synthesized audio** | All 12 sound effects and 2 music tracks were synthesized from scratch (oscillators, noise, modal bells, plucked-string synthesis; no samples) by a generator script written by Claude Opus. The team released them CC0 |

All tools were used under their terms of service. Combat effects are from the open-source pixelfx library (MIT), recolored or extended. Development-time reference docs (Nice-Wolf-Studio Unity skills) are not part of the build.

`[TODO: before submitting, remove any row or item above whose output did NOT end up in the final build, and add anything new. Keep this table identical to CREDITS.md.]`
