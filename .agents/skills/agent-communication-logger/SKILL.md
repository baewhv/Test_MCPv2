---
name: agent-communication-logger
description: 에이전트 간의 실시간 인계(Handoff), 일감 위임, 결과 반환, PR 요청, QA 검수 요청 과정을 당일 타임라인 로그(docs/logs/agent_comm_YYYY-MM-DD.md)에 자동 누적 기록하는 표준 로깅 스킬
---

# Agent Communication Logger Skill

에이전트들이 서로 소통하고 작업을 인계할 때 실시간으로 기록을 남기는 표준 도구입니다.

## 1. CLI 실행 명령어

```bash
node .agents/skills/agent-communication-logger/scripts/log_comm.js --from <발신자> --to <수신자> --type <소통유형> --msg "<전달내용요약>"
```

## 2. 매개변수 (Parameters)
- `--from` (필수): 발신 에이전트명 (`Designer`, `Artist`, `Developer`, `QA`, `GitManager`)
- `--to` (필수): 수신 대상명 (`Designer`, `Artist`, `Developer`, `QA`, `GitManager`, `GitHub PR #nn`)
- `--type` (필수): 소통 유형 (`기획 인계`, `리소스 제작 완료`, `PR 요청`, `QA 검수 요청`, `QA 승인`, `QA 반려/수정요청`, `머지 및 완료`)
- `--msg` (필수): 전달하는 핵심 내용 및 변경점 요약

## 3. 표준 소통 유형 매핑 예시
1. **기획 완료 시**:
   `--from "Designer" --to "Developer" --type "기획 인계" --msg "[기능명] 기획 분석 완료 및 worklist 등록"`
2. **리소스 제작 완료 시**:
   `--from "Artist" --to "Developer" --type "리소스 제작 완료" --msg "[기능명] 에셋 생성 완료 및 status.md 연결 제안 등록"`
3. **개발 완료 시**:
   `--from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 구현 및 프리팹 조립 완료, 커밋/PR 요청"`
4. **PR 생성 시**:
   `--from "GitManager" --to "QA" --type "QA 검수 요청" --msg "[기능명] PR #nn 생성 완료, QA 4대 검수 요청"`
5. **QA 승인 시**:
   `--from "QA" --to "GitManager" --type "QA 승인" --msg "[기능명] QA 4대 검수 통과 및 worklist [x] 완료, 사용자 머지 대기"`
6. **QA 반려 시**:
   `--from "QA" --to "Developer" --type "QA 반려/수정요청" --msg "[기능명] 결함 발견 (세부사항)으로 수정 요청"`
