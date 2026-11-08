# 임시(DRAFT) 스프라이트

플레이 테스트용으로 영웅 걷기·시민 공격 스트립을 게임에 넣었다. 확정 아트가 아니다.

- 에셋: `Assets/Resources/DraftSprites/hero_walk_3f.png`, `citizen_attack_3f.png` (64x64 3프레임, PPU 32, 피벗 아래 중앙).
- 임포트: `Assets/Editor/DraftSpriteImporter.cs`가 해당 폴더만 슬라이스·Point·무압축으로 설정한다.
- 코드: `Assets/Scripts/Core/DraftSprites.cs`(로드·배치), `PlayerController.UpdateVisual`, `TestEnemy.ApplyVisual`/`Awake`.
- 동작: 플레이어는 지상 이동 중 8fps 걷기, 그 외 프레임 0. 시민(Kind.Citizen만)은 Windup=0, Strike=1, Recover=2, 나머지=2. Bug·Android는 기존 사각형.
- 프리팹/씬은 건드리지 않는다. 런타임에 스프라이트·스케일(1)·위치(발 = 콜라이더 바닥)·흰색을 덮어쓴다.

## 끄기 / 제거
- 끄기: `DraftSprites.Use`를 `false`로 (사각형 복귀).
- 완전 제거: `DraftSprites.cs`, `DraftSpriteImporter.cs`, `Assets/Resources/DraftSprites/`를 지우고, `PlayerController`·`TestEnemy`의 `draftSprite` 관련 줄을 삭제.
