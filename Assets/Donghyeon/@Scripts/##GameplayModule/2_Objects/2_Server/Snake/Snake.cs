using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Snake : MonoBehaviour
{
    [field: SerializeField] public SnakeHead Head { get; private set;}
    [field: SerializeField] public SnakeBody Body { get; private set; }
    
    // 2048 게임 기능 관련 변수 추가
    [SerializeField] private int _currentHeadValue = 0;
    [SerializeField] private float _minScale = 1f;
    [SerializeField] private float _maxScale = 2f;
    
    // 각 Body 세그먼트의 값을 저장하는 리스트
    private List<int> _segmentValues = new List<int>();
    
    // Head 값 설정 및 크기 조정
    public void UpdateHeadValue(int newValue)
    {
        _currentHeadValue = newValue;
        Head.UpdateValue(newValue);
        
        // 값에 따라 Head 크기 조정
        UpdateHeadSize();
    }
    
    // Head 크기 조정 (2의 제곱수가 커질수록 크기 증가)
    private void UpdateHeadSize()
    {
        // 값에 따른 스케일 계산 (로그 스케일 사용)
        float scale = CalculateScaleFromValue(_currentHeadValue);
        Head.transform.localScale = new Vector3(scale, scale, scale);
    }
    
    // 값에 따른 크기 계산 (로그 스케일)
    private float CalculateScaleFromValue(int value)
    {
        if (value <= 0) return _minScale;
        
        // 2의 거듭제곱 로그 계산 후 스케일 매핑
        float logScale = Mathf.Log(value, 2);
        return Mathf.Lerp(_minScale, _maxScale, logScale / 10f); // 10은 최대 크기에 도달하는 제곱수 (2^10 = 1024)
    }
    
    // Body 세그먼트 추가 시 값 저장
    public void AddDetailWithValue(GameObject detail, int value)
    {
        Body.AddDetail(detail);
        _segmentValues.Add(value);
        
        // 세그먼트의 크기 조정
        UpdateSegmentSize(detail, value);
    }
    
    // 세그먼트 크기 조정
    private void UpdateSegmentSize(GameObject segment, int value)
    {
        float scale = CalculateScaleFromValue(value);
        segment.transform.localScale = new Vector3(scale, scale, scale);
    }
    
    // 기존 메소드
    public void AddDetail(GameObject detail) => 
        Body.AddDetail(detail);

    public GameObject RemoveDetail() => 
        Body.RemoveLastDetail();

    public void ResetRotation() => 
        Head.ResetRotation();

    public void LookAt(Vector3 target) => 
        Head.LookAt(target);

    public IEnumerable<Vector3> GetBodyDetailPositions()
    {
        yield return Head.transform.position;
        foreach (var detailPosition in Body.GetBodyDetailPositions())
            yield return detailPosition;
    }
    
    // 현재 Head 값 반환
    public int GetHeadValue()
    {
        return _currentHeadValue;
    }
    
    // 세그먼트 값 가져오기 (인덱스 기반)
    public int GetSegmentValue(int index)
    {
        if (index >= 0 && index < _segmentValues.Count)
        {
            return _segmentValues[index];
        }
        return 0;
    }
}
