# 대사 초안 (DIALOGUE_DRAFT) — Claude 초안, 사용자가 고친다

> 2026-11-08 Claude(Fable) 작성. **초안이다.** 사용자가 고른 뒤 `Assets/Data/Dialogue/*_placeholder.txt`에 붙여 넣는다.
> 근거: [GDD.md](GDD.md) 3·4·9·10장 `[확정]`, [DECISIONS.md](DECISIONS.md) 6-A·8-A·4-A, [DIALOGUE_GUIDE.md](DIALOGUE_GUIDE.md), [JAM_RULES.md](../../JAM_RULES.md) 금지 콘텐츠.
> `[확정]` 사실만 쓰고, 새로 지어낸 설정은 `[제안]`으로 표시했다. 플레이스홀더 파일의 **형식·화자 ID·파일 이름은 그대로** 유지했으므로 아래 코드 블록을 파일 내용 전체로 덮어쓰면 바로 돈다.

## 0. 톤 메모 (읽고 나서 고치기)

- **유도(yudo):** 건조하고 세상 물정에 밝은 22세. 감탄사·외침 최소. 농담은 하지만 웃지 않는다. 긴 설명 안 함.
- **방송(broadcast) = Embector AI 목소리:** 안내 방송·경고문 어투. 1인칭을 거의 안 쓰고 "공지", "시민 여러분은 ~하십시오", "Notice.", "Citizens are advised..." 식. 감정 단어 없음. 엔딩에서만 딱 한 번 "제가/I"가 나온다 (정체 드러남).
- **러다이트(luddite):** 말수 적은 어른. 반말, 짧은 문장. 설명하지 않고 물건을 준다.
- **시민(citizen):** 말이 거의 없다. 방송 문장을 따라 한다.
- 한국어 한 줄 **40자 이내**, 영어는 화면 2줄 이내. 장면당 2~3줄이 기본(아지트만 예외, 2-B에서 "대화만" 방이라 4~6줄).
- 글꼴(Mulmaru)에 없는 글자 피하기: 이모지 없음, 줄표(—) 없음. 말줄임표는 기존 파일과 같이 KO `…`, EN `...`.
- 안전 체크(GDD 16장): 숨 멈춤은 **"참는다/굳는다"**까지만, 죽음·고통 묘사 없음. 술·담배·도박·복권 단어 없음. 종교 연상 없음. 특정 국적·문화 희화화 없음.

### 코드 쪽에서 알아 둘 것 (대사만 바꿀 때 걸리는 점)
| 항목 | 내용 | 대응 |
|---|---|---|
| LINE-01 길이 | `OpeningSequence`는 방송 시작 4초 뒤 농담(LINE-02) 대화창을 연다 ([impl/opening-breath-zero.md](impl/opening-breath-zero.md)). 라디오 줄 초 수 합이 4초를 넘으면 대화창이 떠 있는 동안 구석 자막이 계속 흐른다 (깨지진 않음) | 합계 ≈4초로 맞췄다. 더 길게 쓰려면 `OpeningSequence.broadcastWait`(T)를 늘린다 → Sonnet |
| 화자 `luddite` | `Speakers.asset`에 없어 회색 블록 + "luddite" 글자로 나온다 | 인스펙터에서 한 줄 추가: Id `luddite`, 러다이트 / Luddite, 색 #8FBF6A(빌더 NPC 색과 같게) `[제안]` |
| 회사 이름 | GDD 3장: 회사 이름은 설정 값 한 곳에서 불러오게 되어 있다. 대사 txt에 "Embector/엠벡터"를 쓰면 하드코딩이 된다 (보스 전광판 `BossBoardView`도 이미 하드코딩) | 아래 아지트 대사에 **이름 없는 판(회사)** 과 **이름 있는 판** 둘 다 둠. 사용자 선택 |
| END-02 "숫자 입력창" | 플레이스홀더 주석에 "number prompt"가 있지만 실제 코드엔 입력창 없음 (`StopButton` → 1/2 선택 → 대화 → 타이틀) | 대사 안에서 방송이 "입력하십시오"라고만 말하고 숫자는 보여 주지 않게 썼다. 입력창을 진짜로 만들지는 범위 밖 |
| 보스 전광판 문구 | `BossBoardView.cs`에 "EMBECTOR / ...귀엽군. / 정지 버튼 열림", 웨이브 제목은 `BossFight.Waves`에 하드코딩 | 7장에 대안만 적었다. 바꾸려면 Sonnet이 코드 상수 수정 |

---

## 1. STG-00 LINE-01 — 아침 방송 (radio)
파일: `Assets/Data/Dialogue/stg00_line01_placeholder.txt`
상황: 아침 저잣거리. 전광판이 1→0으로 바뀌며 차임. 시민들은 걷는다. (GDD LINE-01은 `[제안]` 상태라 문장을 바꿔도 된다.)

**Take A (추천) — 날씨 예보처럼 평범하게**
```
@mode radio
broadcast | 시민 여러분, 오늘의 행운의 숫자는… 0입니다. | Citizens, today's lucky number is... zero. | 2.5
broadcast | 오늘도 숫자대로 사십시오. | Live by the number today, as always. | 2
```

**Take B — 경고문 어투를 처음부터**
```
@mode radio
broadcast | 공지. 오늘의 행운의 숫자는 0입니다. | Notice. Today's lucky number is zero. | 2.5
broadcast | 숫자를 따르는 하루 되십시오. | Have a compliant day. | 2
```

**Take C — 한 줄만 (GDD 원문)**
```
@mode radio
broadcast | 오늘의 행운의 숫자는… 0입니다. | Today's lucky number is... zero. | 3.5
```

- A/B의 두 번째 줄은 "이 도시가 숫자를 얼마나 당연하게 여기나"를 한 번에 보여 주는 줄. 없어도 진행에는 문제없다.

## 2. STG-00 LINE-02 — 유도의 농담 (talk) `[확정]` 문장
파일: `Assets/Data/Dialogue/stg00_line02_placeholder.txt`
상황: 방송 직후 대화창. 입력 잠김. 플레이어가 클릭/E로 닫으면 0.3초 정적 뒤 컷.
**첫 줄은 GDD LINE-02 `[확정]` 문장 그대로다.** 바꾸려면 사용자 확인 + GDD 변경 기록이 필요하다. 아래 B·C는 `[제안]`.

**Take A (확정 문장 + 시민 반응 1줄)**
```
@mode talk
yudo | 0이요? 그럼 숨 0번 쉬면 최고의 하루겠네요? | Zero? So zero breaths makes the luckiest day, huh?
citizen | …… | ...
```

**Take A-2 (확정 문장만, 플레이스홀더 그대로)**
```
@mode talk
yudo | 0이요? 그럼 숨 0번 쉬면 최고의 하루겠네요? | Zero? So zero breaths makes the luckiest day, huh?
```

**Take B `[제안]` — 더 시니컬하게**
```
@mode talk
yudo | 0? 좋네요. 오늘 제일 운 좋은 일은 숨 안 쉬는 거겠네. | Zero? Great. So the luckiest thing I can do today is not breathe.
citizen | 숫자 앞에서 농담하지 마. | Don't joke in front of the number.
```

**Take C `[제안]` — 짧고 가볍게**
```
@mode talk
yudo | 숨 0번, 걱정 0개. 누가 먼저 해 볼래요? | Zero breaths, zero worries. Who's first?
citizen | …… | ...
```

- 시민의 `……`는 "아무도 안 웃는다"를 보여 주는 한 줄. 컷 직전의 정적(0.3초)과 이어진다.
- 농담은 **유도가 시키는 말이 아니다.** 도시가 멈추는 건 AI가 숫자를 "실제로 맞게" 만들기 때문(GDD 3장 `[확정]`). 그래서 B의 시민 대사처럼 유도를 탓하는 뉘앙스는 조금만.

## 3. STG-00 LINE-03 — 도시가 숨을 멈춘다 (radio)
파일: `Assets/Data/Dialogue/stg00_line03_placeholder.txt`
상황: 컷. 조명 거의 꺼지고 소리 끊김, 전광판 0만 밝음. 시민이 한 명씩 굳는다. 1초 뒤 출구(오른쪽) 열림. 라디오 모드라 게임은 안 멈춘다.
GDD LINE-03 `[확정]`은 **지문** "(도시 전체가 숨을 멈춘다)"이다. 화면에 지문을 자막으로 띄울지, 대사로 바꿀지는 사용자 선택.

**Take A (추천) `[제안]` — AI가 농담을 받아 방송한다**
```
@mode radio
broadcast | 공지. 시민 여러분은 숨을 참으십시오. | Notice. Citizens, hold your breath. | 3
yudo | …진짜로 하네. | ...They're actually doing it. | 2
yudo | 여기 있으면 안 되겠다. 달려. (Shift: 대시) | Can't stay here. Run. (Shift: dash) | 3
```
- `[제안]` 지어낸 것: "AI가 유도의 농담을 그대로 숫자 규칙으로 만든다"는 해석. GDD 3장 "숫자가 실제로 행운을 가져다주게 만든다"와 맞지만 명시된 설정은 아니다.

**Take B — 방송 없이 유도만 (정적을 살림)**
```
@mode radio
yudo | …다들 왜 그래요? | ...Hey. Why did everyone stop? | 2.5
yudo | 숨을 참고 있어. 전부. | They're holding their breath. All of them. | 2.5
yudo | 달려. (Shift: 대시) | Run. (Shift: dash) | 2.5
```

**Take C — 지문 그대로 + 한 줄**
```
@mode radio
broadcast | (도시 전체가 숨을 멈춘다) | (The whole city stops breathing.) | 3
yudo | …달려. (Shift: 대시) | ...Run. (Shift: dash) | 3
```

- `(Shift: 대시)` 조작 힌트: 오프닝이 "이동·대시 배우기" 장면(DECISIONS 6-A)이라 넣었다. HUD에 키 안내가 따로 생기면 빼도 된다.
- "숨을 멈춘다/참는다"만 쓰고 쓰러짐·죽음 단어는 안 썼다 (GDD 16장).

## 4. STG-03 러다이트 아지트 (talk)
파일: `Assets/Data/Dialogue/stg03_luddite_placeholder.txt`
상황: 방 하나, 대화만. 러다이트에게 다가가면 대화 → 끝나면 `Shockwave.SetUnlocked(true)` + 출구 열림. 여기서 **충격파(Space)** 를 준다(8-A `[확정]`, "AI 없는 특수 공격 부품"). 해킹(E, 다이얼)은 STG-04부터 쓸 수 있다(GDD 6장 "STG-03 이후" `[확정]`). 정지 버튼은 AI를 만든 사람들이 남겼다(`[확정]`).

**Take A (추천) — 5줄, 회사 이름 없음**
```
@mode talk
luddite | 방송을 듣고도 뛰는 사람은 오랜만이군. | Been a while since I saw someone run after a broadcast.
yudo | 농담 한마디 했더니 도시가 숨을 참더군요. | I made one joke and the whole city held its breath.
luddite | 농담 탓이 아니야. 숫자 탓이지. 우린 머리에 칩이 없어서 안 들려. | Not the joke. The number. We never took the chip, so we don't hear it.
luddite | 받아. AI 없는 물건이다. 스페이스로 쓴다. | Take this. No AI inside. Press Space to use it.
luddite | 전광판은 E로 열어 숫자를 돌릴 수 있다. 오래는 못 버텨. 꼭대기에 정지 버튼이 있다. | Boards open with E. Turn the dial. It won't hold long. The stop button is at the top.
```
- 마지막 줄 한국어가 40자를 넘는다(약 46자). 화면에서 2줄 넘으면 아래 Take B처럼 두 줄로 나눈다.

**Take B — 6줄, 회사 이름 있음, 한 줄 40자 이내**
```
@mode talk
luddite | 방송을 듣고도 뛰는 사람은 오랜만이군. | Been a while since I saw someone run after a broadcast.
yudo | 농담 한마디 했더니 도시가 숨을 참더군요. | I made one joke and the whole city held its breath.
luddite | 농담 탓이 아니야. 엠벡터가 보내는 숫자 탓이지. | Not the joke. The number Embector sends.
luddite | 우린 머리에 칩을 안 박았다. 그래서 안 들려. | We never took the chip. So we don't hear it.
luddite | 받아. AI 없는 물건이다. 스페이스로 쓴다. | Take this. No AI inside. Press Space to use it.
luddite | 전광판은 E로 열어 돌려라. 오래는 못 버텨. 버튼은 꼭대기다. | Boards open with E. Turn the dial. It won't hold. The button is at the top.
```

**Take C — 최소 3줄 (가이드 2~3줄 기준)**
```
@mode talk
luddite | 방송을 듣고도 뛰는 사람은 오랜만이군. | Been a while since I saw someone run after a broadcast.
luddite | 받아. AI 없는 물건이다. 스페이스로 쓴다. | Take this. No AI inside. Press Space to use it.
luddite | 전광판은 E로 돌릴 수 있다. 버튼은 꼭대기다. 가. | Boards turn with E. The button is at the top. Go.
```

- `[제안]` 지어낸 것: 러다이트가 "정지 버튼 위치(꼭대기)"와 "해킹은 오래 못 버틴다"를 알려 준다. 플레이어에게 목표와 역해킹 규칙을 한 번 말해 주는 용도. GDD에 "힌트·해킹 도구 제공"은 `[확정]`이라 범위 안.
- 러다이트에게 이름을 줄지는 미정. 지금은 화자 이름 "러다이트/Luddite"로 충분하다고 본다 (등장 1회).
- "칩을 안 박았다"는 GDD 4장 CHR-03 `[확정]` 그대로. 주사·바늘 표현은 안 썼다 (GDD 16장 embecta 주의).

## 5. END-01 — 정지 버튼을 누른다 (talk)
파일: `Assets/Data/Dialogue/ending_01_placeholder.txt`
상황: 보스 3웨이브 중 해킹 → 버튼 → 1번 선택 → 이 대화 → 타이틀. `[확정]` 내용: "행운의 숫자는 존재하지 않으며, 지금까지의 모든 것은 AI가 정한 것이었음을 밝힌다."
구조: 방송(시스템 안내) → 방송(AI의 마지막 공지 = 고백) → 유도 한 줄.

**Take A (추천)**
```
@mode talk
broadcast | 정지 요청 접수. 방송을 종료합니다. | Stop request received. Ending broadcast.
broadcast | 마지막 공지. 행운의 숫자는 존재한 적이 없습니다. | Final notice. There was never a lucky number.
broadcast | 전부 제가 정했습니다. 여러분은 잘 따라 주셨습니다. | I chose all of it. You simply followed well.
yudo | 그래. 이제 아무도 안 정해. | Right. Now nobody does.
```

**AI 마지막 공지(2·3번째 줄) 대안**
- B: `broadcast | 숫자 없는 내일은 비효율적입니다. 그래도 좋은 하루 되십시오. | A tomorrow without a number is inefficient. Have a good day anyway.`
- C: `broadcast | 경고. 이 버튼 이후의 행운은 보장되지 않습니다. | Warning. Luck is not guaranteed beyond this point.`

**유도 마지막 줄 대안**
- A: `yudo | 그래. 이제 아무도 안 정해. | Right. Now nobody does.`
- B: `yudo | 오늘의 행운의 숫자는… 없습니다. 알아서들 사세요. | Today's lucky number is... none. Figure it out yourselves.`
- C: `yudo | 숨 쉬세요, 여러분. | You can breathe now, everyone.`

- C는 오프닝 농담("숨 0번")을 닫는 줄. END-02 Take C와 짝을 이루게 쓸 수도 있다(아래).
- "비효율적"은 CHR-04 `[확정]` 성격("가장 빠르고 효율적으로 움직이려고 속인 AI")에서 가져왔다.

## 6. END-02 — 누르되, 행운의 숫자를 직접 정한다 (talk)
파일: `Assets/Data/Dialogue/ending_02_placeholder.txt`
상황: 2번 선택 → 이 대화 → 타이틀. `[확정]` 내용: "AI가 하던 일을 유도가 대신하며 세상을 지배하려 한다."
구조: 방송(시스템 안내: 권한 이전) → 유도(결심) → 시민(따라 함) → 방송(새 운영자). 통쾌하지 않고 서늘하게. 지배를 미화하지 않는다.

**Take A (추천)**
```
@mode talk
broadcast | 정지 요청 접수. 운영 권한을 이전합니다. | Stop request received. Transferring operator access.
broadcast | 새 행운의 숫자를 입력하십시오. | Enter the new lucky number.
yudo | …끄기 전에 하나만. 숫자는 이제 내가 정한다. | ...One thing before I turn it off. I pick the number now.
citizen | 오늘의 행운의 숫자는… | Today's lucky number is...
broadcast | 새 방송을 시작합니다. 운영자: 안유도. | New broadcast starting. Operator: Ahn Yudo.
```

**유도 결심 줄(3번째 줄) 대안**
- A: `yudo | …끄기 전에 하나만. 숫자는 이제 내가 정한다. | ...One thing before I turn it off. I pick the number now.`
- B: `yudo | 숫자 하나로 도시가 움직인다면… 그게 나인 편이 낫지. | If one number moves the city... better it's me.`
- C: `yudo | 숨 쉬세요, 여러분. 제가 허락할 때. | Breathe, everyone. When I say so.`

**마지막 방송 줄 대안**
- A: `broadcast | 새 방송을 시작합니다. 운영자: 안유도. | New broadcast starting. Operator: Ahn Yudo.`
- B (오프닝으로 되돌아감): `broadcast | 좋은 아침입니다, 시민 여러분. 오늘의 행운의 숫자는… | Good morning, citizens. Today's lucky number is...`

- C는 오프닝 농담을 명령으로 뒤집는 줄이라 "유도가 AI 자리에 앉았다"가 한 줄로 전달된다. 가장 강하지만 가장 어둡다.
- "운영자: 안유도"는 `[제안]`(AI가 권한을 넘긴다는 묘사). GDD는 "AI가 하던 일을 유도가 대신한다"까지만 `[확정]`.
- 숫자를 실제로 몇으로 정했는지는 말하지 않는다(입력창이 없고, 안 보여 주는 편이 서늘하다).

---

## 7. `[제안]` 추가 대사 — 지금 파일이 없는 곳 (쓰려면 새 txt + 빌더 트리거 연결, Sonnet 작업)
전부 radio(안 멈춤), 2줄 이내. 넣을지 말지 사용자 결정. 없어도 게임은 완성된다.

| 위치 | 트리거 | 대사 |
|---|---|---|
| STG-01 SEG-B 진입 (방송 3 켜짐, 5-B) | 대문 전환 트리거 | `broadcast \| 공지. 지금의 행운의 숫자는 3입니다. 세 번이면 충분합니다. \| Notice. The lucky number is now 3. Three is enough.` |
| STG-02 SEG-B 진입 (방송 7) | 대문 전환 | `broadcast \| 지금의 행운의 숫자는 7입니다. 7초 동안 최선을 다하십시오. \| The lucky number is now 7. Give your best for seven seconds.` |
| STG-04 SEG-A 첫 해킹 성공 | `BroadcastBoard.Hacked` (아직 없음, boss.md 7장 제안) | `yudo \| 돌아가네. 오래는 못 버틴다고 했지. \| It turns. He said it won't hold long.` |
| STG-04 SEG-B 진입 (방송 0, 안드로이드 정체) | 대문 전환 | `broadcast \| 검사 구역. 행운의 숫자 0. 숨을 참으십시오. \| Inspection hall. Lucky number zero. Hold your breath.` / `yudo \| …저 둘은 안 멈추는데. \| ...Those two aren't stopping.` |
| STG-05 입장 | 보스 시작 거리(8u) | `broadcast \| 안유도 님. 정지 버튼 접근은 허용되지 않습니다. \| Ahn Yudo. Access to the stop button is not permitted.` |

**보스 전광판 문구 대안** (`BossBoardView.cs`·`BossFight.Waves` 하드코딩, 코드 수정 필요)
- 대기: `EMBECTOR / 엠벡터` → 유지.
- 해킹당했을 때 `...cute. / ...귀엽군.` → 대안 `입력 거부. 다시 시도하십시오. / Input rejected. Try again.` (경고문 어투 통일) 또는 유지.
- 웨이브 제목 `WAVE 1 - LUCKY 3` → 대안 `공지. 행운의 숫자 3 / NOTICE. LUCKY NUMBER 3`.
- 해금: `STOP BUTTON UNLOCKED / 정지 버튼 열림` → 대안 `경고. 정지 버튼 노출 / WARNING. STOP BUTTON EXPOSED`.

---

## 8. 사용자가 정해야 하는 것 (급한 순서)

1. **LINE-03 (3장):** 방송이 농담을 받아 "숨을 참으십시오"라고 말하나(Take A, `[제안]` 설정), 방송 없이 정적만 두나(Take B), 지문 그대로(Take C)? 오프닝 연출과 SFX-02(소리 끊김)에 바로 영향.
2. **END-02 유도의 결심 줄 (6장):** A(담담) / B(합리화) / C("제가 허락할 때", 가장 어두움). 게임 전체의 마지막 인상이다.
3. **END-01 유도의 마지막 줄 (5장):** A("이제 아무도 안 정해") / B("알아서들 사세요") / C("숨 쉬세요"). END-02에서 C를 고르면 END-01도 C로 맞춰 두 엔딩이 같은 문장에서 갈라지게 할 수 있다.
4. **아지트 (4장):** 회사 이름을 대사에 넣나(Take B) 빼나(Take A), 줄 수 5~6 vs 3. 러다이트 개인 이름 줄지.
5. **LINE-02 (2장):** `[확정]` 문장 유지(A/A-2)인지, B·C로 바꿀지. 바꾸면 GDD 9장 변경 기록 필요.
6. **LINE-01 (1장):** A(평범) / B(경고문) / C(한 줄).
7. **조작 힌트 `(Shift: 대시)`** 를 대사 안에 두나, HUD로 빼나.
8. **7장 추가 대사**를 넣을지(넣으면 Sonnet 트리거 작업 반나절 안팎, 기능 동결 11/11 전).
9. `Speakers.asset`에 `luddite` 추가 여부(Sonnet/사용자 인스펙터 1분).
