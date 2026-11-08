# 안드로이드 처치 기믹 A (과부하) 구현 기록

> 2026-11-08. 근거: [DECISIONS.md](../DECISIONS.md) "검수 Top 5" 1번 (사용자 확정). 나중에 바꾸거나 지울 수 있다.
> Unity 6000.3.28f1 배치 모드 컴파일 오류 0. Play 모드 확인은 아직 안 했다.

## 1. 구현한 규칙
- 안드로이드(`TestEnemy.Kind.Android`)는 평소처럼 플레이어의 공격을 **막는다** (흰색 번쩍, 데미지 없음). 체력 5 규칙은 대체되어 **안드로이드는 체력을 잃지 않는다.**
- **연타의 마지막 일격을 패링하면**, 또는 **숫자가 7일 때(마지막 일격이 패링 불가) 그 일격을 대시로 통과해 피하면** 안드로이드가 **과부하(`State.Overload`)** 에 들어간다.
- 과부하 동안(1.5초) 안드로이드는 멈추고, 청록/원래 색으로 깜빡이며 `hit-spark`가 0.2초마다 몸 근처에서 튄다. 숫자 라벨이 `ERR`와 원래 숫자 사이에서 깜빡인다.
- 과부하 중 **다음 한 대**는 일반 적(시민·벌레)과 같은 즉사 경로다: 같은 넉백·히트스톱·카메라 흔들림, 0.5초 뒤 사라짐(`Juice.KillVanishDelay`). 이제 안드로이드도 쓰러진 뒤 사라진다.
- 과부하가 안 쓰이고 끝나면 숫자를 다시 뽑고(`FinishReset`) 평소 패턴(Approach)으로 돌아간다.
- 황금 부채꼴 특수 공격은 그대로다.

## 2. 조정값 (T)
| 값 | 기본 | 위치 |
|---|---|---|
| 과부하 지속 | 1.5초 | `Assets/Scripts/Core/Juice.cs` `OverloadSeconds` |
| 스파크 간격 | 0.2초 | `Juice.cs` `OverloadSparkInterval` |
| 깜빡임 색 | 청록 | `TestEnemy.cs` `OverloadColor` |
| 깜빡임 속도 | 몸 16Hz / 라벨 12Hz (켜고 끄는 횟수 기준, 눈에 보이는 깜빡임은 8Hz / 6Hz) | `TestEnemy.ApplyVisual`, `EnemyNumber.Update` |

## 3. 감지 방법 (`TestEnemy.DoStrike`)
- **패링:** `special`(= `strikesLeft == 1`, 연타의 마지막 일격)이고 `ReceiveEnemyHit` 결과가 `Parried`.
- **7번째 회피:** `special`이고 `unparryable`(`unparryableNumber`와 숫자가 같음, 빌더가 7로 지정)이고, 일격 판정 순간 플레이어가 히트박스 안에 있으면서 `State.Dash`(`dashedThrough`)이며 결과가 `Dodged`. 기존 "대시 통과 스태거"와 같은 조건이라 새 판정 코드가 없다.
  - 대시로 범위 밖으로 빠져 헛친 경우, 피격 후 무적으로 피한 경우는 세지 않는다 (대시 통과만 인정).
- 숫자 7이 아닌 안드로이드가 마지막 일격을 대시로 통과하면 이전처럼 `Staggered`가 된다 (아래 5번 참고).
- 안드로이드 외 종류는 영향 없다 (`kind == Android` 조건).

## 4. 확인한 소비처
- **BossFight 3웨이브:** NUM-0은 `Citizen`만 얼린다 (`OnBroadcastChanged`). 안드로이드는 영향이 없어서 그대로 과부하 규칙이 적용된다. `BossFight.AllStunned()`는 `Current == Stunned`만 보는데 과부하 처치도 `Stunned`로 들어가므로 웨이브 클리어 알림이 같은 경로로 간다. 쓰러진 뒤 `SetActive(false)`되어도 목록에 `Stunned`로 남는다.
- **Shockwave `ForceStagger`:** 과부하 중에는 무시한다 (처치 창을 지키려고). 그 밖에는 이전처럼 `Staggered`로 보낸다. 다만 `Staggered`에서도 안드로이드는 이제 막으므로 충격파는 "연타 끊기"만 한다 (아래 5번).
- **NUM-7 도핑:** `UpdateDope`(탈진 `Open`으로 보내는 곳)는 과부하 중에는 건너뛴다. 도핑 중 속도·예고 단축은 `DoStrike` 판정과 무관하므로 회피 감지는 그대로 동작한다. 탈진 구간이면 과부하가 끝난 다음 프레임에 `Open`이 된다.
- **STG-04 빌더(`StageBuildKit.cs:253`, `Stg05Builder.cs:113`, `EnemyVariantsTestSetup.cs:81`):** 안드로이드 `maxHp = 5`는 이제 쓰이지 않는다. 빌더는 건드리지 않았다. (`TestEnemy.maxHp` 주석에만 표시)

## 5. 확인이 필요한 점 (사용자 판단)

> **2026-11-08 사용자 결정: 둘 다 추천대로 (a) 지금 그대로.** 과부하 밖에서는 모든 상태에서 막고, 7번째는 판정 안 대시 통과만 인정한다. 플레이 후 너무 어려우면 (b)로 바꾼다.

- **기존 동작에서 바뀐 점:** 바꾸기 전 코드는 안드로이드가 남은 타수가 0일 때(`Open`, `Staggered`, 마지막 타 직후 `Recover`)와 `Turn`(뒤를 잡혔을 때)에는 공격을 맞고 체력이 깎였다. 지금은 과부하가 아니면 이 상태들에서도 전부 막는다. 그래서 NUM-7 탈진, 충격파(`ForceStagger`), 7이 아닌 숫자의 대시 통과 경직, 뒤잡기는 안드로이드에게 아무 틈도 주지 않는다.
  - 선택지 (b): `ReceivePlayerHit`에서 `Open`/`Staggered`일 때 맞으면 과부하로 보내기 (두 번 쳐서 처치). 한 줄 변경이다.
- **"7번째를 대시로 피하면"의 범위:** 지금은 대시로 공격 판정 안을 **통과**했을 때만 인정한다 (기존 대시 통과 경직 규칙과 같다). 판정 밖으로 대시해서 피하거나 점프로 피하면 인정하지 않는다.

## 6. 체력 5 규칙으로 되돌리기
- `TestEnemy.ReceivePlayerHit`의 안드로이드 분기 **전체**를 공용 처치 코드보다 앞에 옛 코드로 복원한다 (중간만 붙이면 남은 타수 0인 안드로이드가 아래의 한 방 처치로 흘러간다):
  `if (kind == Kind.Android) { if (strikesLeft > 0 && state != State.Turn && state != State.Frozen) { Flash(Color.white); return false; } hp -= info.Damage; Flash(Color.red); if (hp <= 0) EnterState(State.Stunned); return true; }`
- `Update`/`FixedUpdate`의 `Stunned` 줄에 `kind != Kind.Android`를 되살린다.
- `DoStrike`의 "Android gimmick A" 블록과 `UpdateState`의 `Overload` case를 지우면 과부하로 들어갈 길이 없어진다.
- 남는 죽은 코드도 지운다: `EnterState`의 `Overload` case, `ForceStagger`/`UpdateDope`의 Overload 제외 조건, `ApplyVisual`의 Overload case, `OverloadColor`, `nextSparkAt`, `EnemyNumber.SetBroken`/`broken`/`lastErrShown`, `Juice.OverloadSeconds`/`OverloadSparkInterval`.

## 7. Play 모드 확인 목록
1. 안드로이드(숫자 2~5)의 마지막 일격을 패링 → 몸 깜빡임, 스파크, 라벨 `ERR` 깜빡임.
2. 과부하 중 한 대 → 넉백 쓰러짐, 0.5초 뒤 사라짐.
3. 1.5초 동안 안 때리면 평소 패턴으로 복귀, 라벨이 숫자로 돌아옴.
4. 과부하 밖에서 때리면 흰색 번쩍만 하고 안 죽음 (연타 중간, 탈진 중 포함).
5. 숫자 7 안드로이드의 7번째 일격을 대시로 통과 → 과부하. 패링 시도는 맞는다.
6. 보스 3웨이브 (NUM-0): 안드로이드가 얼지 않고, 과부하 처치도 웨이브 클리어에 센다.
7. 충격파가 과부하 창을 끊지 않는다.
8. 부채꼴 특수 공격이 그대로 나간다.
