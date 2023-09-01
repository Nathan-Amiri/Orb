using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Relay relay;

    [SerializeField] private GameObject dimPivot;

    private Vector2 mousePosition;

    private readonly float cooldown = .5f;
    private bool onCooldown;

    public static Orb orbMouseOver; //set by Orb, does not sync across connections

    private void Update()
    {
        if (!player.IsOwner) return;

        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!onCooldown)
        {
            //fire purple if purple

            if (Input.GetKeyDown(KeyCode.W))
                UseAbility(Relay.AbilityColor.red);
            else if (Input.GetKeyDown(KeyCode.S))
                UseAbility(Relay.AbilityColor.blue);
            else if (Input.GetKeyDown(KeyCode.A))
                UseAbility(Relay.AbilityColor.yellow);
            else if (Input.GetKeyDown(KeyCode.D))
                UseAbility(Relay.AbilityColor.green);
        }
        else //if on cooldown
            dimPivot.transform.localScale -= new Vector3(0, 1 / cooldown * Time.deltaTime);
    }

    private void UseAbility(Relay.AbilityColor color)
    {
        if (player.purple.enabled)
            color = Relay.AbilityColor.purple;


        if (color == Relay.AbilityColor.red || color == Relay.AbilityColor.blue)
            relay.InputRelayServerRpc(color, mousePosition);
        //perform check now to ensure no null networkbehaviour reference is sent
        else if (orbMouseOver != null)
            relay.InputRelayServerRpc(color, default, orbMouseOver);
    }

    public IEnumerator StartCooldown() //run by Player
    {
        dimPivot.transform.localScale = new(1, 1);
        dimPivot.SetActive(true);
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
        dimPivot.SetActive(false);
    }
}