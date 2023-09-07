using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScene : MonoBehaviour
{
    [SerializeField] private GameObject connectingCanvas;
    [SerializeField] private TMP_Text connectingConsole;

    [SerializeField] private Button practiceButton;
    [SerializeField] private Button challengeButton;
    [SerializeField] private Button versusButton;

    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [SerializeField] private TMP_Text console;

    private void Start()
    {
        //at Start, connectionStatus is Connecting when game starts, and is Connected or Offline when returning to main menu from game scene
        //if connecting, wait for GameManager to call StartMenu
        if (GameManager.Instance.connectionStatus == GameManager.ConnectionStatus.Connected)
            Connected();
        else if (GameManager.Instance.connectionStatus == GameManager.ConnectionStatus.Offline)
            StartOfflineMode();
    }
    
    public IEnumerator StartMenu(bool connected) //run by GameManager
    {
        connectingConsole.text = connected ? "Connected!" : "Connection failed";

        yield return new WaitForSeconds(1);

        if (connected)
            Connected();
        else
            StartOfflineMode();
    }

    private void Connected()
    {
        connectingCanvas.SetActive(false);
        console.text = "Select game mode";
        ToggleButtonsInteractable(true);
    }

    private void StartOfflineMode()
    {
        connectingCanvas.SetActive(false);
        console.text = "In offline mode. To play Versus, check your internet connection and restart the game";
        ToggleButtonsInteractable(true);
    }

    private void ToggleButtonsInteractable(bool interactable)
    {
        practiceButton.interactable = interactable;
        //challengeButton.interactable = interactable;
        versusButton.interactable = interactable && GameManager.Instance.connectionStatus == GameManager.ConnectionStatus.Connected;
    }

    public void SelectPractice()
    {
        ToggleButtonsInteractable(false);

        GameManager.Instance.PracticeLobby();
    }
    public void SelectChallenge()
    {
        ToggleButtonsInteractable(false);

        GameManager.Instance.ChallengeLobby();
    }
    public void SelectVersus()
    {
        console.text = "Loading...";
        ToggleButtonsInteractable(false);

        GameManager.Instance.VersusLobby();
    }

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