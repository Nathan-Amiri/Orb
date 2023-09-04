using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InputRelay : NetworkBehaviour
{
    public Player player;

    public enum AbilityColor { red, yellow, green, blue, purple }

    [ServerRpc(RequireOwnership = false)]
    public void InputRelayServerRpc(AbilityColor color, Vector2 aimPoint = default, NetworkBehaviourReference newTarget = default)
    {
        //perform necessary checks

        //if missing aimPoint
        if (color == AbilityColor.red || color == AbilityColor.blue)
        {
            if (aimPoint == default)
            {
                Debug.LogError("Server received default aim point");
                return;
            }
        }
        //if blue/green/purple target doesn't exist or isn't ready
        else
        {
            Orb target = GetFromReference.GetOrb(newTarget);
            if (target == null || !target.ready)
                return;
        }

        //if player doesn't have enough orbs, (or has orbs and attempting to cast purple) return
        switch (color)
        {
            case AbilityColor.red:
                if (player.redOrbs.Count == 0)
                    return;
                break;
            case AbilityColor.blue:
                if (player.blueOrbs.Count == 0)
                    return;
                break;
            case AbilityColor.yellow:
                if (player.yellowOrbs.Count == 0)
                    return;
                break;
            case AbilityColor.green:
                if (player.greenOrbs.Count == 0)
                    return;
                break;
            case AbilityColor.purple:
                {
                    //check purple on both caster and server
                    if (player.redOrbs.Count > 0) return;
                    if (player.blueOrbs.Count > 0) return;
                    if (player.yellowOrbs.Count > 0) return;
                    if (player.greenOrbs.Count > 0) return;
                }
                break;
        }

        //send to clients

        InputRelayClientRpc(color, aimPoint, newTarget);
    }

    [ClientRpc]
    private void InputRelayClientRpc(AbilityColor color, Vector2 aimPoint, NetworkBehaviourReference targetReference)
    {
        Orb target = null;
        if (color != AbilityColor.red && color != AbilityColor.blue)
        {
            target = GetFromReference.GetOrb(targetReference);
            if (target == null)
                return;
        }

        player.ReceiveAbility(color, aimPoint, target);
    }

}