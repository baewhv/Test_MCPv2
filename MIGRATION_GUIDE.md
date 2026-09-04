# 최신 멀티 에이전트 아키텍처 마이그레이션 지침서 (Migration Guide)

이 문서는 `Test_MCPv2` 프로젝트를 `TestMCP`의 최신 5대 전문 에이전트 및 고도화된 워크플로우 시스템으로 전환할 때 참조하는 단독 마이그레이션 지침서입니다.

---

## 1. 최신 아키텍처의 8대 핵심 개선 사항 요약

| 구분 | 이전 구조 (Test_MCPv2) | 최신 아키텍처 (TestMCP) | 핵심 효과 |
| :--- | :--- | :--- | :--- |
| **1. 최상위 헌법** | 장황한 규칙이 포함된 거대 `GEMINI.md` | `Global Rules` 분리 + 0-Tool-Call `[SETUP_COMPLETED]` 초경량 `GEMINI.md` | 시작 시 토큰 낭비 제거 및 0초 즉시 판정 |
| **2. 오케스트레이션** | 메인 에이전트가 모든 단계를 동기식 중계 | `PM` 에이전트 신설 + 서브에이전트 간 **Direct Handoff(직접 위임)** | 중간 릴레이 병목 제거 및 연속 병행 개발 |
| **3. C#/엔진 스킬화** | `csharp_coding_rule.md` 단일 규칙 | `unity-coding-rule` 및 `unity-work-rule`(Zero-Override) 스킬화 | C# 직렬화 캡슐화와 씬 오버라이드 0건 원칙 분리 |
| **4. 기술/기획 제안** | `status.md`에 제안 텍스트 누적 기록 | **GitHub Issue(`[AI_developer]`, `[AI_designer]`) 4단계 라이프사이클** | 이슈 트래커 일원화, 중복 방지, 반려 재제안 지원 |
| **5. 기획 설계 방식** | 챕터당 기계적으로 4개씩 쪼개는 형식적 분할 | **2단계 심층 설계 파이프라인 (`docs/tech_spec/` 작성 ➔ 태스크 도출)** | 기획서 의도 100% 명세화 후 실체적 태스크 도출 |
| **6. 코드 중복 탐색** | 소스 코드를 에이전트마다 2~3회씩 반복 조회 | **Spec-Driven 개발 및 `docs/ARCHITECTURE.md` API 계약 색인화** | 코드 탐색 생략으로 파일 I/O 및 토큰 70% 절감 |
| **7. develop 충돌 방지** | QA 검수 산출물 미커밋으로 PR 머지 후 충돌 | **develop 상시 Zero-Dirty 보장 및 Unity 에디터 캐시 ignore** | develop 워킹 트리 항상 100% Clean 유지 |
| **8. 코딩/테스트 표준** | 네임스페이스 및 테스트 폴더 모호 | **네임스페이스 일체 금지(No-Namespace)** + `Tests/Editor/`, `Tests/Runtime/` | 어셈블리 결합 복잡도 해소 및 테스트 명확화 |

---

## 2. 마이그레이션 교체 대상 파일 목록

`TestMCP` (소스) ➔ `Test_MCPv2` (대상)로 복사/덮어쓸 파일 목록:

### ① 최상위 설정 및 규칙
- `GEMINI.md` ➔ 최신 0-Tool-Call 및 Direct Handoff 지침서로 교체
- `.gitignore` ➔ ProjectAuditorSettings 및 TMP Fallback font cache ignore 추가

### ② 에이전트 지침서 (`.agents/agents/`)
- `.agents/agents/pm.md` **(신규 추가)**: 전체 오케스트레이션 및 FSM 총괄
- `.agents/agents/designer.md`: 2단계 심층 설계(`docs/tech_spec/`) 파이프라인 적용본
- `.agents/agents/developer.md`: Spec-Driven 개발 및 No-Namespace 적용본
- `.agents/agents/git_manager.md`: GitHub Issue 독점 전담 및 Zero-Dirty 워킹 트리 보장 적용본
- `.agents/agents/qa.md`: 블랙박스 명세 기반 검수 적용본
- `.agents/agents/artist.md`: 다이렉트 인계 적용본

### ③ 규칙 (`.agents/rules/`)
- `.agents/rules/csharp_coding_rule.md` ➔ **삭제** (스킬로 분리 이관됨)
- `.agents/rules/asset_generation_rule.md` ➔ 다이렉트 인계 문구 정돈본으로 교체
- `.agents/rules/doc_generation_rule.md` ➔ 최신본으로 교체
- `.agents/rules/git_rule.md` ➔ Zero-Dirty 워킹 트리 보장 규칙 추가본으로 교체
- `.agents/rules/unity_folder_rule.md` ➔ `Tests/Editor/`, `Tests/Runtime/` 분류 추가본으로 교체

### ④ 스킬 (`.agents/skills/`)
- `.agents/skills/unity-coding-rule/` **(신규 추가)**: C# 코딩 규칙 및 `code_style_sample.cs` 템플릿
- `.agents/skills/unity-work-rule/` **(신규 추가)**: Zero-Override 프리팹 조립 스킬
- `.agents/skills/agent-communication-logger/` ➔ 최신 스킬 지침 갱신
- `.agents/skills/unity-cli-runner/` ➔ 최신 스킬 지침 갱신
- `.agents/skills/unity-devlog-workflow/` ➔ 최신 스킬 지침 갱신

### ⑤ 문서 구조 (`docs/`)
- `docs/tech_spec/` **(신규 폴더 생성)**: Designer의 기획 상세 명세서 보관 폴더
- `docs/INDEX.md` ➔ 최신 마스터 색인으로 교체
- `docs/work/status.md` ➔ `[기획 필요항목]`, `[개발 요소 제안항목]` 섹션을 제거하고 순수 `[현재 상태]` FSM 상태판으로 슬림화
- `docs/work/worklist.md` ➔ 최상단에 `## 사용자 최우선 지시 사항` 섹션 신설

---

## 3. 원클릭 PowerShell 자동 마이그레이션 스크립트

아래 PowerShell 명령어를 터미널에서 실행하면, `TestMCP`의 모든 최신 구조가 `Test_MCPv2`로 즉시 동기화됩니다:

```powershell
$src = "C:\Users\KGA1\Desktop\TestMCP"
$dst = "C:\Users\KGA1\Desktop\Test_MCPv2"

# 1. GEMINI.md & .gitignore 복사
Copy-Item "$src\GEMINI.md" "$dst\GEMINI.md" -Force
Copy-Item "$src\.gitignore" "$dst\.gitignore" -Force

# 2. .agents 폴더 전체 동기화
Copy-Item "$src\.agents\*" "$dst\.agents\" -Recurse -Force
if (Test-Path "$dst\.agents\rules\csharp_coding_rule.md") {
    Remove-Item "$dst\.agents\rules\csharp_coding_rule.md" -Force
}

# 3. docs/tech_spec 생성 및 INDEX.md 복사
if (!(Test-Path "$dst\docs\tech_spec")) {
    New-Item -ItemType Directory -Path "$dst\docs\tech_spec" -Force | Out-Null
}
Copy-Item "$src\docs\INDEX.md" "$dst\docs\INDEX.md" -Force

Write-Host "최신 에이전트 아키텍처 마이그레이션이 완료되었습니다."
```

---

## 4. 마이그레이션 후 작업 실행 방법

마이그레이션 완료 후 평소처럼 아래와 같이 요청하시면 최신 아키텍처가 자동으로 가동됩니다:
- **기획 분석 요청**: `"기획서 분석해줘"` ➔ `Designer`가 `docs/tech_spec/`에 기획 명세서 작성 후 `worklist.md` 태스크 세분화
- **작업 실행 요청**: `"다음 작업 진행해줘"` 또는 `"3개 작업 진행해줘"` ➔ `Developer ➔ GitManager ➔ QA` 다이렉트 인계 루프 가동
- **긴급 수정/리팩토링**: `"이 버그 고쳐줘"` ➔ `Explain-First` 원인 분석 ➔ `worklist.md` 최우선 등록 후 착수
