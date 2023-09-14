using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Orb : NetworkBehaviour
{
    public SpriteRenderer sr; //used by Setup
    public EntityMovement entityMovement; //read by Player

    [SerializeField] private SpriteRenderer dim;
    [SerializeField] private CircleCollider2D trigger;
    [SerializeField] private Explosion explosion;

    //the player that just launched this red orb
    [NonSerialized] public Player redCaster;
    //[NonSerialized] public bool redPickup; //true if red and if can currently get orbs

    [SerializeField] private bool readyAtStart;

    public Player.AbilityColor color;

    public bool ready { get; private set; }

    public override void OnNetworkSpawn()
    {
        ready = readyAtStart;
    }

    private void Update()
    {
        dim.enabled = !ready;
    }

    public void OnMouseOver()
    {
        PlayerInput.orbMouseOver = this;
    }

    public void OnMouseExit()
    {
        if (PlayerInput.orbMouseOver == this)
            PlayerInput.orbMouseOver = null;
    }

    public void ChangeReady(bool readyOn)
    {
        ready = readyOn;

        //flicker collider to re-trigger OnTriggerEnter in any overlaps players/orbs
        if (readyOn)
        {
            trigger.enabled = false;
            Invoke(nameof(Flicker), 0);
        }
    }
    private void Flicker()
    {
        trigger.enabled = true;
    }

    public void Disappear()
    {
        entityMovement.Reset();

        transform.position = new Vector2(-15, 0);
    }

    public IEnumerator Explode()
    {
        yield return new WaitForSeconds(.7f);

        explosion.TurnOnOff(true);

        yield return new WaitForSeconds(.5f);

        explosion.TurnOnOff(false);
        ChangeReady(true);
        redCaster = null;
    }



    //red pickup:
    private void OnTriggerEnter2D(Collider2D col)
    {
        Trigger(col);
    }

    public void Trigger(Collider2D col) //called by Explosion
    {
        if (redCaster == null) return;
        if (!col.CompareTag("Orb")) return;

        Orb orb = col.GetComponent<Orb>();

        if (!orb.ready) return; //red can't pick up destined orbs (check here and on server)

        RedPickupServerRpc(redCaster, orb);
    }

    [ServerRpc (RequireOwnership = false)]
    private void RedPickupServerRpc(NetworkBehaviourReference playerReference, NetworkBehaviourReference orbReference)
    {
        Orb orb = GetFromReference.GetOrb(orbReference);

        if (!orb.ready) return; //red can't pick up destined orbs

        RedPickupClientRpc(playerReference, orb);
    }

    [ClientRpc]
    private void RedPickupClientRpc(NetworkBehaviourReference playerReference, NetworkBehaviourReference orbReference)
    {
        Orb orb = GetFromReference.GetOrb(orbReference);
        Player caster = GetFromReference.GetPlayer(playerReference);

        caster.AddOrb(orb);
    }
}