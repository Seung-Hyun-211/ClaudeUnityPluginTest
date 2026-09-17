# ClaudeUnityPluginTest

Claude Code Unity 플러그인 사용 테스트용 Unity 프로젝트입니다.

## 프로젝트 정보

- Unity 버전: 6000.5.4f1 (Unity 6)
- 렌더 파이프라인: Universal Render Pipeline (URP), 2D 설정 포함
- 목적: Claude Code의 Unity 연동 기능(에디터 제어, 씬/에셋 조작 등)을 검증하기 위한 샌드박스 프로젝트

## 구조

- `Assets/` — 씬, URP/2D 설정, 입력 액션 등 프로젝트 에셋
- `Packages/` — UPM 패키지 매니페스트 (2D 툴체인, URP, Input System 등)
- `ProjectSettings/` — Unity 프로젝트 설정

## 참고

`Library/`, `Temp/`, `Logs/`, `UserSettings/` 등 Unity가 자동 생성하는 디렉터리는 `.gitignore`로 추적에서 제외되어 있습니다.
