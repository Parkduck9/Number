# 대사 시안 v2 (2026-11-13) — 아직 게임에 넣지 않음

말투 기준
- **유도(안유도):** 22살, 숫자를 안 믿는다. 반말, 짧고 건조하게, 한 줄에 농담 하나.
- **연락책(helper):** 반체제 조직, 무전으로만 말함. 해요체, 급하고 빠르게.
- **방송(Embector AI):** 높낮이 없는 기계 음성. 합니다체, 친절한데 섬뜩하게.
- **시민:** 숫자에서 막 깨어난 사람. 멍하고 짧게.

스토리 보강 포인트 (굵게 = 새 줄)
1. 왜 공격받나: 방송이 "숫자를 따르지 않는 사람 = 불운"이라며 유도를 지목한다.
2. 시민은 왜 적인가: 머리의 칩이 숫자대로 움직이게 한다. 베면 쓰러질 뿐, 방송이 멈추면 깨어난다.
3. 공장(STG-04): 숫자 칩을 찍어 내는 곳.
4. 보스전: AI가 직접 말한다(등장 + 페이즈 전환 2번). ← 코드에 짧은 무전 연결 필요
5. 엔딩: 마지막 줄은 "오늘은… 대길." 유지(미정).

---

## 프롤로그 (STG-00)

| 누가 | 한국어 | English |
|---|---|---|
| 방송 | 엠벡터 방송. 오늘의 행운의 숫자는… 0입니다. | Embector broadcast. Today's lucky number is... zero. |
| **방송** | **숫자를 따르지 않는 분은 불운을 부릅니다. 가까운 불운을 치워 주십시오.** | **Those who ignore the number bring bad luck. Please remove any bad luck near you.** |
| 유도 | 초대받아 왔더니, 마중 대신 방송이네. | I came on an invitation. Got a broadcast instead. |
| 유도 | 0이라고? 누가 정했는데. | Zero? Says who? |
| 연락책 | 도사님, 초대장 보낸 사람이에요. 방금 그 '불운'이 당신이에요. 달려요! | It's me, I sent the invitation. That 'bad luck' is you. Run! |

## 튜토리얼 (단계별 카드)

| 단계 | 누가 | 한국어 | English |
|---|---|---|---|
| 1 이동 | 연락책 | A, D로 걷고 스페이스로 상자를 넘어요. | A and D to walk, Space to hop the crates. |
| 2 베기 | 연락책 | 머리에 칩 박힌 경비예요. 좌클릭! 베면 쓰러질 뿐, 안 죽어요. | A chipped guard. Left click! A cut knocks them out, it won't kill. |
| 3 일섬 | 연락책 | 이 녀석은 정면을 막아요. 같은 높이에서 Shift! | This one blocks head-on. Get level with it and press Shift! |
| 4 패링 | 연락책 | 안드로이드는 앞에서 베면 막아요. 숫자는 공격까지 남은 박자예요. | Androids block cuts from the front. Its number counts down to the strike. |
| 4 패링 | 연락책 | 고리가 겹칠 때 좌클릭, 패링! 멈추면 그때 베요. | Click when the rings meet to parry. When it stalls, cut. |
| 5 밧줄 | 연락책 | 너무 높죠? 우클릭으로 지붕 끝에 밧줄을 걸어요. | Too high? Right click to hook the roof edge. |
| 6 주사위 | 연락책 | 그 주사위가 당신을 부른 이유예요. 거의 채워 뒀으니 한 명만 쓰러뜨려요. | That die is why we called you. It's nearly charged: one more down. |
| 6 주사위 | 연락책 | 일곱 명 쓰러뜨리면 저절로 굴러요. 나온 눈이 기술이 돼요. | Seven down and it rolls itself. The face picks your skill. |
| 6 주사위 | 유도 | 남이 불러 주는 숫자 말고, 내가 굴리는 숫자라. 마음에 드네. | Not a number called out for me. One I roll. I like it. |

## 스테이지 무전

| 스테이지 | 누가 | 한국어 | English |
|---|---|---|---|
| STG-02 | 연락책 | 지붕으로 올라가요! 저격수의 붉은 선이 굳으면 쏴요. | Get on the roofs! When a sniper's red line locks, it fires. |
| STG-02 | **유도** | **다들 숫자 하나에 목숨을 거네. 칩 때문이라며?** | **All of them betting their lives on one number. The chips, right?** |
| STG-02 | **연락책** | **네. 방송이 멈추면 다 깨어나요.** | **Yes. Stop the broadcast and they all wake up.** |
| STG-04 | 연락책 | 여기서 숫자 칩을 찍어 내요. 방송 장치는 궁 왕좌실에 있어요. | They stamp out the number chips here. The broadcaster is in the throne room. |
| STG-04 | 유도 | 운도 공장에서 찍어 내는군. | So luck comes off a production line. |
| STG-05 | 연락책 | 저 기계가 도시의 운을 쥐고 있어요. 코어가 열리면 쳐요! | That machine holds the city's luck. Hit the core when it opens! |
| STG-05 | 유도 | 네 숫자는 다 읽었어. 이제 내가 굴린다. | I've read all your numbers. My roll now. |

## 보스전 (새로 — 코드에 무전 연결 필요)

| 때 | 누가 | 한국어 | English |
|---|---|---|---|
| 시작 | 방송 | 불운 요소를 확인했습니다. 행운을 위해 제거합니다. | Bad-luck element confirmed. Removing it, for your luck. |
| 2페이즈 | 방송 | 계산 오차 발생. 확률을… 다시 계산합니다. | Calculation error. Recalculating... the odds. |
| 2페이즈 | 유도 | 계산은 네가 해. 굴리는 건 나야. | You do the math. I'll do the rolling. |
| 3페이즈 | 방송 | 경고. 오늘의 숫자가… 0이 아닙니다. | Warning. Today's number is... not zero. |
| 핵 베기 | 유도 | 남이 정한 행운은, 여기까지. | Luck chosen for us ends here. |

## 엔딩 (END-01)

| 누가 | 한국어 | English |
|---|---|---|
| 방송 | 오늘의… 행운의… 숫자… 는… | Today's... lucky... number... is... |
| 방송 | ……………… | ................ |
| 연락책 | 방송이 멈췄어요. 사람들이… 깨어나요! | The broadcast is dead. People are... waking up! |
| 유도 | 버튼이 많길래 다 부쉈어. | Too many buttons. I broke them all. |
| 시민 | …어? 숫자가 안 보여. | ...Huh? The number's gone. |
| 시민 | 오늘 운은… 내가 정해도 돼? | So today's luck... is up to me? |
| 유도 | 그래. 읽고, 쌓고, 직접 굴려. | Yeah. Read it, build it, roll it yourself. |
| 유도 | 오늘은… 대길. | Today... great luck. |
