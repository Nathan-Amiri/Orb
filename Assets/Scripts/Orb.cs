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

    //the player that just launched this orb
    [NonSerialized] public Player caster; //set by Player, read by Explosion

    [SerializeField] private bool readyAtStart;

    public Player.AbilityColor color;

    public bool ready { get; private set; } //true if orb can be 'gotten'
    public bool enemyAIReady { get; private set; } //true if enemyAI can attempt to get this orb

    //enemyAI gives humanPlayers a chance to target orbs before targeting them
    private readonly float graceTime = .5f;

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

    public IEnumerator ChangeReady(bool readyOn)
    {
        ready = readyOn;

        //flicker collider to re-trigger OnTriggerEnter in any overlaps players/orbs
        if (readyOn)
        {
            trigger.enabled = false;
            yield return new WaitForSeconds(0);
            trigger.enabled = true;
        }

        //give humanPlayers a chance to target orb before enemyAI can
        yield return new WaitForSeconds(graceTime);

        enemyAIReady = ready;
    }

    public void Disappear()
    {
        entityMovement.Reset();

        transform.position = new Vector2(-15, 0);
    }

    public IEnumerator Explode(Player newCaster)
    {
        caster = newCaster;

        yield return new WaitForSeconds(.7f);

        //if enemyAI, IsOwner = true despite being an enemy (since there is only one client)
        bool isEnemyExplosion = caster.isEnemyAI || !caster.IsOwner;
        explosion.TurnOnOff(true, isEnemyExplosion);

        yield return new WaitForSeconds(.5f);

        explosion.TurnOnOff(false); //no need to specify isEnemy
        StartCoroutine(ChangeReady(true));
        caster = null;
    }



    //red pickup:
    private void OnTriggerEnter2D(Collider2D col)
    {
        Trigger(col);
    }

    public void Trigger(Collider2D col) //called by Explosion
    {
        if (color != Player.AbilityColor.red) return;
        if (caster == null) return;
        if (!col.CompareTag("Orb")) return;

        Orb orb = col.GetComponent<Orb>();

        if (!orb.ready) return; //red can't pick up destined orbs (check here and on server)

        RedPickupServerRpc(caster, orb);
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