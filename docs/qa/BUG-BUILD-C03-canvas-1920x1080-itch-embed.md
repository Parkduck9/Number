검증: CONFIRMED (BuildWebGL.cs:28-31 확인. itch 임베드 영향은 PLAUSIBLE)

# BUG-BUILD-C03 — 고정 1920×1080 캔버스는 itch.io 페이지 임베드에 너무 크다

## 재현 단계
1. `Assets/Editor/BuildWebGL.cs:28` — 템플릿 `APPLICATION:Default` (캔버스가 고정 px 크기).
2. `Assets/Editor/BuildWebGL.cs:30-31` — `defaultWebScreenWidth/Height`를 1920×1080으로 설정.
3. 주석(`BuildWebGL.cs:27`)대로 itch.io Viewport dimensions도 1920×1080으로 설정한다.
4. 1366×768 또는 1920×1080 노트북 화면의 브라우저에서 itch.io 게임 페이지를 연다.

## 기대 결과
- 게임 화면 전체가 스크롤 없이 페이지 안에 보이고, 원하면 전체 화면 버튼으로 키운다.

## 실제 결과
- itch.io 게임 페이지 본문 폭은 보통 1000px 안팎이라 1920px 임베드는 가로로 잘리거나 스크롤이 생긴다.
  브라우저 주소창·탭·itch 헤더를 빼면 세로 1080px도 대부분 화면에 들어가지 않는다.
- Default 템플릿 캔버스는 고정 크기라 창에 맞춰 줄어들지 않는다. 심사자가 첫 화면에서 UI 일부(하단 대사창 등)를 못 볼 수 있다.

## 수정 제안 (담당: 코드 + 사용자)
- 기준 해상도와 같은 960×540(16:9)으로 캔버스를 두고(`BuildWebGL.cs:30-31`), itch.io Viewport도 960×540 + "Fullscreen button" 체크.
  UI는 이미 960×540 기준(`UiTheme.referenceResolution`, `DialoguePlayer.RefResolution`)이라 Canvas Scaler가 그대로 맞춘다.
- 또는 itch.io에서 "Click to launch in fullscreen" 사용.

## 심각도
중간 — 기능은 되지만 첫인상(디자인 점수)과 조작에 바로 영향을 준다.
