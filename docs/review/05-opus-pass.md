# 리뷰 05 — Opus 검수·수정 (2026-11-08)

> 대상: 01-fixes 이후 커밋 `f9164ff`(Fable 수정), `7d0380c`(안드로이드 과부하), `89dacdc`(STG-00 BREATH ZERO), `fa22aa6`(STG-01/02/04 적 배치 + 씬), `398df34`(BugSwarmVisual). `git diff f9164ff~1..HEAD -- Assets`와 impl 문서 4개를 읽고, 생성된 씬 YAML이 빌더와 같은지 확인했다.
> 추가로 7d6b735에서 사용자가 확정한 QA 답 2개(01-fixes #6, #11)를 코드에 넣었다. 해킹 다이얼 시점(01-fixes #5, `allowHack`)은 qa-boss 세션 담당이라 손대지 않았다.
> 배치 모드 컴파일: `error CS` 0건. 빌더는 바꾸지 않았으므로 씬은 다시 만들지 않았다. 폰트 에셋은 바뀌지 않아 되돌릴 것이 없었다. Play 모드는 실행하지 않았다.

| 문제 | 근거 | 수정 | 상태 |
|---|---|---|---|
| 보스 3웨이브 **예고(2초) 중** 해킹하면 적 소환 없이 정지 버튼이 열림 (01-fixes #6, DECISIONS "예고 중 해킹 무시") | `Assets/Scripts/Boss/BossFight.cs:59` — `StartWave`가 `waveIndex`를 먼저 올리고 Announce 상태에서도 Unlock | Unlock 조건을 `state == Spawning \|\| state == Fighting`으로. 예고 중 해킹은 "새 해킹"으로 소비되고 무시됨 → 적이 나온 뒤 다시 해킹해야 열림 | 수정됨 |
| 충격파가 이미 열린 반격 창(Open/Staggered, NUM-3 1.5초·NUM-7 탈진 2초)을 1초로 줄임 (01-fixes #11, DECISIONS "더 긴 쪽 유지") | `Assets/Scripts/Enemy/TestEnemy.cs` `ForceStagger` — 상태를 무조건 Staggered 1초로 덮어씀 | Open/Staggered이고 남은 시간이 충격파 경직(`seconds`) 이상이면 그대로 둠. 더 짧으면 기존처럼 경직. Overload·Stunned·Frozen은 기존대로 무시 | 수정됨 |
| 히트스톱 입력 버퍼 (`f9164ff`) | `PlayerController.cs` `BufferDuringHitStop`, `pendingParry/pendingDash` | 점프·공격은 버퍼 시각, 패링·대시는 끝나는 프레임에 한 번 처리. InputLocked·일시정지에서 초기화. 문제 없음 | 확인 |
| 충격파 HUD·히트스톱 중 Space (`f9164ff`) | `Shockwave.cs` `pendingPress`, `UpdateHud` | 일시정지 중엔 입력이 잠겨 새지 않음. 문자열은 0.1초 단위가 바뀔 때만 생성. 캔버스는 씬 오브젝트라 씬과 함께 사라짐 | 확인 |
| 탄 BoxCast 스윕 (`f9164ff`) | `EnemyProjectile.cs:43-58` | 시작 겹침도 잡음(기존 동작 유지). 프레임당 할당 없음. STG-01 발판 위 원거리 적의 탄은 발판 위로 지나감(탄 아래 3.9 > 발판 2.85) | 확인 |
| 안드로이드 과부하 (`7d0380c`) | `TestEnemy.cs` DoStrike/Overload/ReceivePlayerHit, `EnemyNumber.SetBroken` | 들어가기(마지막 타 패링, 7번째 대시 통과)·끝(FinishReset에서 ERR 해제)·처치(Hide에서 해제, 0.5초 뒤 사라짐) 모두 정상. 보스 클리어 판정(Stunned), NUM-0(안드로이드는 안 얼음), NUM-7 탈진·충격파는 과부하 중 건너뜀. 원거리 안드로이드는 없음(빌더 확인). 사용자 결정(과부하 밖은 전부 막음, 7번째는 대시 통과만)과 일치 | 확인 |
| BREATH ZERO 오프닝 (`89dacdc`) | `BreathZero.cs`, `OpeningSequence.cs`, `OpeningCitizen.cs` | 소리 끔은 정상 종료·씬 전환·재시작(OnDestroy) 모두에서 복구. 전역 Light2D 1개만 어두워지고 TMP 숫자는 빛 영향 없음. STG-00에는 TestEnemy가 없어 방송 0을 먼저 띄워도 시민이 일찍 굳지 않음 | 확인 |
| 버그 점 떼 외형 (`398df34`) | `BugSwarmVisual.cs`, `BugSwarmBootstrap` | 비활성 보스 템플릿에 붙어 복제본이 물려받음(템플릿은 Start 전이라 자식 중복 없음). 화면 밖이면 계산 생략, 프레임당 할당 없음(96×96 업로드만). 리플렉션은 직렬화 필드라 스트리핑 위험 낮음 — WebGL 빌드에서 벌레가 사각형으로 나오면 public getter로 교체 | 확인 (빌드 확인 필요) |
| 적 배치 (`fa22aa6`) | `Stg01/02/04Builder.cs`, 씬 YAML의 `E-*` 이름·`ranged`·`kind 2` | 씬이 빌더와 같음(STG-01 8, STG-02 10, STG-04 11기). 적과 플레이어는 충돌하지 않아 길을 막는 적 없음. LEDGE-A1(0.6u 단단한 계단)은 점프로 오름. 숫자 상한 4는 STG-02 후반 전까지 지켜짐 | 확인 |

## Play 모드 확인 목록
| 무엇을 하면 | 무엇이 보여야 하나 |
|---|---|
| STG-05에서 3웨이브 제목("WAVE 3 - LUCKY 0")이 떠 있는 2초 동안 전광판 해킹 | 정지 버튼이 **열리지 않고** 적이 나온다. 적이 나온 뒤 다시 해킹하면 열린다 |
| STG-05 3웨이브 적이 나오는 중/싸우는 중 해킹 | 바로 정지 버튼이 열린다 (기존과 같음) |
| STG-01 SEG-B(방송 3)에서 근접 적이 3연타를 끝내고 멈춘 직후 Space(F9로 해금) | 빈틈이 1초로 줄지 않고 1.5초 그대로 (적이 숨 고르는 시간이 더 김) |
| 같은 적의 빈틈이 거의 끝날 때 Space | 경직이 1초로 늘어난다 |
| 연타 중인 적에게 Space | 연타가 끊기고 1초 경직 (기존과 같음) |
| 과부하 중인 안드로이드에게 Space | 과부하가 끊기지 않는다 |

---

## 2차 (같은 날) — 타이틀·엔딩 다이얼·allowHack·팔레트 도구
> 대상: `4ceb214`(타이틀), `3a2cf71`(엔딩 선택이 숫자 다이얼 재사용, `NumberDial` 공유), `2a7bcf5`(allowHack, STG-01/02 HUD만), `4293c9f`(PaletteQuantizer, 코드만). 배치 모드 컴파일 `error CS` 0건(현재 HEAD). **확정된 버그 없음 → 코드 수정 없음.** 빌더를 바꾸지 않았으므로 씬은 다시 만들지 않았고, 폰트 에셋도 바뀌지 않았다.

| 문제 | 근거 | 수정 | 상태 |
|---|---|---|---|
| 엔딩 흐름이 멈추거나 두 번 실행될 수 있나 | `StopButton.cs` Idle→Choosing→Dial→Playing→Done | 죽은 상태면 Choosing/Dial 모두 Cancel(잠금 해제). Esc/P/Backspace는 `EscConsumedFrame` + 잠금 해제가 같은 프레임이라 일시정지가 같이 열리지 않음(`PauseMenu` 실행 순서 1000 + `lockedAtFrameStart`). 다이얼의 Esc·P는 선택지로 돌아가며 잠금 유지. E 확정과 같은 프레임의 E는 대사 입력 보호(0.25초)로 첫 줄을 넘기지 않음. `Finish`는 Done으로 바꾼 뒤 한 번만 타이틀 로드. 대사 파일·DialoguePlayer가 없으면 바로 타이틀(잠금 해제) | 확인 |
| 언어 전환 | `StopButton.RefreshTexts`, `NumberDial.RefreshTexts` | 프롬프트·선택지·다이얼 제목·힌트·0 경고 모두 다시 씀. BuildUi 전에는 null 가드로 건너뜀. 정지 버튼은 보스 승리 전 비활성이라 OnEnable에서 구독 | 확인 |
| 공유 다이얼 리팩터링이 해킹 다이얼을 망가뜨렸나 | `BroadcastHackUi.cs` UpdateDial, `NumberDial.Read` | 키 우선순위(Esc > E > 숫자/화살표/휠), Esc 소비, 열던 프레임에 E로 바로 확정되지 않음(if/else), 걸어 나가거나 잠기거나 죽으면 닫힘 — 모두 기존과 같음. 문자열은 값이 바뀔 때만 생성 | 확인 |
| BuildHud 변경이 STG-04/05 해킹·보스 전광판을 막나 | 씬 YAML `allowHack` | STG-01·02 = 0(HUD만), STG-04 = 기본값(true), STG-05는 자체 빌더라 기본값(true). 보스 전광판·`BossFight` 해킹 판정은 `BroadcastBoard`를 직접 읽어 영향 없음. STG-03은 다시 만들지 않았지만 전광판이 없어 차이 없음 | 확인 |
| 타이틀 입력·AudioListener·새 게임 초기화 | `TitleScreen.cs:25-31`, `Title.unity` | `RespawnManager.ResetState`, `Shockwave.SetUnlocked(false)`, `timeScale 1`, `InputLocked false` 그대로. 씬에 AudioListener 있음(+런타임 보충). 조작법 패널은 버튼/Esc로 토글, 플레이어가 없어 일시정지 메뉴는 안 열림. 깜빡임은 단계가 바뀔 때만 색을 씀 | 확인 |
| PaletteQuantizer | `Assets/Editor/PaletteQuantizer.cs` | 에디터 전용, 확인 창 뒤에만 PNG 덮어씀, 팔레트 파일 자체는 건너뜀. 팔레트 PNG는 Resources 밖이라 빌드에 안 들어감 | 확인 |

### Play 모드 확인 목록 (2차)
| 무엇을 하면 | 무엇이 보여야 하나 |
|---|---|
| STG-05 정지 버튼 → 2 → 다이얼에서 Esc | 1/2 선택지로 돌아감 (일시정지 안 열림), 다시 Esc면 게임으로 |
| 다이얼에서 숫자 고르고 E | END-02 대사가 시작되고 첫 줄이 바로 넘어가지 않음, 끝나면 타이틀 |
| 선택지/다이얼에서 일시정지 → 언어 전환이 안 되므로 대신 언어를 바꾼 뒤 정지 버튼 열기 | 프롬프트·선택지·다이얼 제목·0 경고가 바꾼 언어 |
| STG-01·02 전광판 아래에서 E | 프롬프트·다이얼 없음, 위쪽 방송 숫자 HUD는 보임 |
| STG-04 전광판 아래에서 E → 숫자 → E | 해킹됨 (기존과 같음) |
| 타이틀 → Controls → Esc → Start | 패널이 닫히고, 새 게임은 충격파 잠김·체크포인트 기록 없음으로 시작 |
