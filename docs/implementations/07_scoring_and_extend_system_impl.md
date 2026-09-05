# [Implementation 07] 점수 산정(Scoring) 및 익스텐드(Extend Life) 시스템 구현 기술문서

## 1. 개요 및 목적
- **작업 브랜치**: `feat_phase4_score_extend_system`
- **목적**:
  1. 원작 Galaga의 적 기체 유형별(Zako, Goei, Boss Galaga) 및 상태별(대기, 단독 다이브, 호위 다이브) 차등 점수 산정 시스템을 구축합니다.
  2. 1차(20,000점) 및 2차/이후(매 70,000점 누적 시) 보너스 잔기(Extend Life) 지급 메커니즘을 `ScoreManager`와 `PlayerHealth.AddLife()` 연동으로 구현합니다.
  3. 적 기체(`EnemyBase`) 격파(`Die()`) 시 `ScoreManager.Instance.AddEnemyScore()`를 자동 호출하여 점수가 실시간으로 가산되는 통합 파이프라인을 확립합니다.
  4. 독립 완제품 프리팹 `PF_ScoreManager.prefab`을 신규 생성하여 씬 독립성과 Zero-Override 무결성을 보장합니다.

---

## 2. 변경 파일 및 클래스별 상세 구현

### 2.1 `ScoreManager.cs` (`Assets/Scripts/Gameplay/Score/ScoreManager.cs`)
- **네임스페이스**: `Galaga.Gameplay.Score`
- **싱글톤 아키텍처**:
  - `public static ScoreManager Instance { get; private set; }`를 통한 글로벌 접근성 제공 및 `Awake()` 시 중복 인스턴스 파괴.
- **주요 프로퍼티 및 필드**:
  - `_currentScore`: 현재 획득 누적 점수.
  - `_highScore`: 현재 기록된 최고 점수 (기본 20,000점).
  - `_nextExtendScore`: 다음 익스텐드 목표 점수 (초기 20,000점, 1차 달성 후 70,000점, 이후 +70,000점 단위 누적).
  - `_firstExtendScore` (20,000) / `_repeatExtendInterval` (70,000).
  - `_playerHealth`: 잔기 지급을 위한 `PlayerHealth` 직렬화 참조 (런타임 `FindAnyObjectByType` fallback 지원).
- **C# 이벤트 시스템**:
  - `public event Action<int> OnScoreChanged`: 점수 변경 시 실시간 UI 갱신용 이벤트.
  - `public event Action<int> OnHighScoreChanged`: 최고 점수 갱신 시 이벤트.
  - `public event Action<int> OnExtendLife`: 익스텐드 달성 시 현재 점수 전달 이벤트.
- **핵심 메서드**:
  - `CalculateEnemyScore(EnemyType type, bool isDiving, int escortCount = 0)`:
    - **자코 (Zako)**: 대기 50점 / 비행(급강하) 100점.
    - **고에이 (Goei)**: 대기 80점 / 비행(급강하) 160점.
    - **보스 (Boss Galaga)**: 대기 150점 / 단독 비행 400점 / 호위 1기 비행 800점 / 호위 2기 비행 1,600점.
  - `AddScore(int points)`: 점수 가산, 하이스코어 갱신 검사, `CheckExtend()` 호출.
  - `AddEnemyScore(EnemyType type, bool isDiving, int escortCount = 0)`: 적 유형별 점수 산정 후 `AddScore()` 호출.
  - `CheckExtend()`:
    - 1차 미달성 상태에서 `_currentScore >= _firstExtendScore` (20,000) 달성 시 익스텐드 발동 및 다음 목표를 70,000점으로 설정.
    - 1차 달성 이후 `_currentScore >= _nextExtendScore` 도달 시마다 익스텐드 발동 및 목표를 +70,000점 누적 가산.
    - 대량 점수 일괄 가산 시에도 반복 검사를 통해 누락 없는 익스텐드 처리.
  - `TriggerExtend()`: `OnExtendLife?.Invoke(_currentScore)` 발행 및 `_playerHealth?.AddLife(1)` 호출.
  - `Initialize(int initialHighScore = 20000, PlayerHealth playerHealth = null)` 및 `ResetScore()`: 테스트 및 런타임 수명주기 초기화 API 제공.

### 2.2 `PlayerHealth.cs` (`Assets/Scripts/Gameplay/Player/PlayerHealth.cs`)
- **잔기 증가 API**:
  - `public void AddLife(int count = 1)`:
    - 사망 상태(`_isDead == true`)가 아닐 때 `_currentLives = Mathf.Min(_currentLives + count, _maxLives)`로 잔기 증가.
    - `OnLivesChanged?.Invoke(_currentLives)` 이벤트를 발행하여 UI 실시간 동기화.

### 2.3 `EnemyBase.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyBase.cs`)
- **격파 점수 가산 파이프라인 연동**:
  - `Die()` 메서드 내부에서 `ScoreManager.Instance` 존재 시 `ScoreManager.Instance.AddEnemyScore(Type, isDiving, _escortCount)` 호출.
  - `isDiving` 판정: `_currentState == EnemyState.Diving || _currentState == EnemyState.Returning`.
  - 호위기 수 추적용 `_escortCount` 필드 및 `EscortCount` 프로퍼티 추가.

### 2.4 `EnemyDiveController.cs` (`Assets/Scripts/Gameplay/Enemy/EnemyDiveController.cs`)
- **호위 편대 다이브 시 호위기 수 동기화**:
  - `TriggerBossEscortDive()`에서 보스 기체에 `boss.EscortCount = escorts.Count`를 바인딩하여 호위기 동반 격파 시 800점(1기)/1,600점(2기) 정산 보장.

---

## 3. 프리팹 및 씬 직렬화 무결성 (Zero-Override Integrity)

### 3.1 `PF_ScoreManager.prefab` 신규 조립
- 독립 완제품 프리팹 `Assets/Prefabs/PF_ScoreManager.prefab` 신규 생성.
- `ScoreManager` 컴포넌트 부착:
  - `_firstExtendScore: 20000`
  - `_repeatExtendInterval: 70000`
  - `_initialHighScore: 20000`
- 씬 배치 시 별도의 인스펙터 오버라이드 없이 즉시 사용 가능한 Zero-Override 구조 확립.

---

## 4. 검증 결과
- **C# 컴파일 무결성**: Zero Error / Zero Warning 달성.
- **점수 산정 정합성**:
  - 자코: 50점 / 100점
  - 고에이: 80점 / 160점
  - 보스: 150점 / 400점 / 800점 / 1600점
- **익스텐드 시퀀스**:
  - 20,000점 도달 ➔ 잔기 +1, `OnExtendLife` 발행
  - 70,000점 도달 ➔ 잔기 +1, `OnExtendLife` 발행
  - 140,000점 도달 ➔ 잔기 +1, `OnExtendLife` 발행
