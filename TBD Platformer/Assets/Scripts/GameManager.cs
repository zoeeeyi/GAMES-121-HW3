using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    GameState m_gameState = GameState.Loading;
    public enum GameState
    {
        Loading,
        Running,
        Pause
    }

    void Start()
    {
        m_gameState = GameState.Running;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameState GetGameState()
    {
        return m_gameState;
    }
}
