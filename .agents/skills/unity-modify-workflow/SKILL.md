---
name: unity-modify-workflow
description: Developer 에이전트가 docs/tech_spec/ 변경사항을 분석하고 작업 브랜치 일치 검증(Safety Gate), .meta GUID를 보존하는 In-place 코드 수정, 컴파일 검증, 직접 커밋 및 문서 최신화를 완결하는 표준 코드 수정/리팩토링 워크플로우 스킬입니다.
---

# Unity 기존 코드 수정 및 리팩토링 워크플로우

이 스킬은 Developer 에이전트가 기획 변경, 버그 수정, 리팩토링 등 기존 구현물의 변경 작업을 수신했을 때 무분별한 전체 탐색, 스크립트 삭제(delete_script), execute_code 오남용을 방지하고, 문서 역색인 및 .meta GUID 보존을 통해 최소 변경으로 완결하는 5단계 표준 절차를 정의합니다.

---

## 1. 코드 수정 5단계 표준 워크플로우

### [1단계: tech_spec 변경사항 파악 및 작업 브랜치 일치 검증 (Safety Gate)]
1. `docs/tech_spec/[시스템명]_tech_spec.md` 또는 기획 변경 요청 사항을 분석하여 변경 요구사항을 파악합니다.
2. **브랜치 일치 여부 자가 검증 (Safety Gate)**:
   - 터미널에서 현재 체크아웃된 브랜치를 확인합니다 (`git branch --show-current`).
   - `docs/work/status.md`에 명시된 `**작업 브랜치**`와 현재 브랜치가 100% 일치하는지 대조합니다.
   - 불일치 시 작업을 즉시 중단하고 PM에게 보고 후 대기합니다.

### [2단계: ARCHITECTURE 및 implementations 역색인 타겟 특정]
1. `docs/ARCHITECTURE.md` 및 `docs/implementations/[태스크명]_impl.md`를 역색인하여 수정이 필요한 C# 스크립트(`.cs`) 및 프리팹을 핀포인트로 특정합니다.

### [3단계: 핀포인트 In-place C# 코드 수정 (.meta GUID 보존) 및 컴파일 검증]
1. **`delete_script` 및 `execute_code` 절대 금지**:
   - 기존 스크립트를 삭제 후 재생성하거나 execute_code로 C# 파일 I/O를 수행하지 않고, 로컬 파일 시스템 도구(In-place Overwrite 또는 `replace_file_content`)로 파일 내용만 직접 수정하여 **`.meta` 파일의 고유 GUID를 100% 보존**합니다.
2. 아래 명령을 실행하여 컴파일 에러 0건을 검증합니다:
   ```bash
   node .agents/skills/unity-cli-runner/scripts/unity_cli.js compile
   ```
3. 표준 터미널 Git 명령어로 작업 브랜치에서 **순수 작업물(`Assets/`)만** 직접 커밋합니다 (`docs/` 문서는 로컬 보존):
   ```bash
   git add Assets/
   # 결함 수정 시: git commit -m "[fix] : [버그명] 원인 수정 및 검증 완료"
   # 구조 개선 시: git commit -m "[refactor] : [개선명] 구조 개선 및 최적화 완료"
   git push origin HEAD
   ```
4. **물리적 커밋 및 푸시 증거 확인 (Proof-of-Commit)**:
   ```bash
   git log -1 --oneline
   ```


### [4단계: implementations 기술문서 및 ARCHITECTURE 최신화 (네이티브 도구 사용)]
1. 네이티브 파일 도구를 사용하여 `docs/implementations/[태스크명]_impl.md` 및 `docs/ARCHITECTURE.md`의 변경사항을 갱신합니다.

### [5단계: 상태 현황판 갱신 및 GitManager PR 인계]
1. `docs/work/status.md`의 `**진행 상태**`를 `[Developer] [기능명] 수정 및 커밋 완료 ➔ git_manager에게 PR 발행 인계`로 갱신합니다.
2. 소통 로거를 실행하고 턴을 종료합니다:
   ```bash
   node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Developer" --to "GitManager" --type "PR 요청" --msg "[기능명] C# 코드 수정 및 직접 커밋 완료, PR 발행 요청"
   ```
