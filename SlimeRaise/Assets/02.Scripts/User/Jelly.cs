using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : MonoBehaviour
{
    void Update()
    {
        if(transform.position.y < -20)
            Destroy(gameObject);
    }
}
