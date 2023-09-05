using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{
    public void SelectMainMenu()
    {
        GameManager.Instance.BackToMainMenu();
    }
}