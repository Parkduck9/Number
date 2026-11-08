# WEBGL-BUILD-1 — 첫 WebGL 빌드 결과

> 2026-11-08 (Claude Opus + Fable 정적 분석). 기준: `origin/main` 542f896, 브랜치 `webgl-test`, 클론 `D:\Git\Number`.
> 방법: `Unity.exe -batchmode -quit -nographics -buildTarget WebGL -executeMethod BuildWebGL.Build` (Library 첫 임포트 포함). 로그: `D:\Git\Number\Logs\webgl-build-1.log` (저장소에 넣지 않음).
> 출력: `D:\Git\Number\Build\WebGL` (gitignore). itch.io 업로드는 하지 않았다.

## 1. 결과: **성공**

| 항목 | 값 |
|---|---|
| 결과 | `[BuildWebGL] Succeeded: Build/WebGL (11.9 MB, 347s)` |
| C# 컴파일 오류 | 0 (경고 1: `TestEnemy.staggerTime` 미사용 CS0414) |
| IL2CPP·스트리핑 오류 | 없음 (TMP 큰 메서드 분리 안내 경고만) |
| 빌드가 바꾼 설정 | `codeOptimization: BuildTimes → DiskSize` (BuildWebGL.ApplySettings, UserBuildSettings라 저장소 파일 아님) |
| 포함 씬 | Title, STG00~05 (Prototype·SampleScene은 꺼져 있음) |

### 크기 (Brotli 압축 후 다운로드 크기)
| 파일 | 크기 |
|---|---|
| `WebGL.wasm.unityweb` | 7.3 MB |
| `WebGL.data.unityweb` | 4.4 MB |
| `WebGL.framework.js.unityweb` | 74 KB |
| `WebGL.loader.js` | 115 KB |
| **합계** | **약 12 MB** |

압축 전 에셋 17.1 MB: 텍스처 13.6 MB(79%), 셰이더 0.9 MB, 기타 2.3 MB. 큰 것 (압축 전):

| 크기 | 에셋 | 메모 |
|---|---|---|
| 3.0 MB | `Assets/Resources/FX/parry.png` | FX 시트, 압축 끔 |
| 2.7 MB | 내장 Unity 로고 스플래시 | 스플래시 끄면 빠짐 (Unity 6은 Personal도 끌 수 있음 — 사용자 확인) |
| 1.5 MB | `Assets/Fonts/Mulmaru/Mulmaru.ttf` | 한글 동적 폰트 원본, 필요 |
| 1.3 MB | `Assets/Resources/FX/sword-slash.png` | FX 시트 |
| 1.0 MB + 0.3 MB + 0.3 MB | TMP LiberationSans SDF·TTF, EmojiOne | TMP 기본 Resources. 안 쓰면 뺄 수 있음 |
| 0.85 MB | `PLACEHOLDER_stg01_segA_bg.png` | 임시 배경 |
| 0.68 / 0.32 MB | FX `hit-spark.png`, `number-ember.png` | |
| 약 2.8 MB | URP FilmGrain 텍스처 11장 | URP 후처리 데이터에 딸려 옴 |

12 MB는 itch.io 웹 게임으로 문제없는 크기다. 줄일 필요가 생기면 위 표 순서로 본다 (아래 3절).

## 2. 확인한 위험 요소 (Fable 정적 분석 + 로그)

| # | 항목 | 결과 |
|---|---|---|
| 1 | `SpriteFx` 메타 클래스 + JsonUtility | 안전. `Root/Meta/Info`가 `[Serializable]`이고 `FromJson<Root>`로 직접 참조되어 Medium 스트리핑에서 살아남는다. |
| 2 | `LudditeMeeting` → `Shockwave.SetUnlocked` | main에서는 이미 **리플렉션이 아니라 직접 호출**(`LudditeMeeting.cs:28`). link.xml 필요 없음. |
| 3 | MCP for Unity 런타임 | **들어감.** `MCPForUnity.Runtime.dll` + 의존 `Newtonsoft.Json.dll`이 플레이어에 컴파일된다. 런타임 쪽에 소켓·스레드·자동 실행 코드가 없어 스트리핑으로 거의 다 빠짐 → 깨지지 않음, 크기 영향 작음. |
| 4 | Visual Scripting | **들어감.** `Unity.VisualScripting.Core/Flow.dll`. Core의 `[RuntimeInitializeOnLoadMethod]` 3개가 시작 때 실행되어 살아남음 → wasm 약 0.5~1 MB + 시작 몇 ms. Assets에서 쓰는 곳 없음. |
| 5 | WebGL 불가 API (스레드·파일·소켓·Cursor) | 없음. 입력은 Input System만. `Application.Quit`은 WebGL에서 숨김 처리됨. |
| 6 | 우클릭 막기 | `WebGLBrowserFix.cs` + `Plugins/WebGL/NumberBrowserFix.jslib` 이름 일치, `#if UNITY_WEBGL && !UNITY_EDITOR` 정상. |
| 7 | 한글 폰트 | `Mulmaru SDF`가 Dynamic + TTF 포함 → 브라우저에 OS 폰트가 없어도 런타임에 글자를 만든다. 씬의 폰트 참조는 모두 Mulmaru. 단, 처음 나오는 한글마다 약간 끊김 가능, TMP Settings 대체 폰트 목록이 비어 있어 폰트를 지정하지 않은 라벨은 한글이 네모로 나온다. |
| 8 | 저장 (PlayerPrefs) | WebGL은 IndexedDB. `Loc.cs`가 `PlayerPrefs.Save()`를 불러 정상. |
| 9 | 에디터 코드 섞임 | 없음. UnityEditor 사용은 모두 `Assets/Editor/` 또는 `#if UNITY_EDITOR`. |

## 3. 고친 것 / 고칠 것

이번에 **코드 수정은 없다** (빌드 성공, 깨지는 문제 없음).

빌드·임포트가 자동으로 바꾼 파일 5개(`ProjectSettings.asset`의 WebGL 배칭 항목, `UniversalRP.asset` 셰이더 프리필터 값, `UniversalRenderPipelineGlobalSettings.asset`, `DefaultVolumeProfile.asset`, `Mulmaru SDF.asset` 아틀라스 바이트)는 C: 작업과 충돌하지 않게 **되돌렸다.** C:에서 처음 WebGL 빌드할 때도 같은 변경이 생길 것이다 — 그때 한 번 커밋하면 된다.

나중에 (사용자 결정 또는 다른 세션, 우선순위 순):
1. **Visual Scripting 패키지 제거** (`Packages/manifest.json`의 `com.unity.visualscripting`) — 크기·시작 시간. 쓰는 곳 없음. 모든 세션이 같이 다시 컴파일하므로 C: 작업이 조용할 때.
2. **스플래시 끄기** (Player Settings → Splash Image → Show Splash Screen 끔) — 2.7 MB. 사용자 라이선스 확인.
3. **TMP Settings 대체 폰트에 Mulmaru SDF 추가** — 폰트 빠진 라벨의 한글 네모 방지 (에셋 설정, Codex/Sonnet).
4. FX 시트 크기 줄이기 (parry 3 MB 등, 압축 끔 유지하되 시트 여백·해상도 확인) — 에셋 담당.
5. MCP for Unity는 제출 빌드 전에 빼도 되고 그대로 둬도 된다 (깨지지 않음).
6. `TestEnemy.staggerTime` 미사용 경고 — TestEnemy는 C: 세션 담당이라 손대지 않음.

## 4. 사용자가 브라우저에서 확인할 것

로컬에서 열기: Brotli 파일은 `file://`로 열면 안 된다. 압축 해제 대체가 켜져 있어도 로컬 서버가 필요하다. 예:

```bash
cd /d/Git/Number/Build/WebGL && python -m http.server 8000
```

그다음 `http://localhost:8000` (Chrome과 Firefox 둘 다).

- [ ] 로딩 바 → 타이틀 화면이 뜬다. 콘솔(F12)에 빨간 오류가 없다.
- [ ] 타이틀에서 시작 → STG-00 → STG-01로 넘어간다. Quit 버튼은 보이지 않는다.
- [ ] 게임 화면에서 **우클릭해도 브라우저 메뉴가 안 뜬다.**
- [ ] 전체 화면 버튼 → 전체 화면 → **Esc로 나온다.** 이때 일시정지 메뉴가 같이 열리는지 확인(브라우저가 Esc를 먼저 가져감, BUG-BUILD-A02 참고). P키로도 일시정지된다.
- [ ] 언어를 한국어로 바꾸면 **한글이 네모 없이** 나온다 (대사창·HUD·해킹 다이얼·정지 버튼). 처음 한글이 뜰 때 끊김이 심하지 않은지.
- [ ] 새로고침 뒤 언어 설정이 유지된다 (PlayerPrefs → IndexedDB).
- [ ] 소리 (첫 클릭 뒤부터 나오는 게 정상).
- [ ] Firefox에서 WebGPU 경고는 무시해도 된다 (WebGL 2로 돈다).
