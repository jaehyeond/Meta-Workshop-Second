using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Assets.Scripts.Scene;
using Object = UnityEngine.Object;
using Unity.Assets.Scripts.UI;
using VContainer.Unity;
using Unity.Services.Lobbies.Models;
using Unity.Assets.Scripts.Network;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// 시작 화면 UI 관리 클래스
    /// </summary>
    public partial class UI_MainMenu : UI_Scene
    {
        #region Enums
        
        enum Texts
        {
        }
        
        enum Images
        {
        }
        
        enum GameObjects
        {
            Matching,
            UI_Bottom,
            Main_P,
            Hero_P,
            Shop_P,
            Guild_P,
            Lock_P

        }
        enum Buttons
        {
            RandomMatch
        }

        #endregion



        #region Injected Dependencies and Action
        
        // [Inject] private MainMenuScene _MainMenuScene;
        public static event Action OnRandomMatchRequested;

        #endregion



        #region Properties
        

        private GameObject MatchingObject => GetObject((int)GameObjects.Matching);
        private GameObject UI_BottomObject => GetObject((int)GameObjects.UI_Bottom);
        #endregion

        #region Events
        
        // UI 이벤트 정의 - 다른 클래스에서 구독할 수 있는 정적 이벤트
        
        #endregion

        #region Unity Lifecycle Methods
        
        private void Start()
        {
        }
        
        protected override void OnDestroy()
        {
            Debug.Log("[UI_MainMenu] OnDestroy 메서드 호출됨");
            UnsubscribeEvents();
            
            // 부모 클래스의 OnDestroy 호출
            base.OnDestroy();
        }
        
        #endregion

        #region Initialization
        
        public override bool Init()
        {

            if (base.Init() == false)
                return false;

            BindTexts(typeof(Texts));
            BindImages(typeof(Images));
            BindObjects(typeof(GameObjects));
            BindButtons(typeof(Buttons));

            // DebugComponents.LogHierarchy(gameObject, "[UI_MainMenu]");



            GetButton((int)Buttons.RandomMatch).gameObject.BindEvent((evt) => {

                Debug.Log("[UI_MainMenu] randomMatchButton 버튼 클릭됨");
                OnRandomMatchRequested?.Invoke();

            }, Define.EUIEvent.Click);

            Bottom_UIs bottomUIs = Util.GetOrAddComponent<Bottom_UIs>(GetObject((int)GameObjects.UI_Bottom));

            // 패널 배열 생성
            GameObject[] panels = new GameObject[] {
                GetObject((int)GameObjects.Main_P),
                GetObject((int)GameObjects.Hero_P),
                GetObject((int)GameObjects.Shop_P),
                GetObject((int)GameObjects.Guild_P),
                GetObject((int)GameObjects.Lock_P)
            };
            
            // 패널만 설정
            bottomUIs.SetPanels(panels);
            // Bottom_UIs 초기화
            bottomUIs.SetupPanelsAndButtons();


            return true;
        }
        
   
        
        /// <summary>
        /// 모든 UI 이벤트를 구독합니다.
        /// </summary>
        protected override void SubscribeEvents()
        {

            base.SubscribeEvents();
            LobbyUIMediator.OnWaitingStateChanged += OnWaitingStateChanged;

        }
        

            protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents(); // 부모 클래스의 구현 호출
            LobbyUIMediator.OnWaitingStateChanged -= OnWaitingStateChanged;
        }
        // 이벤트 핸들러
        private void OnWaitingStateChanged(bool isWaiting)
        {
            if (MatchingObject != null)
            {
                MatchingObject.SetActive(isWaiting);
                Debug.Log($"[UI_MainMenu] 매칭 UI {(isWaiting ? "활성화" : "비활성화")}");

            }
        }
        #endregion

        #region Event Handlers
        
        
        #endregion

        #region Editor Methods
        
        #if UNITY_EDITOR
        [ContextMenu("로그: Matching 하위 객체 출력")]
        private void LogMatchingChildrenMenu()
        {
            DebugComponents.LogHierarchy(MatchingObject, "[UI_MainMenu]");
        }
        
        // Inspector에서 버튼으로 표시되는 메서드
        // [CustomEditor(typeof(UI_MainMenu))]
        // public class UI_MainMenuEditor : DebugComponents.UIDebugEditorBase
        // {
        //     public override void OnInspectorGUI()
        //     {
        //         DrawDefaultInspector();
                
        //         UI_MainMenu script = (UI_MainMenu)target;
                
        //         // UIDebugLogger의 에디터 확장 기능 사용
        //         AddDebugButtons(script.gameObject, "Matching", "[UI_MainMenu]");
        //     }
        // }
        #endif
        
        #endregion
    }
}

