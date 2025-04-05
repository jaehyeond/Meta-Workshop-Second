using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Scene;
using Unity.Assets.Scripts.Auth;
using Object = UnityEngine.Object;
using Unity.Assets.Scripts.Network;
namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// 시작 화면 UI 관리 클래스
    /// </summary>
    public partial class UI_StartUpScene : UI_Scene
    {
        #region Enums
        
        enum Texts
        {
            DisplayText
        }
        
        enum Images
        {
            ProgressBar,
            ProgressBarBackground,
            Fill
        }
        
        enum GameObjects
        {
            ProgressBarArea
        }
        
        /// <summary>
        /// 로딩 단계 정의
        /// </summary>
        public enum LoadingStep
        {
            Initialize,      // 초기화
            ResourceLoad,    // 리소스 로드
            ConnectionLoad,  // 네트워크 연결 로드
            AuthLoad,        // 인증 로드
            Complete         // 완료
        }
        
        #endregion

        #region Constants
        
        private const string PRELOAD_LABEL = "PreLoad";
        private const float NEXT_SCENE_DELAY = 1.0f; // 로딩 완료 후 다음 씬으로 전환하기 전 대기 시간
        
        // 진행률 범위 상수
        private const float INIT_PROGRESS_START = 0.0f;
        private const float INIT_PROGRESS_END = 0.3f;
        private const float RESOURCE_PROGRESS_START = 0.3f;
        private const float RESOURCE_PROGRESS_END = 0.5f;
        private const float AUTH_PROGRESS_START = 0.5f;
        private const float AUTH_PROGRESS_END = 0.7f;
        private const float CONNECTION_PROGRESS_START = 0.7f;
        private const float CONNECTION_PROGRESS_END = 0.9f;
        
        private const float COMPLETE_PROGRESS_START = 0.9f;
        private const float COMPLETE_PROGRESS_END = 1.0f;
        
        #endregion

        #region Injected Dependencies
        // [Inject] private FirebaseManager m_FirebaseManager;
        [Inject] private StartUpScene _startUpScene;
        [Inject] private AuthManager _authManager;
        [Inject] private ConnectionManager _connectionManager;

        #endregion

        #region Private Fields
        
        private bool _isResourceLoaded = false;
        private bool _isProgressBarInitialized = false;
        private bool _isLoading = false;
        private bool _isAuthenticated = false;
        
        private float _progress = 0f;
        private string _status = "";
        private LoadingStep _currentStep = LoadingStep.Initialize;
        
        #endregion

        #region Properties
        
        public float Progress 
        { 
            get => _progress;
            private set => _progress = Mathf.Clamp01(value);
        }
        
        public string Status
        {
            get => _status;
            private set => _status = value;
        }
        private AsyncOperation m_AsyncOperation;

        #endregion

        #region Events
        
        // 프로그레스 변경 이벤트 (진행률, 상태 메시지)
        public event Action<float, string> OnProgressChanged;
        
        // 프로그레스 완료 이벤트
        public event Action OnProgressComplete;
        
        #endregion

        #region Unity Lifecycle Methods
        
        private void Start()
        {
            LogDebug("[UI_StartUpScene] Start 메서드 호출됨");
            
            // StartUpScene이 주입되지 않은 경우 직접 찾기
            if (_startUpScene == null)
            {
                _startUpScene = FindAnyObjectByType<StartUpScene>();
                if (_startUpScene == null)
                {
                    LogError("[UI_StartUpScene] StartUpScene을 찾을 수 없습니다!");
                }
                else
                {
                    LogDebug("[UI_StartUpScene] StartUpScene을 직접 찾았습니다.");
                }
            }
        }
        
        private void OnDestroy()
        {
            LogDebug("[UI_StartUpScene] OnDestroy 메서드 호출됨");
            UnsubscribeEvents();
        }
        
        #endregion

        #region Initialization
        
        public override bool Init()
        {
            if (base.Init() == false)
                return false;

            LogDebug("[UI_StartUpScene] Init 메서드 호출됨");
            
            try
            {
                BindUI();
                SubscribeEvents();
                StartCoroutine(InitializeLoadingProcess());
                return true;
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 초기화 중 오류 발생: {e.Message}\n{e.StackTrace}");
                UpdateDebugInfo($"Error: {e.Message}");
                return false;
            }
        }
        
        private void BindUI()
        {
            BindTexts(typeof(Texts));
            BindImages(typeof(Images));
            LogDebug("[UI_StartUpScene] UI 요소 바인딩 완료");
        }
        
        private void SubscribeEvents()
        {
            // 프로그레스 이벤트 구독
            OnProgressChanged += UpdateProgressUI;
            OnProgressComplete += OnLoadingComplete;
        }
        
        private void UnsubscribeEvents()
        {
            // 이벤트 구독 해제
            OnProgressChanged -= UpdateProgressUI;
            OnProgressComplete -= OnLoadingComplete;

        }
        
        #endregion

        #region Loading Process
        
        /// <summary>
        /// 로딩 프로세스 초기화 및 시작
        /// </summary>
        private IEnumerator InitializeLoadingProcess()
        {
            _isLoading = true;
            
            // 1. 초기화 단계
            yield return StartCoroutine(ProcessInitializeStep());
            

            // 2. 리소스 로드 단계
            yield return StartCoroutine(ProcessResourceLoadStep());

            // yield return CheckThirdPartyServiceInit();
            
            // 3. 앱 버전 검증
            if (!ValidateAppVersion())
            {
                Debug.LogError("[UI_StartUpScene] 앱 버전 검증 실패");
                yield break;
            }
            
               // 4. 인증 단계
            yield return StartCoroutine(ProcessAuthStep());

            // 3. 네트워크 연결 단계
            yield return StartCoroutine(ProcessConnectionLoadStep());
            
            // 5. 완료 단계
            yield return StartCoroutine(ProcessCompleteStep());
            
            _isLoading = false;
            UpdateProgress(1.0f, "Loading Complete!");
        }
        
        /// <summary>
        /// 초기화 단계 처리 (0% ~ 30%)
        /// </summary>
        private IEnumerator ProcessInitializeStep()
        {
            _currentStep = LoadingStep.Initialize;
            UpdateProgress(INIT_PROGRESS_START, "Installing...");
            
            // 초기화 단계에서 진행률 서서히 증가
            float elapsed = 0f;
            float duration = 1.0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float progress = Mathf.Lerp(INIT_PROGRESS_START, INIT_PROGRESS_END, t);
                UpdateProgress(progress, "Installing...");
                
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
            
            // 마지막에 정확한 종료 진행률로 설정
            UpdateProgress(INIT_PROGRESS_END, "Installing...");
        }
        
        /// <summary>
        /// 리소스 로드 단계 처리 (30% ~ 70%)
        /// </summary>
        private IEnumerator ProcessResourceLoadStep()
        {
            _currentStep = LoadingStep.ResourceLoad;
            UpdateProgress(RESOURCE_PROGRESS_START, "Resource Load...");
            
            LogDebug($"[UI_StartUpScene] Resource Load...");
            UpdateDebugInfo($"Resource Load...");
            
            // 리소스 로딩 진행 상황 추적을 위한 변수
            int totalResourceCount = 0;
            int loadedResourceCount = 0;
            
            // StartUpScene이 null인지 확인
            if (_startUpScene == null)
            {
                LogError("[UI_StartUpScene] StartUpScene이 null입니다.");
                // 직접 찾기 시도
                _startUpScene = FindAnyObjectByType<StartUpScene>();
                
                if (_startUpScene == null)
                {
                    LogError("[UI_StartUpScene] StartUpScene을 찾을 수 없습니다!");
                    UpdateDebugInfo($"Error: Resource Load...");
                    // 리소스 로드 단계를 건너뛰고 다음 단계로 진행
                    _isResourceLoaded = true;
                    yield break;
                }
                else
                {
                    LogDebug("[UI_StartUpScene] StartUpScene을 직접 찾았습니다.");
                }
            }
            
            try
            {
                LogDebug("[UI_StartUpScene] 리소스 로드 이벤트 구독 시작");
                
                // StartUpScene의 리소스 로드 이벤트 구독
                _startUpScene.OnResourceLoadProgress += (key, count, totalCount) =>
                {
                    // 총 리소스 개수 업데이트
                    totalResourceCount = totalCount;
                    loadedResourceCount = count;
                    
                    // 진행률 계산 및 업데이트 (30% ~ 70% 범위로 조정)
                    float loadProgress = (float)count / totalCount;
                    float adjustedProgress = RESOURCE_PROGRESS_START + (loadProgress * (RESOURCE_PROGRESS_END - RESOURCE_PROGRESS_START));
                    
                    // 상태 메시지 업데이트
                    string status = $"Resource Load... ({count}/{totalCount})";
                    
                    // 진행률 및 상태 업데이트
                    UpdateProgress(adjustedProgress, status);
                    
                    LogDebug($"[UI_StartUpScene] 리소스 로드 중: {key}, {count}/{totalCount}, 진행률: {loadProgress:P0}, 조정된 진행률: {adjustedProgress:P0}");
                };
                
                // 리소스 로드 완료 이벤트 구독
                _startUpScene.OnResourceLoadComplete += OnResourceLoadingComplete;
                
                LogDebug("[UI_StartUpScene] 리소스 로드 이벤트 구독 완료");
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 리소스 로딩 중 오류 발생: {e.Message}\n{e.StackTrace}");
                UpdateDebugInfo($"Error: {e.Message}");
            }
            
            UpdateProgress(RESOURCE_PROGRESS_END, "Resource Load Complete");
            LogDebug("[UI_StartUpScene] 리소스 로드 단계 완료");
        }
        
        /// <summary>
        /// 네트워크 연결 단계 처리 (70% ~ 80%)
        /// </summary>
        private IEnumerator ProcessConnectionLoadStep()
        {
            _currentStep = LoadingStep.ConnectionLoad;
            UpdateProgress(CONNECTION_PROGRESS_START, "Network Connection...");
            
            LogDebug($"[UI_StartUpScene] 네트워크 연결 시작");
            UpdateDebugInfo($"Network Connection...");

            // ConnectionManager가 null인지 확인
            if (_connectionManager == null)
            {
                LogError("[UI_StartUpScene] ConnectionManager가 null입니다.");
                UpdateProgress(CONNECTION_PROGRESS_END, "Offline Mode...");
                yield break;
            }

            bool isNetworkReady = false;
            bool isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            
            if (!isOnline)
            {
                LogWarning("[UI_StartUpScene] 인터넷 연결이 없습니다. 오프라인 모드로 진행합니다.");
                UpdateProgress(CONNECTION_PROGRESS_END, "Offline Mode...");
                yield break;
            }

            // ConnectionManager를 통한 네트워크 상태 확인
            var networkCheckTask = _connectionManager.CheckNetworkStatusAsync();
            
            while (!networkCheckTask.IsCompleted)
            {
                yield return null;
            }
            
            try
            {
                isNetworkReady = networkCheckTask.Result;
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 네트워크 연결 확인 중 오류 발생: {e.Message}");
                UpdateProgress(CONNECTION_PROGRESS_END, "Offline Mode...");
                yield break;
            }
            
            if (!isNetworkReady)
            {
                LogWarning("[UI_StartUpScene] 네트워크 연결이 불안정합니다. 오프라인 모드로 진행합니다.");
                UpdateProgress(CONNECTION_PROGRESS_END, "Offline Mode...");
                yield break;
            }

            // 온라인 상태 확인 완료
            LogDebug("[UI_StartUpScene] 네트워크 연결 확인 완료");
            UpdateProgress(CONNECTION_PROGRESS_END, "Network Connection Complete");
        }
        
        /// <summary>
        /// 인증 단계 처리 (80% ~ 90%)
        /// </summary>
        private IEnumerator ProcessAuthStep()
        {
            _currentStep = LoadingStep.AuthLoad;
            UpdateProgress(AUTH_PROGRESS_START, "Authentication...");
            
            LogDebug("[UI_StartUpScene] 인증 단계 시작");
            
            // 1. AuthManager 확인
            if (_authManager == null)
            {
                LogError("[UI_StartUpScene] AuthManager가 null입니다");
                UpdateProgress(AUTH_PROGRESS_END, "Authentication Failed (Offline Mode)");
                yield break;
            }
            
            // 2. 진행률 표시 애니메이션
            float elapsed = 0f;
            float animDuration = 0.5f;
            while (elapsed < animDuration)
            {
                float t = elapsed / animDuration;
                float progress = Mathf.Lerp(AUTH_PROGRESS_START, AUTH_PROGRESS_START + 0.1f, t);
                UpdateProgress(progress, "Authentication...");
                
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
            
            // 3. Unity 서비스 초기화
            UpdateProgress(AUTH_PROGRESS_START + 0.1f, "Unity Service Initialize...");
            
            // Unity 서비스 초기화 작업 시작
            var initTask = Unity.Services.Core.UnityServices.InitializeAsync();
            
            // 작업 완료 대기
            while (!initTask.IsCompleted)
            {
                yield return null;
            }
            
            LogDebug("[UI_StartUpScene] Unity Service Initialize Complete");
            
            // 4. 인증 수행
            bool success = false;
            bool isAlreadyAuthenticated = false;
            
            // 이미 인증되어 있는지 확인
            try
            {
                isAlreadyAuthenticated = _authManager.IsAuthenticated;
                if (isAlreadyAuthenticated)
                {
                    LogDebug($"[UI_StartUpScene] 이미 인증됨: 플레이어 ID = {_authManager.PlayerId}");
                    success = true;
                }
            }
            catch (System.Exception e)
            {
                LogError($"[UI_StartUpScene] 인증 상태 확인 중 오류: {e.Message}");
                isAlreadyAuthenticated = false;
            }
            
            // 인증이 필요한 경우
            if (!isAlreadyAuthenticated)
            {
                // 인증 시도
                UpdateProgress(AUTH_PROGRESS_START + 0.3f, "Authentication...");
                
                // 인증 작업 시작
                var authTask = _authManager.InitializeAndAuthenticateAsync();
                
                // 인증 작업 완료 대기
                while (!authTask.IsCompleted)
                {
                    yield return null;
                }
                
                // 결과 확인
                try
                {
                    success = authTask.Result;
                }
                catch (System.Exception e)
                {
                    LogError($"[UI_StartUpScene] 인증 중 오류: {e.Message}");
                    UpdateDebugInfo($"Authentication Error: {e.Message}");
                    success = false;
                }
            }
            
            // 5. 결과 처리
            if (success)
            {
                _isAuthenticated = true;
                LogDebug($"[UI_StartUpScene] 인증 성공: 플레이어 ID = {_authManager.PlayerId}");
                UpdateProgress(AUTH_PROGRESS_END, $"Authentication Complete: {_authManager.PlayerId}");
            }
            else
            {
                LogError("[UI_StartUpScene] 인증 실패");
                UpdateProgress(AUTH_PROGRESS_END, "Authentication Failed (Offline Mode)");
            }
            
            LogDebug("[UI_StartUpScene] 인증 단계 완료");
        }
        
        /// <summary>
        /// 완료 단계 처리 (90% ~ 100%)
        /// </summary>
        private IEnumerator ProcessCompleteStep()
        {
            _currentStep = LoadingStep.Complete;
            
            // 완료 단계에서 진행률 서서히 증가
            float elapsed = 0f;
            float duration = 1.0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float progress = Mathf.Lerp(COMPLETE_PROGRESS_START, COMPLETE_PROGRESS_END, t);
                UpdateProgress(progress, "Finish");
                
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
            
            // 마지막에 정확한 종료 진행률로 설정
            UpdateProgress(COMPLETE_PROGRESS_END, "Finish");
        }
        
        /// <summary>
        /// ResourceManager 이벤트 핸들러 - 로드 완료
        /// </summary>
        private void OnResourceLoadingComplete()
        {
            _isResourceLoaded = true;
            // 완료 처리는 InitializeLoadingProcess 코루틴에서 수행
        }
        
        #endregion
  


// 추가 메서드
        // private IEnumerator CheckThirdPartyServiceInit()
        // {
        //     // 기존의 FindObjectOfType 또는 유사 로직 제거
        //     // FirebaseManager firebaseManager = FindObjectOfType<FirebaseManager>(); // <- 이런 코드 제거

        //     // 주입된 _firebaseManager 사용
        //     if (m_FirebaseManager == null)
        //     {
        //         // 이 경우는 VContainer 주입 설정이 잘못되었거나 FirebaseManager 등록에 실패한 경우
        //         Debug.LogError("[UI_StartUpScene] FirebaseManager가 주입되지 않았습니다! VContainer 설정을 확인하세요.");
        //         yield break; // 또는 다른 에러 처리
        //     }

        //     Debug.Log("[UI_StartUpScene] FirebaseManager 확인 중...");

        //     float elapsedTime = 0f;
        //     // FirebaseManager의 IsInit() 같은 초기화 완료 확인 메소드 사용
        //     while (!m_FirebaseManager.IsInit()) // IsInit()이 public이어야 함
        //     {
        //         elapsedTime += Time.deltaTime;
        //         if (elapsedTime > Define.THIRD_PARTY_SERVICE_INIT_TIME) // 타임아웃 (Define 값 사용)
        //         {
        //             Debug.LogError("[UI_StartUpScene] FirebaseManager 초기화 시간 초과!");
        //             // TODO: 초기화 실패 시 UI 표시 또는 재시도 로직
        //             yield break;
        //         }
        //         yield return null; // 다음 프레임까지 대기
        //     }

        //     Debug.Log("[UI_StartUpScene] FirebaseManager 초기화 완료.");
        //     // 이제 _firebaseManager 사용 가능
        // }


    private bool ValidateAppVersion()
    {
        try
        {
            bool result = false;
            
            // if (Application.version == FirebaseManager.Instance.GetAppVersion())
            // {
            //     Debug.Log("<color=green>[UI_StartUpScene] 앱 버전 검증 성공</color>");
            //     result = true;
            // }
            if(true)
            {
                result = true;
            }
            else
            {
                // 버전 업데이트 알림 UI 표시
                // var uiData = new ConfirmUIData();
                // uiData.ConfirmType = ConfirmType.OK_CANCEL;
                // uiData.TitleTxt = string.Empty;
                // uiData.DescTxt = "App version is outdated. Will you update your app?";
                // uiData.OKBtnTxt = "Update";
                // uiData.CancelBtnTxt = "Cancel";
                // uiData.OnClickOKBtn = () =>
                // {
                // #if UNITY_ANDROID
                //     Application.OpenURL(GlobalDefine.GOOGLE_PLAY_STORE);
                // #elif UNITY_IOS
                //     Application.OpenURL(GlobalDefine.APPLE_APP_STORE);
                // #endif
                // };
                // uiData.OnClickCancelBtn = () =>
                // {
                //     Application.Quit();
                // };
            }
            
            return result;
        }
        catch (System.Exception e)
        {
            LogError($"[UI_StartUpScene] 앱 버전 검증 중 오류: {e.Message}");
            return false;
        }
    }



    }

  }
