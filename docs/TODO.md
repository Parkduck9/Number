# 남은 작업 목록 (2026-11-08 저녁 기준)

> 개발 마감 **2026-11-13 00:00 KST**, 제출 마감 2026-11-14 21:00 KST.
> 상태: ✅ 끝 / 🔄 진행 중 / ⏳ 대기(의존) / 👤 사용자가 할 일 / ☐ 시작 전
> 코드는 Sonnet 작성 → Opus(큰 건 Fable) 검수 → 커밋. 병렬 세션은 한 번에 최대 5개.
> 결정 근거: [design/DECISIONS.md](design/DECISIONS.md) (추천안 `[확정]`, 나중에 바꿀 수 있음). 구현 기록: [design/impl/](design/impl/). 패턴 정리: [design/ARCHITECTURE_PATTERNS.md](design/ARCHITECTURE_PATTERNS.md).

## 1. 지금 진행 중
| 작업 | 담당 | 상태 |
|---|---|---|
| QA 5개 영역 (전투·적/방송·흐름·보스/메뉴·빌드/제출) — Claude 2회 + Antigravity 2회, 리포트마다 검증 | QA 세션 5개 → `docs/qa/` | 🔄 |

## 2. 사용자가 할 일 (막혀 있는 순서)
| 할 일 | 왜 필요한가 |
|---|---|
| 👤 **Unity에서 Play 테스트** (6000.3.28f1로 열기, Title부터 끝까지) | 아직 아무도 실제로 해 보지 않음. 이펙트 크기·위치, 보스, 충격파 감각 확인 |
| 👤 **주인공 스프라이트 확정** (Card C 시안 또는 새 시안) | 동작 프레임·적 스프라이트 4종이 전부 여기에 묶임 |
| 👤 **오디오 청취·승인** — [청취 페이지](https://claude.ai/artifact/2RCARCepYagWREGz2i6PdF) | BGM을 직접 만든 곡 / CC0 곡 중 선택. 승인 후 `claude/audio-4c66f5` 병합 |
| 👤 **HUD 아트 시안 확인** — [검토 페이지](https://claude.ai/artifact/9bYK8AFpoTSt4VhdMLyVfZ) | 얼굴 체력 6단계·방송 숫자·홀로그램 창. 승인 후 게임 연결 |
| 👤 **대사 작성** (장면당 2~3줄, EN/KO, [DIALOGUE_GUIDE.md](design/DIALOGUE_GUIDE.md)) | STG-00 오프닝·STG-03 아지트·엔딩은 지금 자리표시 |
| 👤 다른 PC의 **네온 조명(Card H)·배경 도트(Card G)** 푸시 | GitHub에 없음 |
| 👤 C 드라이브 공간 정리 (약 11GB 남음) | 끝난 worktree의 Unity 캐시(각 2GB) 삭제 승인, 안 쓰는 Unity 버전 삭제 |
| 👤 팀원 정보, 만 19세 이상 확인 (CREDITS.md Team) | 제출 필수 |

## 3. Claude가 이어서 할 일
| 작업 | 의존 | 상태 |
|---|---|---|
| QA 결과 합치기 → 고칠 순서 정리 → 수정 (Sonnet 구현, Opus/Fable 검수) | QA 세션 완료 | ⏳ |
| 배경 아트 시안 STG-02~05 (6장, Codex) — `D:\Git\Number`에서 작업 | 세션 자리 | ⏳ (준비 완료) |
| 오디오 병합 + 소리 연결 (훅 목록: `design/impl/audio.md`) | 오디오 승인 | ⏳ |
| HUD 아트 연결 | HUD 시안 승인 | ⏳ |
| 적 스프라이트 4종 시안 (시민·원거리·안드로이드·AI 벌레) | 주인공 확정 | ⏳ |
| 주인공 동작 프레임 (대기·이동·점프·공격·패링·대시·피격) | 주인공 확정 | ⏳ |
| 배경 시안을 레벨 빌더에 연결 (자리표시 교체) | 배경 시안 승인 | ⏳ |
| 대사 넣기 | 대사 작성 | ⏳ |
| 이펙트 미세 조정 (크기·위치) | Play 테스트 피드백 | ⏳ |
| 대화 중 연출 애니메이션 (TASK-10) | **맨 마지막** | ⏳ |

## 4. 마무리 (11/12~)
| 작업 | 담당 |
|---|---|
| WebGL 빌드 (에디터 닫은 상태에서 CLI), 용량 1GB 이하 확인, 브라우저 실행 확인 | Claude |
| itch.io 업로드, "브라우저에서 플레이" 체크, 설명·조작법·스크린샷 | 👤 사용자 (Claude가 문구 초안) |
| CREDITS.md 최종 정리, 생성형 AI 사용 공개 문구 | Claude |
| 제출 체크리스트 최종 확인 (`docs/qa/SUBMISSION_CHECKLIST.md`) | Claude + 👤 |
| 정리: 남은 worktree·임시 파일 삭제 | Claude (승인 후) |

## 5. 끝난 것 (2026-11-08)
- ✅ 프로토타입: 이동·점프·공격·패링·대시, 숫자 예고, 체크포인트, 방송판·해킹, 타이틀·일시정지·언어
- ✅ 전투 이펙트 4종(pixelfx) + 대시 잔상, Fable 검수 반영
- ✅ 결정 10개 추천안 확정 ([DECISIONS.md](design/DECISIONS.md))
- ✅ 방송 3·7·0 규칙, 안드로이드 황금 탄, STG-01 SEG-B 방송 3
- ✅ STG-05 보스(전광판 AI 3웨이브), Space 충격파 업그레이드
- ✅ STG-00 오프닝, STG-02·03·04 빌더·씬, Title 씬, 빌드 순서 Title → STG00~05
- ✅ 병합 후 Fable 종합 검수 + 버그 7건 수정, 패턴 문서
- ✅ Unity 6000.3.28f1 설치, 컴파일 오류 0
