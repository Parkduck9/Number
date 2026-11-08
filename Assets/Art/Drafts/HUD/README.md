# HUD 픽셀 아트 초안

**초안(draft), 게임에 연결하지 않음(not wired into the game).**
Codex (gpt-6.1-sol)가 작성한 Python 스크립트의 문자 격자로 모든 픽셀을 직접 지정했다.
이미지 생성 모델, 폰트, 리샘플링, 흐림, 안티앨리어싱, 그라데이션은 사용하지 않았다.

| 파일 | 크기(px) / 용도 |
|---|---|
| `face_0.png` ~ `face_5.png` | 각 48×48, 피격 0~5회: 사람 → 귀 → 코 → 눈 → 턱 → 칩·눈 표식 |
| `face_sheet.png` | 288×48, 위 6단계 가로 배열, 간격 없음 |
| `broadcast_digits_magenta.png`, `broadcast_digits_cyan.png` | 129×18, 0~9 순서. 원형 글리프 10×16 + 사방 1px 외곽선 = 셀 12×18, 셀 사이 1px |
| `broadcast_arcs_magenta.png`, `broadcast_arcs_cyan.png` | 23×16, 왼쪽·오른쪽 파동 각 11×16, 사이 1px |
| `broadcast_7_magenta.png`, `broadcast_3_cyan.png` | 40×18, 방송 숫자 조립 예시 |
| `holo_window_gold.png`, `holo_window_red.png` | 20×16, 숫자 없는 창 |
| `holo_digits_gold.png`, `holo_digits_red.png` | 79×11, 7×11 글리프(색 테두리 포함), 0~9, 사이 1px |
| `holo_gold_3.png`, `holo_red_0.png`, `holo_red_7.png` | 20×16, 창 안 숫자 조립 예시 |
| `src~/make_hud_drafts.py` | 유일한 생성 스크립트. `~` 폴더는 Unity 임포트 제외 |

팔레트: 얼굴 전체 공통 16색(`#05061A #0B0E2A #14162E #2A2C4A #E0A986 #C07F62 #8A5444 #1C1F4A #2E3366 #D8DCE8 #9097B0 #5A6178 #9AA3B8 #3FE6F0 #B8FFFF #D9A13A`).
방송 마젠타: `#FF3FB4 #FFC2E8 #3A0A2E #C02C8A #7A1E5C`, 시안: `#3FE6F0 #D0FFFF #062A33 #2A9CB0 #1A5A70`.
홀로그램 황금: `#FFD23F #E0A020 #FFF6C8 #FFB000`, 빨강: `#FF3B3B #C81E1E #FFE6E6 #FF2020`.
좌상단 광원, 단색 단계 음영. 얼굴 배경·테두리는 불투명, 나머지 빈 배경은 투명.
홀로그램 내부만 채움 알파 70, 3px 간격 스캔라인 알파 110; 창 테두리와 숫자는 알파 255.
창은 어두운 1px 사각 테두리와 밝은 3×3 L자 모서리만 사용한다. 숫자는 2px 근백색 획과 1px 밝은 외곽선으로 창보다 밝게 읽힌다.
얼굴 공통 베이스는 3×2 열린 눈(바깥쪽 흰자 1px), 비대칭 눈썹, 3px 비대칭 입, 왼섶이 위로 겹치는 저고리 V자 깃, 화면 오른쪽의 수직 2px 어깨끈을 사용한다.
방송 파동은 16px 높이의 둥근 괄호 3겹(1px 획, 사이 2px)이며, 조립 예시에서 파동과 숫자 셀 사이 간격은 2px이다.
5단계는 칩과 두 눈의 3×3 표식을 함께 추가하며, 각 단계에서 지정 부위 밖 픽셀은 유지한다.

재생성: Python 3와 Pillow가 있는 환경에서 `python Assets/Art/Drafts/HUD/src~/make_hud_drafts.py`.
실행 시 PNG별 크기·RGBA 고유 색상 수(투명색 포함)를 출력하고 팔레트·알파·누적 변경을 검사한다.
Unity 연결 시 Point 필터·압축 없음·PPU 32를 사용하고 실제 960×540 게임 화면 위에서 최종 확인할 것.
CREDITS.md 기록은 Claude 담당이며 여기서는 수정하지 않았다.
