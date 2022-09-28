using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Test : MonoBehaviour
{
    [Space]
    [Header("Wall Jump Properties")]
    //Wall jump parameters
    [SerializeField] bool m_wallJumpActivate = true;
    [HideIfGroup("m_wallJumpActivate")]
    [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 wallJumpClimb;
    [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 wallJumpOff;
    [BoxGroup("m_wallJumpActivate/Wall Jump")][SerializeField] Vector2 wallLeap;
    public float wallStickTime = 0.25f;//Specifically for wall leaping, we want to temporarily pause player on each leap so that they don't slide down

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
