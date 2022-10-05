using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StopWatch : MonoBehaviour
{
    TextMeshProUGUI stopWatchUI;
    float stopWatch;
    double stopWatchRounded;
    GameManager m_gameManager;
    bool m_gameStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        m_gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        stopWatchUI = GetComponent<TextMeshProUGUI>();
        stopWatch = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_gameStarted)
        {
            m_gameStarted = m_gameManager.GetGameStart();
            return;
        }

        stopWatch += Time.deltaTime;
        stopWatchRounded = System.Math.Round(stopWatch, 2);
        stopWatchUI.text = stopWatchRounded.ToString();
    }
}