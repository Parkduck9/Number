# itch.io 제출 페이지 초안

> 2026-11-08 초안 (Claude). 페이지 본문은 **영어**(게임 UI·심사 언어), 아래 한국어는 사용자 메모.
> 근거: [JAM_RULES.md](../JAM_RULES.md) "제출 페이지 필수 항목", [CREDITS.md](../CREDITS.md), [design/GDD.md](design/GDD.md), [design/DECISIONS.md](design/DECISIONS.md).
> `⚠ 확인` 표시는 제출 전에 사용자가 사실을 확인하거나 채워야 하는 곳이다.

## 제출 전 체크 (사용자 메모)
- [ ] 팀원 이름·역할 채우기, 전원 만 19세 이상·최근 12개월 Unity 수상 없음 확인 (JAM_RULES 참가 자격)
- [ ] 아직 안 들어간 에셋(오디오·HUD·배경·스프라이트)이 들어가면 Credits와 AI 공개를 그에 맞게 고치기
- [x] STG-00·STG-01 배경(`PLACEHOLDER_stg01_segA_bg.png`)은 Codex로 만든 AI 이미지 → CREDITS와 아래 AI 공개에 추가함 (QA BUILD C01). 최종 아트로 교체하면 이 줄들을 고칠 것
- [ ] itch 설정: Kind = HTML, "This file will be played in the browser" 체크, 화면 크기 960×540 (전체화면 버튼 켜기)
- [ ] 스크린샷 3~5장, 커버 이미지 630×500
- [ ] 장르 태그: Action, Platformer, Pixel Art, Cyberpunk, Short

---

## Page text (English)

### Title
**NUMBER**

### Tagline (short description)
Read the lucky number above every enemy. It tells you exactly how many times they will strike.

### Description
In a Joseon-styled cyberpunk city, everyone carries a hologram of their "lucky number", and the AI of the tech company **Embector** quietly makes those numbers come true. The city has gone numb to numbers. You haven't.

Every enemy shows a number in front of its face. **That number is how many strikes are coming.** Street enemies drop in one hit, if you hit first. Can't? Count the strikes, parry, dash. Androids only break when you parry their last strike, so you have to count to the end. Then hack the city's broadcast boards to turn the numbers against the AI, reach the stop button, and decide what happens to the numbers.

- **The number is the fight.** No health bars to read: count the strikes, parry, and punish the opening.
- **Broadcast numbers change the whole street.** 3 makes enemies fast and relentless, 7 overclocks them before they burn out, 0 makes the city hold its breath.
- **Hack the boards.** Take a broadcast back for a few seconds and flip the fight.
- **Two endings.** At the stop button, you decide.

About 10–15 minutes. Made in 7 days for the Official Unity Game Jam 2026, theme **"Lucky Number"**.

### Controls
| Action | Key |
|---|---|
| Move | A / D |
| Jump | W |
| Attack | Left mouse |
| Parry | Right mouse |
| Dash (dodge, 2 s cooldown) | Shift |
| Interact / hack a broadcast board | E |
| Shockwave (unlocked during the story) | Space |
| Pause | Esc or P |

Tip: an enemy's final strike glows red. A **7th** strike from a strong enemy can't be parried, so dash.
Language: English / 한국어 (Pause menu).
Keyboard and mouse only (no gamepad support).

### Credits
**Team** ⚠ 확인
- (Name) — (role)

**Third-party assets**
- Galmuri pixel font — quiple, SIL OFL 1.1 — https://github.com/quiple/galmuri
- Mulmaru pixel font — mushsooni, SIL OFL 1.1 — https://github.com/mushsooni/mulmaru
- pixelfx (sword slash, parry, hit spark effects) — Patch / Hugo Duprez, MIT — https://github.com/patch-gg/pixelfx
- (audio, art and other assets added later: copy from CREDITS.md) ⚠ 확인

Made with Unity 6.

### Generative AI disclosure
This game was made with heavy use of generative AI tools. The human team designed the game (concept, world, rules, decisions on every option) and directed, reviewed and approved all output. ⚠ 확인 (사람이 한 일을 실제에 맞게 고칠 것)

| Tool | What it was used for |
|---|---|
| Claude (Anthropic) via Claude Code — Fable, Opus, Sonnet models | Writing most of the game code (player, enemies, broadcast rules, boss, levels, UI), architecture and code review, design documents, the "number ember" warning flame effect (written as code in the pixelfx library) |
| Codex CLI (OpenAI GPT models) | Concept art and the city background used in the opening and first stage (AI-generated image), pixel art assets and asset setup ⚠ 확인 (최종 빌드에 들어간 것만: HUD, 배경, 스프라이트) |
| Antigravity CLI (Google Gemini) | QA: bug hunting and bug reports (no game code) |
| (Audio) | Sound effects / music made with a code-based sound generator written by Claude ⚠ 확인 (오디오 승인 결과에 맞게: 직접 만든 곡 vs CC0 곡) |

All AI tools were used under their terms of service; no paid generation services were used. Combat effects are from the open-source pixelfx library (MIT) and recolored or extended.

---

## 한국어 요약 (페이지에 한국어 설명을 함께 넣을 경우)
조선풍 사이버펑크 도시. 모든 적의 얼굴 앞에 뜬 행운의 숫자가 **다음 공격 횟수**를 알려 준다. 거리의 적은 먼저 치면 한 방. 못 치면 숫자를 세며 패링하고 대시로 피하라. 안드로이드는 마지막 일격을 패링해야만 무너진다. 방송 전광판을 해킹해 숫자를 뒤집고, AI 정지 버튼에 닿아 숫자의 운명을 정하라. 약 10~15분, 엔딩 2개.
