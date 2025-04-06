using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Assets.Scripts.Scene;
using VContainer;
using Unity.Assets.Scripts.Resource;
using Object = UnityEngine.Object;
using Unity.Netcode;
using Unity.Assets.Scripts.UI;
using Unity.Assets.Scripts.Objects;
using VContainer.Unity;
using System;
using static Define;


namespace Unity.Assets.Scripts.Scene
{
public class BasicGameScene : BaseScene
{

	// [Inject] private ServerMonster _serverMonster; // MonoBehaviour는 이런 방식으로 주입받을 수 없습니다.
	
    // VContainer.IObjectResolver 추가

    [Inject] private UIManager _uiManager;
	// greenslime 몬스터 ID
	
	// 스폰된 몬스터 관리 리스트

    // OnGridSpawned 이벤트가 너무 일찍 호출되는 것을 방지하기 위한 플래그
    private bool _isInitialized = false;
	
    private bool _isEventsSubscribed = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = EScene.BasicGame;
 
      	_uiManager.ShowBaseUI<UI_Joystick>();

        return true;
    }

    public override void Clear()
    {

    }

    private void OnEnable()
    {
        // 이벤트 구독은 Init에서만 수행
    }

    private void OnDisable()
    {
        // 이벤트 해제는 Clear에서만 수행
    }

    private void SubscribeEvents()
    {

    }

    private void UnsubscribeEvents()
    {

    }

 


}

}