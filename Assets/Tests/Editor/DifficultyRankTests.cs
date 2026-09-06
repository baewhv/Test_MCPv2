using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Galaga.Gameplay.Difficulty;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Stage;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class DifficultyRankTests
    {
        private GameObject _holderObject;
        private DifficultyRankManager _rankManager;

        [SetUp]
        public void SetUp()
        {
            _holderObject = new GameObject("Test_DifficultyRankManager");
            _rankManager = _holderObject.AddComponent<DifficultyRankManager>();
            _rankManager.AutoUpdateInGame = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_holderObject != null)
            {
                Object.DestroyImmediate(_holderObject);
            }
        }

        // =========================================================
        // 1. 순수 수학 공식 (CalculateRank) 검증 테스트
        // =========================================================

        [Test]
        public void CalculateRank_Stage1_InitialState_ReturnsRank2()
        {
            // Stage 1 * 2 + Floor(0 / 30) - 0 * 3 = 2
            int rank = DifficultyRankManager.CalculateRank(1, 0f, 0);
            Assert.AreEqual(2, rank);
        }

        [Test]
        public void CalculateRank_Stage2_InitialState_ReturnsRank4()
        {
            // Stage 2 * 2 + Floor(0 / 30) - 0 * 3 = 4
            int rank = DifficultyRankManager.CalculateRank(2, 0f, 0);
            Assert.AreEqual(4, rank);
        }

        [Test]
        public void CalculateRank_SurvivalTimeIncrease_IncreasesRankEvery30Seconds()
        {
            // Stage 1 (기본 2) 기준
            Assert.AreEqual(2, DifficultyRankManager.CalculateRank(1, 0f, 0));
            Assert.AreEqual(2, DifficultyRankManager.CalculateRank(1, 29.9f, 0));
            Assert.AreEqual(3, DifficultyRankManager.CalculateRank(1, 30.0f, 0));
            Assert.AreEqual(3, DifficultyRankManager.CalculateRank(1, 59.9f, 0));
            Assert.AreEqual(4, DifficultyRankManager.CalculateRank(1, 60.0f, 0));
            Assert.AreEqual(5, DifficultyRankManager.CalculateRank(1, 90.0f, 0));
        }

        [Test]
        public void CalculateRank_DeathPenalty_ReducesRankBy3PerDeath()
        {
            // Stage 5 (10) + 60s (2) = 12
            Assert.AreEqual(12, DifficultyRankManager.CalculateRank(5, 60f, 0));
            // 1회 사망 시: 12 - 3 = 9
            Assert.AreEqual(9, DifficultyRankManager.CalculateRank(5, 60f, 1));
            // 2회 사망 시: 12 - 6 = 6
            Assert.AreEqual(6, DifficultyRankManager.CalculateRank(5, 60f, 2));
        }

        [Test]
        public void CalculateRank_ClampsBetweenMinAndMax_1To32()
        {
            // 하한선 검증: 음수로 떨어질 경우 1로 클램프
            int minClamped = DifficultyRankManager.CalculateRank(1, 0f, 10); // 2 - 30 = -28 -> 1
            Assert.AreEqual(1, minClamped);

            // 상한선 검증: 32를 초과할 경우 32로 클램프
            int maxClamped = DifficultyRankManager.CalculateRank(20, 600f, 0); // 40 + 20 = 60 -> 32
            Assert.AreEqual(32, maxClamped);
        }

        // =========================================================
        // 2. 랭크 구간별 파라미터 매핑 (GetParametersForRank) 검증 테스트
        // =========================================================

        [Test]
        public void GetParametersForRank_Tier1To5_ReturnsTier1Values()
        {
            for (int rank = 1; rank <= 5; rank++)
            {
                DifficultyRankParameters parameters = DifficultyRankManager.GetParametersForRank(rank);
                Assert.AreEqual(rank, parameters.Rank);
                Assert.AreEqual(8.33f, parameters.DiveSpeed, 0.001f);
                Assert.AreEqual(11.11f, parameters.BulletSpeed, 0.001f);
                Assert.AreEqual(2, parameters.MaxConcurrentDives);
                Assert.AreEqual(3.0f, parameters.DiveInterval, 0.001f);
                Assert.AreEqual(0.20f, parameters.TractorBeamProbability, 0.001f);
            }
        }

        [Test]
        public void GetParametersForRank_Tier6To15_ReturnsTier2Values()
        {
            for (int rank = 6; rank <= 15; rank++)
            {
                DifficultyRankParameters parameters = DifficultyRankManager.GetParametersForRank(rank);
                Assert.AreEqual(rank, parameters.Rank);
                Assert.AreEqual(12.50f, parameters.DiveSpeed, 0.001f);
                Assert.AreEqual(15.28f, parameters.BulletSpeed, 0.001f);
                Assert.AreEqual(3, parameters.MaxConcurrentDives);
                Assert.AreEqual(1.8f, parameters.DiveInterval, 0.001f);
                Assert.AreEqual(0.50f, parameters.TractorBeamProbability, 0.001f);
            }
        }

        [Test]
        public void GetParametersForRank_Tier16To25_ReturnsTier3Values()
        {
            for (int rank = 16; rank <= 25; rank++)
            {
                DifficultyRankParameters parameters = DifficultyRankManager.GetParametersForRank(rank);
                Assert.AreEqual(rank, parameters.Rank);
                Assert.AreEqual(15.50f, parameters.DiveSpeed, 0.001f);
                Assert.AreEqual(18.00f, parameters.BulletSpeed, 0.001f);
                Assert.AreEqual(4, parameters.MaxConcurrentDives);
                Assert.AreEqual(1.2f, parameters.DiveInterval, 0.001f);
                Assert.AreEqual(0.75f, parameters.TractorBeamProbability, 0.001f);
            }
        }

        [Test]
        public void GetParametersForRank_Tier26To32_ReturnsTier4Values()
        {
            for (int rank = 26; rank <= 32; rank++)
            {
                DifficultyRankParameters parameters = DifficultyRankManager.GetParametersForRank(rank);
                Assert.AreEqual(rank, parameters.Rank);
                Assert.AreEqual(17.36f, parameters.DiveSpeed, 0.001f);
                Assert.AreEqual(20.83f, parameters.BulletSpeed, 0.001f);
                Assert.AreEqual(5, parameters.MaxConcurrentDives);
                Assert.AreEqual(0.8f, parameters.DiveInterval, 0.001f);
                Assert.AreEqual(0.90f, parameters.TractorBeamProbability, 0.001f);
            }
        }

        // =========================================================
        // 3. DifficultyRankManager 생명주기 및 런타임 제어 검증 테스트
        // =========================================================

        [Test]
        public void DifficultyRankManager_Initialize_SetsCorrectInitialState()
        {
            _rankManager.Initialize(1);

            Assert.AreEqual(1, _rankManager.CurrentStage);
            Assert.AreEqual(0f, _rankManager.SurvivalSeconds);
            Assert.AreEqual(0, _rankManager.DeathCount);
            Assert.AreEqual(2, _rankManager.CurrentRank);
            Assert.AreEqual(8.33f, _rankManager.CurrentParameters.DiveSpeed, 0.001f);
            Assert.IsFalse(_rankManager.IsManualRankOverride);
        }

        [Test]
        public void DifficultyRankManager_UpdateSurvivalTime_UpdatesRankAndFiresEvents()
        {
            _rankManager.Initialize(1);

            int lastFiredRank = -1;
            DifficultyRankParameters lastFiredParams = default;
            _rankManager.OnRankChanged += r => lastFiredRank = r;
            _rankManager.OnParametersChanged += p => lastFiredParams = p;

            // 30초 생존 시간 누적 -> 랭크 3으로 상승
            _rankManager.UpdateSurvivalTime(30.0f);

            Assert.AreEqual(30.0f, _rankManager.SurvivalSeconds, 0.001f);
            Assert.AreEqual(3, _rankManager.CurrentRank);
            Assert.AreEqual(3, lastFiredRank);
            Assert.AreEqual(3, lastFiredParams.Rank);
        }

        [Test]
        public void DifficultyRankManager_RecordDeath_DecreasesRank()
        {
            _rankManager.Initialize(3); // Stage 3 -> 기본 랭크 6
            Assert.AreEqual(6, _rankManager.CurrentRank);

            _rankManager.RecordDeath(); // 6 - 3 = 3
            Assert.AreEqual(1, _rankManager.DeathCount);
            Assert.AreEqual(3, _rankManager.CurrentRank);
            Assert.AreEqual(8.33f, _rankManager.CurrentParameters.DiveSpeed, 0.001f);
        }

        [Test]
        public void DifficultyRankManager_SetStage_UpdatesBaseStageRank()
        {
            _rankManager.Initialize(1);
            Assert.AreEqual(2, _rankManager.CurrentRank);

            _rankManager.SetStage(4); // Stage 4 -> 기본 랭크 8
            Assert.AreEqual(4, _rankManager.CurrentStage);
            Assert.AreEqual(8, _rankManager.CurrentRank);
            Assert.AreEqual(12.50f, _rankManager.CurrentParameters.DiveSpeed, 0.001f);
        }

        [Test]
        public void DifficultyRankManager_ManualRankOverride_WorksCorrectly()
        {
            _rankManager.Initialize(1);
            Assert.AreEqual(2, _rankManager.CurrentRank);

            // 강제 랭크 20 설정 (Tier 3)
            _rankManager.SetManualRank(20);
            Assert.IsTrue(_rankManager.IsManualRankOverride);
            Assert.AreEqual(20, _rankManager.CurrentRank);
            Assert.AreEqual(15.50f, _rankManager.CurrentParameters.DiveSpeed, 0.001f);
            Assert.AreEqual(4, _rankManager.CurrentParameters.MaxConcurrentDives);

            // 오버라이드 상태에서는 생존 시간 변경 무시
            _rankManager.UpdateSurvivalTime(100f);
            Assert.AreEqual(20, _rankManager.CurrentRank);

            // 오버라이드 해제 시 정상 계산 복귀
            _rankManager.ClearManualRankOverride();
            Assert.IsFalse(_rankManager.IsManualRankOverride);
            Assert.AreEqual(2, _rankManager.CurrentRank); // Stage 1, 0 deaths, 0 survival (초기화 기준)
        }

        [Test]
        public void DifficultyRankManager_AppliesParametersToEnemyDiveController()
        {
            GameObject diveObj = new GameObject("Test_DiveController");
            EnemyDiveController diveController = diveObj.AddComponent<EnemyDiveController>();
            diveController.AutoDiveEnabled = false;

            _rankManager.EnemyDiveController = diveController;
            _rankManager.Initialize(1);

            // Stage 1 (Rank 2) -> Tier 1 수치
            Assert.AreEqual(8.33f, diveController.DiveSpeed, 0.001f);
            Assert.AreEqual(3.0f, diveController.DiveInterval, 0.001f);
            Assert.AreEqual(2, diveController.MaxConcurrentDives);

            // Rank 18 (Tier 3) 수동 설정
            _rankManager.SetManualRank(18);
            Assert.AreEqual(15.50f, diveController.DiveSpeed, 0.001f);
            Assert.AreEqual(1.2f, diveController.DiveInterval, 0.001f);
            Assert.AreEqual(4, diveController.MaxConcurrentDives);

            Object.DestroyImmediate(diveObj);
        }
    }
}
