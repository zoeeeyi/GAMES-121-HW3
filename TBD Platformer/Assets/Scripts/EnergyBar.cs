using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnergyBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_energyText;
    Slider m_slider;
    GameManager m_gameManager;

    void Start()
    {
        m_slider = GetComponent<Slider>();
        m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        m_slider.maxValue = m_gameManager.GetMaxEnergyLevel();
        m_energyText.text = m_slider.maxValue.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        m_slider.value = m_gameManager.GetEnergyLevel();
        m_energyText.text = Mathf.Round(m_slider.value).ToString();
    }
}
