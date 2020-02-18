using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeProducer : MonoBehaviour
{
    private float curTime = 0.0f;

    private readonly float maxTime = 5.0f;

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
            g.transform.localScale = new Vector3(5, 5, 5);
            g.transform.position += new Vector3(0, 30, 0);
            curTime = 0;
        }
    }
}
