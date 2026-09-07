# 기체 2D 스프라이트 에셋 프리팹 세팅 및 렌더러 연동 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `feat/setup-ship-sprites`
- **목적**: 기존 프로토타입 단계에서 3D 큐브 메쉬(`MeshFilter` + `MeshRenderer`)로 렌더링되던 플레이어 기체 및 적 기체 3종(자코, 고에이, 보스 갤러그)을 2D 스프라이트 에셋 기반의 `SpriteRenderer` 구조로 전환하고, ScriptableObject 및 단위 테스트와의 연동 무결성을 확보합니다.
- **적용 대상 에셋**:
  - 플레이어: `Assets/_Imports/Sprites/Player/spr_player_fighter.png` (GUID: `0357e9bf1556ef7489e786b86b153d14`)
  - 적(자코): `Assets/_Imports/Sprites/Enemies/spr_enemy_zako.png` (GUID: `e33d6d81af846e2409b10d7aa2cc5e44`)
  - 적(고에이): `Assets/_Imports/Sprites/Enemies/spr_enemy_goei.png` (GUID: `e20f6f3adfad532408d18dff918a5a2f`)
  - 적(보스 갤러그): `Assets/_Imports/Sprites/Enemies/spr_enemy_boss_galaga.png` (GUID: `9dd43a71b1fc0d54a8eae1810e7f9ba2`)

---

## 2. 스프라이트 에셋 메타데이터 (`.meta`) 설정
스프라이트 파일(1024x1024 해상도)을 유니티 2D 시스템에서 1.0 Unit 스케일과 1:1로 매핑되도록 다음과 같이 TextureImporter를 구성하였습니다.

- **Texture Type**: `8` (`Sprite (2D and UI)`)
- **Sprite Mode**: `1` (`Single Sprite`, Sub-asset FileID: `21300000`)
- **Pixels To Units (PPU)**: `1024`
  - 1024px 원본 이미지가 1 Unity World Unit(1.0f) 크기로 정확히 매핑됨
  - 기존 `Transform.localScale` (Player: 0.8, Zako: 0.8, Goei: 0.85, Boss: 1.0) 및 `BoxCollider2D` 사이즈와 완벽한 정합성 유지
- **Filter Mode**: `0` (`Point` / Point (no filter)) - 레트로 아케이드 픽셀 아트 선명도 보존
- **Alpha Is Transparency**: `1` (투명 알파 채널 활성화)
- **Texture Format / Compression**: Default Normal Compression (RGBA32)

---

## 3. 프리팹 구조 개편 (Zero-Override & Missing Reference 방지)

### 3.1 PF_Player.prefab
- 기존 `MeshFilter` (fileID: 218725082323752309) 제거
- 기존 `MeshRenderer` (fileID: 6344913608605287558)를 `SpriteRenderer`로 전환
  - Sprite: `spr_player_fighter` (`fileID: 21300000, guid: 0357e9bf1556ef7489e786b86b153d14, type: 3`)
  - Material: Sprites-Default (`fileID: 10754, guid: 0000000000000000f000000000000000, type: 0`)
  - SortingOrder: `10`
- `PlayerHealth._playerRenderer` 참조가 동일한 fileID의 `SpriteRenderer`를 정상적으로 가리키도록 설정 (피격/무적 깜빡임 기능 호환)

### 3.2 PF_Enemy_Zako.prefab
- `MeshFilter` 제거 및 `MeshRenderer` -> `SpriteRenderer` 전환
  - Sprite: `spr_enemy_zako` (`fileID: 21300000, guid: e33d6d81af846e2409b10d7aa2cc5e44, type: 3`)
  - SortingOrder: `5`
- `EnemyBase._renderer`: `SpriteRenderer` 컴포넌트 바인딩 완료

### 3.3 PF_Enemy_Goei.prefab
- `MeshFilter` 제거 및 `MeshRenderer` -> `SpriteRenderer` 전환
  - Sprite: `spr_enemy_goei` (`fileID: 21300000, guid: e20f6f3adfad532408d18dff918a5a2f, type: 3`)
  - SortingOrder: `5`
- `EnemyBase._renderer`: `SpriteRenderer` 컴포넌트 바인딩 완료

### 3.4 PF_Enemy_Boss.prefab
- 루트 오브젝트의 `MeshFilter` 제거 및 `MeshRenderer` -> `SpriteRenderer` 전환
  - Sprite: `spr_enemy_boss_galaga` (`fileID: 21300000, guid: 9dd43a71b1fc0d54a8eae1810e7f9ba2, type: 3`)
  - SortingOrder: `5`
- 자식 오브젝트 `TractorBeam` (빔 연출용 MeshRenderer/MeshFilter) 및 `CapturedFighterSlot` 계층 구조 및 참조 무결성 유지

---

## 4. ScriptableObject 및 스크립트 파이프라인 보강

### 4.1 EnemyDataSO.cs
- `_sprite` 필드 (`[SerializeField] private Sprite _sprite;`) 및 `public Sprite Sprite => _sprite;` 프로퍼티 추가
- `Initialize` 메서드에 `Sprite sprite = null` 선택적 매개변수를 추가하여 기존 단위 테스트와의 하위 호환성 100% 보장
- `SO_Enemy_Zako.asset`, `SO_Enemy_Goei.asset`, `SO_Enemy_Boss.asset`에 해당 스프라이트 sub-asset GUID 바인딩 및 원본 색상 보존을 위한 `_normalColor: {r: 1, g: 1, b: 1, a: 1}` 설정

### 4.2 EnemyBase.cs
- `Initialize(EnemyDataSO data)` 호출 시 `_enemyData.Sprite`가 존재하고 렌더러가 `SpriteRenderer`인 경우 자동으로 `spriteRenderer.sprite`를 동적 할당하도록 연동

---

## 5. 검증 및 테스트 결과
1. **컴파일 검증**:
   - `dotnet build Assembly-CSharp.csproj`: 빌드 성공 (오류 0개)
   - `dotnet build Assembly-CSharp-Editor.csproj`: 빌드 성공 (오류 0개)
2. **단위 테스트 추가 (`EnemyDataTests.cs`)**:
   - `EnemyDataSO_Sprite_CanBeSetAndRetrieved`: Sprite 세팅/조회 및 `EnemyBase.Initialize` 시 `SpriteRenderer.sprite` 정상 할당 검증 통과
