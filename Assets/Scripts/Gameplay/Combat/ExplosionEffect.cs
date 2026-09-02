using System;
using System.Collections;
using UnityEngine;

namespace Galaga.Gameplay.Combat
{
    /// <summary>
    /// 적 격파 및 플레이어 기체 피격/폭발 시 시각 효과(Particle System / 스케일 연출)를 제어하는 폭발 이펙트 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExplosionEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("폭발 지속 시간(초)")]
        [SerializeField] private float _duration = 0.5f;

        [Tooltip("폭발 연출용 Particle System")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Tooltip("활성화 시 자동 재생 여부")]
        [SerializeField] private bool _autoPlayOnEnable = true;

        private Action<ExplosionEffect> _onCompleteCallback;
        private Coroutine _playCoroutine;

        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0.05f, value);
        }

        public ParticleSystem ParticleSystem => _particleSystem;
        public bool IsPlaying => gameObject.activeSelf;

        private void Awake()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }

        private void OnEnable()
        {
            if (_autoPlayOnEnable)
            {
                Play(transform.position, _duration);
            }
        }

        private void OnDisable()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
        }

        /// <summary>
        /// 지정된 위치와 스케일로 폭발 이펙트를 재생합니다.
        /// </summary>
        public void Play(Vector3 position, float duration = 0.5f, float scale = 1.0f, Action<ExplosionEffect> onComplete = null)
        {
            transform.position = position;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
            _duration = duration;
            _onCompleteCallback = onComplete;

            if (_particleSystem != null)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _particleSystem.main;
                main.duration = _duration;
                _particleSystem.Play();
            }

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
            }

            if (gameObject.activeInHierarchy)
            {
                _playCoroutine = StartCoroutine(PlaybackRoutine());
            }
        }

        private IEnumerator PlaybackRoutine()
        {
            yield return new WaitForSeconds(_duration);

            Complete();
        }

        /// <summary>
        /// 재생을 완료하고 풀에 반환합니다.
        /// </summary>
        public void Complete()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Stop();
            }

            gameObject.SetActive(false);
            _onCompleteCallback?.Invoke(this);
        }
    }
}
