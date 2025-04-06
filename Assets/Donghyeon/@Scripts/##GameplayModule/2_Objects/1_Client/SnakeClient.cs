using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Donghyeon.GameplayModule.Input;

namespace Unity.Assets.Scripts.Objects
{
    /// <summary>
    /// 클라이언트 측 Snake 제어를 담당하는 클래스
    /// </summary>
    public class SnakeClient : NetworkBehaviour
    {
        [Header("설정")]
        [SerializeField] private float _inputSensitivity = 1.0f;
        [SerializeField] private float _cameraFollowSpeed = 5.0f;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 10, -5);
        
        [Header("컴포넌트 참조")]
        [SerializeField] private Transform _snakeHead;
        
        // 입력 관련
        private ISnakeInputProvider _inputProvider;
        private Vector2 _currentDirection = Vector2.zero;
        private bool _inputInitialized = false;
        
        // 카메라 관련
        private Camera _mainCamera;
        private Transform _cameraTransform;
        
        // 상태 관련
        private bool _isActive = false;
        
        #region 네트워크 초기화
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[SnakeClient] OnNetworkSpawn - IsOwner: {IsOwner}, IsClient: {IsClient}");
            
            if (IsOwner)
            {
                // 오너 클라이언트 초기화
                StartCoroutine(InitializeOwnerClient());
            }
            else
            {
                // 원격 클라이언트 초기화
                InitializeRemoteClient();
            }
        }
        
        private IEnumerator InitializeOwnerClient()
        {
            Debug.Log("[SnakeClient] 오너 클라이언트 초기화 시작");
            
            // 카메라 설정
            yield return StartCoroutine(SetupCamera());
            
            // 입력 설정
            yield return new WaitForSeconds(1.0f);
            SetupInput();
            
            // 활성화
            _isActive = true;
            
            Debug.Log("[SnakeClient] 오너 클라이언트 초기화 완료");
        }
        
        private void InitializeRemoteClient()
        {
            Debug.Log("[SnakeClient] 원격 클라이언트 초기화");
            // 원격 클라이언트는 제한된 기능만 사용
            _isActive = false;
        }
        #endregion
        
        #region 초기화 메서드
        private IEnumerator SetupCamera()
        {
            Debug.Log("[SnakeClient] 카메라 설정 시작");
            
            // 메인 카메라 찾기
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[SnakeClient] 메인 카메라를 찾을 수 없습니다!");
                yield break;
            }
            
            _cameraTransform = _mainCamera.transform;
            
            // 카메라 설정 (주시 대상, 오프셋 등)
            if (_snakeHead == null)
            {
                _snakeHead = transform;
                Debug.LogWarning("[SnakeClient] Snake Head가 설정되지 않아 자신의 Transform을 사용합니다.");
            }
            
            // 카메라 위치 초기화
            Vector3 targetPosition = _snakeHead.position + _cameraOffset;
            _cameraTransform.position = targetPosition;
            _cameraTransform.LookAt(_snakeHead);
            
            Debug.Log("[SnakeClient] 카메라 설정 완료");
            yield return null;
        }
        
        private void SetupInput()
        {
            Debug.Log("[SnakeClient] 입력 설정 시작");
            
            // JoystickInputProvider 찾기
            _inputProvider = FindObjectOfType<JoystickInputProvider>();
            
            if (_inputProvider == null)
            {
                Debug.LogWarning("[SnakeClient] JoystickInputProvider를 찾을 수 없습니다. 새로 생성합니다.");
                
                // 프로바이더가 없으면 생성
                GameObject joystickProviderGO = new GameObject("JoystickInputProvider");
                _inputProvider = joystickProviderGO.AddComponent<JoystickInputProvider>();
                
                // 생성 후 DontDestroyOnLoad 설정
                DontDestroyOnLoad(joystickProviderGO);
            }
            
            // 입력 이벤트 구독
            _inputProvider.OnMovementDirectionChanged += HandleDirectionChanged;
            _inputInitialized = true;
            
            Debug.Log("[SnakeClient] 입력 설정 완료");
        }
        #endregion
        
        #region 업데이트 로직
        private void Update()
        {
            if (!IsOwner || !_isActive) return;
            
            // 여기서 추가 입력 처리나 다른 업데이트 로직 수행
            UpdateCamera();
            
            // 디버깅 정보
            if (_inputInitialized && Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log($"[SnakeClient] 현재 방향: {_currentDirection}");
            }
        }
        
        private void UpdateCamera()
        {
            if (_cameraTransform == null || _snakeHead == null) return;
            
            // 부드러운 카메라 이동
            Vector3 targetPosition = _snakeHead.position + _cameraOffset;
            _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, targetPosition, Time.deltaTime * _cameraFollowSpeed);
            
            // 스네이크를 향해 카메라 회전
            Quaternion targetRotation = Quaternion.LookRotation(_snakeHead.position - _cameraTransform.position);
            _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, targetRotation, Time.deltaTime * _cameraFollowSpeed);
        }
        #endregion
        
        #region 이벤트 핸들러
        private void HandleDirectionChanged(Vector2 newDirection)
        {
            // 클라이언트는 서버 소유 객체이므로 ServerRpc 호출
            if (_inputInitialized && IsOwner)
            {
                _currentDirection = newDirection * _inputSensitivity;
                SendDirectionToServerRpc(new Vector2(_currentDirection.x, _currentDirection.y));
            }
        }
        
        [ServerRpc]
        private void SendDirectionToServerRpc(Vector2 direction)
        {
            // 서버에서 방향 설정 (PlayerSnakeController에 전달)
            PlayerSnakeController controller = GetComponent<PlayerSnakeController>();
            if (controller != null)
            {
                controller.SetDirection(direction);
            }
            else
            {
                Debug.LogError("[SnakeClient] PlayerSnakeController를 찾을 수 없습니다!");
            }
        }
        #endregion
        
        #region 정리
        public override void OnNetworkDespawn()
        {
            if (_inputInitialized && _inputProvider != null)
            {
                _inputProvider.OnMovementDirectionChanged -= HandleDirectionChanged;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void OnDestroy()
        {
            if (_inputInitialized && _inputProvider != null)
            {
                _inputProvider.OnMovementDirectionChanged -= HandleDirectionChanged;
            }
        }
        #endregion
    }
} 