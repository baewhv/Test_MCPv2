---
name: git-doc-sync
description: 사용자가 PR을 머지한 후, 최신 develop 브랜치를 pull하여 누적된 작업 문서(docs/)를 일괄 커밋/푸시하여 1루프를 최종 완결하는 Post-Merge 문서 동기화 스킬입니다.
---

# Post-Merge 작업 문서 최종 동기화 워크플로우

이 스킬은 사용자가 GitHub에서 PR을 검토하고 머지(Merge)한 후, PM 또는 GitManager가 `develop` 브랜치를 최신 상태로 동기화하고 누적된 작업 문서(`docs/`, `status.md`, `worklist.md`, `implementations/` 등)를 `develop`에 일괄 커밋/푸시하는 표준 절차를 정의합니다.

---

## 1. Post-Merge 문서 동기화 4단계 절차

### [1단계: develop 브랜치 전환 및 최신화 (Pull)]
사용자가 머지한 최신 커밋을 가져옵니다:
```bash
git checkout develop
git pull origin develop
```

### [2단계: 작업 문서 일괄 스테이징]
로컬에 누적 작성되었던 상태판, 체크리스트, 구현 기술문서, 소통 로그를 스테이징합니다:
```bash
git add docs/
```

### [3단계: 문서 최종 커밋 및 원격 푸시]
```bash
git commit -m "[docs] : [기능명] 작업 완료 문서 및 상태판 최종 갱신"
git push origin develop
```

### [4단계: 1사이클 최종 완결 보고 및 다음 태스크 준비]
- `git status`로 깨끗한 Working Tree 상태를 확인합니다.
- PM은 `unity-pm-orchestration` 5절의 **`[2) 1사이클 최종 완결 보고]`** 양식으로 사용자에게 1사이클 종료를 공식 보고합니다.
- 동기화 완료 후 비로소 1사이클이 공식 종료되며, 다음 작업의 신규 브랜치 분리 파이프라인으로 안전하게 진입할 준비가 완료됩니다.
