// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.EventSystems;

// public class Camera_BasicGame : NetworkBehaviour
// {
//     Camera cam;
//     UI_Spawn_Holder holder = null;
//     UI_Spawn_Holder Move_Holder = null;
//     string HostAndClient = "";

//     private void Start()
//     {
//         cam = Camera.main;
//         HostAndClient = NetUtils.LocalID() == 0 ? "HOST" : "CLIENT";
//     }

//     // Update is called once per frame
//     private void Update()
//     {
//         if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) 
//         {
//             MouseButtonDown();
//         }

//         if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
//         {
//             MouseButton();

//         }
//         if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
//         {
//             MouseButtonUp();
//         }

//     }

//     private void MouseButtonDown()
//     {
//         Ray ray = cam.ScreenPointToRay(Input.mousePosition);
//         RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

//         if (holder != null)
//         {
//             holder.ReturnRange();
//             holder = null;
//         }

//         if (hit.collider != null)
//         {
//             holder = hit.collider.GetComponent<UI_Spawn_Holder>();
//             Debug.Log(holder);
//             if(holder.Holder_Name == null)
//             {
//                 holder = null;
//                 return;
//             }

//             bool CanGet = false;
//             int value = (int)NetworkManager.Singleton.LocalClientId;

//             if (value == 0) CanGet = holder.Holder_Part_Name.Contains("HOST");
//             else if (value == 1) CanGet = holder.Holder_Part_Name.Contains("CLIENT");

//             if (!CanGet) holder = null;
//         }
//     }

//     private void MouseButton()
//     {
//         // if (holder != null)
//         // {
//         //     holder.G_GetClick(true);
//         //     Ray ray = cam.ScreenPointToRay(Input.mousePosition);
//         //     RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);




//         //     if(hit.collider != null && hit.collider.transform != holder.transform)
//         //     {
//         //         if(hit.collider.GetComponent<UI_Spawn_Holder>() == null) return;
//         //         if(hit.collider.GetComponent<UI_Spawn_Holder>().Holder_Part_Name.Contains(HostAndClient) == false)
//         //         {
//         //             return;
//         //         }

//         //         if (Move_Holder != null)
//         //         {
//         //             Move_Holder.S_SetClick(false);
//         //         }

//         //         Move_Holder = hit.collider.GetComponent<ClientHero_Holder>();
//         //         Move_Holder.S_SetClick(true);
//         //     }

//         }

//     private void MouseButtonUp()
//     {
//         // if (holder == null) return;

//         // if(Move_Holder == null)
//         // {
//         //     Ray ray = cam.ScreenPointToRay(Input.mousePosition);
//         //     RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

//         //     if (hit.collider != null)
//         //     {
//         //         if (holder.transform == hit.collider.transform)
//         //         {
//         //             holder.GetRange();

//         //         }
//         //     }
//         // }
//         // else
//         // {

//         //     Move_Holder.S_SetClick(false);

//         //     UI_Spawn_Holder.instance.Holder_Position_Set(holder.Holder_Part_Name, Move_Holder.Holder_Part_Name);
//         // }
//         // if (holder != null)
//         // {   
//         //     holder.G_GetClick(false);
//         // }

//         // Move_Holder = null;

//     }
// }
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class Camera_BasicGame : NetworkBehaviour
{
    Camera cam;
    UI_Spawn_Holder holder = null;
    UI_Spawn_Holder Move_Holder = null;
    string HostAndClient = "";

    [Inject] private NetUtils _netUtils;
    [Inject] private MapSpawnerFacade _mapSpawnerFacade; // MapSpawnerFacade 주입 추가

    [Inject]
    public void Construct(NetUtils netUtils, MapSpawnerFacade mapSpawnerFacade)
    {
        _netUtils = netUtils;
        _mapSpawnerFacade = mapSpawnerFacade;
    }

    private void Start()
    {
        cam = Camera.main;
        HostAndClient = _netUtils.LocalID_P() == 0 ? "HOST" : "CLIENT";
        Debug.Log($"[Camera_BasicGame] 초기화 완료. 클라이언트 타입: {HostAndClient}");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) 
        {
            MouseButtonDown();
        }

        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            MouseButton();
        }
        
        if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            MouseButtonUp();
        }
    }

    private void MouseButtonDown()
    {
        Debug.Log("[Camera_BasicGame] MouseButtonDown 시작");
        
        // 레이캐스트 실행
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        Debug.Log($"[Camera_BasicGame] 레이캐스트 결과: {(hit.collider != null ? hit.collider.name : "없음")}");
        
        // 안전한 방식으로 이전 홀더 처리
        if (holder != null)
        {
            try
            {
                Debug.Log($"[Camera_BasicGame] ReturnRange 호출 시도: {holder.name}");
                holder.ReturnRange();
                Debug.Log("[Camera_BasicGame] ReturnRange 호출 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Camera_BasicGame] ReturnRange 호출 중 오류: {e.Message}\n{e.StackTrace}");
            }
            
            holder = null;
        }
        
        // 새 홀더 찾기
        if (hit.collider != null)
        {
            try
            {
                // UI_Spawn_Holder 컴포넌트 찾기
                UI_Spawn_Holder hitHolder = hit.collider.GetComponent<UI_Spawn_Holder>();
                
                if (hitHolder == null)
                {
                    hitHolder = hit.collider.GetComponentInParent<UI_Spawn_Holder>();
                    
                    if (hitHolder == null)
                    {
                        Debug.Log("[Camera_BasicGame] UI_Spawn_Holder를 찾을 수 없습니다");
                        return;
                    }
                }
                
                // 히어로가 있는지 확인
                if (hitHolder.m_Heroes == null || hitHolder.m_Heroes.Count == 0)
                {
                    Debug.Log("[Camera_BasicGame] 홀더에 히어로가 없습니다");
                    return;
                }
                
                // 소유권 확인
                bool canGet = false;
                ulong localId = _netUtils.LocalID_P();
                string localType = localId == 0 ? "HOST" : "CLIENT";
                
                if (!string.IsNullOrEmpty(hitHolder.Holder_Part_Name))
                {
                    canGet = hitHolder.Holder_Part_Name.Contains(localType);
                    
                    if (!canGet)
                    {
                        Debug.Log("[Camera_BasicGame] 다른 플레이어의 홀더입니다");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("[Camera_BasicGame] Holder_Part_Name이 null 또는 빈 문자열입니다");
                    return;
                }
                
                // 모든 조건을 통과했으면 홀더 설정
                holder = hitHolder;
                Debug.Log($"[Camera_BasicGame] 새 홀더 설정: {holder.name}, Holder_Part_Name: {holder.Holder_Part_Name}, Heroes: {holder.m_Heroes.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Camera_BasicGame] 홀더 처리 중 오류: {e.Message}\n{e.StackTrace}");
                holder = null;
            }
        }
        
        Debug.Log("[Camera_BasicGame] MouseButtonDown 완료");
    }
    // private void MouseButtonDown()
    // {
    //     Ray ray = cam.ScreenPointToRay(Input.mousePosition);
    //     RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

    //     if (holder != null)
    //     {
    //         holder.ReturnRange();
    //         holder = null;
    //     }

    //     if (hit.collider != null)
    //     {
    //         holder = hit.collider.GetComponent<UI_Spawn_Holder>();
    //         Debug.Log($"[Camera_BasicGame] Hit object: {hit.collider.name}, Holder found: {holder != null}");
    //         Debug.Log($"[Camera_BasicGame] Holder_Name: {holder.Holder_Name}");
    //         Debug.Log($"[Camera_BasicGame] Holder_Part_Name: {holder.Holder_Part_Name}");
    //         Debug.Log($"[Camera_BasicGame] HostAndClient: {HostAndClient}");


    //         if (holder == null || holder.Holder_Name == 0)
    //         {
    //             holder = null;
    //             return;
    //         }

    //         bool canGet = false;
    //         if (_netUtils.LocalID_P() == 0) // 호스트인 경우
    //             canGet = holder.Holder_Part_Name.Contains("HOST");
    //         else // 클라이언트인 경우
    //             canGet = holder.Holder_Part_Name.Contains("CLIENT");

    //         if (!canGet)
    //         {
    //             Debug.Log($"[Camera_BasicGame] 다른 플레이어의 홀더입니다: {holder.Holder_Part_Name}");
    //             holder = null;
    //         }
    //     }
    // }

    private void MouseButton()
    {
        if (holder != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.transform != holder.transform)
            {
                UI_Spawn_Holder hitHolder = hit.collider.GetComponent<UI_Spawn_Holder>();
                if (hitHolder == null) return;
                
                if (!hitHolder.Holder_Part_Name.Contains(HostAndClient))
                {
                    return;
                }

                if (Move_Holder != null)
                {
                    // 이전 이동 홀더 상태 리셋 (필요에 따라 구현)
                }

                Move_Holder = hitHolder;
                Debug.Log($"[Camera_BasicGame] 이동 홀더 설정: {Move_Holder.Holder_Part_Name}");
            }
        }
    }
    private void MouseButtonUp()
    {
        if (holder == null) return;

        try
        {
            if (Move_Holder == null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

                if (hit.collider != null)
                {
                    UI_Spawn_Holder hitHolder = hit.collider.GetComponentInParent<UI_Spawn_Holder>();
                    
                    // 같은 홀더를 클릭했고, 히어로가 있는 경우에만 범위 표시
                    if (hitHolder == holder && hitHolder.m_Heroes.Count > 0)
                    {
                        Debug.Log($"[Camera_BasicGame] 선택된 홀더 범위 표시: {holder.name}");
                        holder.GetRange();
                    }
                    else
                    {
                        holder.ReturnRange();
                        holder = null;
                    }
                }
                else
                {
                    holder.ReturnRange();
                    holder = null;
                }
            }
            else
            {
                Debug.Log($"[Camera_BasicGame] 홀더 위치 교환: {holder.Holder_Part_Name} <-> {Move_Holder.Holder_Part_Name}");
                _mapSpawnerFacade.Holder_Position_Set(holder.Holder_Part_Name, Move_Holder.Holder_Part_Name);
                
                holder = null;
                Move_Holder = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Camera_BasicGame] MouseButtonUp 처리 중 오류: {e.Message}");
            holder = null;
            Move_Holder = null;
        }
    }
}