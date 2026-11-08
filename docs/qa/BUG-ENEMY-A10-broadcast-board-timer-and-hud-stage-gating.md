검증: PLAUSIBLE (시나리오 A는 C02와 중복·확인됨. B 해킹 해금/HUD 분리, C 언어 갱신은 미검증)

# BUG-ENEMY-A10 — 전광판 역해킹 타이머의 입력 잠금 미처리, STG-01/02 해킹 UI·방송 HUD 분리 누락 및 다국어 미갱신

## 재현 단계
1. **시나리오 A (대화·전환 중 역해킹 타이머 진행):** 전광판(`BroadcastBoard`)을 해킹한 직후 대사(`DialoguePlayer` Talk 모드)나 구간 이동 문(`LevelTransition`, `GameFlow.InputLocked == true`)에 진입하여 역해킹 진행 바와 유지 시간을 관찰한다.
2. **시나리오 B (STG-01/02 해킹 해금 및 상단 방송 HUD):**
   - STG-01 SEG-B에 진입해 전광판 앞에서 `E` 키를 눌러본다.
   - STG-02(SEG-A 방송 3, SEG-B 방송 7)에 진입해 화면 상단 중앙에 방송 숫자 HUD(`((( 3 )))`, `((( 7 )))`)가 표시되는지 확인한다.
3. **시나리오 C (언어 전환):** 전광판 앞에서 `L` 키(또는 일시정지 메뉴)로 한/영 언어를 전환한 뒤 `[E] HACK` 프롬프트와 해킹 다이얼 창(숫자 `0` 선택 시 경고 문구 포함)을 확인한다.

## 기대 결과
- **시나리오 A:** `BroadcastBoard.cs:15` 주석(`Game time (Time.time) is used, so it matches enemy timers`) 및 `TestEnemy.cs:110-119`와 동일하게, `GameFlow.InputLocked == true`인 동안에는 역해킹 타이머(`hackedAt`)도 함께 멈춰야 한다.
- **시나리오 B:** 기획서(`docs/design/GDD.md:158, 183-186` `[확정]`, `docs/design/DECISIONS.md:77` 5-B 확정)에 따라, 전광판 해킹 기능은 **STG-03 이후**에만 사용할 수 있어야 하는 반면, 화면 상단 방송 숫자 HUD(`((( N )))`)는 방송 전광판이 있는 모든 구역(STG-01 SEG-B, STG-02 SEG-A/B 등)에서 표시되어야 한다.
- **시나리오 C:** 기획서(`docs/design/GDD.md:41, 284` `[확정]`)에 따라 런타임 언어 전환 시 해킹 프롬프트·다이얼 텍스트가 선택한 언어로 갱신되어야 하며, 숫자 `0` 선택 시 경고 문구도 한국어 번역을 지원해야 한다.

## 실제 결과
1. **입력 잠금 중 역해킹 시간 소모 및 만료:** `BroadcastBoard.Update()`는 `GameFlow.InputLocked`를 확인하지 않아 대화나 구간 전환 페이드 중에도 역해킹 시간(8초/5초)이 그대로 줄어들며, 잠금 도중 만료되면 `SetNumber(aiNumber)` → `TestEnemy.OnBroadcastChanged`가 호출되어 적 상태(`Frozen` 해제, `dopedUntil` 재설정 등)가 잠금 중에 변경된다.
2. **STG-02 방송 HUD 증발 및 STG-01 조기 해킹 가능:** `BroadcastHackUi`가 상단 방송 숫자 HUD와 `[E] HACK` 해킹 다이얼을 한 컴포넌트에 묶어 두었고, `StageBuildKit.BuildHud(bool hackUi)`는 `hackUi == false`일 때 `BroadcastHackUi` 자체를 생성하지 않는다. 그 결과 **STG-02(`BuildHud(false)`)에서는 상단 방송 숫자 HUD(`((( 3 )))`, `((( 7 )))`)가 아예 나타나지 않으며**, 반대로 `BroadcastHackUi`를 켠 **STG-01 SEG-B에서는 STG-03 이전에 이미 전광판을 해킹할 수 있다.**
3. **다국어 미갱신 및 하드코딩:** `BroadcastHackUi.BuildUi()`가 `Awake()`에서만 텍스트를 설정하고 `Loc.Changed`를 구독하지 않아 런타임 언어 변경이 반영되지 않으며, `RefreshDial()`의 `"EVERYONE STOPS BREATHING"`은 한국어 분기 없이 하드코딩되어 있다.

## 심각도
중간

## 근거 (파일:줄)
- `Assets/Scripts/Broadcast/BroadcastBoard.cs:49, 60-67` — `Update()` 및 `CounterHackProgress`에서 `GameFlow.InputLocked`일 때 `hackedAt += Time.deltaTime` 보정이 누락됨(비교: `Assets/Scripts/Enemy/TestEnemy.cs:110-119`, `Assets/Scripts/Combat/EnemyProjectile.cs:41`).
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:52-66, 84-104` — 상단 방송 HUD(`UpdateHud`)와 해킹 다이얼(`Open`/`UpdateDial`)이 분리되지 않고 해금 여부 체크가 없음.
- `Assets/Editor/StageBuildKit.cs:297, 332-335`, `Assets/Editor/Stg02Builder.cs:89`, `Assets/Editor/Stg01Builder.cs:99, 361` — STG-02는 `BuildHud(false)`로 인해 상단 방송 HUD까지 사라지고, STG-01은 해킹이 활성화됨.
- `Assets/Scripts/Broadcast/BroadcastHackUi.cs:45, 154, 192, 206, 221` — `Loc.Changed`(`Assets/Scripts/UI/Loc.cs:9`) 미구독 및 154줄 경고 문구 영어 하드코딩.
