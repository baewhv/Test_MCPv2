using System.Collections.Generic;
using UnityEngine;
using Galaga.Gameplay.Enemy;
using Galaga.Gameplay.Player;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 폭발 이펙트(PF_Explosion) 오브젝트 풀을 관리하고, 적 격파 및 플레이어 피격 시 폭발 연출을 스폰하는 매니저 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplosionManager : MonoBehaviour
    {
        public static ExplosionManager Instance { get; private set; }

        [Header("Pool Configuration")]
        [Tooltip("폭발 완제품 프리팹 (PF_Explosion)")]
        [SerializeField] private GameObject _explosionPrefab;

        [Tooltip("초기 풀 크기")]
        [SerializeField] private int _initialPoolSize = 10;

        private readonly List<ExplosionEffect> _pool = new List<ExplosionEffect>();
        private readonly HashSet<ExplosionEffect> _activeEffects = new HashSet<ExplosionEffect>();
        private Transform _poolRoot;

        public GameObject ExplosionPrefab
        {
            get => _explosionPrefab;
            set => _explosionPrefab = value;
        }

        public int InitialPoolSize
        {
            get => _initialPoolSize;
            set => _initialPoolSize = Mathf.Max(1, value);
        }

        public int ActiveExplosionCount => _activeEffects.Count;
        public int TotalPoolCount => _pool.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 풀을 초기화하고 사전 인스턴스를 생성합니다.
        /// </summary>
        public void InitializePool()
        {
            if (_poolRoot == null)
            {
                _poolRoot = transform;
            }

            if (_explosionPrefab == null)
            {
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewExplosionInstance();
            }
        }

        private ExplosionEffect CreateNewExplosionInstance()
        {
            GameObject obj;
            if (_explosionPrefab != null)
            {
                obj = Instantiate(_explosionPrefab, _poolRoot);
            }
            else
            {
                obj = new GameObject("ExplosionEffect");
                obj.transform.SetParent(_poolRoot);
            }

            obj.SetActive(false);

            ExplosionEffect effect = obj.GetComponent<ExplosionEffect>();
            if (effect == null)
            {
                effect = obj.AddComponent<ExplosionEffect>();
            }

            _pool.Add(effect);
            return effect;
        }

        /// <summary>
        /// 지정된 위치와 크기로 폭발 이펙트를 스폰하여 재생합니다.
        /// </summary>
        public ExplosionEffect SpawnExplosion(Vector3 position, float scale = 1.0f, float duration = 0.5f)
        {
            ExplosionEffect effect = GetPooledExplosion();
            effect.gameObject.SetActive(true);
            effect.Play(position, duration, scale, OnExplosionCompleted);

            _activeEffects.Add(effect);
            return effect;
        }

        private ExplosionEffect GetPooledExplosion()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }

            return CreateNewExplosionInstance();
        }

        private void OnExplosionCompleted(ExplosionEffect effect)
        {
            if (effect != null)
            {
                _activeEffects.Remove(effect);
            }
        }

        /// <summary>
        /// 적 기체 격파 이벤트에 연결하여 자동으로 폭발을 재생하는 리스너입니다.
        /// </summary>
        public void HandleEnemyDestroyed(EnemyBase enemy)
        {
            if (enemy != null)
            {
                float scale = (enemy.Type == EnemyType.BossGalaga) ? 1.5f : 1.0f;
                SpawnExplosion(enemy.transform.position, scale);
            }
        }

        /// <summary>
        /// 플레이어 사망 이벤트에 연결하여 자동으로 폭발을 재생하는 리스너입니다.
        /// </summary>
        public void HandlePlayerDied(Vector3 playerPos)
        {
            SpawnExplosion(playerPos, 1.8f, 0.8f);
        }
    }
}
