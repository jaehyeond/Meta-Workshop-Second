using UnityEngine;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;

public class SnakeBodyHandler : MonoBehaviour
{
    private Snake _snake;
    private List<BaseObject> _bodySegments = new List<BaseObject>();
    private float _segmentSpacing = 1.0f;
    private float _smoothSpeed = 10f;
    private float _turnSpeed = 5f;
    private Queue<Vector3> _positionHistory = new Queue<Vector3>();
    private float _updateInterval = 0.05f;
    private float _lastUpdateTime;

    public void Initialize(Snake snake)
    {
        _snake = snake;
        _bodySegments.Clear();
        _positionHistory.Clear();
        _lastUpdateTime = Time.time;
    }

    private void UpdatePositionHistory()
    {
        if (Time.time - _lastUpdateTime >= _updateInterval)
        {
            _positionHistory.Enqueue(_snake.Head.transform.position);
            _lastUpdateTime = Time.time;

            // 히스토리 크기 제한
            while (_positionHistory.Count > 100)
            {
                _positionHistory.Dequeue();
            }
        }
    }

    public void AddBodySegment(BaseObject segment, SnakeBodySegment segmentComponent)
    {
        if (segment != null)
        {
            _bodySegments.Add(segment);
            // 새 세그먼트의 초기 위치 설정
            if (_bodySegments.Count > 1)
            {
                var lastSegment = _bodySegments[_bodySegments.Count - 2];
                segment.transform.position = lastSegment.transform.position;
            }
            else
            {
                segment.transform.position = _snake.Head.transform.position - (_snake.Head.transform.forward * _segmentSpacing);
            }
        }
    }

    public void UpdateBodySegmentsPositions()
    {
        if (_bodySegments.Count == 0 || _snake == null || _snake.Head == null) return;

        UpdatePositionHistory();

        // 첫 번째 세그먼트는 헤드의 히스토리를 따라감
        if (_bodySegments.Count > 0)
        {
            int delay = 3; // 첫 번째 세그먼트의 지연
            Vector3 targetPos = GetHistoryPosition(delay);
            Vector3 direction = (targetPos - _bodySegments[0].transform.position).normalized;
            
            UpdateSegmentPosition(0, targetPos, direction);
        }

        // 나머지 세그먼트들은 앞 세그먼트를 따라감
        for (int i = 1; i < _bodySegments.Count; i++)
        {
            if (_bodySegments[i] == null || _bodySegments[i - 1] == null) continue;

            BaseObject currentSegment = _bodySegments[i];
            BaseObject leadingSegment = _bodySegments[i - 1];
            
            Vector3 direction = (leadingSegment.transform.position - currentSegment.transform.position).normalized;
            Vector3 targetPos = leadingSegment.transform.position - (direction * _segmentSpacing);
            
            UpdateSegmentPosition(i, targetPos, direction);
        }
    }

    private Vector3 GetHistoryPosition(int delay)
    {
        if (_positionHistory.Count <= delay)
        {
            return _snake.Head.transform.position - (_snake.Head.transform.forward * _segmentSpacing);
        }

        return _positionHistory.ToArray()[_positionHistory.Count - delay - 1];
    }

    private void UpdateSegmentPosition(int index, Vector3 targetPosition, Vector3 direction)
    {
        var segment = _bodySegments[index];
        if (segment == null) return;

        float speed = _snake.IsServer ? _smoothSpeed : _smoothSpeed * 0.8f;
        
        // 위치 업데이트
        segment.transform.position = Vector3.Lerp(
            segment.transform.position,
            targetPosition,
            Time.deltaTime * speed
        );

        // 회전 업데이트
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            segment.transform.rotation = Quaternion.Lerp(
                segment.transform.rotation,
                targetRotation,
                Time.deltaTime * _turnSpeed
            );
        }
    }

    public BaseObject GetBodySegment(int index)
    {
        return index >= 0 && index < _bodySegments.Count ? _bodySegments[index] : null;
    }

    public int GetBodySegmentCount()
    {
        return _bodySegments.Count;
    }

    public float GetSegmentSpacing()
    {
        return _segmentSpacing;
    }

    public BaseObject GetLastSegment()
    {
        return _bodySegments.Count > 0 ? _bodySegments[_bodySegments.Count - 1] : null;
    }

    public int CalculateSegmentValue()
    {
        int headValue = _snake._networkHeadValue.Value;
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2HeadValue);
        
        int segmentPower = headPower - _bodySegments.Count - 1;
        return segmentPower > 0 ? (int)Mathf.Pow(2, segmentPower) : 2;
    }
} 
