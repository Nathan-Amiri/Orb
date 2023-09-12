using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Setup : NetworkBehaviour
{
    //assigned in prefab
    [SerializeField] private GameObject playerPref;
    [SerializeField] private GameObject enemyAIPref;
    [SerializeField] private GameObject orbPref;

    //assigned in scene
    [SerializeField] private Overlay overlay;
    [SerializeField] private GameObject waitingForOpponent;
    [SerializeField] private int requiredConnections;

    public enum GameMode { practice, challenge, versus};
    public static GameMode CurrentGameMode { get; private set; }
    [SerializeField] private GameMode gameMode; //set in inspector

    private readonly List<ulong> playerIDs = new();

    private void Awake() //start doesn't consistently run before OnNetworkSpawn
    {
        CurrentGameMode = gameMode;
    }

    public override void OnNetworkSpawn()
    {
        CheckAllPlayersConnectedServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CheckAllPlayersConnectedServerRpc(ulong clientId)
    {
        //wait until all players have loaded into the scene
        playerIDs.Add(clientId);
        if (playerIDs.Count != requiredConnections) return;


        foreach (ulong id in playerIDs)
            SpawnPlayer(id, false);

        if (CurrentGameMode == GameMode.challenge)
            SpawnPlayer(NetworkManager.Singleton.LocalClientId, true);
    }

    private void SpawnPlayer(ulong ownerId, bool spawnEnemyAI) //only run on the server
    {
        GameObject spawnPref = spawnEnemyAI ? enemyAIPref : playerPref;

        Vector2 spawnPosition = Vector2.zero; //if practice, spawn in center
        if (CurrentGameMode == GameMode.challenge)
        {
            //player spawns on left, enemyAI spawns on right
            if (!spawnEnemyAI)
                spawnPosition = new Vector2(-7, 0);
            else
                spawnPosition = new Vector2(7, 0);
        }
        else if (CurrentGameMode == GameMode.versus)
        {
            //host spawns on left, client spawns on right
            if (ownerId == NetworkManager.Singleton.LocalClientId)
                spawnPosition = new Vector2(-7, 0);
            else
                spawnPosition = new Vector2(7, 0);
        }

        GameObject playerObj = Instantiate(spawnPref, spawnPosition, Quaternion.identity);
        if (ownerId != default)
            playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId, true);
        else //if enemyAI
            playerObj.GetComponent<NetworkObject>().Spawn(true);

        NetworkBehaviourReference[] orbs = new NetworkBehaviourReference[4]
        {
            SpawnOrb(Orb.OrbColor.red, Color.red),
            SpawnOrb(Orb.OrbColor.blue, Color.blue),
            SpawnOrb(Orb.OrbColor.yellow, Color.yellow),
            SpawnOrb(Orb.OrbColor.green, Color.green)
        };

        PlayerSetupClientRpc(playerObj.GetComponent<Player>(), orbs);
    }

    private Orb SpawnOrb(Orb.OrbColor orbColor, Color spriteColor) //only run on the server
    {
        GameObject orbObj = Instantiate(orbPref, new Vector2(-15, 0), Quaternion.identity);
        orbObj.name = orbColor.ToString();
        orbObj.GetComponent<NetworkObject>().Spawn(true);

        Orb orb = orbObj.GetComponent<Orb>();
        OrbSetupClientRpc(orb, orbColor, spriteColor);

        return orb;
    }

    [ClientRpc]
    private void OrbSetupClientRpc(NetworkBehaviourReference reference, Orb.OrbColor orbColor, Color spriteColor)
    {
        Orb orb = GetFromReference.GetOrb(reference);

        orb.color = orbColor;
        orb.sr.color = spriteColor;

        if (waitingForOpponent != null) //true in versus mode
            waitingForOpponent.SetActive(false);
    }

    [ClientRpc]
    private void PlayerSetupClientRpc(NetworkBehaviourReference playerReference, NetworkBehaviourReference[] orbReferences)
    {
        Player player = GetFromReference.GetPlayer(playerReference);

        Orb[] orbs = new Orb[4];
        for (int i = 0; i < 4; i++)
            orbs[i] = GetFromReference.GetOrb(orbReferences[i]);

        player.redOrbs.Add(orbs[0]);
        player.blueOrbs.Add(orbs[1]);
        player.yellowOrbs.Add(orbs[2]);
        player.greenOrbs.Add(orbs[3]);

        if (CurrentGameMode == GameMode.practice)
            PlayerInput.stunned = false;
        else
            overlay.StartCountdown();
    }
}