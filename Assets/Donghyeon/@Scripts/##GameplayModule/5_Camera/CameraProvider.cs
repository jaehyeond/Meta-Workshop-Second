


using CameraLogic;
using UnityEngine;
using VContainer;
using Unity.Assets.Scripts.Resource;

public class CameraProvider
{
    public static CameraProvider Instance { get; private set; }

    public Camera Current { get; }

    private CameraFollow _cameraFollow;
    


    public CameraProvider()
    {
        if (Instance != null)
        {
            Debug.LogWarning("[CameraProvider] 싱글톤 인스턴스가 이미 존재합니다. 이전 인스턴스를 사용합니다.");
        }
        else
        {
            Instance = this;
            Debug.Log("[CameraProvider] 싱글톤 인스턴스가 설정되었습니다.");
        }

        Debug.Log("[CameraProvider] 생성자 호출됨");
        Current = Camera.main;
        _cameraFollow = Current?.GetComponent<CameraFollow>();
        Debug.Log($"[CameraProvider]Camera: {Current}");
        Debug.Log($"[CameraProvider]CameraFollow: {_cameraFollow}");

        Initialize();
    }
    

    public void Follow(Transform target)
    {
        if (Instance == null)
        {
            Debug.LogError("[CameraProvider] Follow 호출 시 Instance가 null입니다!");
            return;
        }

        if (_cameraFollow == null)
        {
            Initialize();
            if (_cameraFollow == null)
            {
                Debug.LogError("[CameraProvider] CameraFollow 컴포넌트를 찾을 수 없어 Follow를 실행할 수 없습니다.");
                return;
            }
        }
        Debug.Log($"[CameraProvider] Follow 호출됨");
        Debug.Log($"[CameraProvider] target: {target}");
        Debug.Log($"[CameraProvider] _cameraFollow: {_cameraFollow}");
        // Debug.Log($"[CameraProvider] _cameraFollow.Follow: {_cameraFollow.Follo}");
        _cameraFollow.Follow(target); // 이 줄만 남기고

        // 실제 CameraFollow 스크립트의 타겟 설정 방법에 맞게 수정이 필요합니다.
        // 다음 중 하나를 시도해보세요:
        
        // 방법 1:
        
        // 또는 방법 2:
        // _cameraFollow.followTarget = target;
        
        // 또는 방법 3:
        // _cameraFollow.targetTransform = target;
    }

    
    public void Initialize()
    {

        if (_cameraFollow == null)
        {
            _cameraFollow = Camera.main?.GetComponent<CameraFollow>();
            Debug.Log($"[CameraProvider] Initialize: CameraFollow 찾기 결과: {_cameraFollow != null}");
        }
    }

}
