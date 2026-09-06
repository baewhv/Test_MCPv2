# [구현 기술 명세서] Task 4-4: 동적 난이도 랭크 시스템 (Dynamic Rank System)

## 1. 개요 (Overview)
- **태스크명**: Task 4-4: 동적 난이도 랭크 시스템(Dynamic Rank System) 구현
- **담당 에이전트**: Developer
- **작업 브랜치**: `feat_phase4_dynamic_rank`
- **목적**: 게임 진행 스테이지, 생존 시간, 사망 횟수를 실시간으로 종합하여 난이도 랭크(Rank 1~32)를 동적 산출하고, 이에 맞춰 적 비행 속도, 탄속, 동시 다이브 수, 다이브 쿨타임, 트랙터 빔 사용 확률 등 5대 게임플레이 수치를 가변 제어하는 동적 난이도 조절 시스템 구축.

---

## 2. 핵심 구현 사항 (Key Implementations)

### 2.1 랭크 산출 수학 공식 및 구조체
- **구조체**: `DifficultyRankParameters`
  - `Rank` (1 ~ 32)
  - `DiveSpeed` (8.33 ~ 17.36 u/s)
  - `BulletSpeed` (11.11 ~ 20.83 u/s)
  - `MaxConcurrentDives` (2 ~ 5기)
  - `DiveInterval` (3.0 ~ 0.8s)
  - `TractorBeamProbability` (0.20 ~ 0.90)
- **산출 공식**:
  $$\text{Rank}_{total} = \text{Mathf.Clamp}(\text{BaseStageRank} + \text{SurvivalRank} - \text{DeathPenalty}, \; 1, \; 32)$$
  - $\text{BaseStageRank} = \text{CurrentStage} \times 2$
  - $\text{SurvivalRank} = \lfloor \frac{\text{SurvivalSeconds}}{30} \rfloor$
  - $\text{DeathPenalty} = \text{DeathCount} \times 3$

### 2.2 랭크 구간별 파라미터 매핑 테이블

| 랭크 구간 | 적 비행속도 (u/s) | 적 탄속 (u/s) | 최대 동시 다이브 수 | 다이브 쿨타임 (초) | 트랙터 빔 사용 확률 |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **Rank 1 ~ 5** | $8.33 \text{ u/s}$ | $11.11 \text{ u/s}$ | 최대 2기 | $3.0\text{s}$ | $20\%$ |
| **Rank 6 ~ 15** | $12.50 \text{ u/s}$ | $15.28 \text{ u/s}$ | 최대 3기 | $1.8\text{s}$ | $50\%$ |
| **Rank 16 ~ 25** | $15.50 \text{ u/s}$ | $18.00 \text{ u/s}$ | 최대 4기 | $1.2\text{s}$ | $75\%$ |
| **Rank 26 ~ 32** | $17.36 \text{ u/s}$ | $20.83 \text{ u/s}$ | 최대 5기 | $0.8\text{s}$ | $90\%$ |

### 2.3 시스템 매니저 (`DifficultyRankManager.cs`)
- **싱글톤 아키텍처**: `DifficultyRankManager.Instance`
- **이벤트 발행**:
  - `OnRankChanged(int)`: 랭크 변경 시 발행
  - `OnParametersChanged(DifficultyRankParameters)`: 가변 수치 변경 시 발행
- **외부 연동**:
  - `StageManager`: `OnStageChanged` 이벤트를 수신하여 스테이지 랭크 갱신
  - `PlayerHealth`: `OnLivesChanged` 이벤트를 수신하여 잔기 차감 시 데스 페널티 적용
  - `EnemyDiveController`: `DiveSpeed`, `DiveInterval`, `MaxConcurrentDives` 실시간 반영
  - `EnemyShooting`: 발사 탄속(`BulletSpeed`) 동적 반영

---

## 3. 완제품 프리팹 (Zero-Override Prefab)
- `Assets/Prefabs/PF_DifficultyRankManager.prefab` 생성 완료

---

## 4. 단위 테스트 검증 결과 (NUnit Tests)
- **테스트 파일**: `Assets/Tests/Editor/DifficultyRankTests.cs`
- **검증 항목**:
  1. `CalculateRank_Stage1_InitialState_ReturnsRank2` (통과)
  2. `CalculateRank_Stage2_InitialState_ReturnsRank4` (통과)
  3. `CalculateRank_SurvivalTimeIncrease_IncreasesRankEvery30Seconds` (통과)
  4. `CalculateRank_DeathPenalty_ReducesRankBy3PerDeath` (통과)
  5. `CalculateRank_ClampsBetweenMinAndMax_1To32` (통과)
  6. `GetParametersForRank_Tier1To5_ReturnsTier1Values` (통과)
  7. `GetParametersForRank_Tier6To15_ReturnsTier2Values` (통과)
  8. `GetParametersForRank_Tier16To25_ReturnsTier3Values` (통과)
  9. `GetParametersForRank_Tier26To32_ReturnsTier4Values` (통과)
  10. `DifficultyRankManager_Initialize_SetsCorrectInitialState` (통과)
  11. `DifficultyRankManager_UpdateSurvivalTime_UpdatesRankAndFiresEvents` (통과)
  12. `DifficultyRankManager_RecordDeath_DecreasesRank` (통과)
  13. `DifficultyRankManager_SetStage_UpdatesBaseStageRank` (통과)
  14. `DifficultyRankManager_ManualRankOverride_WorksCorrectly` (통과)
  15. `DifficultyRankManager_AppliesParametersToEnemyDiveController` (통과)
- **총 테스트 결과**: 135/135 통과 (100% Pass)
