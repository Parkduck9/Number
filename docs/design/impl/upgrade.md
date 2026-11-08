# 업그레이드 구현 기록 — 충격파 (결정 8-A)

근거: [DECISIONS.md](../DECISIONS.md) 8-A `[확정]` (2026-11-08). 방향이 바뀌면 아래 "바꾸기·빼기"를 따라 쉽게 고치거나 뺄 수 있게 만들었다.

## 무엇이 되었나
- **Space**(게임패드 East 버튼)를 누르면 플레이어 **앞쪽 3u × 높이 2u** 상자 안의 적이 모두 **1초 경직**(TestEnemy의 기존 `Staggered` 상태)된다. 쿨다운은 **5초**다.
- 경직되면 연타 체인이 끊기고(`strikesLeft = 0`) 그 1초 동안 **공격이 들어간다**. 대시로 마지막 타를 피했을 때의 경직과 같은 반격 창이다. 경직이 끝나면 숫자를 다시 굴리고 접근 상태로 돌아간다.
- 기절(Stunned)한 적에게는 효과가 없다. 이미 날아가는 투사체는 그대로 날아온다.
- 경직·사망 중에는 쓸 수 없다. 공격·패리·대시 중에는 쓸 수 있으며, 플레이어 상태는 바뀌지 않는다.
- 이펙트는 새 그림 없이 기존 `ParryFx.Swing` 상자 플래시를 청록(#4CE8D8, 대시 잔상과 같은 색)으로 0.15초 보여 준다. 소리는 아직 없다(audio 세션의 Sfx API가 들어오면 `Used` 이벤트에 붙인다).
- 쿨다운 표시: 해금된 뒤에만 왼쪽 아래에 OnGUI 글자 한 줄(`SPACE Shockwave: READY` / `3.2s`, 한국어 설정이면 `SPACE 충격파: 준비` / `3.2초`)이 뜬다. PrototypeDebug와 같은 임시 방식이다.

## 조정값 (T)
| 값 | 기본 | 위치 |
|---|---|---|
| 범위(앞쪽 거리) | 3 | [Shockwave.cs:12](../../../Assets/Scripts/Player/Shockwave.cs:12) |
| 상자 높이 | 2 | [Shockwave.cs:13](../../../Assets/Scripts/Player/Shockwave.cs:13) |
| 경직 시간 | 1초 | [Shockwave.cs:14](../../../Assets/Scripts/Player/Shockwave.cs:14) |
| 쿨다운 | 5초 | [Shockwave.cs:15](../../../Assets/Scripts/Player/Shockwave.cs:15) |
| 이펙트 색 | #4CE8D8 | [Shockwave.cs:16](../../../Assets/Scripts/Player/Shockwave.cs:16) |
| 키 바인딩 | Space / Gamepad East | [PlayerInputReader.cs:29](../../../Assets/Scripts/Player/PlayerInputReader.cs:29) |

- Shockwave 컴포넌트는 플레이어가 Awake할 때 런타임에 붙는다([PlayerController.cs:89](../../../Assets/Scripts/Player/PlayerController.cs:89)). 그래서 인스펙터에서 바꾼 값은 Play 모드 안에서만 유지된다. 값을 영구히 바꾸려면 위 파일의 기본값을 고친다.
- 경직 시간은 `TestEnemy.staggerTime`과 관계없이 `seconds` 값대로 지속된다([TestEnemy.cs:337](../../../Assets/Scripts/Enemy/TestEnemy.cs:337)).

## STG-03에서 해금하는 법
- 러다이트 대화가 끝날 때(또는 아이템을 줄 때) **`Shockwave.SetUnlocked(true);`** 한 줄을 부른다 ([Shockwave.cs:22](../../../Assets/Scripts/Player/Shockwave.cs:22)).
- 해금 상태는 static 런타임 값이다. 저장하지 않고 PlayerPrefs도 쓰지 않는다. 씬을 넘어가도(STG-04·05) 유지되고 게임을 다시 켜면 꺼진다.
- **새 게임을 시작할 때** 타이틀이나 StageFlow에서 `Shockwave.SetUnlocked(false);`를 불러야 한다. 지금은 아무도 부르지 않는다. 한 번 해금한 뒤 타이틀로 돌아가 새 게임을 하면 해금이 남아 있다.
- 체크포인트 리스폰(씬 리로드)을 해도 해금은 유지된다.
- **디버그:** 에디터와 Development Build에서 **F9**를 누르면 해금이 켜지고 꺼진다. 콘솔에 `[Shockwave] debug unlock: True`가 찍힌다. 릴리스 빌드에서는 F9 코드가 빠진다.

## 쓴 패턴
- **Observer**
  - `Shockwave.UnlockChanged`(static, [Shockwave.cs:20](../../../Assets/Scripts/Player/Shockwave.cs:20)): 해금 연출이나 HUD 아이콘 표시에 쓴다.
  - `Used`(인스턴스, [Shockwave.cs:32](../../../Assets/Scripts/Player/Shockwave.cs:32)): HUD·효과음·화면 흔들림이 구독한다.
  - `CooldownRemaining`·`Ready`([Shockwave.cs:33-34](../../../Assets/Scripts/Player/Shockwave.cs:33)): 매 프레임 읽는 값이다.
- **State 재사용:** 새 상태를 만들지 않았다. TestEnemy의 기존 `Staggered`에 들어가는 공개 메서드 `ForceStagger` 하나만 더했다([TestEnemy.cs:337](../../../Assets/Scripts/Enemy/TestEnemy.cs:337)).
- **플레이어 상태 머신은 건드리지 않았다.** 충격파는 "모션 없는 즉발 능력"이다. 그래서 PlayerController의 `State`에 넣지 않고 별도 컴포넌트로 뺐다. 빼거나 바꾸기 쉽게 하려는 것이다.
- **static 해금 값을 고른 이유:** 업그레이드가 1개뿐이라 별도 매니저나 ScriptableObject를 만들지 않았다.

## 패턴 / 리팩터 제안 (다른 세션 파일이라 코드에 넣지 않음)
1. **`IStaggerable` 인터페이스** (`void ForceStagger(float s)`): 지금 충격파는 `TestEnemy`만 찾는다. 보스(boss 세션)도 충격파에 맞게 하려면 보스가 이 인터페이스를 구현하고, Shockwave는 `GetComponentInParent<IStaggerable>()`로 찾게 바꾼다. 보스 밸런스상 보스는 경직 시간을 줄이거나 무시하는 편이 좋을 수 있다.
2. **`TestEnemy`의 경직 시간:** 지금 `ForceStagger`는 `stateEnteredAt`을 옮겨서 지속 시간을 맞춘다. 이 파일의 소유 세션이 `staggerUntil` 필드를 두고 Staggered 판정을 `Time.time >= staggerUntil`로 바꾸면 더 읽기 쉽다.
3. **HUD:** OnGUI 임시 표시는 HUD 세션(uGUI/TMP)이 `Used`·`CooldownRemaining`을 읽는 아이콘으로 바꾼다. PrototypeDebug의 대시 쿨다운 표시도 같은 HUD로 옮길 수 있다.
4. **새 게임 초기화 지점:** 해금 같은 진행 상태가 더 늘면 `GameFlow.ResetRun()` 같은 한 곳에서 초기화하게 모은다(위 "새 게임" 문제).
5. **입력:** Shockwave 액션은 공유 `.inputactions` 에셋을 피해서 코드로 만들었다. 키 리바인딩 UI를 만들 때는 에셋의 Player 맵으로 옮긴다.

## 바꾸기·빼기
- **수치만 바꾸기:** 위 조정값 표의 줄을 고친다.
- **키 바꾸기:** [PlayerInputReader.cs:29-30](../../../Assets/Scripts/Player/PlayerInputReader.cs:29)의 바인딩 문자열을 고친다.
- **처음부터 쓸 수 있게:** `ResetStatics`의 `Unlocked = false`를 `true`로 바꾼다.
- **완전히 빼기:**
  1. `Shockwave.cs`와 그 `.meta`를 지운다.
  2. PlayerController.cs:89의 AddComponent 줄과 `EnemyMask` 줄(80)을 지운다.
  3. PlayerInputReader.cs의 `shockwave`·`ShockwavePressed` 관련 줄을 지운다.
  4. TestEnemy.cs의 `ForceStagger`를 지운다. 다른 곳에서 쓰지 않을 때만 지운다.
  5. STG-03의 `SetUnlocked` 호출을 지운다.
