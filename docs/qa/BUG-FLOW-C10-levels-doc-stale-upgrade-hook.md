검증: PLAUSIBLE (문서 불일치, LudditeMeeting.cs:28이 Shockwave.SetUnlocked 직접 호출 확인)

# BUG-FLOW-C10 — 구현 기록 levels.md의 충격파 업그레이드 훅 설명이 지금 코드와 다르다 (문서)

## 재현 단계
1. `docs/design/impl/levels.md:33`을 읽는다.
2. `Assets/Scripts/Level/LudditeMeeting.cs`를 연다.

## 기대 결과
문서가 지금 코드를 설명한다.

## 실제 결과 (근거)
- 문서: "`LudditeMeeting.cs:35` `GrantUpgrade()`가 ... `Shockwave.SetUnlocked(true)`를 리플렉션으로 부른다. ... 병합 전에는 TODO(upgrade) 로그만 찍힌다." (`docs/design/impl/levels.md:33`)
- 코드: 파일은 31줄이고 `GrantUpgrade()`·리플렉션이 없다. `Finish()`에서 직접 `Shockwave.SetUnlocked(true)`를 부른다 (`Assets/Scripts/Level/LudditeMeeting.cs:24-30`, 호출 `:28`). 병합 후 직접 호출로 이미 바뀌었다.
- 같은 문서 `levels.md:47`(Stg01Builder가 STG01을 Build Settings 맨 위로 올림)도 지금은 `StageBuildKit.SyncBuildSettings()`를 부르므로(`Assets/Editor/Stg01Builder.cs:166`) 해결된 내용이다.
- 다음 작업자가 "WebGL 코드 스트리핑 위험", "리플렉션 훅 교체"를 남은 일로 오해할 수 있다.

## 수정 제안 (참고)
`levels.md` 3장 "충격파 업그레이드 훅" 항목과 5장 제안 2를 "병합 후 해결됨"으로 고친다 (문서 담당 `[docs]`).

## 심각도
낮음 — 문서만. 게임 동작 영향 없음.
