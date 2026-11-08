검증: CONFIRMED (LevelTransition.cs:34-39 로드 후 페이드인 없음)

# BUG-FLOW-C07 — 스테이지 출구 페이드가 검게 끝난 뒤 다음 스테이지가 페이드 인 없이 툭 켜진다

## 재현 단계
1. STG-00 오프닝을 끝내고 오른쪽 출구(TR-EXIT)에 닿는다. (STG-01·02·03·04 출구도 같음)
2. 화면이 0.3초 동안 검게 어두워진다.
3. 다음 스테이지가 로드된다.

## 기대 결과
다음 스테이지는 검은 화면에서 0.3초 페이드 인으로 시작한다 (같은 스테이지 안의 대문 전환처럼 `Fade(1) → Fade(0)`).

## 실제 결과 (코드 근거)
- Goal 모드는 `Fade(1f)` 뒤 바로 씬을 바꾸고 끝난다: `Assets/Scripts/Level/LevelTransition.cs:34-39`.
- 페이드 오버레이는 씬 안에 만들어지는 일반 오브젝트다 (`LevelTransition.cs:69-85`, `DontDestroyOnLoad` 없음). 씬이 바뀌면 같이 사라지고, 새 씬에는 오버레이가 없으므로 첫 프레임부터 완전히 밝다.
- 결과: 어두워짐 → 갑자기 밝은 새 화면. 대문 전환(`:48-50`)과 연출이 다르다.
- 참고로 확인한 것 (문제 없음): 오버레이 `raycastTarget = false`(`:79`)라 클릭을 막지 않고, 대문 전환 뒤에는 알파 0으로 남는다. 다음 씬·타이틀이 모두 없을 때만 검게 남는데(`docs/design/impl/levels.md:48`), 지금 Build Settings에는 STG00~05·Title이 모두 있다.

## 수정 제안 (참고)
정적 플래그(`fadeInPending`)를 두고, 다음 씬이 시작될 때 오버레이를 알파 1로 만든 뒤 `Fade(0)`을 돈다. 또는 오버레이를 `DontDestroyOnLoad`로 두고 `SceneManager.sceneLoaded`에서 페이드 인한다.

## 심각도
낮음 — 연출 문제. 진행·입력에는 영향 없음.
