using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Tests
{
    /// <summary>
    /// 기체 2D 스프라이트 전환 작업(PR #23)에 대한 프리팹 렌더러 전환 및 ScriptableObject 스프라이트 바인딩 무결성을 검증하는 NUnit 테스트입니다.
    /// </summary>
    [TestFixture]
    public class ShipSpriteSetupTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/PF_Player.prefab";
        private const string ZakoPrefabPath = "Assets/Prefabs/PF_Enemy_Zako.prefab";
        private const string GoeiPrefabPath = "Assets/Prefabs/PF_Enemy_Goei.prefab";
        private const string BossPrefabPath = "Assets/Prefabs/PF_Enemy_Boss.prefab";

        private const string ZakoDataPath = "Assets/ScriptableObjects/Data/SO_Enemy_Zako.asset";
        private const string GoeiDataPath = "Assets/ScriptableObjects/Data/SO_Enemy_Goei.asset";
        private const string BossDataPath = "Assets/ScriptableObjects/Data/SO_Enemy_Boss.asset";

        [Test]
        public void PlayerPrefab_HasSpriteRenderer_AndValidSprite()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(playerPrefab, $"플레이어 프리팹을 찾을 수 없습니다: {PlayerPrefabPath}");

            SpriteRenderer spriteRenderer = playerPrefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spriteRenderer, "PF_Player에 SpriteRenderer 컴포넌트가 존재해야 합니다.");
            Assert.IsNotNull(spriteRenderer.sprite, "PF_Player의 SpriteRenderer에 스프라이트가 할당되어 있어야 합니다.");
            Assert.AreEqual("spr_player_fighter", spriteRenderer.sprite.name, "할당된 플레이어 스프라이트 이름이 일치해야 합니다.");

            // MeshRenderer/MeshFilter 미존재 검증 (2D 전환 검증)
            MeshRenderer meshRenderer = playerPrefab.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = playerPrefab.GetComponent<MeshFilter>();
            Assert.IsNull(meshRenderer, "PF_Player에 레거시 MeshRenderer가 남아있지 않아야 합니다.");
            Assert.IsNull(meshFilter, "PF_Player에 레거시 MeshFilter가 남아있지 않아야 합니다.");
        }

        [TestCase(ZakoPrefabPath, "spr_enemy_zako")]
        [TestCase(GoeiPrefabPath, "spr_enemy_goei")]
        [TestCase(BossPrefabPath, "spr_enemy_boss_galaga")]
        public void EnemyPrefabs_HaveSpriteRenderer_AndValidSprite(string prefabPath, string expectedSpriteName)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(enemyPrefab, $"적 프리팹을 찾을 수 없습니다: {prefabPath}");

            SpriteRenderer spriteRenderer = enemyPrefab.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(spriteRenderer, $"{prefabPath}에 SpriteRenderer 컴포넌트가 존재해야 합니다.");
            Assert.IsNotNull(spriteRenderer.sprite, $"{prefabPath}의 SpriteRenderer에 스프라이트가 할당되어 있어야 합니다.");
            Assert.AreEqual(expectedSpriteName, spriteRenderer.sprite.name, $"할당된 스프라이트 이름이 일치해야 합니다: {expectedSpriteName}");

            // EnemyBase 컴포넌트 및 Renderer 필드 검증
            EnemyBase enemyBase = enemyPrefab.GetComponent<EnemyBase>();
            Assert.IsNotNull(enemyBase, $"{prefabPath}에 EnemyBase 컴포넌트가 존재해야 합니다.");

            // 루트 MeshRenderer 미존재 검증
            MeshRenderer meshRenderer = enemyPrefab.GetComponent<MeshRenderer>();
            Assert.IsNull(meshRenderer, $"{prefabPath} 루트에 레거시 MeshRenderer가 남아있지 않아야 합니다.");
        }

        [TestCase(ZakoDataPath, EnemyType.Zako, "spr_enemy_zako")]
        [TestCase(GoeiDataPath, EnemyType.Goei, "spr_enemy_goei")]
        [TestCase(BossDataPath, EnemyType.BossGalaga, "spr_enemy_boss_galaga")]
        public void EnemyDataSO_Assets_HaveValidSprite_AndWhiteNormalColor(string assetPath, EnemyType expectedType, string expectedSpriteName)
        {
            EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);
            Assert.IsNotNull(data, $"EnemyDataSO 에셋을 찾을 수 없습니다: {assetPath}");

            Assert.AreEqual(expectedType, data.Type, $"EnemyType이 일치해야 합니다: {expectedType}");
            Assert.IsNotNull(data.Sprite, $"{assetPath}에 Sprite가 할당되어 있어야 합니다.");
            Assert.AreEqual(expectedSpriteName, data.Sprite.name, $"EnemyDataSO 스프라이트 이름이 일치해야 합니다: {expectedSpriteName}");
            Assert.AreEqual(Color.white, data.NormalColor, "스프라이트 원본 색상 유지를 위해 NormalColor는 흰색(1,1,1,1)이어야 합니다.");
        }
    }
}
