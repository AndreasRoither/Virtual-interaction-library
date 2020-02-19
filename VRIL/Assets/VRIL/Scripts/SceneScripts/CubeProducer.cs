using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only for testing the WIM to demonstrate new objects will be applied to the WIM
/// </summary>
public class CubeProducer : MonoBehaviour
{
    private float curTime = 0.0f;

    private readonly float maxTime = 3.0f;

    public GameObject Prefab;

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
            GameObject g = Instantiate(Prefab);
            g.AddComponent<SelfDestruction>();
            g.transform.localScale = new Vector3(4, 4, 4);
            g.transform.position += new Vector3(0, 30, 0);
            curTime = 0;
        }
    }
}
