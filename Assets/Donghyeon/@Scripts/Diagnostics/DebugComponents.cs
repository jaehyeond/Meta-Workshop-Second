using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// UI 객체의 디버깅을 위한 유틸리티 클래스입니다.
    /// 모든 UI 스크립트에서 공통으로 사용할 수 있는 로깅 기능을 제공합니다.
    /// </summary>
    public static class DebugComponents
    {
        /// <summary>
        /// 게임 오브젝트의 모든 하위 객체를 로그로 출력합니다.
        /// </summary>
        /// <param name="parent">로그를 출력할 부모 게임 오브젝트</param>
        /// <param name="logPrefix">로그 메시지 앞에 붙일 접두사 (예: "[UI_MainMenu]")</param>
        /// <param name="maxDepth">출력할 최대 깊이 (기본값: 2)</param>
        public static void LogHierarchy(GameObject parent, string logPrefix = "", int maxDepth = 10)
        {
            if (parent == null)
            {
                Debug.LogWarning($"{logPrefix} 객체가 없어 하위 객체를 로그로 출력할 수 없습니다.");
                return;
            }
            
            string objectName = parent.name;
            Debug.Log($"<color=yellow>{logPrefix} === {objectName} 객체의 하위 객체 목록 시작 ===</color>");
            Debug.Log($"{logPrefix} {objectName} 객체 정보: 이름={parent.name}, 활성화={parent.activeSelf}, 위치={parent.transform.position}");
            
            int childCount = parent.transform.childCount;
            if (childCount == 0)
            {
                Debug.Log($"<color=red>{logPrefix} {objectName} 객체에 하위 객체가 없습니다.</color>");
            }
            else
            {
                Debug.Log($"<color=green>{logPrefix} {objectName} 객체에 {childCount}개의 하위 객체가 있습니다.</color>");
                
                // 하위 객체 로깅
                LogChildrenRecursive(parent.transform, logPrefix, 1, maxDepth);
            }
            
            Debug.Log($"<color=yellow>{logPrefix} === {objectName} 객체의 하위 객체 목록 끝 ===</color>");
        }
        
        /// <summary>
        /// 재귀적으로 하위 객체를 로깅합니다.
        /// </summary>
        private static void LogChildrenRecursive(Transform parent, string logPrefix, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth)
                return;
                
            string depthPrefix = new string('-', currentDepth * 2);
            
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string activeState = child.gameObject.activeSelf ? "<color=green>활성화</color>" : "<color=red>비활성화</color>";
                Debug.Log($"{logPrefix} {depthPrefix} 하위 객체: 이름=<color=cyan>{child.name}</color>, 상태={activeState}, 위치={child.position}");
                
                // 컴포넌트 정보 출력
                LogComponents(child, logPrefix, depthPrefix);
                
                // 다음 깊이의 하위 객체 로깅
                if (currentDepth < maxDepth)
                {
                    LogChildrenRecursive(child, logPrefix, currentDepth + 1, maxDepth);
                }
                else if (child.childCount > 0)
                {
                    Debug.Log($"{logPrefix} {depthPrefix} <color=cyan>{child.name}</color>에 {child.childCount}개의 추가 하위 객체가 있습니다. (최대 깊이 제한)");
                }
            }
        }
        
        /// <summary>
        /// 게임 오브젝트의 컴포넌트를 로깅합니다.
        /// </summary>
        private static void LogComponents(Transform obj, string logPrefix, string depthPrefix)
        {
            if (obj == null)
            {
                Debug.LogWarning($"{logPrefix} Transform 객체가 null입니다.");
                return;
            }

            Component[] components = obj.GetComponents<Component>();
            if (components.Length > 0)
            {
                Debug.Log($"{logPrefix} {depthPrefix} <color=cyan>{obj.name}</color>의 컴포넌트 목록 ({components.Length}개):");
                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        Debug.LogWarning($"{logPrefix} {depthPrefix}   null 컴포넌트 발견");
                        continue;
                    }
                    Debug.Log($"{logPrefix} {depthPrefix}   컴포넌트: <color=magenta>{component.GetType().Name}</color>");
                }
            }
        }
                
        // /// <summary>
        // /// 게임 오브젝트의 특정 하위 요소를 가져옵니다.
        // /// </summary>
        // /// <param name="parent">부모 게임 오브젝트</param>
        // /// <param name="childName">하위 요소의 이름</param>
        // /// <returns>찾은 하위 요소, 없으면 null</returns>
        // public static GameObject GetChild(GameObject parent, string childName)
        // {
        //     if (parent == null)
        //         return null;
                
        //     Transform childTransform = parent.transform.Find(childName);
        //     return childTransform?.gameObject;
        // }
        
        // /// <summary>
        // /// 게임 오브젝트의 모든 하위 요소를 가져옵니다.
        // /// </summary>
        // /// <param name="parent">부모 게임 오브젝트</param>
        // /// <returns>하위 요소 목록</returns>
        // public static List<GameObject> GetAllChildren(GameObject parent)
        // {
        //     List<GameObject> children = new List<GameObject>();
            
        //     if (parent == null)
        //         return children;
                
        //     foreach (Transform child in parent.transform)
        //     {
        //         children.Add(child.gameObject);
        //     }
            
        //     return children;
        // }
        
// #if UNITY_EDITOR
//         /// <summary>
//         /// 에디터 확장 - 인스펙터에 디버깅 버튼을 추가하는 기본 클래스
//         /// </summary>
//         public abstract class UIDebugEditorBase : Editor
//         {
//             protected void AddDebugButtons(GameObject targetObject, string objectName, string logPrefix)
//             {
//                 EditorGUILayout.Space();
//                 EditorGUILayout.LabelField("디버깅 도구", EditorStyles.boldLabel);
                
//                 if (GUILayout.Button($"{objectName} 하위 객체 로그 출력"))
//                 {
//                     DebugComponents.LogHierarchy(targetObject, logPrefix);
//                 }
                
//                 if (GUILayout.Button($"{objectName} 객체 활성화"))
//                 {
//                     targetObject.SetActive(true);
//                 }
                
//                 if (GUILayout.Button($"{objectName} 객체 비활성화"))
//                 {
//                     targetObject.SetActive(false);
//                 }
//             }
//         }
// #endif
    }
} 