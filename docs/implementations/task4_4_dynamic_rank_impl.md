# Task 4-4: 동적 난이도 랭크 시스템(Dynamic Rank System) 구현 기술문서

## 1. 개요 및 목적
- **목적**: 스테이지 번호, 플레이어 생존 시간, 사망 횟수를 종합하여 실시간 동적 난이도 랭크($\text{Rank } 1 \sim 32$)를 산출하고 적 기체의 비행속도, 탄속, 동시 다이브 수, 쿨타임, 트랙터 빔 확률을 가변 제어하는 적응형 난이도 엔진 구축
- **작업 브랜치**: `feat_phase4_dynamic_rank` (PR #17)
- **관련 커밋**: `f4e51b6`

---

## 2. 주요 구현 아키텍처 및 로직

### 2.1 랭크 산출 공식 (Pure Math & Deterministic)
$$\text{Rank} = \text{Clamp}\left(\text{CurrentStage} \times 2 + \left\lfloor \frac{\text{SurvivalSeconds}}{30} \right\rfloor - \text{DeathCount} \times 3, \; 1, \; 32\right)$$

- **기본 스테이지 가산**: $\text{Stage} \times 2$ (예: Stage 1 = Rank 2, Stage 5 = Rank 10)
- **생존 시간 가산**: 30초 생존 시마다 $+1$ 랭크 누적
- **사망 페널티**: 피격 사망 1회당 $-3$ 랭크 차감 (최소 Rank 1 하한 보장)
- **최대 상한**: Rank 32

### 2.2 4개 랭크 구간별 파라미터 매핑 테이블
| 구간 (Rank) | 적 비행속도 | 적 탄속 | 최대 동시 다이브 | 다이브 쿨타임 | 트랙터 빔 확률 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Rank 1 ~ 5** | 8.33 u/s | 11.11 u/s | 2기 | 3.0초 | 20% (0.20) |
| **Rank 6 ~ 15** | 12.50 u/s | 15.28 u/s | 3기 | 1.8초 | 50% (0.50) |
| **Rank 16 ~ 25** | 15.50 u/s | 18.00 u/s | 4기 | 1.2초 | 75% (0.75) |
| **Rank 26 ~ 32** | 17.36 u/s | 20.83 u/s | 5기 | 0.8초 | 90% (0.90) |

### 2.3 C# 클래스 및 이벤트 파이프라인
1. `DifficultyRankParameters` (struct)
   - 랭크 가변 수치 매개변수를 담는 불변 구조체 (`IEquatable<DifficultyRankParameters>` 구현)
2. `DifficultyRankManager` (MonoBehaviour, Singleton)
   - `OnRankChanged(int)`: 랭크 정수값 변동 이벤트
   - `OnParametersChanged(DifficultyRankParameters)`: 파라미터 변동 이벤트
   - `StageManager.OnStageChanged`, `PlayerHealth.OnLivesChanged` 구독을 통한 자동 랭크 갱신
   - `EnemyDiveController`에 다이브 속도, 쿨타임, 동시 다이브 수 실시간 주입

---

## 3. 완제품 프리팹 및 에셋 무결성 (Zero-Override)
- **프리팹 위치**: `Assets/Prefabs/Managers/PF_DifficultyRankManager.prefab`
- **인스펙터 직렬화 바인딩**: `_autoUpdateInGame = true`

---

## 4. NUnit 단위 테스트 검증 내역
- **테스트 파일**: `Assets/Tests/Editor/DifficultyRankTests.cs` (15종 테스트)
  - 스테이지 가산, 생존 시간 누적, 사망 감산 공식 검증
  - Rank 1 하한 및 Rank 32 상한 클램프 검증
  - 4개 구간별 파라미터 반환 무결성 검증
  - 수동 오버라이드 및 이벤트 발행 검증
- **테스트 결과**: 135/135 전체 테스트 100% Pass (무인 회귀 검증 완료)
