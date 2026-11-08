검증: CONFIRMED (플레이어 flipX 설정 코드 없음, 전체 Grep에서 TestEnemy.cs:411만 존재)
중복: BUG-COMBAT-A09-attack-dash-cancel-ignores-move-direction.md (일부)

# BUG-COMBAT-C03 — 플레이어 스프라이트가 바라보는 방향으로 뒤집히지 않는다 (패링 방향을 볼 수 없음)

**영역:** COMBAT (Facing, 패링 방향 조건)
**관련 규칙:** PROTOTYPE_ARCH 7절 Q-I `[확정]` "패링 방향 조건: 바라보는 방향 180°", 2절 "바라보는 방향 = 마지막 0 아닌 MoveX"

## 재현 단계
1. 아무 스테이지에서 A를 눌러 왼쪽을 본 뒤 손을 뗀다.
2. 플레이어 그림을 본다. 이어서 D를 눌러 오른쪽을 본 뒤 비교한다.
3. 오른쪽에서 오는 적 공격을, 마지막에 왼쪽을 눌렀던 상태로 패링한다.

## 기대 결과
플레이어 그림이 바라보는 방향으로 뒤집혀서, 지금 앞쪽이 어디인지(=패링이 되는 쪽) 보인다.

## 실제 결과 (코드 근거)
- `Facing`은 `PlayerController.cs:156`에서만 바뀌고, 어디에서도 `visual.flipX`나 스케일에 반영되지 않는다. `UpdateVisual`은 색만 바꾼다 (`Assets/Scripts/Player/PlayerController.cs:284-295`).
- 프리팹 Visual에는 Transform + SpriteRenderer만 있고 (`Assets/Prefabs/Prototype/Player.prefab`, `m_FlipX: 0`), 방향을 바꾸는 다른 스크립트도 없다 (Assets 전체에서 플레이어 flipX 설정 없음. 적은 `TestEnemy.cs:411`에서 함).
- 지금은 좌우 대칭 사각형이라 방향이 전혀 안 보이고, 공격할 때 슬래시 이펙트로만 알 수 있다. 등 뒤 공격은 패링 창이어도 맞으므로 (`PlayerController.cs:261-262`), 플레이어는 왜 패링이 실패했는지 알 수 없다.
- 나중에 비대칭 캐릭터 그림을 넣어도 코드가 뒤집지 않으므로 계속 한쪽만 본다. 대시 잔상도 `source.flipX`를 복사하므로 (`Assets/Scripts/Combat/SpriteFx.cs:93`) 같이 틀린다.

## 심각도
중간 — 핵심 규칙(앞쪽만 패링)을 화면에서 읽을 수 없다. 아트 연결 시 반드시 터진다.

## 수정 제안 (참고)
`UpdateVisual`에서 `visual.flipX = Facing < 0;` 한 줄(원본 그림이 오른쪽을 본다는 가정). 아트 단계 전까지는 임시로 얼굴 쪽 표시(작은 사각형 등)를 두는 것도 방법.

## 확신도
높음 (코드). 다른 영역에서 방향 표시를 따로 계획 중이라면 중복일 수 있음.
