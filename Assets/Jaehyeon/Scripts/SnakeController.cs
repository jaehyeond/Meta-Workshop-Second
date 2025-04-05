// Assets/Jaehyeon/Scripts/SnakeController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define; // 상단에 이 줄이 없다면 추가

public class SnakeController : BaseObject
{
    [SerializeField] private TextMesh _numberText;
    [SerializeField] private MeshRenderer _renderer;
    
    private int _snakeDataId = 1; // 기본 Snake 데이터 ID
    private Data.SnakeData _data;
    private float _moveSpeed;
    private float _rotationSpeed;
    private int _currentValue;
    
    private List<SnakeBody> _bodies = new List<SnakeBody>();
    private Vector3 _direction = Vector3.forward;
    
    public override bool Init()
    {
        ObjectType = EObjectType.Snake;
        return true;
    }
    
    public void SetInfo(int dataId)
    {
        _snakeDataId = dataId;
        _data = Managers.Data.SnakeDic[dataId];
        _moveSpeed = _data.MoveSpeed;
        _rotationSpeed = _data.RotationSpeed;
        _currentValue = _data.InitialValue;
        
        UpdateVisual();
    }
    
    private void Start()
    {
        // 데이터 초기화 확인
        if (_data == null)
        {
            Debug.LogError("Snake 데이터가 없습니다. SetInfo 메서드가 호출되었는지 확인하세요.");
            _moveSpeed = 5.0f; // 기본값 설정
            _rotationSpeed = 180.0f; // 기본값 설정
            _currentValue = 2; // 기본값 설정
        }
        
        Debug.Log($"Snake 초기화: 속도={_moveSpeed}, 회전속도={_rotationSpeed}");
    }
    
    private void Update()
    {
        // 디버그 로그 추가
        //Debug.Log("SnakeController Update 실행 중");
        
        // 키보드 입력 처리 - GetAxis 대신 직접 키 확인 (더 확실한 방법)
        float horizontal = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal = -1;
            //Debug.Log("왼쪽 이동");
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal = 1;
            //Debug.Log("오른쪽 이동");
        }
        
        // 회전
        transform.Rotate(Vector3.up, horizontal * _rotationSpeed * Time.deltaTime);
        
        // 앞으로 이동 (항상 실행)
        _direction = transform.forward;
        transform.position += _direction * _moveSpeed * Time.deltaTime;
        
        // 몸통 위치 업데이트 (몸통이 있을 경우만)
        if (_bodies != null && _bodies.Count > 0)
            UpdateBodyPositions();
    }
    
    private void UpdateVisual()
    {
        // 숫자 텍스트 표시
        if (_numberText != null)
        {
            _numberText.text = _currentValue.ToString();
        }
        
        // 크기 조정 (숫자에 따라 점점 커지게)
        float sizeMultiplier = 1.0f + (Mathf.Log(_currentValue, 2) * 0.1f);
        transform.localScale = Vector3.one * sizeMultiplier;
        
        // 색상 변경 (간단한 방식으로)
        if (_renderer != null)
        {
            // 숫자 값에 따른 색상 변경 (2: 빨강, 4: 주황, 8: 노랑, 16: 초록, 32: 파랑 등)
            float hue = (Mathf.Log(_currentValue, 2) * 0.1f) % 1.0f;
            _renderer.material.color = Color.HSVToRGB(hue, 0.7f, 1.0f);
        }
    }
    
    /*private Data.SnakeColorData GetColorDataForValue(int value)
    {
        Data.SnakeColorData bestMatch = null;
        
        foreach (var colorData in _data.ColorSettings)
        {
            if (colorData.Number <= value && (bestMatch == null || colorData.Number > bestMatch.Number))
                bestMatch = colorData;
        }
        
        return bestMatch;
    }*/
    
    private void OnTriggerEnter(Collider other)
    {
        // 음식과 충돌 처리
        Food food = other.GetComponent<Food>();
        if (food != null)
        {
            // 값 증가
            AddValue(food.Value);
            
            // 음식 제거
            Managers.Object.Despawn(food);
        }
    }
    
    public void AddValue(int value)
    {
        _currentValue += value;
        UpdateVisual();
        
        // 2의 거듭제곱 값일 때 몸통 생성
        if (IsPowerOfTwo(_currentValue) && _currentValue > 2)
        {
            CreateBodySegment();
        }
    }
    
    private bool IsPowerOfTwo(int value)
    {
        return (value != 0) && ((value & (value - 1)) == 0);
    }
    
    private void CreateBodySegment()
    {
        // 현재 헤드의 위치와 방향 저장
        Vector3 spawnPosition;
        Quaternion spawnRotation = transform.rotation;
        
        // 첫 번째 바디 생성 시 헤드 바로 뒤에 위치
        if (_bodies.Count == 0)
        {
            spawnPosition = transform.position - (transform.forward * _data.BodyDistance);
        }
        // 이미 바디가 있으면 마지막 바디 뒤에 위치
        else
        {
            SnakeBody lastBody = _bodies[_bodies.Count - 1];
            spawnPosition = lastBody.transform.position - (lastBody.transform.forward * _data.BodyDistance);
        }
        
        // 새 바디 스폰 (오브젝트 매니저 사용)
        SnakeBody newBody = Managers.Object.Spawn<SnakeBody>(spawnPosition, _snakeDataId);
        
        // 바디 세팅: 헤드 값의 절반
        newBody.SetValue(_currentValue / 2);
        newBody.transform.rotation = spawnRotation;
        
        // 바디 리스트에 추가
        _bodies.Add(newBody);
        
        Debug.Log($"새 바디 생성: 값={_currentValue/2}, 위치={spawnPosition}");
    }
    
    private void UpdateBodyPositions()
    {
        if (_bodies.Count == 0)
            return;
        
        Vector3 targetPosition = transform.position - (transform.forward * _data.BodyDistance);
        Quaternion targetRotation = transform.rotation;
        
        for (int i = 0; i < _bodies.Count; i++)
        {
            // 현재 바디의 위치와 회전 저장
            Vector3 currentPos = _bodies[i].transform.position;
            Quaternion currentRot = _bodies[i].transform.rotation;
            
            // 스무드한 이동과 회전
            _bodies[i].transform.position = Vector3.Lerp(currentPos, targetPosition, Time.deltaTime * _moveSpeed * 1.5f);
            _bodies[i].transform.rotation = Quaternion.Slerp(currentRot, targetRotation, Time.deltaTime * _rotationSpeed * 1.5f);
            
            // 다음 바디의 목표는 현재 바디
            targetPosition = currentPos - (_bodies[i].transform.forward * _data.BodyDistance);
            targetRotation = currentRot;
        }
    }
}