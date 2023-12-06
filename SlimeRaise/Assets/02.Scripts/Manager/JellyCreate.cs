using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyCreate : Singleton<JellyCreate>
{
    [SerializeField] GameObject Jelly;
    AuthManager auth;
    private void Start()
    {
        auth = GameObject.Find("AuthManager").GetComponent<AuthManager>();
        for (int i = 0; i < 1000; i++)
        {
            CreateJelly();
        }
        StartCoroutine(SpawnJelly());
    }

    IEnumerator SpawnJelly()
    {
        while (true)
        {
            CreateJelly();
            if(auth.EventText == "Jelly")
            {
                CreateJelly();
                CreateJelly();
                CreateJelly();
                CreateJelly();
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    public void CreateJelly() => Instantiate(Jelly, new Vector3(Random.Range(-100, 100), 50,
                Random.Range(-100, 100)), Quaternion.identity);
}
