# 유니티 프로젝트 폴더 및 에셋 네이밍 규칙 (Unity Folder & Naming Rules)

이 문서는 프로젝트의 모든 유니티 폴더 구조, 에셋 배치 위치, 테스트 코드 분류 및 네이밍 컨벤션을 규정하는 단독 표준 규칙입니다.

---

## 1. 표준 디렉토리 구조 (Directory Structure)

`Assets/` 루트 하위에 아래의 표준 폴더 구조를 유지합니다:

```
Assets/
├── _Imports/               # [Submodule 대상] 모든 외부 원본 참조 파일 보관
│   ├── Audio/              # BGM/SFX 원본 음원 파일 (.wav, .mp3, .ogg)
│   ├── Fonts/              # 원본 폰트 파일 (.ttf, .otf)
│   ├── Models/             # 3D 원본 모델 및 메시 파일 (.fbx, .obj)
│   └── Textures/           # 2D 원본 이미지 및 텍스처 (.png, .psd, .tga)
├── Animations/             # 유니티 애니메이션 클립(.anim), 컨트롤러(.controller)
├── Materials/              # 유니티 머티리얼(.mat), 물리 머티리얼(.physicMaterial)
├── Prefabs/                # 유니티 프리팹 (UI/, Characters/, Items/, Environment/, VFX/)
├── Scenes/                 # 씬 파일(.unity)
├── ScriptableObjects/      # ScriptableObject 인스턴스 에셋 (Data/, Settings/, Events/)
├── Scripts/                # C# 스크립트 (Core/, Systems/, Gameplay/, UI/, Utils/)
├── Shaders/                # 셰이더 및 셰이더 그래프(.shader, .shadergraph)
├── Sprites/                # 유니티 스프라이트 아틀라스 및 슬라이스 에셋
├── Tests/                  # NUnit 테스트 코드
│   ├── Editor/             # EditMode 단위 테스트 (*Tests.cs: 수식, 단위 로직, SO 검증)
│   └── Runtime/            # PlayMode 통합 테스트 (*Tests.cs: 물리, 충돌, 스폰 풀링 검증)
└── Screenshots/            # QA 검수 캡처 스크린샷
```

---

## 2. _Imports 폴더 운영 원칙 (Submodule Boundary)
1. **원본 보관 전용**: 외부에서 반입되는 모든 원본 리소스(원음 오디오, 원본 이미지, FBX 3D 모델, 원본 폰트 등)는 반드시 `Assets/_Imports/` 하위 전용 폴더에 배치합니다.
2. **Submodule 관리 대비**: `Assets/_Imports/` 디렉토리는 향후 독립된 Git Submodule로 분리 관리될 수 있도록, 유니티 에디터 가공 에셋(Prefab, Material, ScriptableObject, Animation Clip 등)은 이곳에 두지 않고 각각의 상위 전용 폴더(`Prefabs/`, `Materials/` 등)에 배치합니다.

---

## 3. 에셋 네이밍 컨벤션 (Naming Conventions)

| 에셋 종류 | 구분 및 규칙 | 네이밍 예시 |
| :--- | :--- | :--- |
| **프리팹 (Prefab)** | 접두사 `PF_` | `PF_Player.prefab`, `PF_Coin.prefab`, `PF_HUD.prefab` |
| **스크립터블 오브젝트 (SO)** | 접두사 `SO_` | `SO_PlayerStat.asset`, `SO_WeaponConfig.asset` |
| **머티리얼 (Material)** | 접두사 `M_` | `M_CoinGold.mat`, `M_PlayerSkin.mat` |
| **스프라이트 (Sprite)** | 접두사 `SP_` | `SP_HeartIcon.png`, `SP_Coin.png` |
| **일반 씬 (General Scene)** | 접미사 `*Scene` | `MainMenuScene.unity`, `LobbyScene.unity`, `LoadingScene.unity` |
| **스테이지 씬 (Stage Scene)** | 패턴 `Stage[X]-[Y]` | `Stage1-1.unity`, `Stage1-2.unity`, `Stage2-1.unity` |
| **애니메이터 컨트롤러** | 접두사 `AC_` | `AC_Player.controller` |
| **애니메이션 클립** | 접두사 `Anim_` | `Anim_Player_Idle.anim`, `Anim_Player_Run.anim` |
| **텍스처 (Texture)** | 접두사 `T_` | `T_Ground_Albedo.png`, `T_Ground_Normal.png` |
| **오디오 (Audio)** | 접두사 `BGM_` / `SFX_` | `BGM_Title.wav`, `SFX_ButtonClick.wav` |
| **테스트 코드 (Tests)** | 접미사 `*Tests.cs` | `PlayerShootingTests.cs`, `CombatTests.cs` |

---

## 4. 에이전트 준수 의무
- **`Developer`**: 신규 스크립트, 프리팹, ScriptableObject, 머티리얼, 테스트 코드 생성 시 본 규칙의 폴더 경로와 접두사를 반드시 준수합니다.
- **`QA`**: 4대 검수 시 에셋 및 테스트 코드가 올바른 폴더(`Tests/Editor/`, `Tests/Runtime/`)에 위치하고 명명 컨벤션을 준수했는지 확인합니다.
