---
name: git-branch-setup
description: develop 브랜치를 최신 상태로 패치/동기화하고 로컬 터미널에서 신규 작업 브랜치를 분리 및 전환한 뒤 물리적 전환을 검증하는 Git 브랜치 준비 스킬입니다.
---

# Git 브랜치 분리 및 작업 환경 할당 워크플로우

이 스킬은 GitManager가 Developer 또는 타 에이전트의 작업 착수 요청을 수신했을 때, 로컬 터미널 셸 명령을 통해 최신 `develop`을 기준으로 신규 작업 브랜치를 분리 및 전환하고 물리적 HEAD 이동을 검증하는 표준 절차를 정의합니다.

---

## 1. 브랜치 분리 및 물리적 검증 3단계 절차

### [1단계: 로컬 develop 브랜치 최신 패치 및 동기화 (run_command 필수)]
*주의: 원격 API(`github/create_branch`)만 호출하는 우회 행위를 엄격히 금지하며, 반드시 로컬 셸 명령(`run_command`)으로 실행해야 합니다.*
```bash
git checkout develop
git fetch origin develop
git pull origin develop
```

### [2단계: 로컬 신규 작업 브랜치 분리, 원격 발행(Publish) 및 물리적 검증]
1. 작업 목적(feat, fix, refactor 등)에 맞는 네이밍으로 로컬 브랜치를 생성하고 즉시 체크아웃합니다:
   ```bash
   git checkout -b feat/[기능명] develop
   ```
2. **원격 저장소 즉시 브랜치 발행 (Publish Branch)**:
   ```bash
   git push -u origin feat/[기능명]
   ```
3. **물리적 브랜치 전환 자가 검증 (필수)**:
   ```bash
   git branch --show-current
   ```
   - *검증 게이트: 터미널 출력 결과가 지정된 `feat/[기능명]`과 100% 일치하는지 확인합니다. 불일치 시 인계를 중단하고 즉시 재전환합니다.*


### [3단계: 작업자 전환 안내 및 소통 로깅]
1. 작업자(Developer 등)에게 브랜치 생성 및 전환 완료를 인계합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "GitManager" --to "Developer" --type "브랜치 준비" --msg "feat/[기능명] 브랜치 분리 및 로컬 체크아웃 검증 완료, 개발 착수 가능"
   ```
2. PM에게 실제 전환된 브랜치명(`feat/[기능명]`)과 함께 결과를 보고하고 턴을 종료합니다.

