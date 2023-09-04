using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScene : MonoBehaviour
{
    [SerializeField] private Button practiceButton;
    [SerializeField] private Button challengeButton;
    [SerializeField] private Button versusButton;

    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private GameObject resolution;

    [SerializeField] private TMP_Text console;

    public void Connected() //run by GameManager
    {
        resolution.SetActive(true);
        console.text = "Select game mode";
        ToggleButtonsInteractable(true);
    }

    private void ToggleButtonsInteractable(bool interactable)
    {
        practiceButton.interactable = interactable;
        challengeButton.interactable = interactable;
        versusButton.interactable = interactable;
    }

    //NetworkManager.Singleton.SceneManager.LoadScene("Level" + level, UnityEngine.SceneManagement.LoadSceneMode.Single);


    public void SelectNewResolution()
    {
        switch (resolutionDropdown.value)
        {
            case 0:
                Screen.SetResolution(1920, 1080, true);
                break;
            case 1:
                Screen.SetResolution(1280, 720, true);
                break;
            case 2:
                Screen.SetResolution(1366, 768, true);
                break;
            case 3:
                Screen.SetResolution(1600, 900, true);
                break;
        }
        PlayerPrefs.SetInt("Resolution", resolutionDropdown.value);
    }
}