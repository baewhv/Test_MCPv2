---
name: unity-cli-runner
description: Unity 에디터를 띄우지 않고 터미널에서 백그라운드로 C# 컴파일 무결성 검증 및 NUnit 단위/통합 테스트를 무인 자동 실행하는 Unity CLI 러너 스킬
---

# Unity CLI Runner Skill

Unity Editor가 닫혀 있는 상태에서도 터미널 백그라운드(Batchmode)에서 컴파일 검증 및 NUnit 테스트를 즉시 실행할 수 있는 표준 도구입니다.

## 1. CLI 실행 명령어

```bash
# 1. 무인 컴파일 및 에셋 무결성 검증 (Developer / QA)
node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile

# 2. EditMode NUnit 단위 테스트 실행 및 결과 분석 (QA 전담)
node .agents/skills/unity-cli-runner/scripts/unity_cli.js test EditMode

# 3. PlayMode NUnit 통합 테스트 실행 (QA 전담)
node .agents/skills/unity-cli-runner/scripts/unity_cli.js test PlayMode
```

## 2. 주요 활용 주체
- **`Developer`**: C# 스크립트 작성/수정 후 GitManager에게 PR을 요청하기 전 **오직 `compile` 명령어만 사용**하여 컴파일 에러 0건을 자체 검증합니다. (Developer는 테스트 코드 작성 및 `test` 명령어 실행을 일체 수행하지 않습니다.)
- **`QA`**: `docs/tech_spec/` 및 `ARCHITECTURE.md` 기반으로 NUnit 테스트 코드(`*Tests.cs`)를 직접 작성/보강한 후, 에디터가 꺼져 있거나 CI 환경일 때 **`test EditMode` 및 `test PlayMode` 명령어를 전담 실행**하여 테스트 100% 통과(Pass)를 무인 자동 검증합니다.
