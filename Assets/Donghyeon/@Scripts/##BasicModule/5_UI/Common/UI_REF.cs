// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using VContainer;
// using Unity.Assets.Scripts.Resource;
// using Unity.Assets.Scripts.Scene;
// using Object = UnityEngine.Object;
// using Unity.Assets.Scripts.UI;
// using VContainer.Unity;

// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// namespace Unity.Assets.Scripts.UI
// {
//     /// <summary>
//     /// 시작 화면 UI 관리 클래스
//     /// </summary>
//     public partial class UI_REF : UI_Scene
//     {
//         #region Enums
        
//         enum Texts
//         {
//         }
        
//         enum Images
//         {
//         }
        
//         enum GameObjects
//         {
//             Matching,
//             Main
//         }
        

//         #endregion



//         #region Injected Dependencies
        
//         [Inject] private MainMenuScene _MainMenuScene;
//         [Inject] private SceneManagerEx _sceneManager;

//         #endregion



//         #region Properties
        
//         // 바인딩 대신 직접 gameObject 사용
    

//         private GameObject MatchingObject => transform.Find("Matching")?.gameObject;
//         private GameObject MainObject => transform.Find("Main")?.gameObject;
//         #endregion

//         #region Events
        
        
//         #endregion

//         #region Unity Lifecycle Methods
        
//         private void Start()
//         {
//             Debug.Log("[UI_MainMenu] Start 메서드 호출됨");
            
//             // MainMenuScene이 주입되지 않은 경우 직접 찾기
//             if (_MainMenuScene == null)
//             {
//                 _MainMenuScene = FindObjectOfType<MainMenuScene>();
//                 Debug.Log(_MainMenuScene != null 
//                     ? "[UI_MainMenu] MainMenuScene을 직접 찾았습니다." 
//                     : "[UI_MainMenu] MainMenuScene을 찾을 수 없습니다!");
//             }
            
//             // UI 객체들 초기화 - 메서드로 추출
//             InitUIObject("Matching", MatchingObject);
//             InitUIObject("Main", MainObject);
//         }
        
//         private void OnDestroy()
//         {
//             Debug.Log("[UI_MainMenu] OnDestroy 메서드 호출됨");
//             UnsubscribeEvents();
//         }
        
//         #endregion

//         #region Initialization
        
//         public override bool Init()
//         {
//             if (base.Init() == false)
//                 return false;

//             Debug.Log("[UI_MainMenu] Init 메서드 호출됨");
            
//             try
//             {
//                 BindUI();
//                 SubscribeEvents();
                
//                 // 바인딩 상태 확인
//                 bool matchingBound = MatchingObject != null;
//                 bool mainBound = MainObject != null;

//                 if (matchingBound && mainBound)
//                 {
//                     Debug.Log($"<color=green>[UI_MainMenu] Init 완료: 모든 객체 바인딩 성공 (Matching: {MatchingObject.transform.childCount}개, Main: {MainObject.transform.childCount}개)</color>");
//                 }
//                 else
//                 {
//                     string errorMsg = "<color=red>[UI_MainMenu] Init 완료: 바인딩 실패! ";
//                     if (!matchingBound) errorMsg += "Matching 객체 없음. ";
//                     if (!mainBound) errorMsg += "Main 객체 없음. ";
//                     errorMsg += "</color>";
//                     Debug.LogError(errorMsg);
//                 }
                
//                 return true;
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"[UI_MainMenu] 초기화 중 오류 발생: {e.Message}\n{e.StackTrace}");
//                 return false;
//             }
//         }
        
//         private void BindUI()
//         {
//             // Text와 Image만 바인딩
//             BindTexts(typeof(Texts));
//             BindImages(typeof(Images));
//             BindObjects(typeof(GameObjects));
//             // GameObject는 바인딩하지 않고 직접 gameObject 사용
//             Debug.Log($"<color=green>[UI_MainMenu] Matching 객체: \"{gameObject.name}\"</color>");
            
//             // 계층 구조 출력
//             DebugComponents.LogHierarchy(gameObject, "[UI_MainMenu]");
//         }
        
//         private void SubscribeEvents()
//         {

            
//             if (_sceneManager == null)
//             {
//                 // LogError("[UI_MainMenu] SceneManagerEx가 주입되지 않았습니다!");
//             }
//         }
        
//         private void UnsubscribeEvents()
//         {
//             // 이벤트 구독 해제
//         }
//         public void LogChildren()
//         {
//             DebugComponents.LogHierarchy(MatchingObject, "[UI_MainMenu]");
//         }
        
//         #endregion

//         // 메서드 추가
//         private void InitUIObject(string name, GameObject obj)
//         {
//             if (obj != null)
//             {
//                 Debug.Log($"[UI_MainMenu] {name} 객체가 성공적으로 바인딩되었습니다.");
//             }
//             else
//             {
//                 Debug.LogError($"[UI_MainMenu] {name} 객체를 찾을 수 없습니다!");
//             }
//         }




//         // #region Editor Methods
        
//         #if UNITY_EDITOR
//         [ContextMenu("로그: Matching 하위 객체 출력")]
//         private void LogMatchingChildrenMenu()
//         {
//             LogChildren();
//         }
        
//         // Inspector에서 버튼으로 표시되는 메서드
//         // [CustomEditor(typeof(UI_MainMenu))]
//         // public class UI_MainMenuEditor : DebugComponents.UIDebugEditorBase
//         // {
//         //     public override void OnInspectorGUI()
//         //     {
//         //         DrawDefaultInspector();
                
//         //         UI_MainMenu script = (UI_MainMenu)target;
                
//         //         // UIDebugLogger의 에디터 확장 기능 사용
//         //         AddDebugButtons(script.gameObject, "Matching", "[UI_MainMenu]");
//         //     }
//         // }
//         // #endif
        
//         // #endregion
//     }
// }