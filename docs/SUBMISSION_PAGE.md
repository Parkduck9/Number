# itch.io 제출 페이지 초안 (붙여넣기용)

> 2026-11-08 재작성, 2026-11-10 갱신, 2026-11-12 주제 문구·새 이야기 앞부분 반영 (Claude: 엔딩 하나, 도사 주인공, 주사위, 부적, 강한 적 대결, 합체 보스, 튜토리얼 반영). 페이지 본문은 **영어**(게임 UI·심사 언어) + 짧은 한국어 소개. 이 머리말과 체크리스트는 사용자 메모.
> 근거: [JAM_RULES.md](../JAM_RULES.md) "제출 페이지 필수 항목"·7(d) AI 공개·8(e) 금지 콘텐츠, [CREDITS.md](../CREDITS.md), [design/GDD.md](design/GDD.md), [design/DECISIONS.md](design/DECISIONS.md) 11장, [review/REVIEW.md](review/REVIEW.md).
> `[TODO: ...]`는 제출 전에 사용자가 채우거나 확인해야 하는 곳이다. 본문의 `---` 아래부터 itch 페이지 설명란에 그대로 붙여 넣는다 (itch는 Markdown을 지원한다).

## 제출 전 체크 (사용자 메모)
- [ ] `[TODO: team names/roles]` 채우기. 전원 만 19세 이상·최근 12개월 Unity 수상 없음 확인 (JAM_RULES 참가 자격). 제출은 Team Lead 계정으로.
- [ ] CREDITS.md와 아래 Credits / AI 공개가 **최종 빌드에 실제로 들어간 것**과 같은지 대조. 드래프트 스프라이트·HUD 시안·UI 아이콘이 빌드에 들어갔으면 "DRAFT" 표현을 지우고, 빠졌으면 그 줄을 지운다.
- [ ] 크레딧은 페이지 안에 전부 적는다. 저장소는 비공개라 투표자가 CREDITS.md를 볼 수 없으므로 저장소·외부 파일 링크로 대신하지 않는다 (2026-11-10 잼 페이지 재확인).
- [ ] REVIEW 지적: TMP 기본 폰트(LiberationSans, EmojiOne)가 빌드에 남아 있으면 CREDITS.md에 추가하고 아래 Credits에도 한 줄 넣기 (이 문서는 CREDITS.md를 고치지 않는다).
- [ ] itch 설정: Kind = HTML, "This file will be played in the browser" 체크, Viewport 960×540, Fullscreen button 켜기, Mobile friendly 끄기.
- [ ] 스크린샷 3~5장 (오프닝 "숨 0번" 장면 포함), 커버 이미지 630×500.
- [ ] 장르 태그: Action, Platformer, Pixel Art, Cyberpunk, Short, Singleplayer. 입력 태그: Keyboard, Mouse.
- [ ] 금지 콘텐츠 문구 점검 완료: 약물·술·담배 연상 표현 없음 ("dopes" → "overclocks"로 교체됨). 본문을 고칠 때 "doping / high / buzz / drunk" 같은 단어를 쓰지 않는다.

---

# NUMBER

**A lucky number is not something you receive. You read it, you build it up, and you roll it yourself.**

## About

Don't freeze at the number the city forces on you. Read every enemy's countdown, build up your luck one cut at a time, and roll the die with your own hand. A 2D pixel action platformer about the numbers others decide, and the one you make.

In a Joseon-styled cyberpunk city, the AI of the tech company **Embector** broadcasts "today's lucky number" and the whole city obeys. You are **Yudo**, a young fortune-teller dosa invited to the city by a secret group that wants the fake luck stopped. A voice on the radio, the **Benefactor**, guides you. The moment you arrive, the broadcast says zero, the city stops breathing, and the guard robots come for you. Every enemy shows a number in front of its face: that is how many times it will strike. Count the strikes, parry, flash past, and punish the opening. Hack the broadcast boards, roll your own lucky die, and break the machine that sells the city its luck.

- **The number is the fight.** The hologram number tells you how long each attack chain is.
- **Broadcast numbers change the whole street.** 3 makes every enemy fast and relentless, 7 overclocks them for seven seconds before they collapse exhausted, 0 makes the city hold its breath.
- **Hack the boards.** Take a broadcast back for a few seconds and flip the fight. The AI will hack it back.
- **Clash duels.** Strong enemies pull you into a zoomed-in duel: block every one of their strikes and you shatter them.
- **Roll your own luck.** Every 7 kills charge your lucky die; it rolls, and the face you get (1 to 6) is the spell you cast.
- **A fused machine boss** fills the whole background: a bug-husk arm, a giant blade arm and a chest gun bank, each telling you its strike count on a hologram.

About 15 minutes. Made for the Official Unity Game Jam 2026, theme **"Lucky Number"**. Text in English and Korean (switch on the title screen or in the pause menu).

### 한국어 소개

**행운의 숫자는 받는 게 아니라, 읽고 쌓아서 직접 굴리는 것.** 도시가 강요하는 숫자에 멈춰 서지 말고, 적의 카운트다운을 읽고, 한 번씩 베어 행운을 쌓고, 내 손으로 주사위를 굴려라. 비밀 조직의 초대를 받아 도시에 온 젊은 도사 유도는 도착하자마자 "오늘의 행운의 숫자는 0"이라는 방송과 함께 숨을 멈춘 도시, 그리고 자신을 노리는 경비 로봇과 마주친다. 무전 너머의 귀인이 길을 짚어 준다.

조선풍 사이버펑크 도시. 모든 적의 얼굴 앞에 뜬 숫자가 **그 적이 몇 번 연속으로 공격하는지** 알려 준다. 횟수를 세며 패링하고, 일섬으로 스쳐 지나가고, 공격이 끝난 빈틈에 반격하라. 방송 전광판을 해킹해 숫자를 뒤집고, 7번 처치해 채운 행운의 주사위로 도술을 쓰고, 도시의 행운을 파는 거대한 기계를 부숴라. 약 15분. 영어·한국어 지원.

## The number rule (3 lines)

1. **The number in front of an enemy's face = how many strikes its next attack chain has.** It never counts down. You count.
2. **Parry (left click as a strike or bullet arrives).** Just before the last strike, the number flares red. When the chain ends, the enemy pauses: that is your opening.
3. **Street enemies drop in one hit. Androids only break when you parry their final strike.**

## Controls (keyboard + mouse only)

| Action | Key |
|---|---|
| Move | A / D |
| Jump | Space |
| Drop through a ledge | S + Space |
| Aim | Mouse (a crosshair locks onto an enemy in range) |
| Attack: lunge through a locked enemy nearby, otherwise swing in place | Left mouse |
| Parry: left click as a strike or bullet arrives | Left mouse |
| Issen (flash strike): flash behind an enemy roughly level with you; human enemies drop in one hit; 2.5 s cooldown | Shift |
| Grapple rope: throw a rope toward the mouse and get pulled there; swing on it | Right mouse |
| Interact / talk / hack a broadcast board | E (when near) |
| Pause / language | Esc or P |

- Gamepad is not supported.
- Health: 5 hearts beside Yudo's face. Checkpoints (Luddite marks) heal you fully; at 0 health you restart at the last checkpoint.
- A short step-by-step tutorial comes right after the prologue.

## Playing in the browser

- **Click the game once** so it receives keyboard and mouse input.
- Use the **fullscreen button** under the game for the intended 16:9 view. Press Esc to leave fullscreen.
- Tested in **Chrome** (desktop). Other browsers may work but are not tested.
- Unity WebGL build. If the page stays black, wait for the loading bar to finish, then click once.

## Team

- **NIPAD** — solo developer: game design, direction, art direction, playtesting

## Credits

Every asset in the build is listed here.

**Fonts and effects**
- Galmuri9 / Galmuri11 / Galmuri14 pixel fonts v2.40.4 — quiple — SIL OFL 1.1 — https://github.com/quiple/galmuri
- Mulmaru pixel font v1.0 — mushsooni — SIL OFL 1.1 — https://github.com/mushsooni/mulmaru
- Liberation Sans (TextMesh Pro default font) — Red Hat, Inc. / Ascender Corp. — SIL OFL 1.1
- pixelfx v0.3.0 — Patch / Hugo Duprez — MIT — https://github.com/patch-gg/pixelfx — sword slash, parry and hit spark sheets, and the base library for our other effects

**Music (CC0)**
- Field and boss music: "Sketchbook 2024-12-21" and "Sketchbook 2024-02-14 (L03)" from the Music Loop Bundle by Abstraction (Tallbeard Studios) — CC0 1.0 — https://tallbeard.itch.io/music-loop-bundle

**Sound effects (CC0 recordings layered with our own synthesis)**
- 20 Sword Sound Effects — StarNinjas — CC0 1.0 — https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes
- Impact Sounds, Interface Sounds, RPG Audio, Sci-fi Sounds — Kenney — CC0 1.0 — https://kenney.nl
- 75 CC0 breaking / falling / hit sfx — rubberduck — CC0 1.0 — https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx

**Made by the team with generative AI tools** (see the disclosure below)
- Characters: Yudo, the Luddite helper, citizens, androids, snipers, AI bugs, projectiles
- The fused machine boss and the machines smashed in the ending
- Backgrounds and map parts for every stage, title key art and itch cover
- Dialogue portraits (incl. the Benefactor), Embector broadcast screens, ending illustration
- HUD and UI: face and hearts, broadcast and hologram numbers, lucky die, UI kit, control hints, NUMBER logo, checkpoint marks
- Effects built on pixelfx, and code-drawn effects (parry ring, clash duel, boss swarm, danger zones, hit flash)
- Sound effect synthesis layered under the CC0 recordings above

Made with Unity 6 (6000.3). No paid generation services were used.

## Generative AI disclosure

This game was made with heavy use of generative AI tools. The solo developer (NIPAD) designed the game (concept, world, rules, every design decision), made the source images in Google Gemini, playtested every build, directed each tool, and reviewed and approved all output before it went into the build.

| Tool | Used for |
|---|---|
| **Claude** (Anthropic) via Claude Code — Fable, Opus, Sonnet | Game code (player, enemies, number and broadcast rules, hacking, boss, levels, tutorial, UI, editor tooling), design documents and dialogue drafts, code-drawn and pixelfx-based effects, background pixelization scripts, driving Google Gemini and Codex |
| **Codex CLI** (OpenAI GPT-6 Astra / GPT-6.1 Sol / GPT-6 Luna) | Sprites, backgrounds and map parts (image generation and pixel-art scripts), HUD/UI art, logo, key art cleanup, cleanup of Gemini source images, pixelfx effects, sound effect synthesis and editing of the CC0 recordings, dialogue proposals, asset import settings |
| **Google Gemini** | Source images for dialogue portraits, the HUD face set, boss screens, sniper/projectile sheets, backgrounds, key art, machine props and the ending illustration; Codex cleaned them to native pixel size |
| **Antigravity CLI** (Google) | QA: test scenarios, bug reports, build and submission checklist. No game code |

All tools were used under their terms of service.
