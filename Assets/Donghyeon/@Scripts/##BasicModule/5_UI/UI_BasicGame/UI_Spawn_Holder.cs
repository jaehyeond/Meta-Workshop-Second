using System;
using System.Collections.Generic;
using Unity.Assets.Scripts.Objects;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;





public class UI_Spawn_Holder : UI_Base
{

        #region Enums
        
        enum Texts
        {
            Composition_T,
            Sell_T
        }
        
        enum Images
        {
        }
        
        enum GameObjects
        {
            Range_o,

            Bottom_Ca
        }

        enum Buttons
        {
            Sell_B,
            Composition_B,
            Set_o,
            Get_o,
        }

     

     
        #endregion
	private static UI_Spawn_Holder s_instance;

	private static UI_Spawn_Holder Instance { get { Initialize(); return s_instance; } }


    [SerializeField] private ServerHero _serverHero;

    [Inject] private ObjectManager _objectManager;
    [Inject] private DebugClassFacade _debugClassFacade;
    [Inject] private NetworkManager _networkManager; 
    [Inject] private NetUtils _netUtils;
    public string Holder_Part_Name;
    public int Holder_Name;
    public List<ServerHero> m_Heroes = new List<ServerHero>();
    public int index;
    // public HeroData m_Data;

    public readonly Vector2[] One = { Vector2.zero };
    public readonly Vector2[] Two =
        {
        new Vector2(-0.1f, 0.05f),
        new Vector2(0.1f, -0.1f)
    };
    public readonly Vector2[] Three =
        {
        new Vector2(-0.1f, 0.1f),
        new Vector2(0.1f, -0.05f),
        new Vector2(-0.15f, -0.15f)
    };


    public static void Initialize(){}
    

	
    
      public override bool Init()
        {
            if (base.Init() == false)
                return false;

            BindTexts(typeof(Texts));
            BindImages(typeof(Images));
            BindObjects(typeof(GameObjects));
            BindButtons(typeof(Buttons));

            GetButton((int)Buttons.Sell_B).gameObject.BindEvent(OnClickSellButton);
            GetButton((int)Buttons.Composition_B).gameObject.BindEvent(OnClickCompositionButton);

            
            Refresh();

            return true;
        }
        

        void Refresh()
        {
            if (_init == false)
                return;
        }


    private void OnClickSellButton(PointerEventData evt){
        Debug.Log("OnClickSellButton");   
    }

    private void OnClickCompositionButton(PointerEventData evt){
        Debug.Log("OnClickCompositionButton");
    }

    private void OnClickGetButton(PointerEventData evt){
        Debug.Log("OnClickGetButton");
    }

    private void OnClickSetButton(PointerEventData evt){
        Debug.Log("OnClickSetButton");
    }



    private void Start()
    {
        MakeCollider();

        // SellButton.onClick.AddListener(() => Sell());
        // CompositionButton.onClick.AddListener(() => Composition());
    }

    #region SELL
    public void Sell(bool GetNavigation = true)
    {
        // if (GetNavigation)
        // {
        //     UI_Main.instance.GetNavigation(string.Format("gg. {0}{1}",
        //      Net_Utils.RarityColor(m_Heroes[0].HeroRarity),
        //      m_Heroes[0].HeroName));
        // }
        // Net_Utils.HostAndClientMethod(
        //     () => SellServerRpc(Net_Utils.LocalID()),
        //     () => SellCharacter(Net_Utils.LocalID()));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SellServerRpc(ulong clientID)
    {
        SellCharacter(clientID);
    }

    private void SellCharacter(ulong clientID)
    {
        var hero = m_Heroes[m_Heroes.Count - 1];
        ulong heroId = hero.NetworkObjectId;
        NetworkObject obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[heroId];
        SellClientRpc(heroId, clientID);
        obj.Despawn();
    }

    [ClientRpc]
    private void SellClientRpc(ulong heroKey, ulong clientID)
    {
        var obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[heroKey];
        m_Heroes.Remove(obj.GetComponent<ServerHero>());
        if (m_Heroes.Count == 0)
        {
            DestroyServerRpc(clientID);
        }
        CheckGetPosition();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyServerRpc(ulong clientID)
    {
        DestroyClientRpc(clientID);
        Holder_Name = 0;
    }

    [ClientRpc]
    private void DestroyClientRpc(ulong clientID)
    {
        // if (Net_Utils.IsClientCheck(clientID))
        // {
        //     Spawner.Player_spawn_list_Array[index] = false;
        // }
        // else
        // {
        //     Spawner.Other_spawn_list_Array[index] = false;
        // }
    }
    #endregion

    public void Composition()
    {
        List<UI_Spawn_Holder> holders = new List<UI_Spawn_Holder>();

        holders.Add(this);

        // foreach(var data in Spawner.instance.Hero_Holders)
        // {
        //     if(data.Value.Holder_Name == Holder_Name && data.Value != this)
        //     {
        //         string temp = Net_Utils.LocalID() == (ulong)0 ? "HOST" : "CLIENT";
                
        //         if(data.Value.Holder_Part_Name.Contains(temp))
        //             holders.Add(data.Value);
        //     }
        // }
        // int cnt = 0;
        // string[] holderTemp = new string[2];
        // bool GetBreak = false;
        // for(int i = 0; i < holders.Count; i++)
        // {
        //     for (int j = 0; j < holders[i].m_Heroes.Count; j++)
        //     {
        //         if (holders[i].m_Heroes.Count > 0)
        //         {
        //             holderTemp[cnt] = holders[i].Holder_Part_Name;
        //             cnt++;
        //             if (cnt >= 2)
        //             {
        //                 GetBreak = true;
        //                 break;
        //             }
        //         }
        //     }
        //     if (GetBreak) break;
        // }
        // for (int i = 0; i < holderTemp.Length; i++)
        // {
        //     if (holderTemp[i] == "" || holderTemp[i] == null)
        //     {
        //         return;
        //     }

        // }
        // for (int i = 0; i < holderTemp.Length; i++) Spawner.instance.Hero_Holders[holderTemp[i]].Sell(false);
        // ReturnRange();
        // Spawner.instance.Summon("UnCommon");
    }

    public void HeroChange(UI_Spawn_Holder holder)
    {
        List<Vector2> poss = new List<Vector2>();
        switch(m_Heroes.Count)
        {
            case 1: poss = new List<Vector2>(One); break;
            case 2: poss = new List<Vector2>(Two); break;
            case 3: poss = new List<Vector2>(Three); break;
        }

        for(int i = 0; i < poss.Count; i++)
        {
            Vector2 worldPosition = holder.transform.TransformPoint(poss[i]);
            poss[i] = worldPosition;
        }

        for (int i = 0; i < m_Heroes.Count; i++)
        {
            // m_Heroes[i].Position_Change(holder, poss, i);
        }
    }

    public void GetRange()
    {
        Debug.Log("[UI_Spawn_Holder] GetRange 호출됨");
        
        // 서버 히어로가 있는 경우 AtkRange 값을 사용
        float range = 2.0f; // 기본값
        
        if (m_Heroes.Count > 0 && m_Heroes[0] != null)
        {
            range = m_Heroes[0].AtkRange.Value;
            Debug.Log($"[UI_Spawn_Holder] 히어로 범위: {range}");
        }
        
        // Range 오브젝트 활성화
        GameObject rangeObj = GetObject((int)GameObjects.Range_o);
        if (rangeObj != null)
        {
            rangeObj.SetActive(true);
            rangeObj.transform.localScale = new Vector3(range, range, 1.0f);
            Debug.Log($"[UI_Spawn_Holder] 범위 시각화 설정 완료: {range}");
        }

    }
    public void ReturnRange()
    {

        GameObject rangeObj = GetObject((int)GameObjects.Range_o);
        if (rangeObj != null)
        {
            rangeObj.SetActive(false);
            Debug.Log("[UI_Spawn_Holder] 범위 시각화 비활성화");
        }

    }

    private void MakeCollider()
    {
        // 홀더용 콜라이더 생성 및 설정
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // 콜라이더 크기 설정
        circleCollider.radius = 1.0f;
        circleCollider.isTrigger = true;
        
        // Debug.Log($"<color=blue>[UI_Spawn_Holder] {name}에 CircleCollider2D 생성됨</color>");
    }

   

    public void CheckGetPosition()
    {
        UpdateColliderSize();

        for(int i = 0; i < m_Heroes.Count; i++)
        {
            m_Heroes[i].transform.localPosition = Hero_Vector_Pos(m_Heroes.Count)[i];
            _debugClassFacade?.LogInfo(GetType().Name, $"<color=green>[UI_Spawn_Holder] 영웅 {i} 위치 설정: {m_Heroes[i].transform.localPosition}, 홀더 위치: {transform.position}</color>");

        }
    }
    private void UpdateColliderSize()
    {
        if (m_Heroes.Count > 0 && m_Heroes[0] != null)
        {
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = m_Heroes[0].AtkRange.Value;
                Debug.Log($"[UI_Spawn_Holder] 콜라이더 크기 업데이트: 반지름={collider.radius}");
            }
        }
    }


    private Vector2[] Hero_Vector_Pos(int count)
    {
        switch(count)
        {
            case 1: return One;
            case 2: return Two;
            case 3: return Three;
        }
        return null;
    }
    

}
