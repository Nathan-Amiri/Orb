using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private InputRelay inputRelay;

    [SerializeField] private GameObject dimPivot;

    private Vector2 mousePosition;

    private readonly float cooldown = .3f;
    private bool onCooldown;

    public static Orb orbMouseOver; //set by Orb, does not sync across connections

    public static bool stunned = true; //set by Overlay and Explosion

    private void Awake()
    {
        stunned = true; //reset for every new game
    }

    private void Update()
    {
        if (!player.IsOwner) return;
        if (stunned) return; //check here and in InputRelay

        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!onCooldown)
        {
            if (Input.GetKeyDown(KeyCode.W))
                UseAbility(Player.AbilityColor.red);
            else if (Input.GetKeyDown(KeyCode.S))
                UseAbility(Player.AbilityColor.blue);
            else if (Input.GetKeyDown(KeyCode.A))
                UseAbility(Player.AbilityColor.yellow);
            else if (Input.GetKeyDown(KeyCode.D))
                UseAbility(Player.AbilityColor.green);
        }
        else //if on cooldown
            dimPivot.transform.localScale -= new Vector3(0, 1 / cooldown * Time.deltaTime);
    }

    private void UseAbility(Player.AbilityColor color)
    {
        if (player.purple.enabled)
            color = Player.AbilityColor.purple;


        if (color == Player.AbilityColor.red || color == Player.AbilityColor.blue)
            inputRelay.InputRelayServerRpc(color, mousePosition);
        //perform check now to ensure no null networkbehaviour reference is sent
        else if (orbMouseOver != null)
            inputRelay.InputRelayServerRpc(color, default, orbMouseOver);
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