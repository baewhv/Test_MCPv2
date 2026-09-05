---
name: unity-art-asset-workflow
description: 2D 스프라이트, UI/아이콘, Sprite Atlas, 3D 모델 및 오디오(BGM/SFX)를 제작하여 Assets/_Imports/ 표준 경로에 배치하는 리소스 생성 워크플로우 스킬입니다.
---

# Unity 미디어 리소스 및 에셋 제작 워크플로우

이 스킬은 Artist 에이전트가 기획 요구사항에 맞춰 2D 스프라이트, UI 그래픽, Sprite Atlas, 3D 모델, 오디오 리소스를 제작하고 표준 폴더에 배치하는 절차를 정의합니다.

---

## 1. 2D Sprite 및 텍스처 제작
1. **이미지 생성**: `generate_image` 도구를 호출하여 요구 규격의 스프라이트/텍스처를 생성합니다.
2. **저장 경로**:
   - 2D 스프라이트: `Assets/_Imports/Sprites/[카테고리]/`
   - 텍스처/타일셋: `Assets/_Imports/Textures/[카테고리]/`
3. **임포트 세팅**:
   - Sprite Mode: Single / Multiple
   - Pixels Per Unit (PPU): 프로젝트 규격 준수 (기본 16 또는 32)
   - Filter Mode: Point (픽셀아트) 또는 Bilinear

---

## 2. UI 그래픽 및 아이콘 제작
1. **아이콘 규격**: 1:1 정사각형 비율, 투명 배경 PNG로 제작합니다.
2. **UI 요소**: 버튼 프레임, 체력/게이지 바, 슬라이더, 팝업 패널 프레임 등을 분할 제작합니다.
3. **저장 경로**:
   - 아이콘: `Assets/_Imports/UI/Icons/`
   - UI 프레임/패널: `Assets/_Imports/UI/Frames/`

---

## 3. Sprite Atlas 패킹 (드로우콜 최적화)
1. **아틀라스 생성**: 연관된 스프라이트/아이콘들을 단일 번들로 묶기 위해 `Atlas_[카테고리].spriteatlas` 에셋을 생성합니다.
2. **저장 경로**: `Assets/_Imports/Atlases/`
3. **패킹 설정**:
   - `Objects for Packing`에 해당 스프라이트 폴더 등록
   - `Include in Build: True`
   - `Allow Rotation: False` (UI의 경우)

---

## 4. 3D 모델링 및 임포트
1. **모델 생성/임포트**: `generate_model` 또는 `import_model_file`을 활용하여 3D 메시 및 머티리얼을 생성합니다.
2. **저장 경로**:
   - 3D 메시: `Assets/_Imports/Models/`
   - 머티리얼: `Assets/_Imports/Materials/`

---

## 5. 오디오 (BGM / SFX) 제작
1. **사운드 생성**: `generate_audio` 도구를 호출하여 배경음악과 효과음을 생성합니다.
2. **네이밍 및 저장 경로**:
   - BGM: `Assets/_Imports/Audio/BGM/BGM_[이름].[wav/mp3]`
   - SFX: `Assets/_Imports/Audio/SFX/SFX_[이름].[wav/mp3]`
