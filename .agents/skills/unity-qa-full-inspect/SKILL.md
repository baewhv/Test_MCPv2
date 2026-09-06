---
name: unity-qa-full-inspect
description: 사용자 또는 PM의 요청 시 프로젝트 전체 스크립트 정적 분석, 전체 씬/프리팹 Zero-Override 전수 조사, 전체 NUnit 테스트 실행 및 기획-코드-문서 삼각 정합성을 총망라하여 종합 검수 보고서를 발행하는 프로젝트 전체 전수 검수 스킬입니다.
---

# Unity 프로젝트 종합 전체 검수 스킬 (Full Project QA Inspection)

이 스킬은 PR 단위의 국소적 검수가 아닌, **마일스톤 완료, Phase 완료 또는 사용자/PM의 명시적 요청 시(`"전체 검수해줘"`, `"프로젝트 전수 검사해줘"`, `"릴리즈 검수해줘"` 등)**에 QA 에이전트가 프로젝트 전체를 전수 점검하여 무결성을 보증하는 종합 감사 스킬입니다.

---

## 0. 전체 검수 5대 전수 점검 영역 (Inspection Domains)

1. **전체 소스코드 정적 무결성 전수 검사 (`Assets/Scripts/`)**:
   - **Deprecated API (CS0618) 0건**: 구식 API(`FindObjectOfType`, `FindObjectsOfType`, 레거시 Input 등) 전수 grep
   - **No-Namespace 표준 준수**: 모든 C# 스크립트의 전역 네임스페이스 격리 준수 여부 전수 검사
   - **SerializeField 캡슐화**: `public` 필드 노출 대신 `[SerializeField] private` 준수 여부 전수 검사
2. **전체 씬 & 프리팹 Zero-Override 전수 검사 (`Assets/Scenes/`, `Assets/Prefabs/`)**:
   - 모든 씬(`*.unity`) 내 `PrefabInstance` 블록에서 `m_AddedComponents`, `m_RemovedComponents`, 불필요한 `m_Modifications` 존재 여부 전수 검사
   - 프리팹 완제품(`Assets/Prefabs/PF_*`)의 직렬화 바인딩 누락(Missing Mono Script / Missing Reference) 0건 검증
3. **전체 NUnit 단위/통합 테스트 전수 실행 & 통계 집계**:
   - `unity-cli-runner`를 통해 전체 EditMode 및 PlayMode 테스트 실행
   - 총 테스트 수, Pass/Fail 통계, 실행 소요 시간 집계
4. **기획-코드-문서 3대 삼각 정합성 전수 감사**:
   - `docs/tech_spec/` (기획 명세) ➔ `Assets/Scripts/` (실제 C# 코드)
   - `Assets/Scripts/` ➔ `docs/implementations/` (구현 기술문서)
   - `docs/implementations/` ➔ `docs/ARCHITECTURE.md` (중앙 아키텍처 색인)
5. **Git 형상 및 문서 보존 상태 검증**:
   - `docs/work/worklist.md` 체크리스트 일치도, `docs/work/status.md` 상태 정합성 확인

---

## 1. 전체 검수 실행 절차

### [1단계: 소스코드 및 에셋 정적 전수 조사]
```bash
# Deprecated API 전수 검사
grep_search("FindObjectOfType" in "Assets/Scripts")
grep_search("FindObjectsOfType" in "Assets/Scripts")

# No-Namespace 원칙 전수 검사
grep_search("namespace " in "Assets/Scripts")

# Zero-Override 씬 전수 검사
grep_search("m_AddedComponents" in "Assets/Scenes")
grep_search("m_RemovedComponents" in "Assets/Scenes")
```

### [2단계: 전체 NUnit 무인 테스트 일괄 실행]
```bash
node .agents/skills/unity-cli-runner/scripts/unity_cli.js test
```
- 전체 테스트 100% Pass 여부 및 세부 테스트 지표 수집.

### [3단계: 기획-코드-문서 삼각 정합성 대조]
- `docs/tech_spec/`의 핵심 요구사항과 현재 `Assets/Scripts/`의 공개 인터페이스/구조 대조.
- `docs/implementations/` 기술문서들의 최신화 상태 및 `docs/ARCHITECTURE.md` 관계도 색인 확인.

### [4단계: 종합 검수 보고서 대화창 출력]
- 아래 표준 양식에 따라 종합 판정 결과를 대화창에 간결하고 명확하게 출력합니다.

---

## 2. 종합 전체 검수 보고서 양식 (Full QA Inspection Report)

```markdown
## [프로젝트/Phase명] 종합 전체 검수 결과 보고서 [명확한 자료]
> **검수 일시**: YYYY-MM-DD HH:mm | **검수 주체**: QA 에이전트

### 1. 종합 무결성 판정 요약
| 검수 영역 | 세부 항목 | 검수 기준 | 결과 (PASS / FAIL) |
| :--- | :--- | :--- | :---: |
| **정적 코드 무결성** | Deprecated API / namespace | CS0618 0건, namespace 0건 | **PASS** |
| **프리팹 / 씬 무결성** | Zero-Override & Missing Ref | 씬 오버라이드 0건, Missing 0건 | **PASS** |
| **NUnit 회귀 테스트** | 전체 단위/통합 테스트 | N개 테스트 100% Pass | **PASS** (N/N) |
| **삼각 정합성** | 기획-코드-문서 정합성 | 명세-구현-아키텍처 일치 | **PASS** |
| **작업 문서 정합성** | worklist / status / impl | 완료 체크 및 기술문서 누락 0건 | **PASS** |

### 2. 세부 지표 및 통계
- **총 C# 소스 파일 수**: N개
- **NUnit 테스트 총계**: N개 (EditMode N개, PlayMode N개) - 성공률 100%
- **Zero-Override 프리팹 수**: N개 완제품 프리팹 정상 바인딩

### 3. 총평 및 권장 사항
- (결함/경고가 발견된 경우 조치 가이드 제시, 이상 없으면 "전체 무결성 통과 완료" 명시)
```
