// using System.Collections.Generic;
// using UnityEngine;

// public class SnakeHistory
// {
//     // 내부 경로 지점 구조체
//     private struct PathPoint
//     {
//         public Vector3 Position;
//         public Quaternion Rotation;
//     }

//     // 경로 지점 리스트
//     private readonly List<PathPoint> _pathPoints;
//     private readonly int _initialCapacity;

//     // --- Public Properties ---
//     /// <summary>
//     /// 가장 최근에 기록된 경로 지점의 위치 (머리에 가장 가까움)
//     /// </summary>
//     public Vector3 LatestPathPosition => _pathPoints.Count > 0 ? _pathPoints[0].Position : Vector3.zero;

//     /// <summary>
//     /// 현재 기록된 경로 지점의 개수
//     /// </summary>
//     public int Count => _pathPoints.Count;

//     // --- Constructor ---
//     public SnakeHistory(int initialCapacity = 32)
//     {
//         _initialCapacity = initialCapacity;
//         _pathPoints = new List<PathPoint>(_initialCapacity);
//     }

//     // --- Public Methods ---

//     /// <summary>
//     /// 히스토리를 초기화하고 시작 지점을 추가합니다.
//     /// </summary>
//     public void ClearAndInitialize(Transform startTransform)
//     {
//         _pathPoints.Clear();
//         if (startTransform != null)
//         {
//             Add(startTransform.position, startTransform.rotation); // 시작점 추가
//         }
//     }

//     /// <summary>
//     /// 히스토리 리스트 끝에 포인트를 추가합니다 (주로 초기화 시 사용).
//     /// </summary>
//     public void Add(Vector3 position, Quaternion rotation)
//     {
//         _pathPoints.Add(new PathPoint { Position = position, Rotation = rotation });
//     }

//     /// <summary>
//     /// 히스토리 리스트 맨 앞에 포인트를 삽입합니다 (머리 움직임 기록).
//     /// </summary>
//     public void InsertAtFront(Vector3 position, Quaternion rotation)
//     {
//         _pathPoints.Insert(0, new PathPoint { Position = position, Rotation = rotation });
//     }

//     /// <summary>
//     /// 히스토리 리스트의 마지막 포인트(가장 오래된)를 제거합니다.
//     /// </summary>
//     public void RemoveLast()
//     {
//         if (_pathPoints.Count > 0)
//         {
//             _pathPoints.RemoveAt(_pathPoints.Count - 1);
//         }
//     }

//     /// <summary>
//     /// 지정된 두 경로 지점 사이를 보간합니다.
//     /// </summary>
//     /// <param name="newerPointIndex">보간 목표 지점 인덱스 (최신 쪽, 0부터 시작)</param>
//     /// <param name="interpolationFactor">보간 계수 (0~1)</param>
//     /// <returns>보간된 위치와 회전</returns>
//     public (Vector3 position, Quaternion rotation) Lerp(int newerPointIndex, float interpolationFactor)
//     {
//         int olderPointIndex = newerPointIndex + 1; // 내부 변수 이름 변경

//         // 유효하지 않은 인덱스 처리
//         if (newerPointIndex < 0 || olderPointIndex >= _pathPoints.Count)
//         {
//             int validIndex = Mathf.Clamp(_pathPoints.Count - 1, 0, _pathPoints.Count - 1);
//             if (_pathPoints.Count > 0)
//                 return (_pathPoints[validIndex].Position, _pathPoints[validIndex].Rotation);
//             else
//                 return (Vector3.zero, Quaternion.identity); // 히스토리가 비어있는 예외 상황
//         }

//         PathPoint targetPoint = _pathPoints[newerPointIndex]; // 보간 목표 포인트
//         PathPoint prevPoint = _pathPoints[olderPointIndex];   // 보간 시작 포인트

//         // 위치와 회선 선형 보간
//         Vector3 lerpedPosition = Vector3.Lerp(prevPoint.Position, targetPoint.Position, interpolationFactor);
//         Quaternion lerpedRotation = Quaternion.Lerp(prevPoint.Rotation, targetPoint.Rotation, interpolationFactor);

//         return (lerpedPosition, lerpedRotation);
//     }

//     /// <summary>
//     /// 특정 인덱스의 경로 지점 정보를 가져옵니다.
//     /// </summary>
//     public (Vector3 position, Quaternion rotation) GetPathPoint(int index)
//     {
//         if (index >= 0 && index < _pathPoints.Count)
//         {
//             return (_pathPoints[index].Position, _pathPoints[index].Rotation);
//         }
//         // 인덱스 벗어날 경우 처리 (예: 첫번째 또는 마지막 포인트 반환, 오류 발생 등)
//         Debug.LogError($"SnakeHistory.GetPathPoint: Index {index} out of range (Count: {_pathPoints.Count})");
//         if (_pathPoints.Count > 0) return (_pathPoints[0].Position, _pathPoints[0].Rotation);
//         return (Vector3.zero, Quaternion.identity);
//     }
// }
