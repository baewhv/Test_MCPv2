---
name: unity-work-rule
description: 씬 오버라이드 0건(Zero-Override), 독립 완제품 프리팹(PF_*) 우선 조립, 인스펙터 직렬화 바인딩, Missing Reference 방지 및 에디터 스크립팅 제한을 준수하는 유니티 엔진 작업 스킬
---

# 유니티 작업 및 Zero-Override 프리팹 조립 표준 스킬 (Unity Work Skill)

이 스킬은 프로젝트의 유니티 에디터 조작, 씬 충돌 방지, Zero-Override 프리팹 조립, 직렬화 바인딩 및 에디터 스크립팅 제한을 규정하는 작업 표준 지침입니다.

---

## 1. Zero-Override 프리팹 우선 조립 원칙 (Zero-Override Prefab-First Policy)
1. **공용 씬 직접 수정 지양**:
   - 개별 기능 개발 브랜치에서는 공용 씬(`*.unity`)을 직접 수정하지 않으며, YAML 직렬화 머지 충돌을 방지합니다.
2. **독립 완제품 프리팹 우선 정책 (Prefab-First)**:
   - 씬에 배치할 모든 캐릭터, 오브젝트, UI, 스포너는 반드시 **`Assets/Prefabs/PF_[이름].prefab` 에셋 파일로 먼저 완전 조립되어 저장**되어야 합니다.
   - 컴포넌트 조립과 직렬화 필드 바인딩은 프리팹 에셋 내부에서 완결합니다.
3. **Zero-Override Clean Instance 유지**:
   - 씬에 배치된 프리팹 인스턴스는 **어떠한 로컬 수정도 가하지 않은 순수 프리팹 완제품(Zero-Override Clean Instance, Overrides 0건)** 상태를 유지해야 합니다.
   - 수정이 필요한 경우 씬의 인스펙터에서 개별 수정하지 않고 반드시 **프리팹 에셋 원본(Prefab Asset Root)**을 수정하여 모든 인스턴스에 동기화합니다.
4. **씬 통합 및 검수는 순차 수행**:
   - 씬에 프리팹을 배치하고 연동하는 최종 작업은 PR 머지 후 `QA` 단계에서 순차적으로 안전하게 수행합니다.
5. **QA Zero-Override 무결성 검증**:
   - QA는 검수 시 씬 내의 모든 인스턴스가 프리팹 에셋과 연결되어 있는지 확인하며, 인스펙터 오버라이드(Overrides)가 남아있는 경우 즉시 반려(Reject)하고 프리팹 원본 수정을 요청합니다.

---

## 2. 직렬화 바인딩 및 참조 무결성 (Serialization Binding & Integrity)
1. **직렬화 바인딩 절차**:
   - 스크립트 작성 후 본인이 설계한 `[SerializeField] private` 필드에 필요한 컴포넌트, 차일드 오브젝트, ScriptableObject 에셋을 인스펙터 상에서 정확히 드래그 앤 드롭 바인딩합니다.
2. **Missing Reference 방지**:
   - 컴포넌트 삭제나 스크립트 파일명 변경 시 인스펙터 상에 `Missing (Mono Script)` 또는 `Missing Reference`가 남지 않도록 검증합니다.
3. **작업 전후 씬/프로젝트 저장**:
   - 큰 구조 변경 전후 반드시 씬을 저장하고, `AssetDatabase.SaveAssets()`를 통해 프리팹 변경 사항이 온전히 디스크에 기록되도록 합니다.

---

## 3. .meta 파일 무결성 보존 (Unity Meta Integrity)
- Assets 폴더 내의 모든 에셋/스크립트/프리팹 생성, 이동, 삭제 시 반드시 1:1 대응하는 `.meta` 파일이 온전하게 함께 관리되도록 유의합니다.

---

## 4. 에디터 스크립팅 제한 규칙 (Editor Scripting Boundary)
1. **허용되는 에디터 코드 (Inspector Customization)**:
   - 인스펙터 가독성 향상, 필드 유효성 검사, 드롭다운 편의성 제공을 위한 **순수 인스펙터 커스터마이징(`CustomEditor`, `PropertyDrawer`)** 목적의 에디터 코드만 허용합니다.
2. **지양/금지되는 에디터 코드 (No Build / Workflow Automation Scripts)**:
   - "원클릭 빌더(One-Click Build)", "자동 씬 생성기", "메뉴 아이템(`[MenuItem]`) 일괄 배치기" 등 런타임 게임 로직과 무관한 과도한 에디터 스크립트 작성은 엄격히 지양합니다.
3. **표준 해결 원칙**:
   - 모든 기능 구현은 에디터 툴 코드가 아닌 **순수 런타임 표준 컴포넌트(`MonoBehaviour`), Zero-Override 프리팹 우선 조립 및 인스펙터 직렬화 바인딩**으로 완결합니다.
