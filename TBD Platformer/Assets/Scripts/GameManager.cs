using CustomPlatformerPhysics2D;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
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

    void Start()
    {
        m_currentEnergy = m_maxEnergy;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
                m_currentEnergy += Mathf.Abs(m_currentVelocityY) * m_energyRecoveryFreefall;
            }
        }

        m_currentEnergy = Mathf.Clamp(m_currentEnergy, 0, m_maxEnergy);
        Debug.Log(m_currentVelocityY);
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
}
