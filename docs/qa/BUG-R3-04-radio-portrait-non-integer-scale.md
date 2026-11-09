작성: Claude Opus (QA 3라운드, 정적 검사)

# BUG-R3-04 무전(radio) 대화창 초상화가 100px 그림을 48칸에 줄여 픽셀이 깨짐

- 관련: 435d87d (대화 초상화), 0a98ac6 (대화창 초상화 96 → 100)
- 근거 코드: `Assets/Scripts/Dialogue/DialoguePlayer.cs:179` 무전 상자 초상화 48 (참조 960x540), `:181` 대화 상자 100
- 그림: `Assets/Resources/Portraits/{yudo,broadcast,citizen,luddite}.png` 모두 **100x100** (Point 필터)
- 주석 불일치: `DialoguePlayer.cs:157` "48x48, shown 1x radio / 2x talk" — 실제 그림은 100x100

## 재현 단계
1. STG-01 시작 → TR-A-HINT(x 7~8)에서 무전 힌트(`stg01_hint_combat.txt`, yudo/broadcast 화자)가 뜬다.
2. 1920x1080 창에서 왼쪽 위 무전 상자의 초상화를 확대해 본다.

## 기대 결과
- 픽셀 아트가 정수 배율로 보인다 (대화 상자는 100 → 1080p에서 200px = 정확히 2배).

## 실제 결과
- 무전 상자는 48 참조px → 1080p에서 96px. 100px 그림을 0.96배 최근접 축소하므로 행·열 4줄씩이 빠져 눈·윤곽선이 들쭉날쭉해진다 (ANTI_SLOP 체크리스트의 "픽셀 크기 불균일"에 해당).

## 심각도
낮음 (보이는 품질)

## 참고 (수정 방향)
- 무전 초상화를 50(1080p에서 정확히 1배)으로, 또는 무전용 50x50 그림을 따로. 상자 높이 84에 50은 들어간다. 주석도 100x100으로 고친다.

수정: 무전 초상화 48 -> 50 참조px (1080p에서 100px = 정확히 1배), DialoguePlayer 주석을 100x100 기준으로 고침.
