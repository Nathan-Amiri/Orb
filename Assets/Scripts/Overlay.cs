using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class Overlay : NetworkBehaviour
{
    [SerializeField] private TMP_Text console;
    [SerializeField] private GameObject playAgain;

    private bool enemyDisconnected;

    private int playersPlayingAgain; //server only

    public delegate void StartGameAction();
    public static event StartGameAction StartGame;

    private void OnEnable()
    {
        Explosion.EndGame += OnGameEnd;
        GameManager.EnemyDisconnected += EnemyDisconnected;
    }
    private void OnDisable()
    {
        Explosion.EndGame -= OnGameEnd;
        GameManager.EnemyDisconnected -= EnemyDisconnected;
    }

    public void StartCountdown()
    {
        //countdown must be started in this MonoBehaviour (not in Setup) so that StopAllCoroutines can stop it
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown() //called by Setup
    {
        string[] count = { "2", "1", "Go!" };
        yield return new WaitForSeconds(.3f);

        console.text = "3";
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(.9f);
            console.text = count[i];
        }

        PlayerInput.stunned = false;
        StartGame?.Invoke();

        yield return new WaitForSeconds(1.2f);
        console.text = "";
    }

    private void OnGameEnd(bool isWinner)
    {
        if (enemyDisconnected) return;

        console.text = isWinner ? "The enemy has exploded!" : "You have exploded.";

        StopAllCoroutines(); //in case of practice
        StartCoroutine(ClearText());
    }

    private IEnumerator ClearText()
    {
        yield return new WaitForSeconds(2f);


        console.text = "Play again?";
        playAgain.SetActive(true);
    }

    public void SelectPlayAgain()
    {
        playAgain.SetActive(false);

        if (Setup.CurrentGameMode == Setup.GameMode.challenge)
            NetworkManager.SceneManager.LoadScene("ChallengeScene", LoadSceneMode.Single);
        else //if versus
        {
            console.text = "Waiting for opponent...";
            VersusPlayAgainServerRpc();
        }
    }

    [ServerRpc (RequireOwnership = false)]
    private void VersusPlayAgainServerRpc()
    {
        if (enemyDisconnected) return; //check in case enemy disconnects while rpc is arriving

        playersPlayingAgain++;
        if (playersPlayingAgain == NetworkManager.ConnectedClients.Count)
            NetworkManager.SceneManager.LoadScene("VersusScene", LoadSceneMode.Single);
    }

    private void EnemyDisconnected()
    {
        StopAllCoroutines();

        enemyDisconnected = true;
        playAgain.SetActive(false);
        console.text = "Enemy has disconnected.";
    }
}