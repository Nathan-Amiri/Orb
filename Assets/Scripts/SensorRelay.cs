using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SensorRelay : NetworkBehaviour
{
    public Player player;

    [ServerRpc(RequireOwnership = false)]
    public void SensorRelayServerRpc(NetworkBehaviourReference orbReference)
    {
        Orb orb = GetFromReference.GetOrb(orbReference);

        switch (orb.color)
        {
            case Orb.OrbColor.red:
                if (player.redOrbs.Count == 2) return;
                break;
            case Orb.OrbColor.blue:
                if (player.blueOrbs.Count == 2) return;
                break;
            case Orb.OrbColor.yellow:
                if (player.yellowOrbs.Count == 2) return;
                break;
            case Orb.OrbColor.green:
                if (player.greenOrbs.Count == 2) return;
                break;
        }

        //order orbisdestined first so that orb is removed from destinedorbs whether ready or not
        if (OrbIsDestined(orb) || orb.ready)
            AddOrbClientRpc(orb);
    }

    private bool OrbIsDestined(Orb orb)
    {
        if (orb == player.yellowDestinedOrb)
        {
            RemoveYellowDestinedOrbClientRpc();
            return true;
        }
        //orb cannot be both yellow destined and green/purple destined
        foreach (Orb destinedOrb in player.destinedOrbs)
            if (orb == destinedOrb)
            {
                RemoveDestinedOrbClientRpc(destinedOrb);
                return true;
            }
        return false;
    }

    [ClientRpc]
    private void RemoveYellowDestinedOrbClientRpc()
    {
        player.yellowDestinedOrb = null;
    }

    [ClientRpc]
    private void RemoveDestinedOrbClientRpc(NetworkBehaviourReference orbReference)
    {
        Orb orb = GetFromReference.GetOrb(orbReference);

        if (!player.destinedOrbs.Contains(orb))
        {
            Debug.LogError("Destined orb not found on client");
            return;
        }

        player.destinedOrbs.Remove(orb);
    }

    [ClientRpc]
    private void AddOrbClientRpc(NetworkBehaviourReference orbReference)
    {
        Orb orb = GetFromReference.GetOrb(orbReference);

        player.AddOrb(orb);
    }
}