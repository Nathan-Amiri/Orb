using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    public EntityMovement entityMovement; //read by Player

    [SerializeField] private Player player;

    [SerializeField] private SpriteRenderer dim;
    [SerializeField] private CircleCollider2D trigger;

    [NonSerialized] public bool redPickup; //true if red and if can currently get orbs

    [SerializeField] private bool readyAtStart;

    public enum OrbColor { red, blue, yellow, green }
    public OrbColor color;

    public bool ready { get; private set; }

    private void Start()
    {
        ready = readyAtStart;
    }

    private void Update()
    {
        dim.enabled = !ready;
    }

    public void OnMouseOver()
    {
        player.orbMouseOver = this;
    }

    public void OnMouseExit()
    {
        if (player.orbMouseOver == this)
            player.orbMouseOver = null;
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
        yield return new WaitForSeconds(1);
        ChangeReady(true);
        redPickup = false;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!redPickup) return;
        if (!col.CompareTag("Orb")) return;

        Orb orb = col.GetComponent<Orb>();

        if (!orb.ready) return; //red can't pick up destined orbs

        player.GetOrb(orb);
    }
}