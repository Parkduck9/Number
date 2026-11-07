# 대사 시스템 가이드

대사는 **텍스트 파일 하나**에 적습니다. 코드는 건드리지 않아도 됩니다.

## 1. 모드 두 가지

| 모드 | 동작 | 화면 | 진행 |
|---|---|---|---|
| `radio` | 게임이 멈추지 않음 | 왼쪽 위 구석에 작은 초상화 + 자막 | 시간이 지나면 자동으로 다음 줄 |
| `talk` | 이야기 장면. 플레이어 조작과 적이 멈춤 | 화면 아래 대화창 (초상화 + 이름 + 대사) | 마우스 왼쪽 클릭 또는 E (타이핑 중 누르면 한 번에 표시) |

## 2. 대사 파일 쓰는 법

위치: `Assets/Data/Dialogue/` 아래에 `.txt` 파일 (씬마다 하나씩 권장). UTF-8로 저장하세요.

- `#`으로 시작하는 줄은 주석, 빈 줄은 무시됩니다.
- 첫 줄 근처에 `@mode radio` 또는 `@mode talk`를 한 번 적습니다. (파일 하나 = 모드 하나)
- 대사 한 줄: `화자ID | 한국어 | English`
- radio 전용: 맨 끝에 `| 초 수`를 붙이면 그 줄이 떠 있는 시간을 정합니다. 안 붙이면 글자 수에 맞춰 자동.

radio 예시:
```
@mode radio
broadcast | 오늘의 행운의 숫자는… 0입니다. | Today's lucky number is... zero.
broadcast | 시민 여러분은 침착하세요. | Citizens, please stay calm. | 3.5
```

talk 예시:
```
@mode talk
yudo | 0이요? 그럼 숨 0번 쉬면 최고의 하루겠네요? | Zero? So zero breaths makes the luckiest day, huh?
citizen | …… | ...
```

샘플: `sample_radio.txt`, `sample_talk.txt` (`[샘플]` 표시가 붙은 줄은 지워도 되는 임시 대사).

## 3. 화자(Speaker) 추가 / 초상화 넣기

1. `Assets/Data/Dialogue/Speakers.asset`을 선택합니다.
2. Inspector의 `Speakers` 목록에서 `+`로 항목을 추가합니다.
   - `Id`: 대사 파일에 쓰는 영문 소문자 ID (예: `yudo`)
   - `Name Ko` / `Name En`: 화면에 보일 이름
   - `Portrait`: 초상화 스프라이트 (비워 두면 `Color` 색의 네모 블록이 대신 보임)
   - `Color`: 이름 글자색
3. 현재 등록된 ID: `yudo`(안유도/Yudo), `broadcast`(방송/Broadcast), `citizen`(시민/Citizen)
4. 목록에 없는 ID를 쓰면 ID 글자 그대로 이름으로 표시되고 회색 블록이 나옵니다.

초상화 이미지는 비주얼 규칙(`ANTI_SLOP.md`)에 따라 시안 확인 후 넣습니다.

## 4. 씬에 트리거 배치

1. 씬에 `DialoguePlayer` 오브젝트가 있어야 합니다 (프로토타입 씬은 빌더가 자동으로 넣음). 씬마다 하나.
2. 빈 오브젝트를 만들고 `Box Collider 2D`(Is Trigger 체크) + `Dialogue Trigger`를 붙입니다.
3. `Lines`에 대사 `.txt` 파일을 끌어다 놓습니다.
4. 플레이어가 영역에 들어오면 한 번만 재생됩니다.

프로토타입 씬(`NUMBER/Build Prototype Scene`)에는 radio 샘플(x=-2)과 talk 샘플(x=14) 트리거가 있습니다.

## 5. 언어

- 기본은 **영어**, 프로토타입에서는 **L 키**로 한국어/영어를 전환합니다. 선택한 언어만 표시됩니다.
- 한국어 칸이 비어 있으면 영어가, 영어 칸이 비어 있으면 한국어가 대신 나옵니다.
- 이미 화면에 떠 있는 줄은 그대로, 다음 줄부터 바뀐 언어가 적용됩니다.

## 6. 제한과 권장

- 한 장면당 **2~3줄** 이내 (GDD 9장). 한 줄은 짧게: 화면에 2줄 넘게 들어가지 않도록 (한국어 약 40자 이내).
- radio 줄은 순서대로 쌓여서 하나씩 재생됩니다 (새 파일이 재생되면 현재 줄 뒤에 이어짐).
- talk 중에는 조작이 막히고 적의 AI가 멈춥니다 (`GameFlow.InputLocked`). 장면이 끝나면 풀립니다.
- 글꼴은 Mulmaru입니다. 이모지나 글꼴에 없는 특수문자는 쓰지 마세요.
- 모양(색, 크기, 위치)은 임시입니다. 최종 스타일은 HUD 시안 확정 후 `DialoguePlayer.cs` 위쪽 `Style` 상수에서 한꺼번에 바꿉니다.
