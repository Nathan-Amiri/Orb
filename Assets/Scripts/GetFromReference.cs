using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class GetFromReference
{
    public static Orb GetOrb(NetworkBehaviourReference orbReference)
    {
        orbReference.TryGet(out Orb orb);

        if (orb == null)
            Debug.LogError("Received invalid orb reference");

        return orb;
    }

    public static Player GetPlayer(NetworkBehaviourReference playerReference)
    {
        playerReference.TryGet(out Player player);

        if (player == null)
            Debug.LogError("Received invalid player reference");

        return player;
    }
}