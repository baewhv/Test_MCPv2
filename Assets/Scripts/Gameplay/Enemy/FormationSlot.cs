using System;
using UnityEngine;

namespace Galaga.Gameplay.Enemy
{
    /// <summary>
    /// 편대 그리드 내 단일 슬롯의 행/열 좌표, 목표 적 기체 타입, 현재 안착된 적을 관리하는 직렬화 가능 클래스입니다.
    /// </summary>
    [Serializable]
    public class FormationSlot
    {
        [SerializeField] private int _rowIndex;
        [SerializeField] private int _columnIndex;
        [SerializeField] private int _globalIndex;
        [SerializeField] private EnemyType _assignedType;
        [SerializeField] private Vector2 _baseLocalPosition;
        [SerializeField] private Vector2 _currentWorldPosition;
        [SerializeField] private EnemyBase _occupant;

        public int RowIndex => _rowIndex;
        public int ColumnIndex => _columnIndex;
        public int GlobalIndex => _globalIndex;
        public EnemyType AssignedType => _assignedType;
        public Vector2 BaseLocalPosition
        {
            get => _baseLocalPosition;
            set => _baseLocalPosition = value;
        }
        public Vector2 CurrentWorldPosition
        {
            get => _currentWorldPosition;
            set => _currentWorldPosition = value;
        }
        public EnemyBase Occupant => _occupant;
        public bool IsOccupied => _occupant != null && !_occupant.IsDead;

        public FormationSlot(int rowIndex, int columnIndex, int globalIndex, EnemyType assignedType, Vector2 baseLocalPosition)
        {
            _rowIndex = rowIndex;
            _columnIndex = columnIndex;
            _globalIndex = globalIndex;
            _assignedType = assignedType;
            _baseLocalPosition = baseLocalPosition;
            _currentWorldPosition = baseLocalPosition;
            _occupant = null;
        }

        public void AssignOccupant(EnemyBase enemy)
        {
            _occupant = enemy;
        }

        public void ReleaseOccupant()
        {
            _occupant = null;
        }
    }
}
