---
name: unity-vfx-anim-workflow
description: Particle System 완제품 프리팹 조립, Animator Controller 상태 머신 및 파라미터 구성, Developer 직접 인계를 완결하는 VFX/애니메이션 워크플로우 스킬입니다.
---

# Unity VFX 및 애니메이터 조립 워크플로우

이 스킬은 Artist 에이전트가 Particle System 기반의 이펙트 프리팹을 조립하고, Animator Controller 상태 머신을 구성하여 Developer에게 직접 인계하는 절차를 정의합니다.

---

## 1. Particle System 완제품 프리팹 조립 (PF_VFX_*)
1. **독립 완제품 프리팹 조립**:
   - 씬에 직접 배치하지 않고 `Assets/Prefabs/VFX/PF_VFX_[이름].prefab` 형태로 프리팹을 생성합니다.
2. **핵심 파라미터 세팅**:
   - `Stop Action`: `Destroy` 또는 `Disable` (오브젝트 풀러 호환)
   - `Play On Awake`: 이펙트 성격에 맞게 설정 (폭발 등 단발성은 True)
   - `Scaling Mode`: `Hierarchy` 또는 `Shape`
   - 파티클 머티리얼/텍스처 바인딩 확인 (Missing Reference 원천 차단)

---

## 2. Animator Controller 구성 (AC_*)
1. **컨트롤러 생성**: `Assets/Animations/AC_[이름].controller`를 생성합니다.
2. **상태 머신 (FSM) 및 전이 조건 구성**:
   - 기본 상태 (Default State, 예: `Idle`) 지정
   - 상태 전이 (Transitions: `Move`, `Attack`, `Hit`, `Die`) 연결 및 `Has Exit Time` 설정
3. **파라미터 표준화**:
   - C#의 `Animator.StringToHash` 연동을 고려하여 파라미터명(예: `Speed`, `IsAttacking`, `DoHit`)을 명확히 정의합니다.

---

## 3. Developer 직접 인계 및 실시간 소통 로깅
1. **소통 로거 실행 및 직접 인계**:
   - 에셋, VFX 프리팹, 애니메이터 제작 완료 즉시 Developer에게 직접 인계합니다:
     ```bash
     node .agents/skills/agent-communication-logger/scripts/log_comm.js --from "Artist" --to "Developer" --type "에셋 인계" --msg "[에셋/VFX명] Particle System 및 애니메이터 제작 완료, Developer 바인딩 인계"
     ```
2. **PM 보고**: 작업 결과를 요약하여 PM에게 보고하고 턴을 종료합니다.
