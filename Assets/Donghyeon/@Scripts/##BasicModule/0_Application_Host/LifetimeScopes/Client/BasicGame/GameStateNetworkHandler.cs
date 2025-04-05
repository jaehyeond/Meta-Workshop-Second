using Unity.Netcode;
using UnityEngine;

public class GameStateNetworkHandler : NetworkBehaviour
{
    private BasicGameState _gameState;
    private bool _isInitialized = false;

    public void Initialize(BasicGameState gameState)
    {
        if (gameState == null)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화 실패: gameState가 null입니다!");
            return;
        }
        
        _gameState = gameState;
        _isInitialized = true;
        Debug.Log("[GameStateNetworkHandler] 초기화 완료");
    }

    /// <summary>
    /// 타이머 업데이트를 클라이언트에 전달
    /// </summary>
    [ClientRpc]
    public void UpdateTimerClientRpc(float timer)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 UpdateTimerClientRpc 호출됨");
            return;
        }
        
        _gameState.UpdateClientTimer(timer);
    }
    
    /// <summary>
    /// 웨이브 변경을 클라이언트에 전달
    /// </summary>
    [ClientRpc]
    public void WaveChangedClientRpc(int wave, float timer, bool isBossWave)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 WaveChangedClientRpc 호출됨");
            return;
        }
        
        _gameState.UpdateClientWave(wave, timer, isBossWave);
    }
    
    /// <summary>
    /// 돈 업데이트를 클라이언트에 전달
    /// </summary>
    [ClientRpc]
    public void UpdateMoneyClientRpc(int money)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 UpdateMoneyClientRpc 호출됨");
            return;
        }
        
        _gameState.UpdateClientMoney(money);
    }
    
    /// <summary>
    /// 몬스터 수 업데이트를 클라이언트에 전달
    /// </summary>
    [ClientRpc]
    public void UpdateMonsterCountClientRpc(int count)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 UpdateMonsterCountClientRpc 호출됨");
            return;
        }
        
        _gameState.UpdateClientMonsterCount(count);
    }
    
    /// <summary>
    /// 초기 상태를 클라이언트에 전달
    /// </summary>
    [ClientRpc]
    public void SyncInitialStateClientRpc(float timer, int wave, int money, int monsterCount, bool isBossWave)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 SyncInitialStateClientRpc 호출됨");
            return;
        }
        
        _gameState.SyncClientInitialState(timer, wave, money, monsterCount, isBossWave);
    }

// GameStateNetworkHandler.cs에 추가할 메서드
    [ClientRpc]
    public void SyncStateToClientRpc(float timer, int wave, int money, int monsterCount, bool isBossWave, ClientRpcParams clientRpcParams)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[GameStateNetworkHandler] 초기화되지 않은 상태에서 SyncStateToClientRpc 호출됨");
            return;
        }
        
        _gameState.SyncClientInitialState(timer, wave, money, monsterCount, isBossWave);
    }
}