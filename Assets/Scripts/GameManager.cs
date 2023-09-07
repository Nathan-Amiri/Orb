using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [SerializeReference] private MenuScene menuScene;

    private Lobby currentLobby;

    public static GameManager Instance = null;

    public enum ConnectionStatus { Connecting, Connected, Offline}
    [NonSerialized] public ConnectionStatus connectionStatus = ConnectionStatus.Connecting;

    private string gameScene; //PracticeScene, ChallengeScene, VersusScene

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        DontDestroyOnLoad(this);

        _ = ConnectToRelay();
    }

    private async Task ConnectToRelay() //run in Start
    {
        Debug.Log("Connecting...");

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            connectionStatus = ConnectionStatus.Connected;

            //This code only runs at the beginning of the game, so menuScene cannot be null
            StartCoroutine(menuScene.StartMenu(true));
        }
        catch
        {
            connectionStatus = ConnectionStatus.Offline;

            //This code only runs at the beginning of the game, so menuScene cannot be null
            StartCoroutine(menuScene.StartMenu(false));
        }
    }

    private IEnumerator HandleLobbyHeartbeat() //keep lobby active (lobbies are automatically hidden after 30 seconds of inactivity)
    {
        while (currentLobby != null)
        {
            SendHeartbeat();
            yield return new WaitForSeconds(15);
        }
    }
    private async void SendHeartbeat()
    {
        await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
    }

    public void PracticeLobby()
    {
        gameScene = "PracticeScene";
        NetworkManager.Singleton.StartHost();
    }

    public void ChallengeLobby()
    {
        gameScene = "ChallengeScene";
        NetworkManager.Singleton.StartHost();
    }

    public void VersusLobby()
    {
        gameScene = "VersusScene";
        FindLobby();
    }

    private async void FindLobby()
    {
        try
        {
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(null);
            if (queryResponse.Results.Count > 0) //if a lobby exists
            {
                if (queryResponse.Results[0].Data == null || !queryResponse.Results[0].Data.ContainsKey("JoinCode"))
                {
                    //Data is null when no data values exist, such as a JoinCode
                    //JoinCode is created when host is first connected to relay. It's possible to join the lobby before the relay connection
                    //is established and before JoinCode is created
                    Debug.Log("Lobby is still being created, trying again in 2 seconds");
                    Invoke(nameof(FindLobby), 2);
                    return;
                }

                currentLobby = await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
                Debug.Log("Joined Lobby!");

                string joinCode = currentLobby.Data["JoinCode"].Value;

                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

                NetworkManager.Singleton.StartClient();
            }
            else
                CreateLobby();

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void CreateLobby()
    {
        try
        {
            //lobby is public by default
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("NewLobby", 2); //number of players

            Debug.Log("Created Lobby");

            StartCoroutine(HandleLobbyHeartbeat());

            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(1); //number of non-host connections
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(hostAllocation, "dtls"));

            NetworkManager.Singleton.StartHost();

            // Set up JoinAllocation
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            //SaveJoinCodeInLobbyData
            try
            {
                //update currentLobby
                currentLobby = await Lobbies.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject> //JoinCode = S1
                    {
                        //only updates this piece of data--other lobby data remains unchanged
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode, DataObject.IndexOptions.S1) }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public override void OnNetworkSpawn()
    {
        ListenForClientDisconnect();

        if (IsServer)
            NetworkManager.Singleton.SceneManager.LoadScene(gameScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void BackToMainMenu() //called by BackToMainMenu
    {
        LeaveLobby();
        StartCoroutine(WaitForShutdown());
    }
    private IEnumerator WaitForShutdown()
    {
        //delaying the scene change gives time for the shutdown to occur. A temporary solution
        //for a terrible Unity bug in which the scene changing conflicts with the recent shutdown
        //to cause errors on both ends, despite NetworkManager.ShutdownInProgress returning false
        //the whole time.
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }

    public void ExitGame() //called by ExitGame
    {
        LeaveLobby();

        Application.Quit();
    }

    private async void LeaveLobby()
    {
        try
        {
            if (currentLobby != null)
            {
                if (IsServer)
                    await Lobbies.Instance.DeleteLobbyAsync(currentLobby.Id);
                else
                    await Lobbies.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }

            currentLobby = null; //avoid heartbeat errors in editor since playmode doesn't stop

            NetworkManager.Singleton.Shutdown();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public delegate void EnemyDisconnectedAction();
    public static event EnemyDisconnectedAction EnemyDisconnected;

    private void ListenForClientDisconnect() //called in OnNetworkSpawn
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }
    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        //if is host and is shutting down, OnClientDisconnect will be called for each connected
        //client that is being forcibly disconnected
        if (NetworkManager.Singleton.ShutdownInProgress) return;

        if (clientId != NetworkManager.Singleton.LocalClientId)
            EnemyDisconnected?.Invoke();
    }
}