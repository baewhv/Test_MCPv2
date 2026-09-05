---
name: unity-spec-audit
description: QA 에이전트가 사용자 또는 PM의 명시적 요청 시에만 온디맨드로 가동하여, 기획 명세(tech_spec) ➔ 실제 C# 코드(.cs) ➔ 구현 기술문서(implementations) ➔ 아키텍처 지도(ARCHITECTURE.md) 간의 삼각 정합성을 정밀 감사하고 보고서를 작성하는 작업 검수 스킬입니다.
---

# 온디맨드 작업 및 문서 정합성 검수 스킬 (Unity Spec & Doc Audit)

이 스킬은 매 개발 루프마다 자동 실행되지 않고, **사용자(또는 PM)가 작업 검수나 문서 정합성 확인을 명시적으로 요청했을 때(`"작업 검수해줘"`, `"[기능명] 검수해줘"` 등)**에만 QA 에이전트가 가동하는 독립 감사(Audit) 스킬입니다.

---

## 1. 3대 삼각 정합성 검증 기준 (Tri-Party Audit Matrix)

### [체크 1: 기획 충족성 검증 (tech_spec ➔ C# Code)]
1. `docs/tech_spec/[시스템명]_tech_spec.md`에 명시된 공용 인터페이스(`IDamageable`, `IPoolable` 등)가 대상 클래스에 정확히 구현되었는가?
2. 기획 명세의 수치 공식, 파라미터, 속도, 딜레이 및 FSM 상태 전이가 C# 코드에 빠짐없이 반영되었는가?
3. 명세서에 기술된 예외 상황(Edge Cases)이 방어 코드로 충실히 처리되었는가?

### [체크 2: 구현 기술문서 일치도 검증 (C# Code ➔ implementations)]
1. 실제 C# 파일의 모든 `[SerializeField] private` 필드가 `docs/implementations/[태스크명]_impl.md`의 바인딩 표에 누락 없이 기록되었는가?
2. 클래스의 주요 Public 메서드 시그니처와 반환값이 구현 기술문서의 공개 API 계약과 1:1로 일치하는가?
3. 코드에 사용된 핵심 알고리즘, 직렬화 바인딩 규격 및 설계 결정 Rationale이 충실히 서술되었는가?

### [체크 3: 중앙 아키텍처 관계도 색인 검증 (implementations ➔ ARCHITECTURE.md)]
1. 컴포넌트 간 상호작용 및 2D 충돌 매트릭스가 `docs/ARCHITECTURE.md`에 1줄로 반영되었는가?
2. 이벤트 발행/구독 흐름 및 Data SO 바인딩 표에 누락 없이 등록되었는가?

---

## 2. 작업 검수 보고서 출력 양식 (Audit Report Template)

검수 완료 시 대화창에 아래 양식으로 사용자 보고서를 출력합니다:

```markdown
## [기능명] 작업 및 문서 정합성 검수 결과 보고서 [명확한 자료]
> **출처**: docs/tech_spec/[시스템명]_tech_spec.md, Assets/Scripts/.../[클래스명].cs, docs/implementations/[태스크명]_impl.md, docs/ARCHITECTURE.md

### 1. 3대 정합성 검증 판정표
| 검증 영역 | 대조 대상 | 검증 항목 | 판정 (PASS / FAIL) |
| :--- | :--- | :--- | :---: |
| **기획 충족성** | tech_spec ➔ C# 코드 | 인터페이스 상속, 룰셋 수치, FSM 전이, 엣지케이스 방어 | **PASS** |
| **구현 문서 일치도** | C# 코드 ➔ implementations | 직렬화 필드 바인딩, Public API 시그니처, 설계 Rationale | **PASS** |
| **아키텍처 색인** | implementations ➔ ARCHITECTURE.md | 상호작용 매트릭스, 이벤트 흐름, Data SO 바인딩 | **PASS** |

### 2. 세부 발견 사항 및 조치 권장 (Findings)
- (불일치나 누락이 발견된 경우 구체적인 파일 및 수정 권장 사항 명시, 없으면 "완벽 일치" 명시)
```
