using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only for testing the WIM to demonstrate removed objects will be also removed in WIM
/// </summary>
public class SelfDestruction : MonoBehaviour
{

    private float curTime = 0.0f;

    private readonly float maxTime = 60.0f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        curTime += Time.deltaTime;
        if (curTime >= maxTime)
        {
            Destroy(gameObject);
        }
    }

}
