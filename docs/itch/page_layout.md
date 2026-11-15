# itch 페이지 꾸미기 순서 (2026-11-15)

이미지: `docs/itch/page_art/`
글: `docs/itch/description.html` (지금 itch에 들어가 있는 설명문)

itch Edit game → Description 편집기에서, 아래 순서대로 **이미지를 넣고 → 그 아래에 원래 글**을 둔다.
이미지 넣기: 편집기 툴바의 그림 버튼(Insert image) → 업로드. 커서가 있는 자리에 들어간다.
기존 글의 소제목(About, The number rule, Controls, Team, Credits)은 이미지가 대신하니 지워도 된다.

| 순서 | 이미지 | 그 아래 글 |
|---|---|---|
| 1 | (없음) | 맨 위 한 줄: **A lucky number is not something you receive...** |
| 2 | `g4_parry.gif` | (글 없이 바로 다음) — 첫 화면에 실제 플레이가 보이게 |
| 3 | `h1_about.png` | About 문단 2개 + 아래 "0" 설명 한 줄 + 특징 목록 + "About 15 minutes..." + 한국어 소개 |
| 4 | `h2_number_rule.png` | The number rule 3줄 |
| 5 | `h3_how_to_fight.png` | `g2_strike.gif` → "Left click: lunge through a locked enemy" / `g3_issen.gif` → "Shift: issen flash strike" / `g5_rope.gif` → "Right click: grapple rope" / `g6_dice.gif` → "Every 7 kills: your lucky die rolls" (GIF 하나에 설명 한 줄씩) |
| 6 | `h4_controls.png` | 조작 표 + 아래 목록 + Playing in the browser 목록 |
| 7 | `h5_characters.png` | `c_characters.png` (글 없음, 또는 한 줄씩 소개) |
| 8 | `h6_credits.png` | Team + Credits + Generative AI disclosure 전부 |

## About에 추가할 "0" 설명 (댓글 피드백 반영)
```
The opening broadcast number is 0: the broadcast checks that everyone follows the lucky number, and you don't, so the guard robots attack you.
첫 방송의 숫자는 0. 방송은 모두가 행운의 숫자를 따르는지 확인하고, 따르지 않는 유도를 경비 로봇이 공격한다.
```

## Edit theme 색 (게임 팔레트)
| 항목 | 색 |
|---|---|
| Background | `#16142C` (남색) |
| Background 2 (본문 칸) | `#1E1B3A` |
| Text | `#E8E6F0` |
| Link | `#FFD34A` (로고 노랑) |
| Button | `#60E8F0` (청록) |
| Banner | `banner_960x227_logo.png` |

## 파일 크기
GIF 5개는 0.5~0.9MB, 제목 이미지는 각 4KB 이하. 전부 실제 게임 화면(튜토리얼 영상)과 게임 안의 폰트·아이콘·초상화로 만들었다.
