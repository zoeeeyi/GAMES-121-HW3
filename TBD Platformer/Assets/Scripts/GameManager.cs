using CustomPlatformerPhysics2D;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] PlayerController m_playerController;

    [Title("Energy Settings")]
    [SerializeField] float m_maxEnergy;
    [SerializeField] float m_energyReduceMult;
    [SerializeField] float m_energyRecoveryFreefall;
    [SerializeField] float m_energyRecoverySlide;
    float m_currentEnergy;

    [Title("Speed Settings")]
    [SerializeField] float m_maxAllowedVelocityY;
    float m_currentVelocityY;

    //General
    [SerializeField] TextMeshProUGUI m_gameOverText;
    bool m_gameOver = false;
    bool m_gameInitialized = false;

    void Start()
    {
        m_gameOverText.gameObject.SetActive(false);
        m_currentEnergy = m_maxEnergy;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (m_gameOver)
        {
            m_gameOverText.gameObject.SetActive(true);
            return;
        }

        m_currentVelocityY = m_playerController.GetLastDisplacement().y;

        m_currentEnergy -= Mathf.Max(m_currentVelocityY, 0) * m_energyReduceMult;

        if (m_currentVelocityY < 0)
        {
            if (m_playerController.getCollisionInfo().left || m_playerController.getCollisionInfo().right)
            {
                m_currentEnergy += Mathf.Abs(m_currentVelocityY) * m_energyRecoverySlide;
            } else
            {
                m_currentEnergy += Mathf.Pow(Mathf.Abs(m_currentVelocityY), 2) * m_energyRecoveryFreefall;
            }
        }

        if (m_currentEnergy <= 0)
        {
            SetGameOver("Ran Out of Energy!");
        }

        m_currentEnergy = Mathf.Clamp(m_currentEnergy, 0, m_maxEnergy);
    }

    public void SetEnergyLevel(float _e)
    {
        Mathf.Clamp(_e, 0, m_maxEnergy);
        m_currentEnergy = _e;
    }

    public float GetEnergyLevel()
    {
        return m_currentEnergy;
    }

    public float GetMaxEnergyLevel()
    {
        return m_maxEnergy;
    }

    public float GetMaxAllowedYVelocity()
    {
        return m_maxAllowedVelocityY;
    }

    public float GetCurrentYVelocity()
    {
        return m_currentVelocityY;
    }

    public void SetGameOver(string _msg)
    {
        this.m_gameOver = true;
        m_gameOverText.text = "Game Over!\n" + _msg;
    }

    public void SetGameWin()
    {
        this.m_gameOver = true;
        m_gameOverText.text = "Destination Reached!";
    }

    public bool GetGameOver()
    {
        return m_gameOver;
    }

    public void SetGameStart()
    {
        m_gameInitialized = true;
    }

    public bool GetGameStart()
    {
        return m_gameInitialized;
    }
}
