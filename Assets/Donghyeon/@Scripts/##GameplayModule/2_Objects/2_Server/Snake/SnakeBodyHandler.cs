using UnityEngine;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;

public class SnakeBodyHandler : MonoBehaviour
{
    [Header("Body Settings")]
    [SerializeField] private float _segmentSpacing = 0.17f;
    [SerializeField] private float _segmentFollowSpeed = 8f;

    public List<BaseObject> _bodySegments = new List<BaseObject>();
    private List<SnakeBodySegment> _bodySegmentComponents = new List<SnakeBodySegment>();
    private List<Vector3> _segmentVelocities = new List<Vector3>();
    private Vector3 _firstSegmentVelocity = Vector3.zero;

    private Snake _snake;

    public float GetSegmentSpacing() => _segmentSpacing;

    public BaseObject GetLastSegment()
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
//현재는 단순히 snakeBodyHandler.UpdateBodySegmentsPositions()를 호출하는 방식이지만, 실제 구현에서는 위치, 회전 등의 정보를 직렬화하여, 서버에서 클라이언트에 전송해야 합니다. 이 부분은 필요에 따라 추가 개발이 필요합니다
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
                BaseObject firstSegment = _bodySegments[0];
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
                BaseObject segment = _bodySegments[i];
                BaseObject prevSegment = _bodySegments[i - 1];
                
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
    /// <summary>
    /// 세그먼트 값 계산
    /// 헤드 값과 세그먼트 위치에 따라 2의 제곱수 패턴으로 계산
    /// </summary>
    public int CalculateSegmentValue()
    {
        // 이제 Snake의 _networkHeadValue 사용
        int headValue = _snake._networkHeadValue.Value;
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = (int)Mathf.Round(log2HeadValue);
        
        int segmentPower = headPower - _bodySegments.Count - 1;
        return segmentPower > 0 ? (int)Mathf.Pow(2, segmentPower) : 2;
    }


    public void SetSegmentValue(int segmentIndex, int segmentValue)
    {


        if (_bodySegmentComponents.Count > segmentIndex && segmentIndex >= 0) // 인덱스 유효성 검사 추가
        {
            var segmentComponent = _bodySegmentComponents[segmentIndex];
            if (segmentComponent != null)
            {
                segmentComponent.SetValue(segmentValue);
            }
      
        }

    }

    public void AddBodySegment(BaseObject segment, SnakeBodySegment segmentComponent)
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

    /// <summary>
    /// 마지막 바디 세그먼트를 제거합니다.
    /// </summary>
    public void RemoveLastSegment()
    {
        if (_bodySegments.Count <= 0) return;
        
        int lastIndex = _bodySegments.Count - 1;
        
        // 리스트에서 제거
        _bodySegments.RemoveAt(lastIndex);
        
        // 컴포넌트 및 속도 목록 동기화
        if (_bodySegmentComponents.Count > lastIndex)
        {
            _bodySegmentComponents.RemoveAt(lastIndex);
        }
        
        if (_segmentVelocities.Count > lastIndex)
        {
            _segmentVelocities.RemoveAt(lastIndex);
        }
        
        Debug.Log($"[{GetType().Name}] 마지막 세그먼트 제거됨. 남은 세그먼트 수: {_bodySegments.Count}");
    }
} 