using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Module.ApplicationLifecycle.Installers;

namespace Unity.Assets.Scripts.Module.ApplicationLifecycle
{
    /// <summary>
    /// 모듈 상태를 관리하는 클래스
    /// 
    /// 이 클래스는 애플리케이션의 각 모듈(Network, Message, Lobby 등)의 활성화/비활성화 상태를 관리합니다.
    /// 싱글톤 패턴으로 구현되어 있어 애플리케이션 전체에서 하나의 인스턴스만 존재합니다.
    /// 
    /// 데이터 저장 방식:
    /// - 모든 모듈 상태는 Dictionary<ModuleType, ModuleState>에 저장됩니다.
    /// - 키(Key): ModuleType enum 값 (Network, Message, Lobby 등)
    /// - 값(Value): ModuleState enum 값 (Enabled 또는 Disabled)
    /// 
    /// 예시:
    /// {
    ///     { ModuleType.Network, ModuleState.Enabled },     // 네트워크 모듈 활성화
    ///     { ModuleType.Message, ModuleState.Enabled },     // 메시지 모듈 활성화
    ///     { ModuleType.Lobby, ModuleState.Disabled },      // 로비 모듈 비활성화
    ///     { ModuleType.UI, ModuleState.Enabled },          // UI 모듈 활성화
    ///     { ModuleType.Authentication, ModuleState.Enabled }, // 인증 모듈 활성화
    ///     { ModuleType.GameData, ModuleState.Enabled }     // 게임 데이터 모듈 활성화
    /// }
    /// 
    /// 사용 예시:
    /// 1. 모듈 상태 설정:
    ///    ModuleStateManager.Instance.SetModuleState(ModuleType.Network, ModuleState.Disabled);
    /// 
    /// 2. 모듈 상태 확인:
    ///    ModuleState state = ModuleStateManager.Instance.GetModuleState(ModuleType.Network);
    ///    if (state == ModuleState.Enabled) { ... }
    /// 
    /// 3. 모듈 활성화 여부 확인:
    ///    if (ModuleStateManager.Instance.IsModuleEnabled(ModuleType.Network)) { ... }
    /// 
    /// 4. 특정 모듈만 활성화:
    ///    ModuleStateManager.Instance.EnableOnlySpecifiedModules(ModuleType.Network, ModuleType.UI);
    /// 
    /// 5. 모듈 상태 변경 이벤트 구독:
    ///    ModuleStateManager.Instance.OnModuleStateChanged += (moduleType, state) => {
    ///        Debug.Log($"모듈 '{moduleType}' 상태가 '{state}'로 변경되었습니다.");
    ///    };
    /// </summary>
    public class ModuleStateManager
    {
        // 싱글톤 인스턴스
        private static ModuleStateManager s_Instance;
        public static ModuleStateManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ModuleStateManager();
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// 모듈 상태를 관리하는 딕셔너리
        /// 키: ModuleType (Network, Message, Lobby 등)
        /// 값: ModuleState (Enabled 또는 Disabled)
        /// 
        /// 예시:
        /// {
        ///     { ModuleType.Network, ModuleState.Enabled },
        ///     { ModuleType.Message, ModuleState.Enabled },
        ///     { ModuleType.Lobby, ModuleState.Disabled }
        /// }
        /// </summary>
        private Dictionary<ModuleType, ModuleState> m_ModuleStates = new Dictionary<ModuleType, ModuleState>();

        // 모듈 상태 변경 이벤트
        public event Action<ModuleType, ModuleState> OnModuleStateChanged;

        // 생성자는 private으로 선언하여 싱글톤 패턴 구현
        private ModuleStateManager() 
        {
            // 기본적으로 모든 모듈 타입을 활성화 상태로 초기화
            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                m_ModuleStates[moduleType] = ModuleState.Enabled;
            }
            
            // 초기 상태 로그 출력 (디버깅용)
            // LogCurrentState();
        }




    

        /// <summary>
        /// 모듈 상태 설정
        /// </summary>
        /// <param name="moduleType">모듈 타입</param>
        /// <param name="state">설정할 상태</param>
        /// 
        /// 사용 예시:
        /// ModuleStateManager.Instance.SetModuleState(ModuleType.Network, ModuleState.Disabled);
        /// ModuleStateManager.Instance.SetModuleState(ModuleType.UI, ModuleState.Enabled);
        public void SetModuleState(ModuleType moduleType, ModuleState state)
        {
            // 이전 상태와 다를 경우에만 처리
            if (!m_ModuleStates.TryGetValue(moduleType, out var currentState) || currentState != state)
            {
                m_ModuleStates[moduleType] = state;
                Debug.Log($"[ModuleStateManager] 모듈 '{moduleType}' 상태 변경: {state}");
                
                // 이벤트 발생
                OnModuleStateChanged?.Invoke(moduleType, state);
            }
        }

        /// <summary>
        /// 모듈 상태 가져오기
        /// </summary>
        /// <param name="moduleType">모듈 타입</param>
        /// <returns>모듈 상태</returns>
        /// 
        /// 사용 예시:
        /// ModuleState networkState = ModuleStateManager.Instance.GetModuleState(ModuleType.Network);
        /// if (networkState == ModuleState.Enabled) {
        ///     // 네트워크 모듈이 활성화된 경우의 처리
        /// }
        public ModuleState GetModuleState(ModuleType moduleType)
        {
            if (m_ModuleStates.TryGetValue(moduleType, out var state))
            {
                return state;
            }
            
            // 기본값은 활성화 상태 (프로젝트에 포함된 모듈은 기본적으로 활성화)
            return ModuleState.Enabled;
        }

        /// <summary>
        /// 모듈 활성화 여부 확인
        /// </summary>
        /// <param name="moduleType">모듈 타입</param>
        /// <returns>활성화 여부</returns>
        /// 
        /// 사용 예시:
        /// if (ModuleStateManager.Instance.IsModuleEnabled(ModuleType.Network)) {
        ///     // 네트워크 모듈이 활성화된 경우의 처리
        /// } else {
        ///     // 네트워크 모듈이 비활성화된 경우의 처리
        /// }
        public bool IsModuleEnabled(ModuleType moduleType)
        {
            return GetModuleState(moduleType) == ModuleState.Enabled;
        }

        /// <summary>
        /// 모든 모듈 상태 초기화 (모든 모듈을 활성화 상태로 설정)
        /// </summary>
        /// 
        /// 사용 예시:
        /// ModuleStateManager.Instance.ResetAllModuleStates();
        public void ResetAllModuleStates()
        {
            m_ModuleStates.Clear();
            
            // 모든 모듈을 활성화 상태로 초기화
            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                SetModuleState(moduleType, ModuleState.Enabled);
            }
            
            Debug.Log("[ModuleStateManager] 모든 모듈 상태가 초기화되었습니다.");
        }

        /// <summary>
        /// 특정 모듈만 활성화하고 나머지는 비활성화
        /// </summary>
        /// <param name="enabledModules">활성화할 모듈 목록</param>
        /// 
        /// 사용 예시:
        /// // Network와 UI 모듈만 활성화하고 나머지는 비활성화
        /// ModuleStateManager.Instance.EnableOnlySpecifiedModules(ModuleType.Network, ModuleType.UI);
        /// 
        /// // 모든 모듈 비활성화 (빈 배열 전달)
        /// ModuleStateManager.Instance.EnableOnlySpecifiedModules();
        public void EnableOnlySpecifiedModules(params ModuleType[] enabledModules)
        {
            // 모든 모듈을 비활성화
            foreach (ModuleType moduleType in Enum.GetValues(typeof(ModuleType)))
            {
                SetModuleState(moduleType, ModuleState.Disabled);
            }
            
            // 지정된 모듈만 활성화
            foreach (var moduleType in enabledModules)
            {
                SetModuleState(moduleType, ModuleState.Enabled);
            }
            
            // Debug.Log($"[ModuleStateManager] 지정된 모듈만 활성화되었습니다: {string.Join(", ", enabledModules)}");
            // 현재 상태 로그 출력 (디버깅용)
            // LogCurrentState();
        }


        private void LogCurrentState()
            {
                Debug.Log("[ModuleStateManager] 현재 모듈 상태:");
                foreach (var pair in m_ModuleStates)
                {
                    Debug.Log($"  - {pair.Key}: {pair.Value}");
                }
            }



    }
} 