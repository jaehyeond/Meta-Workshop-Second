using UnityEngine;
using System.Collections.Generic;

public class SnakeBodyHandler : MonoBehaviour
{
    [Header("Body Settings")]
    [SerializeField] private float _segmentSpacing = 0.17f;
    [SerializeField] private float _segmentFollowSpeed = 8f;

    private List<GameObject> _bodySegments = new List<GameObject>();
    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    private List<Vector3> _segmentVelocities = new List<Vector3>();
    private Vector3 _firstSegmentVelocity = Vector3.zero;

    private Snake _snake;

    public float GetSegmentSpacing() => _segmentSpacing;

    public GameObject GetLastSegment()
    {
        if (_bodySegments.Count > 0)
        {
            return _bodySegments[_bodySegments.Count - 1];
        }
        return null;
    }

    public void Initialize(Snake snake)
    {
        _snake = snake;
        _segmentVelocities = new List<Vector3>();
        _firstSegmentVelocity = Vector3.zero;
    }

    public void UpdateBodySegmentsPositions()
    {
        if (_bodySegments.Count == 0 || _snake == null || _snake.Head == null) return;

        try
        {
            while (_segmentVelocities.Count < _bodySegments.Count)
            {
                _segmentVelocities.Add(Vector3.zero);
            }
            
            float followRatio = _segmentFollowSpeed * Time.smoothDeltaTime;
            
            Vector3 headPosition = _snake.Head.transform.position;
            Quaternion headRotation = _snake.Head.transform.rotation;
            Vector3 headForward = _snake.Head.transform.forward;
            
            if (_bodySegments.Count > 0)
            {
                GameObject firstSegment = _bodySegments[0];
                if (firstSegment == null) return;
                
                Vector3 targetPosition = headPosition - headForward * _segmentSpacing;
                Vector3 currentVelocity = _firstSegmentVelocity;
                
                firstSegment.transform.position = Vector3.Lerp(
                    firstSegment.transform.position,
                    targetPosition,
                    followRatio * 1.5f
                );
                
                _firstSegmentVelocity = currentVelocity;
                
                firstSegment.transform.rotation = Quaternion.Slerp(
                    firstSegment.transform.rotation,
                    headRotation,
                    followRatio * 1.5f
                );
            }

            for (int i = 1; i < _bodySegments.Count; i++)
            {
                GameObject segment = _bodySegments[i];
                GameObject prevSegment = _bodySegments[i - 1];
                
                if (segment == null || prevSegment == null) continue;
                
                Vector3 prevPosition = prevSegment.transform.position;
                Quaternion prevRotation = prevSegment.transform.rotation;
                Vector3 prevForward = prevSegment.transform.forward;
                
                Vector3 targetPosition = prevPosition - prevForward * _segmentSpacing;
                
                if (i < _segmentVelocities.Count)
                {
                    Vector3 currentVelocity = _segmentVelocities[i];
                    
                    segment.transform.position = Vector3.Lerp(
                        segment.transform.position,
                        targetPosition,
                        followRatio * 1.2f
                    );
                    
                    _segmentVelocities[i] = currentVelocity;
                }
                else
                {
                    segment.transform.position = Vector3.Lerp(
                        segment.transform.position,
                        targetPosition,
                        followRatio
                    );
                }
                
                segment.transform.rotation = Quaternion.Slerp(
                    segment.transform.rotation,
                    prevRotation,
                    followRatio * 1.2f
                );
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] 세그먼트 위치 업데이트 오류: {ex.Message}");
        }
    }

    public void AddBodySegment(GameObject segment, SnakeBodySegment segmentComponent)
    {
        if (segment == null) return;

        _bodySegments.Add(segment);
        
        if (segmentComponent != null)
        {
            _bodySegmentComponents.Add(segmentComponent);
            _segmentVelocities.Add(Vector3.zero);
        }
    }

    public void CleanupBodySegments()
    {
        foreach (var segment in _bodySegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
        
        _bodySegments.Clear();
        _bodySegmentComponents.Clear();
        _segmentVelocities.Clear();
    }

    public int GetBodySegmentCount()
    {
        return _bodySegments.Count;
    }
} 