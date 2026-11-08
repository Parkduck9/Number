검증: CONFIRMED (DialoguePlayer.cs:64 L키 무조건 처리 확인)

# BUG-BUILD-C05 — 숨은 L 키 언어 전환이 릴리스 빌드에서도 게임 중 항상 동작한다

## 재현 단계
1. STG00~STG05 중 DialoguePlayer가 있는 스테이지에서 플레이한다 (일시정지·대화 중 포함).
2. 키보드 `L`을 누른다.
- 근거: `Assets/Scripts/Dialogue/DialoguePlayer.cs:64`
  `if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) Loc.SetKorean(!Loc.Korean);`
  — `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 같은 가드가 없다 (비교: `Assets/Scripts/Player/Shockwave.cs:50`의 F9는 가드됨).

## 기대 결과
- 언어 전환은 타이틀·일시정지 메뉴의 "Language" 버튼(`TitleScreen.cs`, `PauseMenu.cs`)으로만 하거나,
  L 키를 유지한다면 조작법(Controls)과 화면 안내에 적혀 있다.

## 실제 결과
- 안내 없는 키 하나로 게임 전체 언어가 바뀌고 `PlayerPrefs`에 저장된다(`Loc.cs:19-20`). 조작 키(A/D/W/E/C/Shift/Space)와 가까워 잘못 누르기 쉽고,
  누른 플레이어는 이유를 모른 채 한국어 화면을 보게 된다. 일시정지 중(timeScale 0)에도 동작한다.

## 수정 제안 (담당: 코드)
- 디버그용이면 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼다.
- 기능으로 남기면 제출 페이지 Controls에 "L: language EN/KO"를 적는다.

## 심각도
낮음
