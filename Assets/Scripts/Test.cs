using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    bool test;
    public void SelectStart()
    {
        NetworkManager.Singleton.StartClient();
    }

    private void Update()
    {
        if (!test && NetworkManager.Singleton.IsClient)
        {
            test = true;
            Destroy(gameObject);
        }
    }
}