# Audio v2 draft provenance

AI 생성 (Codex / OpenAI GPT), authored as deterministic offline Node.js DSP and composition.

All twelve sound effects and both music loops in this directory were synthesized from oscillators, seeded noise, modal resonators, fractional-delay Karplus–Strong strings, and digital filters. No downloaded samples, external instrument recordings, online generation services, paid APIs, or new dependencies were used. Node.js standard library only.

The existing team generator at `Assets/Audio/Source/gen-audio.mjs.txt` was read as a reference for file mapping and baseline synthesis. These drafts use a new arrangement and instrument implementation. Korean instrument names describe the intended timbre; these are synthetic approximations, not recordings or claims of traditional performance authenticity. The heavy boss buk is still a Korean-inspired drum voice.

Authoring tool: Codex (OpenAI GPT). Generation runtime: local Node.js. Existing project credits state the team's synthesized audio is CC0; these new drafts follow that project policy, subject to team adoption.

Drafts are not imported or used by the game. On adoption, copy this disclosure into the repository's root CREDITS.md and the jam submission's generative AI disclosure. Root CREDITS.md was deliberately left unchanged because this task authorizes writes only inside `docs/design/concept/audio_v2/`.
