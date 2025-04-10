using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode; // NetworkManager 참조 위해 유지


public static class SnakeValueCalculator
{
    /// <summary>
    /// 지정된 인덱스의 스네이크 세그먼트 값을 계산합니다. (리팩토링됨)
    /// </summary>
    public static int CalculateSegmentValue(int headValue, int segmentIndex, int totalSegments)
    {
        // 머리 값의 2의 거듭제곱 지수 계산
        float log2HeadValue = Mathf.Log(headValue, 2);
        int headPower = Mathf.RoundToInt(log2HeadValue);
        if (headPower < 1) headPower = 1; // 최소값 보정 (2^1 = 2)

        int segmentValue;
        // 이 세그먼트에 해당하는 지수 계산 (머리 지수 - (인덱스 + 1))
        int powerIndex = headPower - (segmentIndex + 1);

        // 마지막 세그먼트인지 확인 (segmentIndex가 마지막 인덱스와 같은지)
        bool isLastSegment = (segmentIndex == totalSegments - 1);

        // 마지막 세그먼트거나 계산된 지수가 1 미만이면 최소값 2로 설정
        if (isLastSegment || powerIndex < 1)
        {
            segmentValue = 2;
        }
        else
        {
            int segmentPower = powerIndex;
            segmentValue = (int)Mathf.Pow(2, segmentPower);
        }

        return segmentValue;
    }


}

public class SnakeBody : MonoBehaviour
{
    // --- Settings ---
    [Header("Body Settings")]
    [SerializeField] private float _segmentSpacing = 0.17f; // PlayerSnakeController에서 가져옴
    [SerializeField] private float _segmentFollowSpeed = 8f; // PlayerSnakeController에서 가져옴
    [SerializeField] private int _maxHistorySize = 100; // PlayerSnakeController FixedUpdate 로직에서 가져옴

    // --- References (Set externally or via GetComponent) ---
    private SnakeHead _head; // 머리 참조
    private List<GameObject> _segmentsGO = new List<GameObject>(); // 게임 오브젝트 리스트
    private List<SnakeBodySegment> _segmentsComp = new List<SnakeBodySegment>(); // 컴포넌트 리스트

    // --- Runtime Variables (Moved from PlayerSnakeController) ---
    private Queue<Vector3> _moveHistory = new Queue<Vector3>(); // 이동 기록
    private Vector3 _firstSegmentVelocity = Vector3.zero; // 첫 번째 세그먼트 SmoothDamp용
    private List<Vector3> _segmentVelocities = new List<Vector3>(); // 각 세그먼트 SmoothDamp용

    // --- Public Properties ---
    public int SegmentCount => _segmentsGO.Count;
    public List<GameObject> Segments => _segmentsGO; // PlayerSnakeController에서 사용하기 위함

    // --- Initialization ---
    public void Initialize(SnakeHead head)
    {
        _head = head;
        _segmentsGO.Clear();
        _segmentsComp.Clear();
        _moveHistory.Clear();
        _segmentVelocities.Clear();
        _firstSegmentVelocity = Vector3.zero;
        
        // 자식으로 존재하는 초기 세그먼트들을 등록 (필요한 경우)
        RefreshLocalSegmentList(); 
        
        Debug.Log($"[{GetType().Name}] Initialized with Head: {head?.name}");
    }

    // --- Unity Lifecycle ---
    private void FixedUpdate()
    {
        // 서버에서만 위치 기록 및 세그먼트 위치 업데이트
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            UpdateHistoryAndSegmentPositions();
        }
    }

    // --- Core Logic ---
    
    /// <summary>
    /// 이동 기록을 업데이트하고 모든 바디 세그먼트의 위치를 갱신합니다. (FixedUpdate에서 호출)
    /// </summary>
    private void UpdateHistoryAndSegmentPositions()
    {
        if (_head == null) return;

        // 1. 머리 위치 기록 업데이트
        _moveHistory.Enqueue(_head.transform.position);
        while (_moveHistory.Count > _maxHistorySize)
        {
            _moveHistory.Dequeue();
        }

        // 2. 바디 세그먼트 위치 업데이트 (기존 PlayerSnakeController 로직 기반)
        UpdateBodySegmentsPositionsInternal();
    }

    /// <summary>
    /// 바디 세그먼트들의 위치를 부드럽게 갱신합니다. (내부 사용)
    /// </summary>
    private void UpdateBodySegmentsPositionsInternal()
    {
        if (_segmentsGO.Count == 0 || _head == null) return;

        try
        {
            // 세그먼트 개수만큼 속도 벡터 목록 크기 확인 및 초기화
            while (_segmentVelocities.Count < _segmentsGO.Count)
            {
                _segmentVelocities.Add(Vector3.zero);
            }
            
            float followRatio = _segmentFollowSpeed * Time.fixedDeltaTime; // FixedUpdate 사용
            
            Vector3 headPosition = _head.transform.position;
            Quaternion headRotation = _head.transform.rotation;
            Vector3 headForward = _head.transform.forward;
            
            // 첫 번째 세그먼트 업데이트
            if (_segmentsGO.Count > 0)
            {
                GameObject firstSegmentGO = _segmentsGO[0];
                if (firstSegmentGO == null) return;
                
                Vector3 targetPosition = headPosition - headForward * _segmentSpacing;
                
                // Vector3.Lerp 사용 (SmoothDamp 대체)
                firstSegmentGO.transform.position = Vector3.Lerp(
                    firstSegmentGO.transform.position,
                    targetPosition,
                    followRatio // Lerp 비율 직접 사용
                );
                
                // 부드러운 회전 보간 (Quaternion.Slerp)
                firstSegmentGO.transform.rotation = Quaternion.Slerp(
                    firstSegmentGO.transform.rotation,
                    headRotation,
                    followRatio * 1.2f // 회전은 약간 더 빠르게 반응하도록 조정 가능
                );
            }

            // 나머지 세그먼트 업데이트
            for (int i = 1; i < _segmentsGO.Count; i++)
            {
                GameObject currentSegmentGO = _segmentsGO[i];
                GameObject prevSegmentGO = _segmentsGO[i - 1];
                
                if (currentSegmentGO == null || prevSegmentGO == null) continue;
                
                Vector3 prevPosition = prevSegmentGO.transform.position;
                Quaternion prevRotation = prevSegmentGO.transform.rotation;
                Vector3 prevForward = prevSegmentGO.transform.forward;
                
                Vector3 targetPosition = prevPosition - prevForward * _segmentSpacing;
                
                // Vector3.Lerp 사용
                currentSegmentGO.transform.position = Vector3.Lerp(
                    currentSegmentGO.transform.position,
                    targetPosition,
                    followRatio
                );
                
                // 부드러운 회전 보간 (Quaternion.Slerp)
                currentSegmentGO.transform.rotation = Quaternion.Slerp(
                    currentSegmentGO.transform.rotation,
                    prevRotation,
                    followRatio * 1.1f // 뒤따르는 세그먼트는 조금 더 부드럽게 회전
                );
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetType().Name}] Segment position update error: {ex.Message}\n{ex.StackTrace}");
        }
    }


    #region Segment Management

    /// <summary>
    /// 서버에서 새 세그먼트를 추가하고 초기 값을 설정합니다.
    /// PlayerSnakeController의 AddBodySegmentServerRpc에서 호출될 수 있습니다.
    /// </summary>
    public void AddSegment(GameObject segmentGO, int segmentValue)
    {
         if (segmentGO == null) return;

        SnakeBodySegment segmentComponent = segmentGO.GetComponent<SnakeBodySegment>();
        if (segmentComponent == null)
        {
             Debug.LogError($"[{GetType().Name}] Added segment is missing SnakeBodySegment component!");
             // 필요 시 기본 컴포넌트 추가 로직
             // segmentComponent = segmentGO.AddComponent<SnakeBodySegment>();
             return; // 또는 예외 처리
        }
        
        segmentComponent.SetValue(segmentValue);

        // 리스트에 추가
        _segmentsGO.Add(segmentGO);
        _segmentsComp.Add(segmentComponent);
        _segmentVelocities.Add(Vector3.zero); // 새 세그먼트에 대한 속도 벡터 초기화

        Debug.Log($"[{GetType().Name} - Server] Segment registered. Value: {segmentValue}, Total Count: {_segmentsGO.Count}");
        
        // 필요 시 SnakeMovement 핸들러에 알림 (만약 별도 Movement 클래스 사용 시)
        // _movementHandler?.NotifySegmentAdded(); 
    }

    /// <summary>
    /// 마지막 세그먼트를 제거하고 해당 게임 오브젝트를 반환합니다.
    /// </summary>
    public GameObject RemoveLastDetail()
    {
        if (SegmentCount == 0) return null;
        
        int lastIndex = SegmentCount - 1;
        GameObject removedSegmentGO = _segmentsGO[lastIndex];
        
        _segmentsGO.RemoveAt(lastIndex);
        _segmentsComp.RemoveAt(lastIndex);
        if (_segmentVelocities.Count > lastIndex)
        {
            _segmentVelocities.RemoveAt(lastIndex);
        }
        if (lastIndex == 0) // 첫번째 세그먼트가 제거된 경우
        {
             _firstSegmentVelocity = Vector3.zero;
        }

        Debug.Log($"[{GetType().Name}] Last segment removed. Remaining Count: {_segmentsGO.Count}");
        
        // 필요 시 SnakeMovement 핸들러에 알림
        // _movementHandler?.NotifySegmentRemoved();
        
        return removedSegmentGO; // 제거된 게임 오브젝트 반환 (예: 파괴 처리 위함)
    }
    
    /// <summary>
    /// 로컬 자식 오브젝트들을 기준으로 세그먼트 리스트를 갱신합니다.
    /// </summary>
    private void RefreshLocalSegmentList()
    {
        _segmentsGO.Clear();
        _segmentsComp.Clear();
        
        // 자식 Transform들을 순회하며 SnakeBodySegment 컴포넌트 찾기
        foreach (Transform child in transform) 
        {
            if (child.TryGetComponent<SnakeBodySegment>(out var segmentComp))
            {
                 _segmentsGO.Add(child.gameObject);
                 _segmentsComp.Add(segmentComp);
            }
        }
        
        // 순서 정렬 (Sibling Index 기준 - 옵션)
        // _segmentsGO = _segmentsGO.OrderBy(go => go.transform.GetSiblingIndex()).ToList();
        // _segmentsComp = _segmentsGO.Select(go => go.GetComponent<SnakeBodySegment>()).ToList();

        // 속도 벡터 리스트 크기 맞추기
        while (_segmentVelocities.Count < _segmentsGO.Count)
        {
            _segmentVelocities.Add(Vector3.zero);
        }
        while (_segmentVelocities.Count > _segmentsGO.Count)
        {
             _segmentVelocities.RemoveAt(_segmentVelocities.Count - 1);
        }

        Debug.Log($"[{GetType().Name}] Refreshed local segment list. Found {_segmentsGO.Count} segments.");
    }


    #endregion

    #region Value Update

    /// <summary>
    /// 모든 세그먼트의 값을 주어진 머리 값을 기준으로 다시 계산하고 업데이트합니다. (서버 전용)
    /// PlayerSnakeController의 OnHeadValueChanged 등에서 호출됩니다.
    /// </summary>
    public void UpdateSegmentValues(int headValue)
    {
        if (_segmentsComp.Count == 0) return;

        try
        {
            Debug.Log($"[{GetType().Name}] Updating segment values based on Head Value: {headValue}. Segment Count: {_segmentsComp.Count}");
            for (int i = 0; i < _segmentsComp.Count; i++)
            {
                SnakeBodySegment segment = _segmentsComp[i];
                if (segment == null) 
                {
                    Debug.LogWarning($"[{GetType().Name}] Segment component at index {i} is null during value update.");
                    continue;
                }
                
                // SnakeValueCalculator를 사용하여 값 계산
                int segmentValue = SnakeValueCalculator.CalculateSegmentValue(headValue, i, _segmentsComp.Count);
                segment.SetValue(segmentValue); // 각 세그먼트 값 업데이트
                
                // Debug.Log($"[{GetType().Name}] Segment[{i}] value set to: {segmentValue}");
            }
            
            // 클라이언트에 동기화 (필요한 경우 별도 RPC 또는 NetworkVariable 사용)
            // SyncBodyValuesClientRpc(); // 이 로직은 PlayerController에서 처리할 수도 있음
        }
        catch (System.Exception ex) 
        {
             Debug.LogError($"[{GetType().Name}] Error updating segment values: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // 클라이언트에 Body 값 동기화를 위한 RPC (PlayerSnakeController로 이동 또는 수정될 수 있음)
    // [ClientRpc]
    // private void SyncBodyValuesClientRpc() { ... }

    #endregion

    // --- Public Query Methods ---
    public IEnumerable<Vector3> GetBodyDetailPositions()
    {
        // GameObject 리스트에서 위치 정보 반환
        return _segmentsGO.Select(segmentGO => segmentGO.transform.position);
    }


}
    
