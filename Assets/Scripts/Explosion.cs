using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private CircleCollider2D trigger;

    [SerializeField] private Orb orb;

    [SerializeField] private Color selfExplosionColor;
    [SerializeField] private Color enemyExplosionColor;

    public delegate void EndGameAction(bool isWinner);
    public static event EndGameAction EndGame;

    public void TurnOnOff(bool on, bool isEnemyExplosion = false)
    {
        if (on)
            sr.color = isEnemyExplosion ? enemyExplosionColor : selfExplosionColor;
        sr.enabled = on;
        trigger.enabled = on;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        //red pickup
        orb.Trigger(col);

        if (!IsServer) return;
        if (!col.CompareTag("Player")) return;
        Player triggeredPlayer = col.GetComponent<Player>();
        if (triggeredPlayer == orb.caster) return;

        PlayerInput.stunned = true; //set on server so that InputRelay is informed immediately
        EndGameClientRpc(col.GetComponent<Player>());
    }

    [ClientRpc]
    private void EndGameClientRpc(NetworkBehaviourReference loserReference)
    {
        Player loser = GetFromReference.GetPlayer(loserReference);

        if (Setup.CurrentGameMode == Setup.GameMode.challenge)
            EndGame?.Invoke(loser.isEnemyAI);
        else //versus
            EndGame?.Invoke(!loser.IsOwner);
    }
}