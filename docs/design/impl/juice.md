# 타격감(juice) 구현 메모

모든 수치는 `Assets/Scripts/Core/Juice.cs`의 const 한 곳에 모여 있다. 값을 0으로 두면 해당 효과가 꺼진다.

| 이름 | 파일 | 값 | 설명 |
|---|---|---|---|
| ShakeHitAmp / ShakeHitTime | Juice.cs | 0.04 / 0.08 | 플레이어 타격 성공 흔들림 (월드 단위, 1px = 1/32) |
| ShakeParryAmp / Time | Juice.cs | 0.1 / 0.15 | 패링 (11-09: 0.07 / 0.12) |
| ShakeHurtAmp / Time | Juice.cs | 0.12 / 0.2 | 플레이어 피격 |
| ShakeKillAmp / Time | Juice.cs | 0.14 / 0.22 | 적을 쓰러뜨린 타격. 타격 방향으로 밀림 (`ShakeKick` 0.6) |
| HitStopHit | Juice.cs | 0.05 s | 일반 타격 히트스톱 (11-09: 0.04) |
| HitStopKill | Juice.cs | 0.12 s | 적 기절(마무리) 타격 히트스톱 (11-09: 0.08). 처치 때 불꽃 `KillSparks` 3개 추가 |
| hitStopTime | PlayerController.cs (직렬화) | 0.06 s | 패링 히트스톱. 실제로는 `DashRules.ParryHitStop` 0.15 s가 최소값이라 그쪽이 쓰인다 |
| PopTime / PopScale | EnemyNumber.cs | 0.12 s / 0.5 | 적 공격마다 숫자 라벨이 커졌다 돌아오는 펀치 |
| 피격 붉은 틴트 | PlayerController.UpdateVisual | (1, 0.35, 0.35) | Hurt 상태(0.25s) 동안 |

적 피격 플래시는 기존 TestEnemy.Flash(색 틴트, flashTime 0.1)를 그대로 쓴다. 스프라이트 틴트는 흰색으로 밝힐 수 없으므로 진짜 흰 플래시는 셰이더(에셋 영역)가 필요하다.

## 구조
- 흔들림: `CameraFollow2D.Shake(amp, dur)` (정적). LateUpdate에서 지난 오프셋을 먼저 빼고, 추적 후 새 오프셋을 더한다 -> 추적 위치에 누적되지 않는다. unscaled 시간, 픽셀 스냅, 일시정지 중엔 진행 정지.
- 히트스톱: `PlayerController.StartHitStop` 하나로 패링과 일반 타격이 같이 쓴다. 일시정지/InputLocked 중엔 시작하지 않고, 진행 중이면 더 긴 쪽만 남는다.

## 제거 방법
- 흔들림 전체: Juice.cs의 Shake* 값을 0으로, 또는 `Juice.Shake` 호출 3곳(PlayerController) 삭제 + CameraFollow2D의 Shake/ApplyShake 삭제.
- 일반 타격 히트스톱: HitStopHit/HitStopKill을 0으로 (패링 것은 DashRules.ParryHitStop).
- 숫자 팝: `number.Pop()` 호출(TestEnemy.DoStrike) 삭제, EnemyNumber의 Pop 관련 줄 삭제.
- 피격 붉은 틴트: PlayerController.UpdateVisual의 `State.Hurt` 줄 삭제.

## 속도감 FX
시트: `Assets/Resources/FX/run-dust`, `speed-lines`, `dash-burst` (모두 오른쪽 기준, 왼쪽은 flipX). 재생: `PlayerController.UpdateSpeedFx` / `PlayDashBurst` → `SpriteFx.PlayBehind`(플레이어 스프라이트와 같은 정렬 레이어, order −1 = 몸 뒤).
- **run-dust**: 땅에서 |vx| ≥ moveSpeed × `runFxSpeedFrac`(0.9)로 달리는 동안 `runDustInterval`(0.25 s)마다 발뒤꿈치(feetOffset)에. 착지 순간, 달리다 방향을 바꾼 순간에도 한 번.
- **speed-lines**: 전속력 달리기·Shift 대시·공격 돌진 중 `speedLinesInterval`(0.15 s)마다 몸 중심에, 플레이어에 붙어 따라간다. 대각선 돌진은 돌진 각도로 회전.
- **dash-burst**: Shift 대시·공격 돌진 시작에 한 번, 발뒤꿈치에, 이동 방향으로. 같은 프레임에 speed-lines도 바로 나온다.
- 히트스톱·대화·일시정지(InputLocked) 중에는 새로 생기지 않는다 (이미 나온 것은 0.3~0.5 s 안에 끝남). 튜너블 3개는 PlayerController 직렬화 필드 "Speed FX".
- 끄기: `UpdateSpeedFx` 호출, `PlayDashBurst` 호출 2곳 삭제.

## 일반 적 한 방 처치 (2026-11-08 결정)
- 일반 적(Citizen, Bug)은 어떤 상태에서든 한 대 맞으면 쓰러진다 (`TestEnemy.ReceivePlayerHit`의 `kind != Android` 블록). 맞는 순간 Stunned 상태(눕는 연출 + 숫자 숨김) + 작은 넉백, 이후 `KillVanishDelay` 뒤 `SetActive(false)`로 사라진다 (hit-spark 재생). 처치 히트스톱/흔들림은 그대로 발동.
- Android(특수 적)는 기존 규칙(hp 5, 숫자가 0이 되기 전엔 막힘) 유지.
- 튜너블 (Juice.cs): `KillVanishDelay` 0.5 s, `KillKnockback` (6, 3.5) (11-09: (3, 2)).
- 다시 여러 대 맞게 되돌리기: TestEnemy.ReceivePlayerHit의 `if (kind != Kind.Android) { ... }` 블록과 Update의 vanish 블록(`Juice.KillVanishDelay`)을 삭제하면 기존 hp/막힘 규칙이 돌아온다.

## 일반 적 템포 (한 방 처치 보상)
Citizen/Bug만 해당 (Android 제외). `Juice.NormalTimeMul` **0.55**(이전 0.65), `NormalSpeedMul` **1.5**(이전 1.25), `MinFirstWindup` 0.2 s. 2026-11-08 밤 DECISIONS 12로 더 빠르게 바꿨다. 빌더 값은 그대로이고 TestEnemy가 읽을 때 곱한다. NUM-3/NUM-7 보정은 그 위에 얹힌다. 마지막 타 지연(lastStrikeDelay)은 그대로. 새 돌진 공격(RushWindup·Rush) 수치는 `DashRules.cs`에 따로 있다.

| 항목 | 기존 | 새 유효값 (0.55 / 1.5) |
|---|---|---|
| 첫 타 선딜 (0.5) | 0.5 s | 0.275 s |
| 연타 간격 (0.15) | 0.15 s | 0.0825 s |
| 연타 간격, NUM-3 (`Num3ChainWindupTime` 0.15) | 0.15 s | 0.0825 s |
| 회복 (0.15) | 0.15 s | 0.0825 s |
| 접근 속도 (3) | 3 | 4.5 |

되돌리기: 두 배율을 1로 설정.

## 패링 성공 표시 (플레이테스트 11-09 "패링이 잘 되는지 모르겠음")
- 원인: 드래프트 스프라이트는 흰색이라 색을 곱하는 흰색 번쩍임(플레이어·적)이 보이지 않았다. 흰색 대신 색으로 칠한다: `ParryPlayerTint`(청록), `ParryEnemyTint`(파랑, 0.25 s), 안드로이드 막기 `BlockTint`(회청).
- `ParryFeedback`: 화면 전체 흰색 번쩍임(`ParryFlashAlpha` 0.35, `ParryFlashTime` 0.12 s) + 접촉 지점 위 "PARRY!/패링!" 글자(금색, 24px, 0.5 s 동안 24px 올라가며 뒤 절반에 사라짐). 실제 시간 기준이라 히트스톱 중에도 보인다.
- 패링 히트스톱 `DashRules.ParryHitStop` 0.1 → 0.15 s (처치 0.12보다 길다).
- 패링된 탄은 사라지지 않고 1.3배 속도로 되돌아가며 흰색으로 0.35 s 날다 사라진다(아무것도 맞히지 않음).
- 개발 빌드 F3: `parry 성공/시도 (hit while parrying N)` 카운터.
