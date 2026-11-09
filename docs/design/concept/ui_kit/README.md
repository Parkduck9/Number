# NUMBER pixel UI kit — staging

2026-11-10 · AI 생성 (Codex). 전체 작업 사전 승인에 따라 만든 검토용 세트.
Assets에는 설치하지 않았으며 게임 연결·Unity Play 모드 검증은 수행하지 않았다.

## 디자인과 제작

- 1 source pixel 격자. 960×540 캔버스 목업에서 UI는 정수 2배로 표시.
- 각진 창틀과 작은 단청 회문·처마 끝 형태, 옥색 선, 주홍·황토색 못 장식. 장식은 고정 모서리에만 배치.
- UiTheme.cs 및 실제 Assets/Resources/UiTheme.asset의 #0B0F1E, #3A3F4B, #8A90A0, #C8CCD8, #FFFFFF 사용. panelFill의 반투명값은 가장자리 번짐 방지를 위해 불투명 남색으로 해석.
- Assets/Art/Palette/number_palette.png에서 #1A1D33, #42A9BD, #226076, #7A3A3C, #B8862E 등의 제한된 보조색 사용. 마젠타는 기존 broadcast_digits_magenta의 실제 강조색을 추출. 조준점은 코드의 #EDF2F7 / #4CE8D8, 외곽선 #14141F.
- 참고: DraftUi/ctrl_key_*, shockwave_icon, boss_embector_* 및 DraftHud/holo_window_*, face_*, broadcast_digits_magenta. 기존 프레임의 직각·어두운 금속층과 강조색 비율을 맞춤.
- Python 표준 라이브러리로 정수 좌표에 직접 픽셀 배치. 외부 생성 API·유료 서비스·추가 패키지 사용 없음. build_kit.py가 모든 최종 PNG와 검증 manifest를 재생성한다.
- 로고는 Mulmaru.ttf의 12px 비트맵 격자를 로컬 SingleBitPerPixelGridFit으로 추출한 _mulmaru_reference.png를 사용하고, 정수 3배·개별 자간·N/B의 1px 글리치 위치를 직접 구성. NUMB 흰색, ER #8A90A0. 정확한 N U M B E R 여섯 글자.
- 스킨에는 글자 없음. 예외는 요청한 NUMBER 로고와 앱 아이콘의 방송 숫자 0. 목업 문구는 합성용 Mulmaru 텍스트이며 스킨 파일에 포함되지 않음.
- 모든 최종 이미지 RGBA8, 알파 0/255. AA·블러·그라데이션 없음. 한지 질감은 고정 상하단의 희박한 어두운 점으로 표현하여 중앙 늘림에서 점이 길어지지 않게 함.

## 9-slice 경계

아래 값은 **원본 PNG의 (left, right, top, bottom) px**. Unity spriteBorder는 (x=left, y=bottom, z=right, w=top) 순서이므로 옮길 때 주의.
모서리는 원본 2배 크기를 유지하고 중앙·변만 늘린다. 9-slice 전체 파일을 먼저 비정수 리사이즈하지 않는다.

| PNG | 크기 | Left | Right | Top | Bottom | 최소 권장 표시 크기(2배) |
|---|---|---:|---:|---:|---:|---|
| panel_9s.png | 48×48 | 9 | 9 | 9 | 9 | 96×96 |
| button_9s_normal.png | 32×16 | 5 | 5 | 4 | 4 | 64×32 |
| button_9s_hover.png | 32×16 | 5 | 5 | 4 | 4 | 64×32 |
| button_9s_pressed.png | 32×16 | 5 | 5 | 4 | 4 | 64×32 |
| dialogue_box_9s.png | 64×48 | 10 | 10 | 8 | 8 | 128×96 |
| dialogue_nameplate_9s.png | 32×16 | 9 | 9 | 7 | 7 | 64×32 |
| hint_plate_9s.png | 32×12 | 4 | 4 | 3 | 3 | 64×24 |

Hover는 시안 외곽선, pressed는 어두운 위·왼쪽 1px inset. 버튼 글자는 별도 TMP로 배치하고 pressed에서 글자 위치를 원본 1px(표시 2px) 이동시키면 됨. 힌트 배경은 남색과 어두운 픽셀로 만든 불투명 스킨.

## 나머지 파일

| PNG | 크기 | 사용 메모 |
|---|---|---|
| dialogue_next.png | 8×8 | 기본 아래쪽 진행 화살표 |
| dialogue_next_2f.png | 16×8 | 좌→우 8×8 두 프레임, 두 번째는 1px 아래·어두운 시안. 권장 2fps |
| portrait_frame.png | 56×56 | 중앙 x=3..52, y=3..52의 50×50 완전 투명 영역 |
| hud_face_bezel.png | 56×56 | 중앙 x=4..51, y=4..51의 48×48 투명 영역. 우상단 숫자판은 별도 숫자를 얹는 빈 홈 |
| cursor_arrow.png | 16×16 | OS cursor hotspot=(0,0), 위왼쪽 기준 |
| crosshair_idle.png | 16×16 | 중앙 hotspot=(8,8), off-white 코어, 어두운 외곽선 |
| crosshair_lock.png | 16×16 | 같은 형태, #4CE8D8 코어 |
| lock_bracket.png | 8×8 | 위왼쪽 L. UI 사각형 모서리 기준 0/90/180/270도 회전 또는 미러 |
| enemy_pointer.png | 9×9 | 오른쪽(+X) 화살표. 흰색 스텐실, 중앙 피벗. 런타임 enemy #D77E54 / spawner #42A9BD 틴트용 |
| logo_number.png | 160×40 | NUMBER, NUMB 밝게 / ER 어둡게. N/B에만 1px 시안·마젠타 계열 오프셋 |
| logo_number_2x.png | 320×80 | 위 로고 nearest 2배 |
| app_icon.png | 128×128 | 기존 방송 마젠타 0 셀을 정수 6배, 남색 배경 |
| favicon_32.png | 32×32 | 같은 방송 0, 작은 시안 측면 표시 |
| portrait_android.png | 100×100 | 아래 초상화 변환 메모 참고 |
| portrait_bug.png | 100×100 | 아래 초상화 변환 메모 참고 |
| mockup_preview.png | 960×540 | 화면 전체 1배 목업, UI는 기본 source pixel 2배 |
| mockup_preview_2x.png | 1920×1080 | 목업 전체 nearest 2배 |

16px 조준점은 실제 잉크 중심이 (7,7) 픽셀에 있으므로, 짝수 캔버스의 기하학적 중심과 0.5px 차이가 있다. 현재 MouseCrosshair는 11×11을 픽셀 중앙에 스냅한다. 연결 담당자는 피벗을 (7.5/16, 8.5/16)로 지정하거나 스냅을 짝수 크기에 맞춰 조정해야 한다(이미지의 y는 위에서 아래, Unity 피벗은 아래에서 위). 여기서는 스크립트를 수정하지 않았다.

## 초상화 원본과 임포트

- 원본: Assets/Art/Drafts/Portraits/lineup/portrait_android.png 및 portrait_bug.png, 각각 48×48.
- 기존 Resources/Portraits의 yudo / citizen / luddite / broadcast 파일은 모두 **100×100**. yudo.png.meta는 Sprite Single, PPU 48, Point, Clamp, mipmap off, alpha transparency on, 기본 압축 off, center pivot이다. spriteSheet 안의 과거 작은 rect 항목은 Single 모드의 실제 PNG 크기를 대체하지 않는다.
- 두 새 파일은 원본을 nearest 2배하여 96×96으로 만들고 사방 2px 투명 여백으로 100×100에 배치. 각각 16색을 유지하고 새 색·디더링·반투명 픽셀을 추가하지 않았다. 기존 portrait와 동일한 RGBA PNG 형식, 작은 격자가 2×2 픽셀로 표시되는 처리.
- 새 .meta는 만들지 않았다. 추후 임포트 시 위 설정을 적용하고 WebGL에서도 압축 off를 권장. UI 스킨은 PPU 32 기준, 초상화는 기존 PPU 48을 유지하며 UI RectTransform으로 크기 지정.
- 대화 portrait 영역은 표시 100×100(원본 UI 기준 50×50). 따라서 100×100 초상화 파일에 별도로 2배 확대를 더 적용하지 않는다.
- 목업의 대화창은 android, 오른쪽 소형 비교 인셋은 bug를 표시한다. bug 인셋은 샘플 비교용이며 확정 게임 HUD 요소가 아니다.

## 연결 담당자 메모

- 스테이징만 생성. Assets, 스크립트, 씬, 프리팹은 편집하지 않음. 커밋하지 않음.
- 모든 UI 이미지: Sprite / Full Rect / Point / Clamp / mipmap off / 압축 off / native size에 정수 배율. Image는 9-slice 파일에 Sliced, 나머지는 Simple. raycast 필요 없는 장식은 꺼둘 것.
- MouseCrosshair.cs의 기존 CrossSprite=11×11, CornerSprite=5×5, SpriteFx.Ppu 사용. 새 파일은 요청대로 16×16, 8×8이므로 기존 사이즈를 그대로 가정하지 말 것. idle/lock PNG는 이미 색이 들어가 있어 SpriteRenderer.color=white로 쓰고 상태별 sprite를 교체해야 중복 틴트가 생기지 않음. 브래킷도 같은 규칙. 기존 코드는 white core에 틴트를 곱하는 방식이다.
- EnemyPointer.cs의 표시 RectTransform은 27×27(9×9의 정수 3배), 회전 0도=오른쪽. 제공한 흰색 스텐실은 기존 enemy/spawner 틴트를 그대로 쓸 수 있다. 코드의 동적 텍스처 생성 부분을 파일 로딩으로 바꾸는 작업은 코드 담당자에게 남김.
- TitleScreen.cs는 NUMB/ER를 TMP 색상으로 나누고 있음. 교체 시 로고 PNG 자체의 dim ER 색을 유지하도록 white tint 사용.

## 검증 및 검토 이미지

- manifest.json: 최종 요청 PNG 24개 각각의 실제 크기·불투명 색 개수·알파 집합·SHA-256. borders.json: 기계 판독용 원본 경계값.
- 생성 시 모든 최종 PNG의 알파 0/255, 두 프레임 중앙 투명도, 모든 9-slice 중앙 단색을 assert로 검증.
- review_4x.png / review_4x_2.png: 원본 자산 4배 검토 시트. 목업은 별도로 960×540과 nearest 2배를 확인.
- 배경은 남색·기와 처마 도시 실루엣. 목업에서 panel/세 버튼(hover 1개)/대화창/초상화 프레임/이름표/HUD face bezel/hint/cursor/crosshair/lock bracket/enemy pointer/logo 및 두 새 초상화를 함께 확인.
- Unity에서의 실제 9-slice 설정, 상태 전환, 브래킷 회전·피벗, WebGL 커서 처리는 아직 연결 전이므로 Play 모드 검증 대상.
- 작업 전 git pull은 시도했으나 이 셸에서 git 실행 파일을 찾지 못해 동기화하지 못했다. 네트워크 설치도 제한되어 외부 패키지 없이 제작했다.

재생성: 저장소 루트에서 `python docs/design/concept/ui_kit/build_kit.py`. 추가 의존성 없음. 폰트 참조 atlas도 이 폴더에 포함되어 있어 재생성에 Windows 폰트 API는 필요하지 않음.
