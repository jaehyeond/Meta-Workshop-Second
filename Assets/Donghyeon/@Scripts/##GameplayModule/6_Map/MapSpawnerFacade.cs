using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Unity.Netcode;
using Unity.Assets.Scripts.Objects;
using static Define;

/// <summary>
/// 맵 로드 및 초기화를 담당하는 클래스입니다.
/// </summary>
public class MapSpawnerFacade : NetworkBehaviour
{
    
    [Inject] public ResourceManager _resourceManager;
    [Inject] private NetUtils _netUtils; // NetUtils 추가
    [Inject] private NetworkManager _networkManager;


    [SerializeField] private UI_Spawn_Holder _spawn_Holder;

    private GameObject _mapInstance;
    
    // 리스트를 모두 Public으로 변경
    [SerializeField] public List<Vector2> Player_move_list = new List<Vector2>();      // Host 이동 경로 포인트
    [SerializeField] public List<Vector2> Other_move_list = new List<Vector2>();    // Client 이동 경로 포인트
    [SerializeField] public List<Vector2> _hostSpawnPositions = new List<Vector2>();  // Host 스폰 위치
    [SerializeField] public List<Vector2> _clientSpawnPositions = new List<Vector2>(); // Client 스폰 위치
    [SerializeField] public List<bool> Player_spawn_list_Array = new List<bool>();         // Host 스폰 위치 사용 여부
    [SerializeField] public List<bool> Other_spawn_list_Array = new List<bool>();       // Client 스폰 위치 사용 여부
    
    public Dictionary<string, UI_Spawn_Holder> Hero_Holders = new Dictionary<string, UI_Spawn_Holder>();
    public int[] Host_Client_Value_Index = new int[2];

    public static float xValue, yValue;

    public static event Action GridSpawned;

    private GameObject _mapSpawner;

    RateLimitCooldown m_RateLimitQuery;
    public void Awake(){}
    

 

    public void Initialize()
    {
        Debug.Log("[MapSpawnerFacade] Initialize 시작");
    
    }
    
    public void Load()
    {
            _mapSpawner = this.gameObject;

    }



    public void LoadMap()
    {
        Debug.Log("[MapSpawnerFacade] LoadMap 시작");
        if (_mapSpawner == null)
        {
            _mapSpawner = this.gameObject;
        }
        
        InitializeGridSystem(_mapSpawner);
    }


    
    private void InstantiateMap(GameObject mapPrefab, string mapName)
    {
        try
        {
            if (mapPrefab == null)
            {
                Debug.LogError("[MapSpawnerFacade] 맵 프리팹이 null입니다.");
                return;
            }
            ConfigureMapInstance();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapSpawnerFacade] 맵 인스턴스화 중 예외 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    #region 그리드 시스템
    private void InitializeGridSystem(GameObject mapInstance)
    {
        SetupSpawnGrids(mapInstance);
    }
    
    /// <summary>
    /// 스폰 그리드를 설정합니다.
    /// </summary>
    private void SetupSpawnGrids(GameObject mapInstance)
    {
        // 스폰 그리드 부모 찾기
        Transform playerGridParent = mapInstance.transform.Find("Spawner_Host");
        Transform enemyGridParent = mapInstance.transform.Find("Spawner_Client");

        if (playerGridParent == null || enemyGridParent == null)
        {
            Debug.LogWarning("[ObjectManagerFacade] 스폰 그리드 부모를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"<color=yellow>[ObjectManagerFacade] 그리드 설정 시작: {playerGridParent.name}, {enemyGridParent.name}</color>");
        // 그리드 초기화
        CreateSpawnGrid(playerGridParent, true);
        CreateSpawnGrid(enemyGridParent, false);

        Transform monsterGroundHost = playerGridParent.Find("MonsterGround_Host");
        Transform mosterGroundClient = enemyGridParent.Find("MonsterGround_Client");
        // 이동 경로 설정
        SetupMovePaths(monsterGroundHost, mosterGroundClient );
        
        Debug.Log("[ObjectManagerFacade] 그리드 설정 완료");
      }
    

    /// <summary>
    /// 스폰 그리드를 생성합니다.
    /// </summary>
    private void CreateSpawnGrid(Transform gridTransform, bool isPlayer)
    {
        // 그리드 크기 계산
        SpriteRenderer parentSprite = gridTransform.GetComponent<SpriteRenderer>();
        if (parentSprite == null)
        {
            Debug.LogError("[ObjectManagerFacade] 그리드 부모에 SpriteRenderer가 없습니다.");
            return;
        }
        

        float parentWidth = parentSprite.bounds.size.x;
        float parentHeight = parentSprite.bounds.size.y;
        Debug.Log($"<color=yellow>[ObjectManagerFacade] 스폰 그리드 생성 parentWidth : {parentWidth}, parentHeight : {parentHeight}, {isPlayer}</color>");

        // 그리드 셀 크기 계산 (6x3 그리드)
        float xCount = gridTransform.localScale.x / 6;
        float YCount = gridTransform.localScale.y / 3;


        xValue = xCount;
        yValue = YCount;


        // 그리드 셀 생성 (6x3 그리드)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                // 셀 위치 계산
                float xPos = (-parentWidth / 2) + (col * xCount) + (xCount / 2);
                float yPos = ((isPlayer ? parentHeight : -parentHeight) / 2) + ((isPlayer ? -1 : 1) * (row * YCount)) + (YCount / 2);


                switch (isPlayer)
                {
                    case true:
                        _hostSpawnPositions.Add(new Vector2(
                            xPos,
                            yPos + gridTransform.localPosition.y - YCount));
                        Player_spawn_list_Array.Add(false);
                        break;
                    case false:
                        _clientSpawnPositions.Add(new Vector2(
                            xPos,
                            yPos + gridTransform.localPosition.y));
                        Other_spawn_list_Array.Add(false);
                        break;
                }

                if (_networkManager.IsServer) //_networkManager.IsServer 이렇게 해야 먹힘 왜지?
                {
                    StartCoroutine(DelayHeroHolderSpawn(isPlayer));
                }
            }
        }
        Host_Client_Value_Index[0] = 0; //HOST
        Host_Client_Value_Index[1] = 0; //CLIENT
        Debug.Log($"[ObjectManagerFacade] {(isPlayer ? "Host" : "Client")} 스폰 포인트 {_hostSpawnPositions.Count}개 설정 완료");

        GridSpawned?.Invoke();
        
    }
    

    /// <summary>
    /// @param Player 지금 2인 전용임 True False 둘중 하나임
    /// Host_Client_Value_Index: 10, 0 각각 좌표표
    /// </summary>

    IEnumerator DelayHeroHolderSpawn(bool Player)
    {
        // Player = 지금 2인 전용임 True False 둘중 하나임
        Debug.Log($"<color=yellow>[MapSpawnerFacade] DelayHeroHolderSpawn: {Player}</color>"); //[MapSpawnerFacade] DelayHeroHolderSpawn: True
        Debug.Log($"<color=yellow>[MapSpawnerFacade] Host_Client_Value_Index: {Host_Client_Value_Index[0]}, {Host_Client_Value_Index[1]}</color>"); //[MapSpawnerFacade] Host_Client_Value_Index: 10, 0
        
        var go = Instantiate(_spawn_Holder);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();

        string temp = Player == true ? EOrganizer.HOST.ToString() :  EOrganizer.CLIENT.ToString();
        int value = Player == true ? 0 : 1;
        string Organizers = temp + Host_Client_Value_Index[value].ToString();
        Debug.Log($"<color=yellow>[MapSpawnerFacade] Organizers: {Organizers}</color>"); //[MapSpawnerFacade] Organizers: HOST7
        Host_Client_Value_Index[value]++;

        yield return new WaitForSeconds(0.5f);

        SpawnGridClientRpc(networkObject.NetworkObjectId, Organizers);
    }

    [ClientRpc]
    private void SpawnGridClientRpc(ulong networkID, string Organizers)
    {
        // Player = 지금 2인 전용임 True False 둘중 하나임

        Debug.Log($"<color=green>[MapSpawnerFacade] SpawnGridClientRpc: {networkID}, {Organizers}</color>");
        if (_netUtils.TryGetSpawnedObject_P(networkID, out NetworkObject holderNetworkObject))
        {
            bool isPlayer;

            if (Organizers.Contains("HOST"))
            {
                isPlayer = _netUtils.LocalID_P() == 0 ? true :false;
            }
            else isPlayer = _netUtils.LocalID_P() == 0? false : true;


            UI_Spawn_Holder goHolder = holderNetworkObject.GetComponent<UI_Spawn_Holder>();

            SetPositionHero( holderNetworkObject,
                isPlayer ? _hostSpawnPositions : _clientSpawnPositions,
                isPlayer ? Player_spawn_list_Array : Other_spawn_list_Array);

            Hero_Holders.Add(Organizers, goHolder);
            Debug.Log($"<color=green>[MapSpawnerFacade] Hero_Holders 딕셔너리 내용:</color>");
            foreach (var holder in Hero_Holders)
            {
                Debug.Log($"<color=green>  - Key: {holder.Key}</color>");
                Debug.Log($"<color=green>    - Holder_Part_Name: {holder.Value.Holder_Part_Name}</color>");
            }
            goHolder.Holder_Part_Name = Organizers;

        }
    }

    private void SetPositionHero(NetworkObject obj, List<Vector2> spawnList, List<bool> spawnArrayList)
        {

            int position_value = spawnArrayList.IndexOf(false);
            Debug.Log(spawnArrayList.Count);

            if (position_value != -1) 
            {
                spawnArrayList[position_value] = true;
                obj.transform.position = spawnList[position_value];
            
            }
            UI_Spawn_Holder holder = obj.GetComponent<UI_Spawn_Holder>();
            holder.index = position_value;
            Debug.Log(spawnList[position_value] + " : " + obj.transform.position);
        }

    /// <summary>
    /// 이동 경로를 설정합니다.
    /// </summary>
     private void SetupMovePaths(Transform playerGridParent, Transform enemyGridParent)
    {
        Player_move_list.Clear();
        Other_move_list.Clear();
        
        for (int i = 0; i < playerGridParent.childCount; i++)
        {
            Transform child = playerGridParent.GetChild(i);
            Player_move_list.Add(child.position);
            Debug.Log($"[ObjectManagerFacade] 호스트 이동 경로 추가: {child.name} at {child.position}");
        }

        for (int i = 0; i < enemyGridParent.childCount; i++)
        {
            Transform child = enemyGridParent.GetChild(i);
            Other_move_list.Add(child.position);
            Debug.Log($"[ObjectManagerFacade] 클라이언트 이동 경로 추가: {child.name} at {child.position}");
        }      
     }
    
    
  

    #endregion

    private void ConfigureMapInstance()
    {
        _mapSpawner.transform.localPosition = Vector3.zero;
        _mapSpawner.transform.localScale = new Vector3(1, 1, _mapSpawner.transform.localScale.z);
    }
  

    public GameObject GetMapInstance()
    {
        return _mapInstance;
    }

    /// <summary>
    /// 맵 인스턴스를 반환합니다.///////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public void Holder_Position_Set(string Value01, string Value02)
    {
        NetUtils.HostAndClientMethod(
            () => GetPositionSetServerRpc(Value01, Value02),
            () => GetPositionSet(Value01, Value02));

    }

    [ServerRpc(RequireOwnership = false)]
    private void GetPositionSetServerRpc(string Value01, string Value02)
    {
        GetPositionSet(Value01, Value02);
    }

    private void GetPositionSet(string Value01, string Value02)
    {

        GetPositionSetClientRpc(Value01, Value02);
    }

    [ClientRpc]
    private void GetPositionSetClientRpc(string Value01, string Value02)
    {
        UI_Spawn_Holder holder01 = Hero_Holders[Value01];
        UI_Spawn_Holder holder02 = Hero_Holders[Value02];

        holder01.HeroChange(holder02);
        holder02.HeroChange(holder01);

        (holder01.Holder_Name, holder02.Holder_Name) = (holder02.Holder_Name, holder01.Holder_Name);
        (holder01.m_Heroes, holder02.m_Heroes) = (new List<ServerHero>(holder02.m_Heroes), new List<ServerHero>(holder01.m_Heroes));
        // (holder01.m_Data, holder02.m_Data) = (holder02.m_Data, holder01.m_Data);

    }


}

