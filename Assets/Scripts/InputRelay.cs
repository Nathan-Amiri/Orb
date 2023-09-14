using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InputRelay : NetworkBehaviour
{
    public Player player;

    [ServerRpc(RequireOwnership = false)]
    public void InputRelayServerRpc(Player.AbilityColor color, Vector2 aimPoint = default, NetworkBehaviourReference newTarget = default)
    {
        //perform necessary checks

        if (PlayerInput.stunned) return; //set to true the instant the game ends

        //if missing aimPoint
        if (color == Player.AbilityColor.red || color == Player.AbilityColor.blue)
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
            case Player.AbilityColor.red:
                if (player.redOrbs.Count == 0)
                    return;
                break;
            case Player.AbilityColor.blue:
                if (player.blueOrbs.Count == 0)
                    return;
                break;
            case Player.AbilityColor.yellow:
                if (player.yellowOrbs.Count == 0)
                    return;
                break;
            case Player.AbilityColor.green:
                if (player.greenOrbs.Count == 0)
                    return;
                break;
            case Player.AbilityColor.purple:
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
    private void InputRelayClientRpc(Player.AbilityColor color, Vector2 aimPoint, NetworkBehaviourReference targetReference)
    {
        Orb target = null;
        if (color != Player.AbilityColor.red && color != Player.AbilityColor.blue)
        {
            target = GetFromReference.GetOrb(targetReference);
            if (target == null)
                return;
        }

        player.ReceiveAbility(color, aimPoint, target);
    }

}