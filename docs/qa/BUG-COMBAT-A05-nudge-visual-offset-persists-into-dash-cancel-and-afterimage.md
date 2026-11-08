검증: PLAUSIBLE (ParryFx Nudge 오프셋이 대시 캔슬 후 최대 0.08초 남는 것은 코드상 맞으나 0.125u 시각적 오차뿐; 문제 1은 사소함)

# BUG-COMBAT-A05: `ParryFx.Nudge`가 공격 연타 시 밀려난 위치를 기준 위치(`basePos`)로 덮어써야 할 타이밍에 잘못 복구하거나, 영구적으로 스프라이트 위치가 어긋날 수 있음

## 심각도
**중간**

## 위반 규칙
- **PROTOTYPE_ARCH.md 7절 추가 확정 3**: "휘두르기 임시 표시: 공격 판정 순간 공격 범위 위치에 짧은 섬광(플레이어 흰색, 적 빨간색, 약 0.08초) + 공격자가 앞으로 살짝(약 2px) 내딛는 움직임."

## 코드 근거
- `Assets/Scripts/Combat/ParryFx.cs:49-74`:
  ```csharp
  static void NudgeVisual(Transform attackerVisual, int facing, float time, float nudge)
  {
      if (attackerVisual == null) return;
      Nudge n = attackerVisual.GetComponent<Nudge>();
      if (n == null) n = attackerVisual.gameObject.AddComponent<Nudge>();
      n.Begin(facing * nudge, time);
  }

  class Nudge : MonoBehaviour
  {
      Vector3 basePos;
      float until = -1f;

      void Awake() { basePos = transform.localPosition; }

      public void Begin(float dx, float time)
      {
          transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
          until = Time.time + time;
      }

      void Update()
      {
          if (until >= 0f && Time.time >= until) { transform.localPosition = basePos; until = -1f; }
      }
  }
  ```
- `Assets/Scripts/Combat/ParryFx.cs:25-38`:
  `Swing`에서 생성하는 `SwingFlash` 오브젝트는 `Object.Destroy(go, time)`(기본 0.08초)로 파괴된다.
- **문제 1 (블록 대화 `GameFlow.InputLocked` 중 `Nudge` 및 `SwingFlash` 타이머 불일치)**:
  - `TestEnemy`나 플레이어가 타격한 직후(0.08초 이내) 블록 대화(`GameFlow.InputLocked == true`)가 시작되면, `TestEnemy`의 상태 타이머는 멈추지만 `ParryFx.Nudge.Update()`와 `Object.Destroy(go, time)`는 `GameFlow.InputLocked`를 확인하지 않아 대화 중에 `SwingFlash`가 사라지고 `Nudge`가 원위치로 돌아간다.
- **문제 2 (대시 잔상 `SpriteFx.Afterimage`가 `Nudge`로 어긋난 `visual.transform.position`에서 생성됨)**:
  - `PlayerController.cs:130-131`: 공격 시작 0.08초(`attackHitDelay`)에 `DoAttackHit()`가 호출되어 `ParryFx.Slash`가 `visual.transform.localPosition`을 앞으로 `0.125u`(4px) 이동시킨다(`until = now + 0.08f`).
  - 바로 같은 프레임(`elapsed >= attackHitDelay`)에 플레이어가 대시(Shift)로 공격 후딜레이를 캔슬(`TryDash(now)`, `PlayerController.cs:131`)하면 즉시 `State.Dash`로 진입한다.
  - 그러나 `visual.transform.localPosition`은 여전히 앞으로 `+0.125u` 밀려난 상태이며, 대시 진입 후 0.08초 동안 생성되는 대시 잔상(`SpriteFx.Afterimage(visual, ...)`, `PlayerController.cs:138-142`)과 플레이어 본체 스프라이트가 콜라이더 중심보다 `0.125u` 앞으로 튀어나온 위치에 그려지다가 대시 도중 갑자기 뒤로 뚝 끊기며 복구된다.

## 재현 단계
1. 플레이어로 좌클릭 공격을 실행한다.
2. 공격 판정이 발생하는 시점(0.08초 직후)에 Shift를 눌러 대시 캔슬을 한다.
3. 대시 초반 0.08초 동안의 플레이어 스프라이트 위치 및 잔상 위치를 확인한다.

## 기대 결과
공격을 대시로 캔슬하여 `State.Attack`을 벗어나면 공격 모션의 앞으로 내딛기(`Nudge`) 오프셋이 즉시 초기화되어, 대시 중인 플레이어 비주얼과 대시 잔상이 정확한 본체 위치에서 생성되어야 한다.

## 실제 결과
공격을 대시로 캔슬해도 `ParryFx.Nudge`가 0.08초 동안 그대로 유지되어, 대시 전반부 동안 플레이어 비주얼과 잔상이 실제 물리 위치보다 0.125u(4px) 앞으로 어긋난 채 생성되다가 대시 도중에 갑자기 뒤로 되돌아간다.
