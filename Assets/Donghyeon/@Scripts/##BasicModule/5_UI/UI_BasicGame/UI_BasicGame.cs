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
using UnityEngine.EventSystems;
using Unity.Assets.Scripts.Objects;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Assets.Scripts.UI
{
    /// <summary>
    /// 시작 화면 UI 관리 클래스
    /// </summary>
    public partial class UI_BasicGame : UI_Scene
    {
        #region Enums
        
        enum Texts
        {
       
            Timer_T,
     
        }
        
        enum Images
        {
        }
        
        enum GameObjects
        {
            Main
        }

        enum Buttons
        {
       
        }


        #endregion


        [Inject] private BasicGameState _basicGameState;
        


        private float _elapsedTime = 0.0f;
        private float _updateInterval = 1.0f;

        #region Properties
        


        private GameObject MainObject => GetObject((int)GameObjects.Main);
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

   
            Refresh();

            return true;
        }
        

        void Refresh()
        {
            if (_init == false)
                return;
        }


        private void Update()
        {
            // int monsterCount = _objectManager.MonsterRoot.childCount;
            // setMonsterCount( monsterCount);
            // GetText((int)Texts.MonsterCount_T).text = monsterCount.ToString() + "/" + _basicGameState.MonsterLimitCount.ToString();
            // GetImage((int)Images.Monster_Count_Fill).fillAmount = (float)monsterCount / _basicGameState.MonsterLimitCount;
            // GetText((int)Texts.Money_T).text = _basicGameState.Money.ToString();
            // GetText((int)Texts.Wave_T).text = "Wave " + _basicGameState.Wave.ToString();
            // Game_Mng.instance.GetMoney(1);

            // GetText((int)Texts.Summon_T).text = _basicGameManager.SummonCount.ToString();
            // GetText((int)Texts.Upgrade_Money_T).text = _basicGameManager.UpgradeMoney.ToString();
            GetText((int)Texts.Timer_T).text =  UpdateTimerText();
        
        

       
            // Debug.Log($"monsterCount: {monsterCount}");
            // _elapsedTime += Time.deltaTime;

            // if (_elapsedTime >= _updateInterval)
            // {
            //     float fps = 1.0f / Time.deltaTime;
            //     float ms = Time.deltaTime * 1000.0f;
            //     string text = string.Format("{0:N1} FPS ({1:N1}ms)", fps, ms);
            //     GetText((int)Texts.GoldCountText).text = text;

            //     _elapsedTime = 0;
            // }
        }

        string UpdateTimerText()
        {
            float timer = _basicGameState.Timer;            
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);
            return $"{minutes:00}:{seconds:00}";  // 공백 제거
        }

        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
        }
        
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents(); // 부모 클래스의 구현 호출

        }

        public static event Action OnSummonButtonRequested;

        private void OnClickSummonButton(PointerEventData evt)
        {
            OnSummonButtonRequested?.Invoke();
            Debug.Log("[UI_BasicGame] OnClickSummonButton");
        }

        private void OnClickUpgradeButton(PointerEventData evt)
        {
            Debug.Log("[UI_BasicGame] OnClickUpgradeButton");
            UI_Upgrade_Popup existingPopup = uiManager.FindPopup<UI_Upgrade_Popup>();
            if (existingPopup == null)
            {
                UI_Upgrade_Popup popup = uiManager.ShowPopupUI<UI_Upgrade_Popup>();
                popup.SetInfo();
            }
            else
            {
                Debug.Log("[UI_BasicGame] Upgrade popup is already open");
            }
        }

        protected override void OnDestroy()
        {
            Debug.Log("[UI_MainMenu] OnDestroy 메서드 호출됨");
            UnsubscribeEvents();
            
            // 부모 클래스의 OnDestroy 호출
            base.OnDestroy();
        }
        

        
        #endregion

    }
}

