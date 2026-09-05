---
name: git-pr-workflow
description: 작업 브랜치의 커밋 내역을 확인하고 원격 푸시 후 develop 대상 PR을 발행하고 QA에게 인계하는 표준 PR 워크플로우 스킬입니다.
---

# Git PR 발행 및 검수 인계 워크플로우

이 스킬은 작업자(Developer 등)가 작업을 완료하고 본인의 커밋을 남긴 후, GitManager가 커밋 로그를 확인하여 원격 푸시 및 PR 생성을 완결하는 절차를 정의합니다.

---

## 1. PR 발행 및 검수 인계 4단계 절차

### [1단계: 작업 브랜치 커밋 로그 및 .meta 검증]
1. 작업자가 남긴 커밋 목록을 확인합니다:
   ```bash
   git log origin/develop..HEAD --oneline
   ```
2. `git status`로 `.meta` 파일 누락 여부를 최종 검증합니다.

### [2단계: 작업 브랜치 원격 푸시]
```bash
git push -u origin [작업브랜치명]
```

### [3단계: GitHub Pull Request 생성]
GitHub MCP `create_pull_request` 도구를 호출하여 `develop` 브랜치를 베이스로 PR을 생성합니다:
- **Title**: `[작업내용] - [발의 에이전트]`
- **Head**: `[작업브랜치명]`
- **Base**: `develop`
- **Body**: 구현 및 수정 내역 요약

### [4단계: 상태판 갱신, QA 직접 인계 및 PM 보고]
1. `docs/work/status.md`의 `[현재 상태]`를 `[GitManager] [기능명] PR 생성 완료 (PR #nn) ➔ qa에게 검수 인계`로 갱신합니다.
2. `agent-communication-logger`를 실행하여 QA에게 직접 검수를 요청합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "GitManager" --to "QA" --type "QA 검수 요청" --msg "[기능명] PR #nn 생성 완료, QA 검수 요청"
   ```
3. PM에게 PR 번호와 함께 완료를 보고하고 턴을 종료합니다.
