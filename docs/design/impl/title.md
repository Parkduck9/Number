# 타이틀 화면 구현 메모

리뷰 [03-strategy.md](../../review/03-strategy.md) 2-2 반영. 첫 화면에 **숫자 하나**만 보인다 — 남색 배경 위에 깜빡이는 황금 홀로그램 `0`. 그 아래 로고 "NUMBER"는 "NUMB"가 흰색, "ER"가 회색(numb-er 말장난). 장식 애니메이션은 숫자의 깜빡임 하나뿐 (ANTI_SLOP 4절).

## 파일
| 파일 | 역할 |
|---|---|
| `Assets/Scripts/UI/TitleScreen.cs` | 타이틀 UI를 코드로 조립. 레이아웃 상수, 메뉴(Start / Language / Controls / Quit[WebGL 제외]), Controls 패널 토글, Esc로 패널 닫기 |
| `Assets/Scripts/UI/HologramFlicker.cs` | 숫자·홀로그램 창의 알파를 단계적으로 바꾸는 깜빡임 (unscaled 시간, 프레임당 할당 없음) |
| `Assets/Scripts/UI/UiKit.cs` | `ControlsPanel`이 프레임 루트 `GameObject`를 돌려준다 (토글용). PauseMenu는 반환값을 쓰지 않으므로 그대로 동작 |
| `Assets/Editor/TitleSceneBuilder.cs` | 변경 없음. `NUMBER/Build Title Scene`으로 `Title.unity` 재생성 |
| `Assets/Scenes/Title.unity` | 카메라 + `TitleScreen` 하나. UI는 전부 Start()에서 만든다 |

## 보이는 것 (960×540 기준)
- 홀로그램 창: 160×192, 황금 `#FFD24A` 테두리(알파 0x50) + 채움(알파 0x18), 중심 y=+150.
- 숫자 `0`: Mulmaru 192px(12의 배수), 같은 황금색, 창 중앙.
- 로고 `NUMBER`: 48px, y=0. `NUMB` = 테마 `text`(흰색), `ER` = 테마 `dim`(회색) — TMP 리치 텍스트 `<color>`.
- 메뉴: 360×48 버튼, 60 간격, y=-40부터 아래로. 선택/호버 룩은 기존 `ButtonHighlight`.
- Controls 패널: 기존 600×104 패널을 숨겨 두고 Controls 버튼으로 켜고 끈다. 열리면 로고 자리(y=0)를 덮는다. Esc 또는 Controls 다시 누르기로 닫힘.
- 색은 모두 기존 값 재사용: 황금은 `BroadcastHackUi.BroadcastGold`와 동일, 나머지는 `UiTheme.asset`.

## 깜빡임 (HologramFlicker)
1초 창 단위로 "떨림 창"인지 해시로 정한다(`StutterChance` 0.35). 떨림 창 안에서는 초당 `StepsPerSecond`(24)번, 단계마다 해시로 알파 배율을 고른다: 어두움 `DarkLevel` 0.2 (`DarkShare` 25%), 흐림 `DimLevel` 0.6 (`DimShare` 30%), 나머지 100%. 떨림 창 밖은 100% 고정. 정수 해시라 패턴이 매번 같고(시작 시각에 따라 위상만 다름), 사인파처럼 부드럽지 않다. `Time.unscaledTime` 사용.

## 튜너블
| 이름 | 파일 | 값 |
|---|---|---|
| `Number` | TitleScreen.cs | `"0"` (보여 줄 숫자) |
| `NumberSize` / `LogoSize` | TitleScreen.cs | 192 / 48 |
| `NumberY` / `LogoY` / `MenuTopY` | TitleScreen.cs | 150 / 0 / -40 |
| `WindowSize` | TitleScreen.cs | 160×192 |
| `WindowBorderAlpha` / `WindowFillAlpha` | TitleScreen.cs | 0x50 / 0x18 |
| `StepsPerSecond` / `StutterChance` | HologramFlicker.cs | 24 / 0.35 |
| `DarkLevel` / `DimLevel` / `DarkShare` / `DimShare` | HologramFlicker.cs | 0.2 / 0.6 / 0.25 / 0.3 |

깜빡임을 끄려면 `TitleScreen.Start`의 `AddComponent<HologramFlicker>()` 줄을 지운다.

## Play 확인 목록
| 하면 | 보여야 하는 것 |
|---|---|
| Title 씬 Play | 남색 배경, 화면 위쪽 중앙에 황금 `0`과 그 뒤 옅은 황금 창. 그 아래 `NUMB`(흰)`ER`(회색). 메뉴 3개(에디터는 Quit 포함 4개)가 모두 화면 안에 보임 |
| 3~5초 지켜봄 | 숫자와 창이 가끔 1초 정도 뚝뚝 끊기듯 어두워졌다 돌아옴. 부드럽게 숨 쉬듯 변하지 않음. 다른 어떤 것도 움직이지 않음 |
| 숫자 `0`이 창 안에 있는지 | 위아래·좌우 여백이 비슷해야 함. 어긋나면 `WindowSize`·`NumberY` 조정 |
| 위/아래 화살표, Enter | 선택 테두리가 버튼 사이를 오가고, Start에서 Enter → STG00 로드 (없으면 STG01) |
| Language 클릭 | 버튼·Controls 패널 문구가 한국어/영어로 바뀜. 로고와 숫자는 그대로 |
| Controls 클릭 | 로고 자리에 조작법 패널이 뜸. 다시 클릭 또는 Esc → 사라짐. 패널이 열린 채로 Start를 눌러도 정상 진행 |
| 게임 중 일시정지 → To title | 타이틀이 다시 뜨고 깜빡임이 돌아감(timeScale 1, 입력 잠금 해제). 다시 Start 가능 |
| WebGL 빌드 | Quit 버튼 없음. 메뉴 3개 |

## 알려진 한계
- Mulmaru SDF는 24pt 샘플링이라 192px로 키우면 모서리가 약간 둥글게 보인다. 거슬리면 에셋 쪽에서 큰 샘플링(또는 Raster 모드) 폰트 에셋을 따로 만들어 숫자에만 쓴다.
