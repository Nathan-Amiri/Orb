using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private CircleCollider2D trigger;

    [SerializeField] private Orb orb;

    public delegate void EndGameAction(bool isWinner);
    public static event EndGameAction EndGame;

    public void TurnOnOff(bool on)
    {
        sr.enabled = on;
        trigger.enabled = on;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        //red pickup
        orb.Trigger(col);

        if (!IsServer) return;
        if (!col.CompareTag("Player")) return;

        if (Setup.CurrentGameMode != Setup.GameMode.practice)
            PlayerInput.stunned = true; //set on server so that InputRelay is informed immediately
        EndGameClientRpc(col.GetComponent<Player>());
    }

    [ClientRpc]
    private void EndGameClientRpc(NetworkBehaviourReference loserReference)
    {
        Player loser = GetFromReference.GetPlayer(loserReference);

        if (Setup.CurrentGameMode != Setup.GameMode.practice)
            PlayerInput.stunned = true;

        if (Setup.CurrentGameMode == Setup.GameMode.challenge)
            EndGame?.Invoke(loser.isEnemyAI);
        else //practice or versus
            EndGame?.Invoke(!loser.IsOwner);
    }
}