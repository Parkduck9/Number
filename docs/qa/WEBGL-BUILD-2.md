# WEBGL-BUILD-2 — 보스 v2·주사위 이후 첫 WebGL 빌드

> 2026-11-11 (Claude Opus). 기준 커밋 `8a967fe` (타이틀 부적 로고 포함). 새 PC에 Unity 6000.3.28f1 + WebGL 설치 후 첫 빌드.
> 방법: `Unity.exe -batchmode -quit -nographics -projectPath D:\Git\Number -buildTarget WebGL -executeMethod BuildWebGL.Build -logFile Logs\webgl-build-2.log` (Library 첫 임포트 포함)

## 결과
- **성공**, `Build/WebGL` **13.6MB**, 빌드 304초 (WEBGL-BUILD-1·QA 3차 때 13.8MB)
- 컴파일 오류 없음. 빌드 스크립트가 `codeOptimization: BuildTimes -> DiskSize`로 맞춤 (의도된 동작)
- 빌드 뒤 `Mulmaru SDF.asset` 변경은 되돌림

## 브라우저 확인 (로컬 `python -m http.server`, 앱 내장 브라우저)
| 확인 | 결과 |
|---|---|
| 로딩 → 타이틀 | 정상. 새 부적 로고가 홀로그램 숫자와 메뉴 사이에 겹치지 않고 들어감 |
| Start → STG-00 | 정상. 방송 대사, 조작 힌트, 배경·조명 표시 |
| 콘솔 오류 | 없음 |

## 아직 안 본 것
STG-01 이후 실제 플레이(주사위·일섬·보스 v2·엔딩), 프레임(F3는 개발 빌드만). 사용자 플레이 또는 QA로 확인.
itch.io 업로드는 `Build/WebGL` 폴더 **안의 내용**을 zip (index.html이 zip 맨 위).
