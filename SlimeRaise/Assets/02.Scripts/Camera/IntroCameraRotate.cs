using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroCameraRotate : MonoBehaviour
{
    [HideInInspector] public bool m_isRotate;
    float i = 0;
    Transform m_cameraTrm;
    private void Start()
    {
        i = -41.9f; m_isRotate = true;
        m_cameraTrm = transform;
    }

    private void Update()
    {
        if(m_isRotate)
        {
            i+= 0.001f;
            m_cameraTrm.rotation = Quaternion.Euler(45,i,0);
        }
    }
}
