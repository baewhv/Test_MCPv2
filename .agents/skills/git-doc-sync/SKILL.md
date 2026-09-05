---
name: git-doc-sync
description: 공통 문서(.agents/, docs/, GEMINI.md) 변경 시 작업 중인 코드를 안전하게 Stash하고 develop 브랜치에 직접 커밋/푸시한 뒤 작업 브랜치로 복귀하는 문서 동기화 스킬입니다.
---

# 공통 문서 develop 격리 동기화 워크플로우

이 스킬은 Designer의 기획 명세서, QA의 검수 완료 문서 등 공통 문서가 변경되었을 때, 작업 브랜치의 미완료 코드를 보호하면서 `develop` 브랜치에 깨끗하게 문서를 반영하는 절차를 정의합니다.

---

## 1. 문서 격리 동기화 4단계 절차

### [1단계: 작업 브랜치 미완료 작업 임시 저장 (Stash)]
현재 작업 브랜치에서 진행 중이던 코드나 미커밋 에셋을 안전하게 임시 저장합니다:
```bash
git stash --include-untracked
```

### [2단계: develop 브랜치 전환 및 최신화]
```bash
git checkout develop
git pull origin develop
```

### [3단계: 공통 문서 커밋 및 원격 푸시]
수정된 공통 문서만 스테이징하고 커밋/푸시합니다:
```bash
git add docs/ .agents/ GEMINI.md
git commit -m "[docs] : [문서명/기능명] 명세 및 작업 문서 갱신"
git push origin develop
```

### [4단계: 원래 작업 브랜치 복귀 및 작업 복원 (Stash Pop)]
```bash
git checkout [이전_작업브랜치명]
git stash pop
```
*작업 복원 후 워킹 트리가 이전 작업 상태로 온전히 복구됩니다.*
