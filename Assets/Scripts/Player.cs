using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    //class with non-networked logic

    [SerializeField] private SpriteRenderer playerBack;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private EntityMovement entityMovement;

    [SerializeField] private Color32 ownerColor;
    [SerializeField] private Color32 enemyColor;

    //read by Relay
    public List<Orb> redOrbs = new();
    public List<Orb> blueOrbs = new();
    public List<Orb> yellowOrbs = new();
    public List<Orb> greenOrbs = new();

    [NonSerialized] public readonly List<Orb> destinedOrbs = new(); //read by PlayerSensor, used for green and purple
    [NonSerialized] public Orb yellowDestinedOrb; //read/set by playerInput, read by playerSensor, set to null upon new movement

    [SerializeField] private List<SpriteRenderer> cones = new();
    public SpriteRenderer purple; //read by PlayerInput

    private readonly float redBlueRange = 5;

    public override void OnNetworkSpawn()
    {
        playerBack.color = IsOwner ? ownerColor : enemyColor;
    }

    public void ReceiveAbility(InputRelay.AbilityColor color, Vector2 aimPoint, Orb target)
    {
        switch (color)
        {
            case InputRelay.AbilityColor.red:
                FireRed(aimPoint);
                break;
            case InputRelay.AbilityColor.blue:
                FireBlue(aimPoint);
                break;
            case InputRelay.AbilityColor.yellow:
                FireYellow(target);
                break;
            case InputRelay.AbilityColor.green:
                FireGreen(target);
                break;
            case InputRelay.AbilityColor.purple:
                FirePurple(target);
                break;
        }
    }

    private void Update()
    {
        cones[0].enabled = redOrbs.Count > 0; //red1
        cones[1].enabled = blueOrbs.Count > 0; //blue1
        cones[2].enabled = yellowOrbs.Count > 0; //yellow1
        cones[3].enabled = greenOrbs.Count > 0; //green1
        cones[4].enabled = redOrbs.Count > 1; //red2
        cones[5].enabled = blueOrbs.Count > 1; //blue2
        cones[6].enabled = yellowOrbs.Count > 1; //yellow2
        cones[7].enabled = greenOrbs.Count > 1; //green2

        foreach (SpriteRenderer cone in cones)
        {
            if (cone.enabled)
            {
                purple.enabled = false;
                break;
            }
            purple.enabled = true;
        }
    }

    public void AddOrb(Orb orb) //run by Orb (if red)
    {
        orb.ChangeReady(false);

        orb.Disappear();

        switch (orb.color)
        {
            case Orb.OrbColor.red:
                redOrbs.Add(orb);
                break;
            case Orb.OrbColor.blue:
                blueOrbs.Add(orb);
                break;
            case Orb.OrbColor.yellow:
                yellowOrbs.Add(orb);
                break;
            case Orb.OrbColor.green:
                greenOrbs.Add(orb);
                break;
        }
    }

    private void FireRed(Vector2 aimPoint)
    {
        StartCoroutine(playerInput.StartCooldown());

        Vector2 targetPosition = RedBlueDestination(aimPoint);
        redOrbs[0].transform.position = transform.position;
        redOrbs[0].entityMovement.NewTarget(targetPosition);

        redOrbs[0].redCaster = this;
        StartCoroutine(redOrbs[0].Explode());

        redOrbs.RemoveAt(0);
    }

    private void FireBlue(Vector2 aimPoint)
    {
        StartCoroutine(playerInput.StartCooldown());

        ResetYellowDestinedOrb();

        blueOrbs[0].transform.position = transform.position;

        Vector2 targetPosition = RedBlueDestination(aimPoint);
        entityMovement.NewTarget(targetPosition);

        StartCoroutine(blueOrbs[0].Explode());

        blueOrbs.RemoveAt(0);
    }

    private void FireYellow(Orb target)
    {
        StartCoroutine(playerInput.StartCooldown());

        ResetYellowDestinedOrb();

        target.ChangeReady(false);
        yellowDestinedOrb = target; //yellow effect cancels with new movement

        yellowOrbs[0].transform.position = transform.position;

        entityMovement.NewTarget(target.transform.position);

        StartCoroutine(yellowOrbs[0].Explode());

        yellowOrbs.RemoveAt(0);
    }

    private void FireGreen(Orb target)
    {
        StartCoroutine(playerInput.StartCooldown());

        target.ChangeReady(false);
        destinedOrbs.Add(target);

        greenOrbs[0].transform.position = target.transform.position;

        target.entityMovement.playerTarget = transform;

        StartCoroutine(greenOrbs[0].Explode());

        greenOrbs.RemoveAt(0);
    }

    private void FirePurple(Orb target)
    {
        StartCoroutine(playerInput.StartCooldown());

        target.ChangeReady(false);
        destinedOrbs.Add(target);

        target.entityMovement.playerTarget = transform;
    }

    private void ResetYellowDestinedOrb() //run upon new movement
    {
        if (yellowDestinedOrb != null)
        {
            yellowDestinedOrb.ChangeReady(true);
            yellowDestinedOrb = null;
        }
    }

    private Vector2 RedBlueDestination(Vector2 mousePosition)
    {
        //get destination
        Vector2 destination;
        Vector2 aimDirection = (mousePosition - (Vector2)transform.position).normalized;
        //if mouse is in range
        if (Vector2.Distance(mousePosition, (Vector2)transform.position) <= redBlueRange)
            destination = mousePosition;
        else
            destination = (Vector2)transform.position + (aimDirection * redBlueRange);

        return destination;
    }
}