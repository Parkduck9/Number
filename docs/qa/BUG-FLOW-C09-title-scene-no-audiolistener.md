검증: CONFIRMED (Title.unity AudioListener 0개; 오디오 도입 후 영향)

# BUG-FLOW-C09 — 타이틀 씬 카메라에 AudioListener가 없다 (BGM을 넣으면 타이틀에서 소리가 안 남, 추측 포함)

## 재현 단계
1. `Assets/Scenes/Title.unity`를 연다 (또는 `NUMBER/Build Title Scene`으로 다시 만든다).
2. Main Camera의 컴포넌트를 본다.
3. (나중에) 타이틀에 BGM `AudioSource`를 붙이고 Play.

## 기대 결과
다른 스테이지 씬처럼 Main Camera에 `AudioListener`가 있다. 타이틀 BGM·버튼 효과음이 들린다.

## 실제 결과 (코드 근거)
- `Assets/Editor/TitleSceneBuilder.cs:33-38`은 `Camera`만 붙이고 `AudioListener`를 붙이지 않는다. 스테이지 빌더는 붙인다 (`Assets/Editor/StageBuildKit.cs:268`, `Assets/Editor/Stg05Builder.cs:318`).
- 씬에도 없다: `Assets/Scenes/Title.unity`에 `AudioListener` 항목 없음.
- 지금은 소리 코드가 하나도 없어서(`Assets/Scripts`에 `AudioSource` 없음) 증상이 보이지 않는다. **추측:** DECISIONS 10-A `[확정]`(BGM 2곡·효과음 약 12개)이 들어오면 타이틀에서만 소리가 안 나고 콘솔에 "There are no audio listeners in the scene" 경고가 반복된다.

## 수정 제안 (참고)
`TitleSceneBuilder.cs:34` 다음 줄에 `camGo.AddComponent<AudioListener>();`를 넣고 타이틀 씬을 다시 빌드한다.

## 심각도
낮음 — 지금은 영향 없음. 오디오 작업 때 바로 걸린다.
