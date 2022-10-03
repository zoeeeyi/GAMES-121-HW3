using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedBar : MonoBehaviour
{
    Slider m_slider;
    GameManager m_gameManager;
    float m_speedTarget;
    float m_currentSpeed = 0;
    float m_currentSpeedSmooth;
    [SerializeField] float m_currentSpeedSmoothTime;

    void Start()
    {
        m_slider = GetComponent<Slider>();
        m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        m_slider.maxValue = m_gameManager.GetMaxAllowedYVelocity();
        m_slider.value = 0;
    }

    // Update is called once per frame
    void Update()
    {
        m_speedTarget = Mathf.Abs(Mathf.Min(m_gameManager.GetCurrentYVelocity(), 0));
        m_currentSpeed = Mathf.SmoothDamp(m_currentSpeed, m_speedTarget, ref m_currentSpeedSmooth, m_currentSpeedSmoothTime);
        m_slider.value = m_currentSpeed;
    }
}
