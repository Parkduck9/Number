# 임시(DRAFT) 아트 연결 (스프라이트 · HUD · UI)

플레이 테스트용으로 시안 아트를 전부 게임에 연결했다. 확정 아트가 아니다. 원본은 `Assets/Art/Drafts/`에 그대로 두고, 게임이 읽는 복사본만 `Assets/Resources/`에 있다.

## 스프라이트 스트립 — `Assets/Resources/DraftSprites/<이름>_<N>f.png`
64x64 N프레임, PPU 32, 피벗 아래 중앙. 프레임 수는 파일 이름 끝의 `_Nf`에서 읽는다 (`DraftSpriteImporter`).

| 대상 | 스트립 | 쓰는 상태 |
|---|---|---|
| 플레이어 (`PlayerController.DraftFrame`) | hero_walk_3f / idle_2f / attack_3f / dash_2f / hurt_1f / jump_2f / parry_2f | 걷기 8fps / 제자리 2fps / 공격 0.3초를 3등분 / 대시 / 피격·사망 / 공중(0 상승, 1 하강) / 패링(0 판정 중, 1 이후) |
| 시민 (Kind.Citizen) | citizen_attack_3f, citizen_walk_2f, citizen_stunned_1f | Windup=0, Strike=1, 그 외=2 / Approach 이동 중 걷기 / Stunned |
| 원거리 시민 (`kind` Citizen + `ranged`, 금색 1) | citizen_b_ranged_3f, citizen_b_walk_2f (기절은 시민 것) | 시민과 같음. 원거리 벌레(`kind` Bug + `ranged`)는 초안 그림 없이 Bug 스웜으로 그린다 (DECISIONS 14) |
| 안드로이드 | android_attack_3f, android_walk_2f, android_stunned_1f | Windup·Special=0, Strike=1, 그 외=2 / 걷기 / Stunned·Overload |
| 러다이트 NPC (STG-03) | luddite_npc_idle_2f | `DraftIdleAnim`(Stg03Builder가 붙임), 2fps, 좌우 반전 |

- 플레이어는 `flipX = Facing < 0`로 좌우를 뒤집는다 (아트는 오른쪽을 본다). Bug(근접·원거리 모두)는 스웜 (`TestEnemy`가 Bug에는 초안 그림을 걸지 않는다).
- 프리팹/씬은 건드리지 않고 런타임에 스프라이트·스케일(1)·위치(발 = 콜라이더 바닥)·흰색을 덮어쓴다.

## HUD — `Assets/Resources/DraftHud/` (`DraftHud.cs`가 읽음)
- 얼굴 체력: `face_0..5` → `FaceHealthHud` (96px = 2배, 단계 이름 라벨은 숨김).
- 방송 숫자: `broadcast_digits_magenta`(AI) / `_cyan`(해킹) + `broadcast_arcs_*`(왼·오른 파동) → `BroadcastHackUi`, 3배. TMP `((( N )))`는 그림이 없을 때만.
- 적 숫자: `holo_window_gold/red` + `holo_digits_gold/red` → `EnemyNumber`. 원거리 금색, 근접 빨강. 마지막 타 붉은 불꽃은 그대로, 숫자 창은 붉게 물든다. 그림이 없으면 TMP 숫자.

## UI — `Assets/Resources/DraftUi/`
- 충격파: `shockwave_icon` / `_cooldown` 48px, 옆에 SPACE 또는 남은 초 (`Shockwave.BuildHud`).
- 보스: `boss_embector_idle` / `_angry` — 화면 왼쪽 위, 웨이브 시작~클리어 동안 angry (`BossBoardView`).
- 조작 힌트: `ControlHints` — STG00(A D / W / Shift), STG01(좌·우 클릭) 하단 중앙에 10초(조작 가능한 시간), 런당 한 번. 타이틀·일시정지의 Controls 패널(글자)은 그대로.

## 끄기 / 제거
- 끄기: `DraftSprites.Use`를 `false`로 (사각형·TMP로 복귀. HUD·UI 그림도 함께 꺼진다).
- 완전 제거: `DraftSprites.cs`, `DraftHud.cs`, `DraftIdleAnim.cs`, `ControlHints.cs`, `DraftSpriteImporter.cs`, `Assets/Resources/Draft*/`를 지우고 `PlayerController`·`TestEnemy`·`EnemyNumber`·`FaceHealthHud`·`BroadcastHackUi`·`Shockwave`·`BossBoardView`·`BugSwarmVisual`의 draft 관련 줄을 삭제.
