// using System.Collections.Generic;
// using UnityEngine;

// /// <summary>
// /// 스네이크 몸통의 물리적인 움직임 (경로 추적 및 세그먼트 위치/회전 업데이트)을 담당하는 클래스.
// /// </summary>
// public class SnakeMovement
// {
//     // --- References ---
//     private readonly SnakeBody _ownerBody;

//     // --- Path History Data ---
//     private readonly SnakeHistory _pathHistory;

//     // --- State ---
//     private bool _isInitialized = false;

//     // --- Constructor ---
//     public SnakeMovement(SnakeBody ownerBody)
//     {
//         _ownerBody = ownerBody;
//         // ownerBody에서 설정을 가져와 Path History 초기화
//         _pathHistory = new SnakeHistory(_ownerBody.InitialHistoryCapacity);
//         Initialize();
//     }

//     // --- Public Methods ---

//     /// <summary>
//     /// 움직임 시스템을 초기화합니다.
//     /// </summary>
//     public void Initialize()
//     {
//         _pathHistory.ClearAndInitialize(_ownerBody.HeadTransform);
//         _isInitialized = _ownerBody.HeadTransform != null && _pathHistory.Count > 0;
//         if (!_isInitialized) Debug.LogError("[SnakeMovement] Initialization failed!");
//     }

//     /// <summary>
//     /// 움직임 시스템을 정리합니다.
//     /// </summary>
//     public void Clear()
//     {
//         _pathHistory.ClearAndInitialize(null);
//         _isInitialized = false;
//     }

//     /// <summary>
//     /// 매 프레임(주로 FixedUpdate) 호출되어 세그먼트의 위치와 회전을 업데이트합니다.
//     /// </summary>
//     public void UpdatePositions()
//     {
//         if (!_isInitialized || _ownerBody.HeadTransform == null || _ownerBody.Segments.Count == 0 || _pathHistory.Count == 0) return;

//         float distanceMoved = Vector3.Distance(_ownerBody.HeadTransform.position, _pathHistory.LatestPathPosition);
//         if (distanceMoved >= _ownerBody.SegmentSpacing)
//         {
//             UpdateHistoryTrail(distanceMoved);
//         }
//         MoveSegmentsAlongHistory(distanceMoved);
//     }

//     /// <summary>
//     /// SnakeBody에서 세그먼트가 추가되었음을 알립니다 (히스토리 관리를 위해).
//     /// </summary>
//     public void NotifySegmentAdded()
//     {
//         if (!_isInitialized) return;
//         EnsureHistoryCapacity();
//     }

//     /// <summary>
//     /// SnakeBody에서 세그먼트가 제거되었음을 알립니다 (히스토리 관리를 위해).
//     /// </summary>
//     public void NotifySegmentRemoved()
//     {
//         if (!_isInitialized) return;
//         TrimHistory();
//     }


//     // --- Internal History & Movement Logic ---

//     // 머리 움직임에 따라 히스토리 트레일 업데이트
//     private void UpdateHistoryTrail(float totalDistanceMoved)
//     {
//         float remainingDistance = totalDistanceMoved;
//         Vector3 currentHeadPos = _ownerBody.HeadTransform.position;
//         Quaternion currentHeadRot = _ownerBody.HeadTransform.rotation;
//         Vector3 lastHistoryPos = _pathHistory.LatestPathPosition;
//         float spacing = _ownerBody.SegmentSpacing;

//         while (remainingDistance >= spacing)
//         {
//              Vector3 directionToNewPoint = (currentHeadPos - lastHistoryPos).normalized;
//              if (directionToNewPoint == Vector3.zero && _pathHistory.Count > 1) { var p1 = _pathHistory.GetPathPoint(0); var p2 = _pathHistory.GetPathPoint(1); directionToNewPoint = (p1.position - p2.position).normalized;}
//              else if (directionToNewPoint == Vector3.zero) directionToNewPoint = currentHeadRot * Vector3.forward;

//              Vector3 newPointPosition = lastHistoryPos + directionToNewPoint * spacing;
//              Quaternion newPointRotation = currentHeadRot;
//              _pathHistory.InsertAtFront(newPointPosition, newPointRotation);

//              lastHistoryPos = newPointPosition;
//              remainingDistance -= spacing;
//              TrimHistory();
//         }
//     }

//     // 히스토리를 따라 세그먼트 이동
//     private void MoveSegmentsAlongHistory(float headMoveDistance)
//     {
//         if (_pathHistory.Count < 2) return;
//         float spacing = _ownerBody.SegmentSpacing;
//         float followSpeed = _ownerBody.SegmentFollowSpeed;
//         float segmentLerpFactor = Mathf.Clamp01(headMoveDistance % spacing / spacing);
//         List<SnakeBodySegment> segments = _ownerBody.Segments;

//         for (int i = 0; i < segments.Count; i++)
//         {
//             SnakeBodySegment segment = segments[i];
//             if (segment == null) continue;

//             if (i + 1 < _pathHistory.Count)
//             {
//                 var (interpolatedPosition, interpolatedRotation) = _pathHistory.Lerp(i, segmentLerpFactor);
//                 segment.transform.position = Vector3.Lerp(segment.transform.position, interpolatedPosition, followSpeed * Time.fixedDeltaTime);
//                 segment.transform.rotation = Quaternion.Slerp(segment.transform.rotation, interpolatedRotation, followSpeed * Time.fixedDeltaTime);
//             }
//             else // Fallback
//             {
//                  if (_pathHistory.Count > 0)
//                  {
//                      var lastValidPoint = _pathHistory.GetPathPoint(_pathHistory.Count - 1);
//                      segment.transform.position = Vector3.Lerp(segment.transform.position, lastValidPoint.position, followSpeed * Time.fixedDeltaTime);
//                      segment.transform.rotation = Quaternion.Slerp(segment.transform.rotation, lastValidPoint.rotation, followSpeed * Time.fixedDeltaTime);
//                  }
//             }
//         }
//     }

//     // 히스토리 용량 확보
//     private void EnsureHistoryCapacity()
//     {
//         int segmentCount = _ownerBody.Segments.Count;
//         int historyBuffer = _ownerBody.HistoryBuffer;
//         int requiredCapacity = segmentCount + 1 + historyBuffer;
//         while (_pathHistory.Count < requiredCapacity)
//         {
//             if (_pathHistory.Count > 0) { var lp = _pathHistory.GetPathPoint(_pathHistory.Count - 1); _pathHistory.Add(lp.position, lp.rotation); }
//             else if (_ownerBody.HeadTransform != null) _pathHistory.Add(_ownerBody.HeadTransform.position, _ownerBody.HeadTransform.rotation);
//             else break;
//         }
//     }

//      // 히스토리 리스트 길이 관리
//     private void TrimHistory()
//     {
//         int segmentCount = _ownerBody.Segments.Count;
//         int historyBuffer = _ownerBody.HistoryBuffer;
//         int desiredCount = segmentCount + 1 + historyBuffer;
//         while (_pathHistory.Count > desiredCount && _pathHistory.Count > 1)
//         {
//             _pathHistory.RemoveLast();
//         }
//     }
// }
