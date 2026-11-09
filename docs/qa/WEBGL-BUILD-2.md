# WEBGL-BUILD-2 — 두 번째 WebGL 빌드 결과

> 2026-11-09 (Claude Opus). 기준: `main` 73308be, 클론 `D:\Git\Number`. 방법은 [WEBGL-BUILD-1.md](WEBGL-BUILD-1.md)와 같다 (`BuildWebGL.Build`, `-batchmode -nographics -buildTarget WebGL`).
> 로그: `D:\Git\Number\Logs\webgl-build-2.log` (저장소에 넣지 않음). 출력: `D:\Git\Number\Build\WebGL\index.html`. itch.io 업로드는 하지 않았다.

## 1. 결과: **성공**

| 항목 | 빌드 #1 (542f896) | 빌드 #2 (73308be) |
|---|---|---|
| 결과 | 성공, 347초 | **성공, 300초** |
| 다운로드 크기 | 11.9 MB | **13.7 MB** (+1.8 MB) |
| `WebGL.wasm.unityweb` | 7.3 MB | 7.4 MB |
| `WebGL.data.unityweb` | 4.4 MB | 6.2 MB |
| framework.js + loader.js | 0.19 MB | 0.19 MB |
| 압축 전 에셋 | 17.1 MB (텍스처 13.6) | 31.9 MB (텍스처 27.4, 소리 0.9) |
| C# 컴파일 오류 | 0 | 0 |
| IL2CPP·스트리핑 오류 | 없음 | 없음 |
| WebGL 설정 변경 | `codeOptimization` 1건 | 없음 ("already as required") |

로그 앞쪽의 `[Licensing::...] Error` 줄은 배치 모드 라이선스 핸드셰이크 메시지로, 빌드 결과와 상관없다 (빌드 #1에도 있었음).

## 2. 빌드 #1과 달라진 점

- **새 배경 6장이 들어갔다** (`Assets/Art/Backgrounds/`): 구간 4장 각 2.5 MB, 방 2장 각 1.5 MB (압축 전, RGB 무압축). 텍스처가 13.6 → 27.4 MB로 늘어난 거의 전부다. Brotli 압축 후에는 데이터 파일이 +1.8 MB만 늘었다 (색 수가 적은 픽셀 그림이라 압축이 잘 됨).
- **소리가 처음 들어갔다** (`Assets/Audio/Resources/`): 합계 0.9 MB. BGM 2곡(`bgm_field` 434 KB, `bgm_boss` 304 KB) + 효과음 수십 KB씩.
- **컴파일 경고 2개 늘어남** (모두 무해):
  - `Assets/Scripts/Core/DraftHud.cs(25,32)` CS0162 도달할 수 없는 코드 (const 분기로 보임)
  - `Assets/Scripts/Lighting/LightingController.cs(19,27)` CS0414 `debugToggleKey` 미사용
  - (기존) `Assets/Scripts/Enemy/TestEnemy.cs(21,28)` CS0414 `staggerTime` 미사용
- 그 밖의 큰 항목은 빌드 #1과 같다: `FX/parry.png` 3.0 MB, Unity 로고 스플래시 2.7 MB, `Mulmaru.ttf` 1.5 MB, `FX/sword-slash.png` 1.3 MB, TMP LiberationSans·EmojiOne 약 1.7 MB, URP FilmGrain 텍스처 약 2.8 MB, `PLACEHOLDER_stg01_segA_bg.png` 0.85 MB.

### 큰 항목 (압축 전)
| 크기 | 에셋 |
|---|---|
| 3.0 MB | `Assets/Resources/FX/parry.png` |
| 2.7 MB | 내장 Unity 로고 스플래시 |
| 2.5 MB ×4 | `bg_stg02_segA_rooftops`, `bg_stg02_segB_elevated_rail`, `bg_stg04_segA_production_line`, `bg_stg04_segB_inspection_hall` |
| 1.5 MB | `Assets/Fonts/Mulmaru/Mulmaru.ttf` |
| 1.5 MB ×2 | `bg_stg03_hideout_room`, `bg_stg05_boss_room` |
| 1.3 MB | `Assets/Resources/FX/sword-slash.png` |
| 1.0 MB | TMP `LiberationSans SDF.asset` |
| 0.85 MB | `PLACEHOLDER_stg01_segA_bg.png` |
| 0.68 MB | `FX/hit-spark.png` |
| 0.43 MB | `Audio/Resources/Bgm/bgm_field.wav` |

## 3. 고친 것 / 고칠 것

- **코드 수정 없음** (빌드 성공, 깨지는 문제 없음). `webgl-fix` 브랜치는 만들지 않았다.
- 빌드·임포트가 자동으로 바꾼 파일 5개(빌드 #1과 같은 파일: `ProjectSettings.asset`, `UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`, `Mulmaru SDF.asset`)는 되돌렸다.
- WEBGL-BUILD-1 3절의 크기 줄이기 목록은 그대로 유효하다 (Visual Scripting 제거, 스플래시 끄기, TMP 대체 폰트, FX 시트). 13.7 MB도 itch.io 웹 게임으로 문제없는 크기다.
- 추가 후보: `PLACEHOLDER_stg01_segA_bg.png`(0.85 MB)는 STG-01 정식 배경이 들어오면 빌드에서 빠지는지 확인.
- 경고 3개는 각 파일 담당 세션이 정리할 때 같이 (게임 동작과 무관).

## 4. 사용자가 브라우저에서 확인할 것

WEBGL-BUILD-1 4절 체크리스트 전체 + 이번에 새로 들어간 것:

```bash
cd /d/Git/Number/Build/WebGL && python -m http.server 8000
```

- [ ] STG-02~05에 **새 배경 그림**이 보이고, 픽셀이 뭉개지지 않는다 (Point 필터, 960×540 기준 1:1).
- [ ] 첫 클릭 뒤 **BGM이 나온다** (필드 → 보스 방에서 보스 곡). 효과음(패링·타격·방송)이 끊기지 않는다.
- [ ] 로딩 시간이 빌드 #1보다 눈에 띄게 길지 않다 (다운로드 +1.8 MB).
