using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private CircleCollider2D trigger;

    [SerializeField] private Orb orb;

    public void TurnOnOff(bool on)
    {
        sr.enabled = on;
        trigger.enabled = on;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        orb.Trigger(col); //red pickup

        if (!IsServer) return;
        if (!col.CompareTag("Player")) return;

        EndGameClientRpc(col.GetComponent<Player>());
    }

    [ClientRpc]
    private void EndGameClientRpc(NetworkBehaviourReference loserReference)
    {
        Player loser = GetFromReference.GetPlayer(loserReference);

        Debug.Log(loser.name + " exploded!");
    }
}