# 제출 체크리스트 (JAM_RULES.md 기준) — QA BUILD 점검, 2026-11-08

상태: **완료** / **누락** / **사용자 확인 필요** / **확인 대기**(빌드·업로드 후 확인)
관련 버그: `docs/qa/BUG-BUILD-C01` ~ `C08`

## 일정
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 개발 마감 2026-11-13 00:00 KST 이후 기능 추가 금지 | 사용자 확인 필요 | 11/13부터는 빌드·업로드·페이지 정리·빌드 버그만 |
| 제출 마감 2026-11-14 21:00 KST까지 Game Jam Page에 제출 | 사용자 확인 필요 | 마감 시점에 올라가 있는 버전이 심사 대상 |

## 참가 자격 (팀원 전원)
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 개인, 만 19세 이상 | 사용자 확인 필요 | |
| 제외 지역 거주자 아님 (한국 가능) | 사용자 확인 필요 | |
| 미국 수출 규제 명단에 없음 | 사용자 확인 필요 | |
| 최근 12개월 Unity/계열사 수상 없음 | 사용자 확인 필요 | |
| Unity·심사위원·상품 제공사·Discord 직원 및 가족 아님 | 사용자 확인 필요 | |
| 회사 업무 관련이면 회사 규정 확인 | 사용자 확인 필요 | |
| 팀 1~5명, 팀당 1작품, Team Lead가 제출 | 사용자 확인 필요 | Team Lead 지정 |

## 엔진 / 에셋
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| Unity 버전 (6000.3.28f1 정식) | 완료 | ProjectSettings/ProjectVersion.txt |
| 사용·배포 권리 있는 에셋만 사용 | 완료 (현재 기준) | 폰트 OFL, pixelfx MIT. Asset Store 에셋은 아직 임포트 안 됨 (`docs/design/ASSETS.md`는 후보 목록) |
| 모든 외부 출처 크레딧 | **누락** | AI 컨셉 배경(BUG-BUILD-C01), TMP LiberationSans/EmojiOne(BUG-BUILD-C08). 오디오·Asset Store 에셋을 넣으면 그때마다 추가 |
| 잼 기간 중 새로 만든 게임 (이전 출품작 아님) | 사용자 확인 필요 | |
| 저장소 비공개 유지 (Asset Store 원본 재배포 방지) | 사용자 확인 필요 | 공개 전환 전 Asset Store 폴더 제거 |

## 빌드 / 업로드
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| WebGL 빌드 존재 (상 후보 필수) | 확인 대기 | `NUMBER/Build WebGL` (`Assets/Editor/BuildWebGL.cs`) 준비됨, 아직 빌드 안 함. 반드시 이 메뉴로 빌드 (BUG-BUILD-C04) |
| Build Settings 씬 순서: Title, STG00~STG05 켜짐 / Prototype·SampleScene 꺼짐 | 완료 | `ProjectSettings/EditorBuildSettings.asset` |
| 스레드·System.IO 등 WebGL 불가 API 미사용 | 완료 | `Assets/Scripts`에 Thread/Task/async/System.IO 없음. Resources.Load는 UiTheme·FX만. 예외: 개발 패키지 런타임(BUG-BUILD-C07) |
| Threads Support 꺼짐 (itch.io) | 완료 | ProjectSettings.asset:577 `webGLThreadsSupport: 0` |
| 압축 Brotli + Decompression Fallback | **누락** | 저장소 설정은 Fallback 0 (ProjectSettings.asset:578). 메뉴 실행 후 커밋 (BUG-BUILD-C04) |
| 예외 지원 수준 | **누락** | Explicitly Thrown Only → Full Without Stacktrace 권장 (BUG-BUILD-C02) |
| 캔버스/itch Viewport 크기 | **누락** | 1920×1080 고정은 itch 임베드에 큼 → 960×540 + 전체 화면 버튼 권장 (BUG-BUILD-C03) |
| 텍스처 용량 (1GB 한도) | 완료 | 텍스처 합계 수백 KB, 폰트 TTF 약 17MB(Galmuri 3종은 미사용이라 빌드 제외). 오디오는 아직 없음 → 넣을 때 Vorbis 압축 확인 |
| 디버그 키·훅 제거 | 부분 완료 | F9(Shockwave.cs:50)는 DEVELOPMENT_BUILD 가드 OK, PrototypeDebug(R 재시작)는 꺼진 Prototype 씬에만 있음 OK, Development Build 꺼짐(BuildOptions.None) OK. L 키 언어 전환은 가드 없음 (BUG-BUILD-C05) |
| Debug.Log 남발 없음 | 완료 | 매 프레임 로그 없음, 경고 몇 줄뿐 |
| 브라우저 입력: 키보드 포커스 | 확인 대기 | itch 페이지에서 게임 화면을 한 번 클릭해야 키 입력이 들어감 → 설명에 "Click the game first" 적기 |
| 브라우저 입력: 전체 화면 Esc | 부분 완료 | 일시정지는 P도 됨. 해킹 다이얼·엔딩 선택 취소는 Esc만 (BUG-BUILD-C06) |
| 브라우저 입력: 우클릭(패링 보조키) 시 컨텍스트 메뉴 안 뜸 | 확인 대기 | WebGL 빌드에서 우클릭 연타로 확인 |
| 커서 잠금 | 해당 없음 | Cursor.lockState 사용 안 함 |
| Quit 버튼 WebGL에서 숨김 | 완료 | `TitleScreen.cs` `Application.platform != WebGLPlayer` |
| ZIP 업로드 (index.html이 ZIP 루트) | 사용자 확인 필요 | `Build/WebGL` 폴더 **내용**을 압축 |
| itch.io "This file will be played in the browser" 체크 | 사용자 확인 필요 | |
| 업로드 1GB 이하 | 확인 대기 | 빌드 후 크기 확인 (`BuildWebGL` 로그에 MB 표시) |
| Linux 빌드 올리면 Windows/Mac도 | 해당 없음 | WebGL만 올리면 무관 |
| 외부 다운로드 링크 없음 (모든 파일 itch 안에) | 사용자 확인 필요 | |
| 실제 itch.io에서 처음부터 엔딩까지 플레이 확인 | 확인 대기 | 업로드 후 Chrome·Firefox·Edge에서, 콘솔 오류 확인 |

## 제출 페이지 필수 항목
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 제목, 설명 | 사용자 확인 필요 | productName `NUMBER` (ProjectSettings.asset:16) |
| 조작법 (Controls) | **누락** | README·CREDITS 어디에도 없음. 초안: A/D·←/→ 이동, W 점프, 좌클릭(마우스 조준) 공격·패링, Shift 회피+반격, Space 충격파(해금 후), E 상호작용/대사 넘김, 0~9·↑/↓·휠 다이얼, Esc/P 일시정지, (L 언어 — C05 결정에 따라) |
| 크레딧 (모든 외부 에셋) | **누락** | BUG-BUILD-C01, C08 반영 필요 |
| 팀원 이름 | **누락** | CREDITS.md "## Team"이 `(팀원 이름 / 역할)` 자리표시자 그대로 |
| 생성형 AI 공개 (도구 + 사용 방식) | **누락** | CREDITS.md에 초안은 있음 (Claude Fable/Sonnet/Opus, Codex, Antigravity, pixelfx 기반 number-ember). 빠진 것: Codex 컨셉 이미지 생성(STG00 배경에 사용, C01). 제목에서 "(draft)" 제거, 실제로 안 쓴 도구·모델명(예: Antigravity가 실제로 QA를 했는지, Codex 모델 3종 모두 썼는지)은 사실대로 정리 |
| AI 도구 이용약관상 권리 확보 | 사용자 확인 필요 | Anthropic·OpenAI·Google 약관의 산출물 사용 조건 확인 |
| 잼 페이지 추가 요구사항 | 사용자 확인 필요 | 제출 직전 잼 페이지 재확인 |

## 금지 콘텐츠 (공식 규칙 8.e)
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 음란·선정·혐오·명예훼손 없음 | 사용자 확인 필요 | 대사 파일 대부분 placeholder — 최종 대사 확정 후 재확인 |
| 차별·혐오 조장 없음 (숫자 미신 존중 톤) | 사용자 확인 필요 | 최종 대사·아트 확정 후 확인 |
| 술·약물·담배 홍보 없음 | 사용자 확인 필요 | 배경 간판 등 아트 확정 후 확인 |
| 타인 권리 침해 없음 | 사용자 확인 필요 | 컨셉 이미지의 옛 이름 'LUCKCORP'(04_luckcorp_hq.jpg)는 빌드에 없음 |
| 실제 돈을 거는 듯한 도박 표현 피하기 | 사용자 확인 필요 | |

## 마감 후
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 11/14 이후 게임 파일 수정 금지 (페이지 디자인·설명·스크린샷은 가능) | 사용자 확인 필요 | 패키지 제거(C07) 등 빌드 관련 결정은 업로드 전에 |
| 프로젝트 삭제 금지 | 사용자 확인 필요 | |
| 투표 기간 배정 게임 3개 먼저 평가 | 사용자 확인 필요 | |

## 언어
| 항목 | 상태 | 근거 / 할 일 |
|---|---|---|
| 게임 UI 영어 (한국어 병기) | 완료 | `Loc.T(en, ko)`, 타이틀·일시정지 메뉴 언어 버튼, 기본값 영어(`Loc.cs` PlayerPrefs 기본 0) |
