---
name: git-branch-setup
description: develop 브랜치를 최신 상태로 패치/동기화하고 작업 목적에 맞는 신규 작업 브랜치를 분리 및 전환하는 Git 브랜치 준비 스킬입니다.
---

# Git 브랜치 분리 및 작업 환경 할당 워크플로우

이 스킬은 GitManager가 Developer 또는 타 에이전트의 작업 착수 요청을 수신했을 때, 충돌을 원천 방지하기 위해 최신 develop을 기준으로 신규 작업 브랜치를 분리 및 전환하는 표준 절차를 정의합니다.

---

## 1. 브랜치 분리 3단계 절차

### [1단계: develop 브랜치 최신 패치 및 동기화]
```bash
git checkout develop
git fetch origin develop
git pull origin develop
```

### [2단계: 신규 작업 브랜치 분리 및 전환]
작업 목적(feat, fix, refactor 등)에 맞는 네이밍으로 브랜치를 분리하고 즉시 전환합니다:
```bash
git checkout -b feat/[기능명] develop
```

### [3단계: 작업자 전환 안내 및 소통 로깅]
1. 작업자(Developer 등)에게 브랜치 생성 및 전환 완료를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "GitManager" --to "Developer" --type "브랜치 준비" --msg "feat/[기능명] 브랜치 분리 및 체크아웃 완료, 개발 착수 가능"
   ```
2. PM에게 브랜치 생성 결과를 보고하고 턴을 종료합니다.
