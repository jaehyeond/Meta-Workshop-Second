using UnityEngine;
using Unity.Netcode;

public class SnakeNetworkHandler : MonoBehaviour
{
    private NetworkVariable<int> _networkScore = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _networkSize = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<NetworkString> _networkPlayerId = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> _networkHeadValue = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void InitializeNetworkVariables(string playerId, int initialScore, int initialSize, int initialHeadValue)
    {
        _networkPlayerId.Value = playerId;
        _networkScore.Value = initialScore;
        _networkSize.Value = initialSize;
        _networkHeadValue.Value = initialHeadValue;
    }

    public void UpdateScore(int value)
    {
        _networkScore.Value += value;
    }

    public int GetNetworkSize()
    {
        return _networkSize.Value;
    }
} 