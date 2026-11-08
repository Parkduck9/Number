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
