using UnityEngine;

public class SnakeGameStarter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // 매니저 초기화
        Managers.Init();
        
        // 데이터 매니저 초기화
        Managers.Data.Init();
        
        // Snake 게임 초기화
        Managers.Game.InitSnakeGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
