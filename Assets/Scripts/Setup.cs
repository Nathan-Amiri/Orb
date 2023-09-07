using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Setup : NetworkBehaviour
{
    //assigned in prefab
    [SerializeField] private GameObject playerPref;
    [SerializeField] private GameObject orbPref;

    //assigned in scene
    [SerializeField] private Overlay overlay;
    [SerializeField] private GameObject waitingForOpponent;
    [SerializeField] private int requiredConnections;

    public enum GameMode { practice, challenge, versus};
    public static GameMode CurrentGameMode { get; private set; }
    [SerializeField] private GameMode gameMode; //set in inspector

    private readonly List<ulong> playerIDs = new();

    private void Start()
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
        {
            Vector2 spawnPosition = Vector2.zero;
            if (CurrentGameMode == GameMode.versus)
            {
                //host spawns on left, client spawns on right
                if (id == NetworkManager.Singleton.LocalClientId)
                    spawnPosition = new Vector2(-7, 0);
                else
                    spawnPosition = new Vector2(7, 0);
            }

            GameObject playerObj = Instantiate(playerPref, spawnPosition, Quaternion.identity);
            playerObj.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

            NetworkBehaviourReference[] orbs = new NetworkBehaviourReference[4]
            {
                SpawnOrb(id, Orb.OrbColor.red, Color.red),
                SpawnOrb(id, Orb.OrbColor.blue, Color.blue),
                SpawnOrb(id, Orb.OrbColor.yellow, Color.yellow),
                SpawnOrb(id, Orb.OrbColor.green, Color.green)
            };

            PlayerSetupClientRpc(playerObj.GetComponent<Player>(), orbs);
        }
    }

    private Orb SpawnOrb(ulong ownerId, Orb.OrbColor orbColor, Color spriteColor)
    {
        GameObject orbObj = Instantiate(orbPref, new Vector2(-15, 0), Quaternion.identity);
        orbObj.name = orbColor.ToString();
        orbObj.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId, true);

        Orb orb = orbObj.GetComponent<Orb>();
        OrbSetupClientRpc(orb, orbColor, spriteColor);

        return orb;
    }

    [ClientRpc]
    private void OrbSetupClientRpc(NetworkBehaviourReference reference, Orb.OrbColor orbColor, Color spriteColor)
    {
        if (!reference.TryGet(out Orb orb))
        {
            Debug.LogError("Received invalid orb reference");
            return;
        }

        orb.color = orbColor;
        orb.sr.color = spriteColor;

        if (waitingForOpponent != null)
            waitingForOpponent.SetActive(false);
    }

    [ClientRpc]
    private void PlayerSetupClientRpc(NetworkBehaviourReference playerReference, NetworkBehaviourReference[] orbReferences)
    {
        if (!playerReference.TryGet(out Player player))
        {
            Debug.LogError("Received invalid player reference");
            return;
        }

        Orb[] orbs = new Orb[4];
        for (int i = 0; i < 4; i++)
        {
            if (!orbReferences[i].TryGet(out Orb orb))
            {
                Debug.LogError("Received invalid orb reference");
                return;
            }
            orbs[i] = orb;
        }

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