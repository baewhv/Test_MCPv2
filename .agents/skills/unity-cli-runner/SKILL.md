---
name: unity-cli-runner
description: Unity 에디터를 띄우지 않고 터미널에서 백그라운드로 C# 컴파일 무결성 검증 및 NUnit 단위/통합 테스트를 무인 자동 실행하는 Unity CLI 러너 스킬
---

# Unity CLI Runner Skill

Unity Editor가 닫혀 있는 상태에서도 터미널 백그라운드(Batchmode)에서 컴파일 검증 및 NUnit 테스트를 즉시 실행할 수 있는 표준 도구입니다.

## 1. CLI 실행 명령어

```bash
# 1. 무인 컴파일 및 에셋 무결성 검증
node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile

# 2. EditMode NUnit 단위 테스트 실행 및 결과 분석
node .agents/skills/unity-cli-runner/scripts/unity_cli.js test EditMode

# 3. PlayMode NUnit 통합 테스트 실행
node .agents/skills/unity-cli-runner/scripts/unity_cli.js test PlayMode
```

## 2. 주요 활용 주체
- **`Developer`**: C# 스크립트 작성/수정 후 GitManager에게 PR을 요청하기 전 `compile` 명령어로 에러 0건 자체 검증.
- **`QA`**: 에디터가 꺼져 있거나 CI 환경일 때 `test EditMode` 명령어로 NUnit 테스트 일괄 무인 검증.
