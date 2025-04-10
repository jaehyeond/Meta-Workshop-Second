// // using UnityEngine;
// // using TMPro;

// // /// <summary>
// // /// Snake의 몸통 세그먼트를 관리하는 클래스
// // /// </summary>
// // public class SnakeBodySegment : MonoBehaviour
// // {
// //     [SerializeField] private TextMeshPro _valueText;  // 값을 표시할 TextMeshPro
    
// //     private int _value = 0;  // 초기값 (서버에서 덮어쓰기 전)
    
// //     /// <summary>
// //     /// 세그먼트의 값을 설정합니다.
// //     /// </summary>
// //     public void SetValue(int value)
// //     {
// //         _value = value;
// //         UpdateValueDisplay();
// //     }
    
// //     /// <summary>
// //     /// 현재 세그먼트의 값을 반환합니다.
// //     /// </summary>
// //     public int GetValue()
// //     {
// //         return _value;
// //     }
    
// //     /// <summary>
// //     /// 값 표시를 업데이트합니다.
// //     /// </summary>
// //     private void UpdateValueDisplay()
// //     {
// //         if (_valueText != null)
// //         {
// //             _valueText.text = _value.ToString();
// //         }
// //     }
// // } 

// using UnityEngine;
// using TMPro;
// using Unity.Netcode;
// using System.Collections;

// public class SnakeBodySegment : NetworkBehaviour
// {
//     [SerializeField] private TextMeshPro _valueText;
    
//     private readonly NetworkVariable<int> _value = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
//     public override void OnNetworkSpawn()
//     {
//         base.OnNetworkSpawn();
//         _value.OnValueChanged += OnValueChanged;
//         UpdateValueDisplay();

//         if (!IsServer || IsHost)
//         {
//             StartCoroutine(RegisterWithRetry());
//         }
//     }

//     public override void OnNetworkDespawn()
//     {
//         base.OnNetworkDespawn();
//         _value.OnValueChanged -= OnValueChanged;
//     }

//     private void OnValueChanged(int previousValue, int newValue)
//     {
//         UpdateValueDisplay();
//     }

//     public void SetValue(int value)
//     {
//         if (!IsServer) return;
//         _value.Value = value;
//     }
    
//     public int GetValue()
//     {
//         return _value.Value;
//     }
    
//     private void UpdateValueDisplay()
//     {
//         if (_valueText != null)
//         {
//             _valueText.text = _value.Value.ToString();
//         }
//     }

//     private const int MAX_REGISTER_ATTEMPTS = 10;
//     private const float REGISTER_RETRY_DELAY = 0.1f;

//     private IEnumerator RegisterWithRetry()
//     {
//         PlayerSnakeController controller = null;
//         SnakeBody body = null;
//         int attempts = 0;

//         while (attempts < MAX_REGISTER_ATTEMPTS)
//         {
//             controller = GetComponentInParent<PlayerSnakeController>();
//             if (controller != null)
//             {
//                  body = controller.GetComponent<SnakeBody>();
//                  if (body != null)
//                  {
//                       body.RegisterClientSegment(this);
//                       yield break;
//                  }
//             }
//             attempts++;
//             yield return new WaitForSeconds(REGISTER_RETRY_DELAY);
//         }
//         Debug.LogError($"[{GetType().Name} NetId:{NetworkObjectId}] Failed to register with SnakeBody after {MAX_REGISTER_ATTEMPTS} attempts!");
//     }
// }

using UnityEngine;
using TMPro;

/// <summary>
/// Snake의 몸통 세그먼트를 관리하는 클래스
/// </summary>
public class SnakeBodySegment : MonoBehaviour
{
    [SerializeField] private TextMeshPro _valueText;  // 값을 표시할 TextMeshPro
    
    private int _value = 2;  // 기본값
    
    /// <summary>
    /// 세그먼트의 값을 설정합니다.
    /// </summary>
    public void SetValue(int value)
    {
        _value = value;
        UpdateValueDisplay();
    }
    
    /// <summary>
    /// 현재 세그먼트의 값을 반환합니다.
    /// </summary>
    public int GetValue()
    {
        return _value;
    }
    
    /// <summary>
    /// 값 표시를 업데이트합니다.
    /// </summary>
    private void UpdateValueDisplay()
    {
        if (_valueText != null)
        {
            _valueText.text = _value.ToString();
        }
    }
} 