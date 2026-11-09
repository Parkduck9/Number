# Animation v2 — staging review

기존 DraftSprites 캐릭터의 중간 포즈 작업입니다. 게임에 적용하지 않은 검토용 결과입니다.

모든 PNG는 가로 스트립이며 셀 크기는 **64 × 64**입니다. 인덱스는 **0부터** 시작하며 왼쪽에서 오른쪽 순서입니다. 아래 키 위치에 기존 프레임을 순서대로 픽셀 그대로 보존했습니다.

| 새 PNG | 프레임 | 목표 FPS | 원본 → 새 키 인덱스 | 재생 |
|---|---:|---:|---|---|
| [hero_idle_4f.png](hero_idle_4f.png) | 4 | 4 | hero_idle_2f: 0→0, 1→2 | 루프 |
| [hero_walk_6f.png](hero_walk_6f.png) | 6 | 16 | hero_walk_3f: 0→0, 1→2, 2→4 | 루프 |
| [hero_attack_5f.png](hero_attack_5f.png) | 5 | 15 | hero_attack_3f: 0→0, 1→2, 2→4 | 단발 |
| [hero_jump_4f.png](hero_jump_4f.png) | 4 | 8 | hero_jump_2f: 0→0, 1→3 | 상승 0–1 / 하강 2–3 |
| [hero_dash_3f.png](hero_dash_3f.png) | 3 | 12 | hero_dash_2f: 0→0, 1→2 | 단발 |
| [luddite_npc_idle_4f.png](luddite_npc_idle_4f.png) | 4 | 4 | luddite_npc_idle_2f: 0→0, 1→2 | 루프 |

## 비교 GIF

- [Hero idle](hero_idle_4f_preview.gif)
- [Hero walk](hero_walk_6f_preview.gif)
- [Hero attack](hero_attack_5f_preview.gif)
- [Hero jump](hero_jump_4f_preview.gif)
- [Hero dash](hero_dash_3f_preview.gif)
- [Luddite NPC idle](luddite_npc_idle_4f_preview.gif)

왼쪽 OLD / 오른쪽 NEW. 배경 #0B1026, 캐릭터는 최근접 방식으로 정확히 4배 확대했습니다. 전체 GIF 크기는 512 × 280이며 위 24px는 제목 영역입니다. 모든 GIF는 반복됩니다.

루프는 양쪽 모두 표의 FPS로 재생하므로 원본은 프레임 수에 비례해 더 짧은 주기를 가집니다. 공격·대시는 원본이 먼저 끝나면 마지막 키를 유지한 뒤 새 스트립과 함께 다시 시작합니다. 단발 동작의 마지막→첫 포즈는 루프용으로 연결하지 않았습니다. 점프는 OLD 상승/하강 키를 각각 2틱 유지하고, NEW는 rise → rise-to-apex → apex-to-fall → fall 순서로 8 FPS 재생합니다.

GIF의 10ms 시간 단위 때문에 프레임 지연을 번갈아 배치합니다. 걷기는 두 주기 12프레임/0.75초, 공격은 세 주기 15프레임/1초로 저장하여 평균 FPS를 정확히 맞췄습니다. 나머지도 각각 4, 8, 12 FPS를 확인했습니다.

## 픽셀·임포트 기준

- 여섯 원본 .meta 모두 PPU **32**, 개별 슬라이스 custom pivot **(0.5, 0.03125)**입니다. 새 파일을 향후 임포트할 때 같은 값을 사용해야 합니다. PNG 자체에는 Unity 피벗이 저장되지 않으며 이번 작업은 .meta를 만들거나 수정하지 않았습니다.
- 지면 픽셀은 위에서 세는 0 기반 y 좌표로 Hero의 지상 동작 **61**, NPC **62**를 유지합니다. 점프 원본은 상승 키의 최하단이 53, 하강 키가 61이므로 공중 자세의 발 위치는 변화합니다. 새 점프의 최하단은 53 / 54 / 59 / 61이며 캔버스와 피벗은 고정입니다.
- 키 사이와 루프의 마지막 키→첫 키 사이에 중간 포즈를 추가했습니다. 대기는 어깨 움직임과 머리카락·옷자락의 작은 시차, 걷기는 보폭·소매 위치, 공격은 예비 동작과 팔 회수, 점프는 몸통·다리 전환, 대시는 기울기·보폭 전환을 조정했습니다.
- 도구: Codex의 포즈별 정수 픽셀 좌표 편집과 JavaScript PNG/GIF 인코딩. 기존 팔레트 픽셀을 이동하고 연결부 픽셀을 수정했습니다. 색 보간, 알파 크로스페이드, 회전/확대 리샘플링은 PNG에 사용하지 않았습니다. 새 외부 에셋은 없습니다.

## 확인 결과

- 6개 스트립의 크기·프레임 수, 모든 원본 키의 RGBA 일치, 각 원본 스트립 팔레트에 대한 부분집합, 알파 0/255를 검사했습니다.
- 1배 및 4배 이미지에서 포즈와 연결부를 검토했습니다. 모든 프레임의 불투명 픽셀은 8방향 기준으로 하나의 연결 성분입니다.
- 별도 System.Drawing GIF 디코더로 6개 GIF의 크기·프레임 지연·평균 FPS를 확인했습니다.
- Assets, 스크립트, 씬, 프리팹, CREDITS.md를 수정하지 않았으며 커밋하지 않았습니다. git 실행 파일을 찾지 못하여 작업 전 git pull은 실행되지 않았습니다.
- Unity Play 모드 검증은 하지 않았습니다. 실제 적용 시 걷기 루프 경계, 공격 키 2의 타격 시점, 점프의 상승/하강 쌍 전환을 게임 속도에서 확인해야 합니다.

## 별도 작업 문서

같은 폴더의 적 캐릭터 작업은 [README_enemies.md](README_enemies.md)에 기록되어 있습니다. 위 표와 검증 결과는 이번 Hero/NPC 6종에 해당합니다.
