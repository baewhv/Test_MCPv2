using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Galaga.Core;
using Galaga.Gameplay.Combat;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    [TestFixture]
    public class FighterCaptureSequenceTests
    {
        private GameObject _playerObject;
        private PlayerController _playerController;
        private PlayerShooting _playerShooting;
        private PlayerHealth _playerHealth;
        private SpriteRenderer _playerRenderer;

        private GameObject _bossObject;
        private EnemyBase _bossEnemyBase;
        private EnemyBoss _enemyBoss;
        private BossTractorBeam _tractorBeam;

        private GameObject _controllerObject;
        private FighterCaptureController _captureController;

        [SetUp]
        public void SetUp()
        {
            // 1. 플레이어 오브젝트 구성
            _playerObject = new GameObject("TestPlayer");
            _playerObject.tag = "Player";
            _playerObject.AddComponent<BoxCollider2D>().isTrigger = true;
            _playerRenderer = _playerObject.AddComponent<SpriteRenderer>();
            _playerRenderer.color = Color.white;

            _playerController = _playerObject.AddComponent<PlayerController>();
            _playerShooting = _playerObject.AddComponent<PlayerShooting>();
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();
            _playerHealth.Initialize(3);

            MethodInfo playerAwake = typeof(PlayerController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            playerAwake?.Invoke(_playerController, null);

            // 2. 보스 및 트랙터 빔 구성
            _bossObject = new GameObject("TestBoss");
            _bossObject.tag = "Enemy";
            _bossObject.transform.position = new Vector3(0f, 2.0f, 0f);
            _bossEnemyBase = _bossObject.AddComponent<EnemyBase>();
            _enemyBoss = _bossObject.AddComponent<EnemyBoss>();

            GameObject beamObject = new GameObject("TractorBeam");
            beamObject.transform.SetParent(_bossObject.transform);
            beamObject.transform.localPosition = Vector3.zero;
            _tractorBeam = beamObject.AddComponent<BossTractorBeam>();

            MethodInfo bossAwake = typeof(EnemyBoss).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            bossAwake?.Invoke(_enemyBoss, null);

            MethodInfo beamAwake = typeof(BossTractorBeam).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            beamAwake?.Invoke(_tractorBeam, null);

            _enemyBoss.TractorBeam = _tractorBeam;
            _tractorBeam.OwnerBoss = _bossEnemyBase;

            // 3. 포획 시퀀스 컨트롤러 구성
            _controllerObject = new GameObject("FighterCaptureController");
            _captureController = _controllerObject.AddComponent<FighterCaptureController>();

            MethodInfo controllerAwake = typeof(FighterCaptureController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            controllerAwake?.Invoke(_captureController, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_captureController != null && _captureController.IsCapturing)
            {
                _captureController.CancelCaptureSequence();
            }

            if (_controllerObject != null)
            {
                Object.DestroyImmediate(_controllerObject);
            }

            if (_playerObject != null)
            {
                Object.DestroyImmediate(_playerObject);
            }

            if (_bossObject != null)
            {
                Object.DestroyImmediate(_bossObject);
            }
        }

        #region Initialization & Initial State Tests

        [Test]
        public void FighterCaptureController_InitialState_IsNotCapturingAndPhaseNone()
        {
            Assert.IsFalse(_captureController.IsCapturing);
            Assert.AreEqual(CapturePhase.None, _captureController.CurrentPhase);
            Assert.AreEqual(0f, _captureController.CurrentPhaseTimer);
            Assert.AreEqual(0f, _captureController.TotalCaptureElapsedTime);
            Assert.IsNull(_captureController.CapturedPlayer);
        }

        #endregion

        #region Phase 1: Control Loss & Center Alignment Tests

        [Test]
        public void FighterCaptureController_StartCaptureSequence_EntersPhase1_DisablesPlayerControlAndEnablesInvincible()
        {
            _tractorBeam.ActivateBeam(4.0f);

            bool captureStartedFired = false;
            CapturePhase recordedPhase = CapturePhase.None;

            _captureController.OnCaptureStarted += (player, boss) => captureStartedFired = true;
            _captureController.OnPhaseChanged += (phase) => recordedPhase = phase;

            bool success = _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            Assert.IsTrue(success);
            Assert.IsTrue(_captureController.IsCapturing);
            Assert.AreEqual(CapturePhase.Phase1_ControlLoss, _captureController.CurrentPhase);
            Assert.AreEqual(CapturePhase.Phase1_ControlLoss, recordedPhase);
            Assert.IsTrue(captureStartedFired);

            // Phase 1 제약 검증: CanMove=false, CanShoot=false, 무적 활성화
            Assert.IsFalse(_playerController.CanMove);
            Assert.IsFalse(_playerShooting.CanShoot);
            Assert.IsTrue(_playerHealth.IsInvincible);
        }

        [Test]
        public void FighterCaptureController_Phase1_InterpolatesPlayerPositionTowardsBeamCenterX()
        {
            _playerObject.transform.position = new Vector3(-2.0f, -8.0f, 0f);
            _bossObject.transform.position = new Vector3(0f, 2.0f, 0f);
            _tractorBeam.ActivateBeam(4.0f);

            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            float initialX = _playerObject.transform.position.x;
            _captureController.UpdateSequence(0.1f);

            Assert.Greater(_playerObject.transform.position.x, initialX, "Player X should move towards beam center (0.0)");
        }

        #endregion

        #region Phase 2: Spin & Alignment Tests

        [Test]
        public void FighterCaptureController_Phase1ToPhase2_TransitionsAccurately()
        {
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            // Phase 1 Duration (0.2s) 경과 틱
            _captureController.UpdateSequence(0.21f);

            Assert.AreEqual(CapturePhase.Phase2_SpinAlign, _captureController.CurrentPhase);
        }

        [Test]
        public void FighterCaptureController_Phase2_RotatesZAxisAndPullsUpward()
        {
            _playerObject.transform.position = new Vector3(0f, -8.0f, 0f);
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            // Transition to Phase 2
            _captureController.UpdateSequence(0.2f);
            Assert.AreEqual(CapturePhase.Phase2_SpinAlign, _captureController.CurrentPhase);

            float prevY = _playerObject.transform.position.y;
            _captureController.UpdateSequence(0.5f);

            Assert.Greater(_playerObject.transform.position.y, prevY, "Player Y should increase during Phase 2");
            Assert.AreNotEqual(0f, _playerObject.transform.rotation.eulerAngles.z, "Player should be rotating in Phase 2");
        }

        #endregion

        #region Phase 3: Tractor Pull & Color Inversion Tests

        [Test]
        public void FighterCaptureController_Phase2ToPhase3_TransitionsAndInvertsColor()
        {
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            // Advance through Phase 1 (0.2s) and Phase 2 (1.0s)
            _captureController.UpdateSequence(0.2f);
            _captureController.UpdateSequence(1.01f);

            Assert.AreEqual(CapturePhase.Phase3_TractorPull, _captureController.CurrentPhase);

            // Advance Phase 3 by half duration
            _captureController.UpdateSequence(0.5f);

            // Color should be tinting towards captured red tint
            Assert.AreNotEqual(Color.white, _playerRenderer.color);
        }

        #endregion

        #region Phase 4: Formation Binding & Respawn Tests

        [Test]
        public void FighterCaptureController_Phase3ToPhase4_BindsToBossSlot_AndDeductsLife()
        {
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            int initialLives = _playerHealth.CurrentLives;
            Assert.AreEqual(3, initialLives);

            // Advance through Phase 1 (0.2s), Phase 2 (1.0s), Phase 3 (1.0s)
            _captureController.UpdateSequence(0.2f);
            _captureController.UpdateSequence(1.0f);
            _captureController.UpdateSequence(1.01f);

            Assert.AreEqual(CapturePhase.Phase4_FormationBind, _captureController.CurrentPhase);

            // Phase 4 결속 검증: 보스 상단에 포획기 결속 확인
            Assert.IsTrue(_enemyBoss.HasCapturedFighter);
            Assert.IsNotNull(_enemyBoss.CapturedFighter);
            Assert.AreEqual(2, _playerHealth.CurrentLives, "Player lives must decrease by 1 upon capture bind");
            Assert.IsFalse(_tractorBeam.IsBeamActive, "Tractor beam must deactivate upon binding");
        }

        [Test]
        public void FighterCaptureController_CompleteCaptureSequence_WhenLivesRemain_RespawnsPlayerAndRestoresControl()
        {
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            CapturedFighter completedFighter = null;
            _captureController.OnCaptureCompleted += (fighter) => completedFighter = fighter;

            // Full sequence: 0.2s + 1.0s + 1.0s + 0.8s = 3.0s
            _captureController.UpdateSequence(0.2f);
            _captureController.UpdateSequence(1.0f);
            _captureController.UpdateSequence(1.0f);
            _captureController.UpdateSequence(0.81f);

            Assert.IsFalse(_captureController.IsCapturing);
            Assert.AreEqual(CapturePhase.None, _captureController.CurrentPhase);
            Assert.IsNotNull(completedFighter);

            // 차기 기체 출격 및 조작권 회복 검증
            Assert.IsTrue(_playerController.CanMove);
            Assert.IsTrue(_playerShooting.CanShoot);
            Assert.AreEqual(_playerHealth.RespawnPosition, _playerObject.transform.position);
            Assert.AreEqual(Color.white, _playerRenderer.color);
        }

        [Test]
        public void FighterCaptureController_CompleteCaptureSequence_WhenLastLife_TriggersDeath()
        {
            _playerHealth.SetLives(1);
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            // Full sequence
            _captureController.UpdateSequence(0.2f);
            _captureController.UpdateSequence(1.0f);
            _captureController.UpdateSequence(1.0f);
            _captureController.UpdateSequence(0.81f);

            Assert.AreEqual(0, _playerHealth.CurrentLives);
            Assert.IsTrue(_playerHealth.IsDead);
            Assert.IsFalse(_playerObject.activeSelf);
        }

        #endregion

        #region Cancel & Exceptional Case Tests

        [Test]
        public void FighterCaptureController_CancelCaptureSequence_RestoresPlayerControl()
        {
            _tractorBeam.ActivateBeam(4.0f);
            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            Assert.IsTrue(_captureController.IsCapturing);
            Assert.IsFalse(_playerController.CanMove);

            bool cancelFired = false;
            _captureController.OnCaptureCancelled += () => cancelFired = true;

            _captureController.CancelCaptureSequence();

            Assert.IsFalse(_captureController.IsCapturing);
            Assert.AreEqual(CapturePhase.None, _captureController.CurrentPhase);
            Assert.IsTrue(_playerController.CanMove);
            Assert.IsTrue(_playerShooting.CanShoot);
            Assert.IsTrue(cancelFired);
        }

        [Test]
        public void FighterCaptureController_StartCaptureSequence_WhenAlreadyDead_ReturnsFalse()
        {
            _playerHealth.SetLives(0);
            _tractorBeam.ActivateBeam(4.0f);

            bool success = _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            Assert.IsFalse(success);
            Assert.IsFalse(_captureController.IsCapturing);
        }

        #endregion

        #region CapturedFighter Component Tests

        [Test]
        public void CapturedFighter_InitializeAndAttachToSlot_ManagesStateAndVisual()
        {
            GameObject cfObj = new GameObject("CapturedFighterTest");
            EnemyBase enemyBase = cfObj.AddComponent<EnemyBase>();
            SpriteRenderer sr = cfObj.AddComponent<SpriteRenderer>();
            CapturedFighter capturedFighter = cfObj.AddComponent<CapturedFighter>();

            capturedFighter.Initialize(_enemyBoss);

            Assert.IsTrue(capturedFighter.IsAttachedToBoss);
            Assert.AreEqual(_enemyBoss.CapturedFighterSlot, capturedFighter.transform.parent);
            Assert.AreEqual(capturedFighter.CapturedColor, sr.color);

            capturedFighter.DetachFromBoss();

            Assert.IsFalse(capturedFighter.IsAttachedToBoss);
            Assert.IsNull(capturedFighter.transform.parent);

            Object.DestroyImmediate(cfObj);
        }

        [Test]
        public void CapturedFighter_EnemyBaseDestroyed_FiresOnFighterDestroyedEvent()
        {
            GameObject cfObj = new GameObject("CapturedFighterTestDestroy");
            EnemyBase enemyBase = cfObj.AddComponent<EnemyBase>();
            cfObj.AddComponent<SpriteRenderer>();
            CapturedFighter capturedFighter = cfObj.AddComponent<CapturedFighter>();

            // Trigger Awake & OnEnable
            MethodInfo awakeMethod = typeof(CapturedFighter).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(capturedFighter, null);
            MethodInfo onEnableMethod = typeof(CapturedFighter).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnableMethod?.Invoke(capturedFighter, null);

            bool destroyedFired = false;
            capturedFighter.OnFighterDestroyed += (f) => destroyedFired = true;

            enemyBase.Die();

            Assert.IsTrue(destroyedFired, "OnFighterDestroyed should fire when underlying EnemyBase dies");

            Object.DestroyImmediate(cfObj);
        }

        [Test]
        public void FighterCaptureController_Events_FireInCorrectSequenceThroughAllPhases()
        {
            _tractorBeam.ActivateBeam(4.0f);

            var phaseList = new System.Collections.Generic.List<CapturePhase>();
            bool captureStarted = false;
            bool captureCompleted = false;

            _captureController.OnCaptureStarted += (p, b) => captureStarted = true;
            _captureController.OnPhaseChanged += (phase) => phaseList.Add(phase);
            _captureController.OnCaptureCompleted += (f) => captureCompleted = true;

            _captureController.StartCaptureSequence(_playerController, _tractorBeam, _enemyBoss);

            // Phase 1 -> 2 -> 3 -> 4 -> Complete
            _captureController.UpdateSequence(0.21f);
            _captureController.UpdateSequence(1.01f);
            _captureController.UpdateSequence(1.01f);
            _captureController.UpdateSequence(0.81f);

            Assert.IsTrue(captureStarted);
            Assert.AreEqual(4, phaseList.Count);
            Assert.AreEqual(CapturePhase.Phase1_ControlLoss, phaseList[0]);
            Assert.AreEqual(CapturePhase.Phase2_SpinAlign, phaseList[1]);
            Assert.AreEqual(CapturePhase.Phase3_TractorPull, phaseList[2]);
            Assert.AreEqual(CapturePhase.Phase4_FormationBind, phaseList[3]);
            Assert.IsTrue(captureCompleted);
        }

        #endregion
    }
}
