# AI 리소스 생성 및 프로토타입 규칙 (Asset Generation & Prototype Rules)

이 문서는 토큰 및 API 비용을 극대화하여 절약하는 **프로토타입/더미 리소스 기본 원칙**, **이펙트(Particle System) 및 애니메이션(Animator Controller) 공통 표준**, 그리고 **AI 리소스 정식 제작 파이프라인**을 규정합니다.

---

## 1. 토큰 절약형 프로토타입/더미 리소스 기본 원칙 (Token-Saving Default Policy)

기능 구현 및 템플릿 개발 단계에서는 불필요한 토큰/비용 소모를 방지하기 위해 **항상 아래의 초경량 더미 사양을 기본(Default)으로 사용**합니다:

1. **2D/3D 외형 기본 사양 (Primitive First)**:
   - 화려한 AI 생성 이미지나 3D 모델 대신, **유니티 기본 프리미티브(Primitive: Capsule, Cube, Sphere, Cylinder)** 또는 단순 기본 단색 도형 스프라이트로 기호화하여 구현합니다.
   - 예시:
     - 검 / 둔기 / 무기: `Capsule` 형태의 단순 막대기 오브젝트
     - 플레이어 / 몬스터: `Capsule` 또는 `Cube` 기본 도형에 단색 머티리얼 적용
     - 코인 / 수집 아이템: 얇은 `Cylinder` 또는 노란색 `Sphere`
2. **사운드 기본 사양 (Simple Tone)**:
   - 복잡한 배경음악/효과음 AI 생성을 지양하고, 단순 단음(Simple Beep/Tone) 형태나 무음/더미 오디오 소스로 처리합니다.
3. **3D 모델링 AI 생성 엄격 제한**:
   - 테스트 및 템플릿 개발 단계에서는 **3D 모델 AI 생성을 일체 진행하지 않습니다** (토큰 및 시간 낭비 방지).
4. **정식 AI 리소스 제작 가동 조건**:
   - 사용자가 **"실제 리소스 제작해줘"**, **"고품질 에셋으로 만들어줘"**라고 명시적으로 요청한 경우에만 `Artist` 에이전트가 가동되어 실제 AI 생성을 진행합니다.

---

## 2. 이펙트 및 애니메이션 공통 표준 (VFX & Animation Standards)

이펙트 및 움직임 연출 요청 시 아래의 표준 컴포넌트를 공통으로 사용합니다:

1. **이펙트 제작 표준 (Particle System)**:
   - 모든 시각 효과/이펙트는 유니티 내장 **`Particle System` (파티클 시스템)**을 사용하여 구현합니다.
   - 파티클 프리팹은 `Assets/Prefabs/VFX/` 폴더에 **`PF_VFX_[이름].prefab`** (또는 `PF_Effect_[이름].prefab`) 형태로 조립합니다.
2. **애니메이션 제작 표준 (Animator Controller)**:
   - 캐릭터 및 오브젝트의 움직임/동작 연출은 반드시 **`Animator Controller` (`AC_[이름].controller`)**를 사용하여 상태 머신(State Machine) 기반으로 제어합니다.
   - 애니메이션 클립(`Anim_[이름].anim`)은 `Assets/Animations/` 폴더에 배치하고, `Developer`는 C# 스크립트에서 `Animator.StringToHash()`로 캐싱된 파라미터를 통해 상태 전이를 트리거합니다.

---

## 3. 정식 AI 리소스 생성 도구 및 규격 (사용자 명시 요청 시)

### ① 2D 스프라이트 및 텍스처 (2D Sprites & Textures)
- **도구**: Antigravity `generate_image` (NanoBanana / Imagen) 또는 UnityMCP `generate_image`
- **저장 위치**: 반드시 **`Assets/_Imports/Textures/`** 또는 **`Assets/_Imports/Sprites/`**에 저장합니다.
- **네이밍**: `T_[이름].png` (텍스처), `SP_[이름].png` (스프라이트)
- **임포트 후처리**: UnityMCP `manage_texture`로 `Sprite (2D and UI)` 설정.

### ② 오디오 및 사운드 효과 (Audio: BGM & SFX)
- **도구**: UnityMCP `generate_audio` (fal-ai/stable-audio-25, cassetteai)
- **저장 위치**: 반드시 **`Assets/_Imports/Audio/`**에 저장합니다.
- **네이밍**: `BGM_[이름].wav` (배경음), `SFX_[이름].wav` (효과음)

### ③ 3D 모델 및 메시 (3D Models & Meshes)
- **도구**: UnityMCP `generate_model` (Tripo, Meshy)
- **저장 위치**: 반드시 **`Assets/_Imports/Models/`**에 저장합니다.
- **네이밍**: `M_[이름].[ext]`

---

## 4. 리소스 가공 및 프리팹 완결 4단계 파이프라인

정식 AI 리소스 제작 시 아래 4단계를 거쳐 완제품으로 가공합니다:

```
[1단계: AI 원본 생성] ➔ [2단계: _Imports 격리] ➔ [3단계: 머티리얼/스프라이트 세팅] ➔ [4단계: 프리팹 완제품 조립]
 (사용자 명시 요청 시)   (Audio/Textures/Models)    (Sprite 전환, M_*.mat 생성)     (PF_*.prefab 완성)
```

---

## 5. 에이전트 준수 의무
- **`Developer`**: 평소 기능 개발 시 프리미티브 기반 더미 리소스를 사용하며, 이펙트는 `ParticleSystem`, 움직임은 `Animator Controller`와 C# 스크립트를 연결합니다.
- **`Artist`**: 사용자 요청 시 파티클 이펙트 구성 및 애니메이션 클립(`Anim_*`)을 제작하고 `status.md`에 연결 제안을 남깁니다.
- **`QA`**: 파티클 시스템과 애니메이터 컨트롤러가 Missing 없이 정상 재생되는지 검수합니다.
