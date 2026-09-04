# 기술 및 기획 문서 마스터 색인 (Technical & Specification Index)

이 문서는 프로젝트 내의 모든 사용자 원본 기획서, 기획 상세 명세서, 객체 아키텍처 관계도 및 실시간 작업 문서를 총괄 색인화하는 순수 기술 문서 마스터 색인입니다.
(※ 프로젝트 전역 운영 규칙, 환경 설정 및 문서 권한은 `GEMINI.md` 및 `PROJECT_SPEC.md`를 따릅니다.)

---

## 1. 사용자 원본 기획서 (Specifications - Strict Read-Only)
- [기획서 등록 가이드 및 템플릿](./specs/README.md)
- *(사용자가 `docs/specs/`에 등록한 게임 기획서 목록이 이곳에 색인화됩니다)*

---

## 2. 기획 상세 명세서 (Technical Specifications)
- [기획 상세 명세서 보관소 (`docs/tech_spec/`)](./tech_spec/): Designer가 원본 기획서를 분석하여 작성한 시스템별 상세 규칙, 수치, 상태 머신 FSM 명세서
- *(신규 기능 명세서가 `docs/tech_spec/`에 추가되면 이곳에 링크가 등록됩니다)*

---

## 3. 객체 상호작용 및 아키텍처 지도 (Architecture & Contracts)
- [객체 상호작용 및 아키텍처 관계도 (ARCHITECTURE.md)](./ARCHITECTURE.md):
  - 1. 객체 상호작용 및 충돌 매트릭스 (Interaction Matrix)
  - 2. 객체 생성 및 생명주기 관리 (Spawner / Pool)
  - 3. 이벤트 구독 및 알림 흐름 (Event Flow)
  - 4. 주요 클래스 Public API 계약 (API Contract)
  - 5. ScriptableObject 데이터 참조 구조 (Data Binding)
  - 6. 아키텍처 및 호출 흐름 다이어그램 (Mermaid Diagram)

---

## 4. 실시간 개발 상태 및 태스크 관리 (Work & Status)
- [현재 개발/기획 진행 상태 (status.md)](./work/status.md): AI 실시간 FSM 상태 제어 현황판
- [개발 작업 목록 (worklist.md)](./work/worklist.md): 사용자 최우선 지시 사항 및 세분화 구현 태스크 체크리스트

---

## 5. 아키텍처 피드백 및 회고 (Feedback & Retrospectives)
- [에이전트 아키텍처 피드백 폴더 (`docs/llm_architecture_feedback/`)](./llm_architecture_feedback/): 시스템 구조 및 협업에 대한 기술 회고/피드백 보관소
