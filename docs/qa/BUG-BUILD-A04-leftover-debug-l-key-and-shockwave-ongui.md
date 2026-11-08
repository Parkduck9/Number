검증: CONFIRMED (DialoguePlayer.cs:64, Shockwave.cs:81 OnGUI 가드 없음 확인). L키 부분 중복: BUG-BUILD-C05, OnGUI 부분은 신규

# BUG-BUILD-A04 — 릴리스 빌드에 남아 있는 디버그·프로토타입 코드 (`DialoguePlayer` `L` 키 및 `Shockwave.OnGUI`)

## 심각도
중간

## 관련 파일 및 근거 (`file:line`)
- `Assets/Scripts/Dialogue/DialoguePlayer.cs:64` (`if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) Loc.SetKorean(!Loc.Korean);` — `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 가드 없이 매 프레임 실행)
- `Assets/Scripts/Player/Shockwave.cs:50-56` (`F9` 디버그 해금 키는 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싸져 있음)
- `Assets/Scripts/Player/Shockwave.cs:80-88` (`// Prototype indicator (same style as PrototypeDebug). Replace with a HUD element...` 주석과 함께 `void OnGUI()`가 전처리 가드 없이 릴리스 빌드에 포함됨)

## 재현 단계
1. **사례 A (`L` 키 언어 전환)**: 대화가 포함된 스테이지(`STG00`, `STG03`, `STG05`)를 플레이하면서 대화가 출력 중이지 않을 때를 포함해 아무 때나 키보드 `L` 키를 누른다.
2. **사례 B (`Shockwave.OnGUI` 프로토타입 HUD)**: `STG03`에서 러다이트와 대화해 충격파(`Shockwave`)를 해금한 뒤 `STG03`~`STG05`를 플레이하고, 일시정지 메뉴에서 언어를 한국어(`언어: 한국어`)로 전환하여 화면 좌하단 텍스트를 확인한다.

## 기대 결과
1. 타이틀 화면(`TitleScreen.cs:27`)과 일시정지 메뉴(`PauseMenu.cs:116`)에 정식 언어 전환 버튼이 구현되어 있으므로, 플레이 도중 실수로 눌릴 수 있는 프로토타입용 `L` 키 언어 토글(`DialoguePlayer.cs:64`)은 제거되거나 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 제한되어야 한다.
2. `Shockwave` 쿨다운 표시는 게임의 픽셀 폰트(`Mulmaru SDF`)와 `CanvasScaler`를 사용하는 정식 uGUI/TMP HUD로 표시되거나 프로토타입 `OnGUI()`가 비활성화되어야 하며, WebGL 한국어 모드에서도 글자가 깨지지 않아야 한다.

## 실제 결과
1. `DialoguePlayer.cs:64`에 조건 없는 `L` 키 토글이 남아 있어, 플레이어가 공격/패링/대시 키(`J/K/L` 등)를 탐색하거나 오입력으로 `L`을 누르면 게임 전체 언어가 즉시 바뀌고 `PlayerPrefs`에 저장된다.
2. `Shockwave.cs:81-88`의 `OnGUI()`는 전처리 가드가 없어 릴리스 WebGL 빌드에서도 STG-03 이후 화면 좌하단(`new Rect(10, Screen.height - 40, 600, 30)`)에 레거시 IMGUI `GUI.Label`을 그린다.
3. 특히 WebGL 빌드에서 Unity 기본 IMGUI 폰트(`Arial`)는 한글 글리프를 포함하지 않고 OS 시스템 폰트 폴백도 불가능하므로, 한국어 모드일 때 `Shockwave.cs:85`의 `"SPACE 충격파: 준비"` / `"SPACE 충격파: {cd:0.0}초"` 중 한글 부분이 빈칸 또는 깨진 문자(□)로 출력되며, 매 프레임 전역 `GUI.skin.label.fontSize = 20`을 변경한다.
