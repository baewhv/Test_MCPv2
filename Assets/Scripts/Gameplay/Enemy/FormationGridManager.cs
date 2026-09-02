using System;
using System.Collections.Generic;
using UnityEngine;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 상단 40기 적 편대 그리드 슬롯 좌표를 생성/관리하고, 주기적 Sine wave 좌우 진동 및 수축/팽창 호흡(Hovering) 연출을 총괄하는 매니저 컴포넌트입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public class FormationGridManager : MonoBehaviour
    {
        [Header("Grid Layout Settings")]
        [Tooltip("편대 그리드의 기준 원점 (월드 좌표)")]
        [SerializeField] private Vector2 _gridOrigin = new Vector2(0f, 6.0f);

        [Tooltip("행(Y축) 간격")]
        [SerializeField] private float _rowSpacing = 0.9f;

        [Tooltip("열(X축) 간격")]
        [SerializeField] private float _columnSpacing = 1.0f;

        [Header("Hovering & Breathing Animation")]
        [Tooltip("호흡/진동 애니메이션 활성화 여부")]
        [SerializeField] private bool _enableHoverAnimation = true;

        [Tooltip("좌우 흔들림(Sway) 주기/주파수 (Hz)")]
        [SerializeField] private float _swayFrequency = 1.5f;

        [Tooltip("좌우 흔들림(Sway) 최대 진폭 (units)")]
        [SerializeField] private float _swayAmplitude = 0.6f;

        [Tooltip("수축/팽창 호흡 주기/주파수 (Hz)")]
        [SerializeField] private float _expandFrequency = 2.0f;

        [Tooltip("수축/팽창 호흡 진폭 비율 (예: 0.15 = ±15%)")]
        [SerializeField] private float _expandAmplitude = 0.12f;

        [Header("Slots State (Read-Only Preview)")]
        [SerializeField] private List<FormationSlot> _slots = new List<FormationSlot>();

        public static readonly int[] RowCounts = { 4, 8, 8, 10, 10 };
        public static readonly EnemyType[] RowTypes = {
            EnemyType.BossGalaga,
            EnemyType.Goei,
            EnemyType.Goei,
            EnemyType.Zako,
            EnemyType.Zako
        };

        public const int TotalSlots = 40;

        public Vector2 GridOrigin
        {
            get => _gridOrigin;
            set => _gridOrigin = value;
        }

        public float RowSpacing
        {
            get => _rowSpacing;
            set => _rowSpacing = Mathf.Max(0.1f, value);
        }

        public float ColumnSpacing
        {
            get => _columnSpacing;
            set => _columnSpacing = Mathf.Max(0.1f, value);
        }

        public bool EnableHoverAnimation
        {
            get => _enableHoverAnimation;
            set => _enableHoverAnimation = value;
        }

        public float SwayFrequency
        {
            get => _swayFrequency;
            set => _swayFrequency = value;
        }

        public float SwayAmplitude
        {
            get => _swayAmplitude;
            set => _swayAmplitude = value;
        }

        public float ExpandFrequency
        {
            get => _expandFrequency;
            set => _expandFrequency = value;
        }

        public float ExpandAmplitude
        {
            get => _expandAmplitude;
            set => _expandAmplitude = value;
        }

        public IReadOnlyList<FormationSlot> Slots => _slots;
        public int OccupiedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i].IsOccupied)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool IsFull => OccupiedCount >= TotalSlots;

        private void Awake()
        {
            InitializeGrid();
        }

        private void Update()
        {
            if (_enableHoverAnimation)
            {
                UpdateFormationHover(Time.time);
            }
        }

        /// <summary>
        /// 5행 40기의 슬롯과 기본 로컬 좌표를 생성/초기화합니다.
        /// </summary>
        public void InitializeGrid()
        {
            _slots.Clear();
            int globalIndex = 0;

            for (int r = 0; r < RowCounts.Length; r++)
            {
                int countInRow = RowCounts[r];
                EnemyType rowType = RowTypes[r];
                float rowWidth = (countInRow - 1) * _columnSpacing;
                float localY = -r * _rowSpacing;

                for (int c = 0; c < countInRow; c++)
                {
                    float localX = -rowWidth * 0.5f + (c * _columnSpacing);
                    Vector2 baseLocalPos = new Vector2(localX, localY);
                    FormationSlot slot = new FormationSlot(r, c, globalIndex, rowType, baseLocalPos);
                    slot.CurrentWorldPosition = _gridOrigin + baseLocalPos;
                    _slots.Add(slot);
                    globalIndex++;
                }
            }
        }

        /// <summary>
        /// 주어진 시간에 따른 좌우 사인파 진동 및 수축/팽창 호흡을 계산하여 슬롯 위치를 갱신하고 안착된 적을 동기화합니다.
        /// </summary>
        public void UpdateFormationHover(float time)
        {
            if (_slots == null || _slots.Count == 0)
            {
                return;
            }

            float swayOffset = Mathf.Sin(time * _swayFrequency) * _swayAmplitude;
            float expandScale = 1.0f + (Mathf.Sin(time * _expandFrequency) * _expandAmplitude);

            for (int i = 0; i < _slots.Count; i++)
            {
                FormationSlot slot = _slots[i];
                Vector2 baseLocal = slot.BaseLocalPosition;
                Vector2 worldPos = _gridOrigin + new Vector2(
                    (baseLocal.x * expandScale) + swayOffset,
                    baseLocal.y * expandScale
                );

                slot.CurrentWorldPosition = worldPos;

                if (slot.IsOccupied && slot.Occupant != null && slot.Occupant.CurrentState == EnemyState.Formation)
                {
                    slot.Occupant.transform.position = new Vector3(worldPos.x, worldPos.y, slot.Occupant.transform.position.z);
                }
            }
        }

        /// <summary>
        /// 행(row)과 열(col)에 해당하는 슬롯을 가져옵니다.
        /// </summary>
        public FormationSlot GetSlot(int row, int col)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].RowIndex == row && _slots[i].ColumnIndex == col)
                {
                    return _slots[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 전역 인덱스(0~39)로 슬롯을 가져옵니다.
        /// </summary>
        public FormationSlot GetSlotByIndex(int index)
        {
            if (index >= 0 && index < _slots.Count)
            {
                return _slots[index];
            }
            return null;
        }

        /// <summary>
        /// 특정 행/열 슬롯의 현재 월드 좌표를 반환합니다.
        /// </summary>
        public Vector2 GetSlotWorldPosition(int row, int col)
        {
            FormationSlot slot = GetSlot(row, col);
            return slot != null ? slot.CurrentWorldPosition : _gridOrigin;
        }

        /// <summary>
        /// 해당 적 타입의 비어 있는 슬롯 중 첫 번째 슬롯에 적을 배정합니다.
        /// </summary>
        public FormationSlot AssignEnemyToNextAvailableSlot(EnemyType type, EnemyBase enemy)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                FormationSlot slot = _slots[i];
                if (slot.AssignedType == type && !slot.IsOccupied)
                {
                    slot.AssignOccupant(enemy);
                    if (enemy != null)
                    {
                        enemy.OnDestroyed += ReleaseEnemy;
                    }
                    return slot;
                }
            }
            return null;
        }

        /// <summary>
        /// 특정 행/열 슬롯에 적을 명시적으로 배정합니다.
        /// </summary>
        public FormationSlot AssignEnemyToSlot(int row, int col, EnemyBase enemy)
        {
            FormationSlot slot = GetSlot(row, col);
            if (slot != null)
            {
                slot.AssignOccupant(enemy);
                if (enemy != null)
                {
                    enemy.OnDestroyed += ReleaseEnemy;
                }
            }
            return slot;
        }

        /// <summary>
        /// 적 기체의 편대 슬롯 점유를 해제합니다.
        /// </summary>
        public void ReleaseEnemy(EnemyBase enemy)
        {
            if (enemy == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Occupant == enemy)
                {
                    _slots[i].ReleaseOccupant();
                    break;
                }
            }
        }
    }
}
