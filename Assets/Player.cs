using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private EntityMovement entityMovement;
    [SerializeField] private GameObject dimPivot;

    [SerializeField] private List<Orb> redOrbs = new();
    [SerializeField] private List<Orb> blueOrbs = new();
    [SerializeField] private List<Orb> yellowOrbs = new();
    [SerializeField] private List<Orb> greenOrbs = new();

    [SerializeField] private List<Orb> destinedOrbs = new(); //used for green and purple
    private Orb yellowDestinedOrb; //set to null upon new movement

    [SerializeField] private List<SpriteRenderer> cones = new();
    [SerializeField] private SpriteRenderer purple;

    [NonSerialized] public Orb orbMouseOver; //set by Orb

    private Vector2 mousePosition;

    [SerializeField] private float redBlueRange = 5;

    private readonly float cooldown = .5f;
    private bool onCooldown;

    private void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (!onCooldown)
        {
            if (Input.GetKeyDown(KeyCode.W))
                FireRed();
            else if (Input.GetKeyDown(KeyCode.S))
                FireBlue();
            else if (Input.GetKeyDown(KeyCode.A))
                FireYellow();
            else if (Input.GetKeyDown(KeyCode.D))
                FireGreen();
        }

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

        if (onCooldown)
            dimPivot.transform.localScale -= new Vector3(0, 1 / cooldown * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Orb")) return;

        Orb orb = col.GetComponent<Orb>();

        //order orbisdestined first so that orb is removed from destinedorbs whether ready or not
        if (OrbIsDestined(orb) || orb.ready)
            GetOrb(orb);
    }

    private bool OrbIsDestined(Orb newOrb)
    {
        if (newOrb == yellowDestinedOrb)
        {
            yellowDestinedOrb = null;
            return true;
        }
        //orb cannot be both yellow destined and green/purple destined
        foreach (Orb orb in destinedOrbs)
            if (newOrb == orb)
            {
                destinedOrbs.Remove(orb);
                return true;
            }
        return false;
    }

    public void GetOrb(Orb orb) //run by Orb (if red)
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

    private void FireRed()
    {
        if (redOrbs.Count == 0)
        {
            FirePurple();
            return;
        }

        StartCoroutine(StartCooldown());

        Vector2 targetPosition = RedBlueDestination();//maxOrbPosition);
        redOrbs[0].transform.position = transform.position;
        redOrbs[0].entityMovement.NewTarget(targetPosition);

        redOrbs[0].redPickup = true;
        StartCoroutine(redOrbs[0].Explode());

        redOrbs.RemoveAt(0);
    }

    private void FireBlue()
    {
        if (blueOrbs.Count == 0)
        {
            FirePurple();
            return;
        }

        StartCoroutine(StartCooldown());

        ResetYellowDestinedOrb();

        blueOrbs[0].transform.position = transform.position;

        Vector2 targetPosition = RedBlueDestination();//maxPlayerPosition);
        entityMovement.NewTarget(targetPosition);

        StartCoroutine(blueOrbs[0].Explode());

        blueOrbs.RemoveAt(0);
    }

    private void FireYellow()
    {
        if (yellowOrbs.Count == 0)
        {
            FirePurple();
            return;
        }
        if (orbMouseOver == null || !orbMouseOver.ready) return;

        StartCoroutine(StartCooldown());

        ResetYellowDestinedOrb();

        orbMouseOver.ChangeReady(false);
        yellowDestinedOrb = orbMouseOver; //yellow effect cancels with new movement

        yellowOrbs[0].transform.position = transform.position;

        entityMovement.NewTarget(orbMouseOver.transform.position);

        StartCoroutine(yellowOrbs[0].Explode());

        yellowOrbs.RemoveAt(0);
    }

    private void FireGreen()
    {
        if (greenOrbs.Count == 0)
        {
            FirePurple();
            return;
        }
        if (orbMouseOver == null || !orbMouseOver.ready) return;

        StartCoroutine(StartCooldown());

        orbMouseOver.ChangeReady(false);
        destinedOrbs.Add(orbMouseOver);

        greenOrbs[0].transform.position = orbMouseOver.transform.position;

        orbMouseOver.entityMovement.playerTarget = transform;

        StartCoroutine(greenOrbs[0].Explode());

        greenOrbs.RemoveAt(0);
    }

    private void FirePurple()
    {
        if (redOrbs.Count > 0) return;
        if (blueOrbs.Count > 0) return;
        if (yellowOrbs.Count > 0) return;
        if (greenOrbs.Count > 0) return;

        if (orbMouseOver == null) return;

        StartCoroutine(StartCooldown());

        orbMouseOver.ChangeReady(false);
        destinedOrbs.Add(orbMouseOver);

        orbMouseOver.entityMovement.playerTarget = transform;
    }

    private IEnumerator StartCooldown()
    {
        dimPivot.transform.localScale = new(1, 1);
        dimPivot.SetActive(true);
        onCooldown = true;
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
        dimPivot.SetActive(false);
    }

    private void ResetYellowDestinedOrb() //run upon new movement
    {
        if (yellowDestinedOrb != null)
        {
            yellowDestinedOrb.ChangeReady(true);
            yellowDestinedOrb = null;
        }
    }

    private Vector2 RedBlueDestination()//Vector2 maxPosition)
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