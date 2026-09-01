---
name: unity-devlog-workflow
description: >-
  작업 종료 시 Notion "학습일지" 캘린더 데이터베이스에 당일 구현 내역 및 Git 커밋 요약 일지 페이지를 자동 생성하고,
  AI 회고 피드백을 접힌 토글(Toggle) 블록으로 부착하여 일일 개발을 완결하는 표준 워크플로우 스킬입니다.
---

# Notion 학습일지 자동 생성 및 접힘 토글 피드백 워크플로우

이 스킬은 사용자가 작업을 종료할 때 Notion "학습일지" 캘린더에 당일 작업 요약 일지를 자동으로 생성하고, AI 기술 피드백을 **기본 접힌(collapsed) 상태의 토글 블록**으로 부착하는 표준 절차를 정의합니다.

---

## 1. 워크플로우 요약

```
[1단계: 종료 트리거 수신] ➔ [2단계: Git 커밋/PR 분석] ➔ [3단계: Notion 일지 본문 자동 작성] ➔ [4단계: AI 피드백 토글 접힘 부착]
("오늘 작업 마칠게" 등)       (당일 구현 내역 수집)         (핵심 기능/결정사항 요약)          (클릭하여 펼치는 Toggle 블록)
```

---

## 2. 세부 실행 절차

### 1단계: 일지 생성 대상 DB 확인
- **대상 데이터베이스**: `docs/PROJECT_SPEC.md`의 `Notion Database ID`를 참조합니다. (기본값: `13cc49b1-3a07-814e-b7b5-cf14b64ca1ee`)
- **제목 형식**: `[YYYY-MM-DD] 작업 기록` (예: `[2026-09-02] 작업 기록`)
- **속성 설정**:
  - `Date`: 작업 당일 날짜 (`YYYY-MM-DD`)
  - `분류`: `일지` (Select)

### 2단계: 당일 작업 내역 수집 및 본문 자동 작성
1. `git log --since="today"` 및 `docs/work/worklist.md` 완료 태스크를 분석하여 당일 구현 내역, 주요 C# 컴포넌트, 프리팹 생성 사항을 추출합니다.
2. `API-post-page`를 호출하여 아래 양식의 본문을 포함한 페이지를 생성합니다:
   - **오늘의 구현 요약**: 완성된 기능 및 PR 목록
   - **기술적 결정 및 구조**: 새로 도입된 컴포넌트, ScriptableObject, 아키텍처 연동 내역

### 3단계: AI 기술 회고 및 피드백 토글(Toggle) 부착 (기본 접힘 상태)
- 본문 최하단에 **토글 블록(Toggle block)**을 생성하여, AI의 기술 제언 및 다음 작업 권장사항을 작성합니다.
- 사용자가 필요할 때만 클릭하여 펼쳐볼 수 있도록 깔끔하게 접어둡니다.

```json
{
  "object": "block",
  "type": "toggle",
  "toggle": {
    "rich_text": [
      {
        "type": "text",
        "text": { "content": "AI 기술 회고 및 개선 피드백 (클릭하여 펼치기)" }
      }
    ],
    "children": [
      {
        "object": "block",
        "type": "paragraph",
        "paragraph": {
          "rich_text": [
            {
              "type": "text",
              "text": { "content": "오늘 작업된 컴포넌트 구조와 직렬화 바인딩이 csharp_coding_rule.md에 맞춰 잘 완결되었습니다. 다음 작업 시 MonsterSpawner의 풀링 연결을 검토하시면 더욱 안정적입니다." }
            }
          ]
        }
      }
    ]
  }
}
```
