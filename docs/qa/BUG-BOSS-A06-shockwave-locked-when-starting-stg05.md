검증: PLAUSIBLE (정상 진행에서는 STG-03에서 해금됨. STG05 단독 시작 시에만 잠김이라 개발·QA 한정, 낮음)

# BUG-BOSS-A06 — 보스전(STG-05)을 단독 실행하거나 사망 후 재시작/체크포인트 재시작 시 충격파(Shockwave) 업그레이드 보유 상태가 기획(GDD 6·10장)과 불일치함

## 심각도
중간

## 재현 단계
1. **사례 A (STG-05 직접 진입 / 에디터 테스트):** `STG05.unity`를 열거나 STG-03을 거치지 않고 보스전을 시작하여 `Space` 키를 눌러 충격파(`Shockwave`) 스킬을 사용해 본다.
2. **사례 B (엔딩 또는 타이틀 복귀 후 재진입):** 게임을 클리어하여 타이틀(`TitleScreen`)로 돌아가면 `Shockwave.SetUnlocked(false)`가 호출된다 ([TitleScreen.cs:12](../../Assets/Scripts/UI/TitleScreen.cs#L12)).

## 기대 결과
- 기획서([GDD.md:155](../../docs/design/GDD.md#L155), [GDD.md:202](../../docs/design/GDD.md#L202))에 `"두 엔딩 모두 유도는 업그레이드(AI 없는 특수 공격 부품)를 장착한 채 AI 정지 버튼에 도달한다"`라고 `[확정]`되어 있으며, 결정 8-A([DECISIONS.md:103-106](../../docs/design/DECISIONS.md#L103-L106))에서도 STG-03에서 얻은 충격파(`Space`)를 보스 웨이브에서 활용하도록 설계되어 있다.
- 따라서 STG-04 및 STG-05(보스전)에서는 정상 진행이든 씬 단독 로드든 충격파 업그레이드가 활성화된 상태여야 한다.

## 실제 결과
- `Shockwave.Unlocked`는 기본값이 `false`인 런타임 전용 `static` 변수이고 ([Shockwave.cs:19-30](../../Assets/Scripts/Player/Shockwave.cs#L19-L30)), 오직 STG-03의 `LudditeMeeting.Finish()`에서만 `Shockwave.SetUnlocked(true)`를 호출한다 ([LudditeMeeting.cs:28](../../Assets/Scripts/Level/LudditeMeeting.cs#L28)).
- `Stg04Builder`나 `Stg05Builder`(`BossFight`)에서는 씬 시작 시 `Shockwave.SetUnlocked(true)`를 보장하지 않는다.
- 그 결과:
  1. QA나 개발 중 STG-05(또는 STG-04)를 바로 시작하면 충격파가 잠겨 있어(`Shockwave.Unlocked == false`) GDD 확정 규칙("두 엔딩 모두 업그레이드한 상태로 정지 버튼에 도달")이 지켜지지 않고, 보스전 밸런스를 의도대로 플레이할 수 없다 (릴리스/WebGL 빌드에서는 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`에 묶인 `F9` 디버그 해금도 동작하지 않음 — [Shockwave.cs:50-56](../../Assets/Scripts/Player/Shockwave.cs#L50-L56)).

## 근거 파일 및 위치
- [GDD.md:155](../../docs/design/GDD.md#L155), [GDD.md:202](../../docs/design/GDD.md#L202)
- [DECISIONS.md:103-106](../../docs/design/DECISIONS.md#L103-L106)
- [Shockwave.cs:19-30](../../Assets/Scripts/Player/Shockwave.cs#L19-L30), [Shockwave.cs:50-56](../../Assets/Scripts/Player/Shockwave.cs#L50-L56)
- [LudditeMeeting.cs:28](../../Assets/Scripts/Level/LudditeMeeting.cs#L28)
- [BossFight.cs:54](../../Assets/Scripts/Boss/BossFight.cs#L54) (STG-05 시작 시 `Shockwave.SetUnlocked(true)` 미호출)
