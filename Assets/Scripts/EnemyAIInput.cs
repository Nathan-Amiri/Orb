using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAIInput : MonoBehaviour
{
    private readonly bool debugMode; //for AI logic debugging



    [SerializeField] private InputRelay inputRelay;
    [SerializeField] private Player player;

    private Transform humanPlayer;
    private readonly List<Orb> orbs = new();

    private readonly float abilitySpeed = .3f;

    private readonly List<Player.AbilityColor> colorIndex = new() //used in some for loops in ai logic
    {
        Player.AbilityColor.red, Player.AbilityColor.blue, Player.AbilityColor.yellow, Player.AbilityColor.green
    };


    private void OnEnable()
    {
        Overlay.StartGame += OnStartGame;
    }
    private void OnDisable()
    {
        Overlay.StartGame -= OnStartGame;
    }

    public void OnSpawn() //run by Setup after all Orbs have been spawned
    {
        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
            if (playerObject != gameObject)
                humanPlayer = playerObject.transform;
        foreach (GameObject orbObject in GameObject.FindGameObjectsWithTag("Orb"))
            orbs.Add(orbObject.GetComponent<Orb>());

        if (humanPlayer == null)
            Debug.LogError("EnemyAI couldn't find human player");

        if (orbs.Count != 8)
            Debug.LogError("EnemyAI found " + orbs.Count + " orbs instead of 8");
    }

    private void OnStartGame()
    {
        StartCoroutine(AbilityTimer());
    }
    private IEnumerator AbilityTimer()
    {
        while (true)
        {
            ChooseAbilityColor();
            yield return new WaitForSeconds(abilitySpeed);
        }
    }

        //AI logic:

    //choose which color ability to use
    private void ChooseAbilityColor()
    {
        if (debugMode) Debug.Log("choosing color");

        //check to see which colors are available to use
        bool anyReadyOrbsInScene = false;
        foreach (Orb orb in orbs)
            if (orb.ready)
            {
                anyReadyOrbsInScene = true;
                break;
            }

        bool redAvailable = player.redOrbs.Count > 0;
        bool blueAvailable = player.blueOrbs.Count > 0;
        bool yellowAvailable = player.yellowOrbs.Count > 0 && anyReadyOrbsInScene;
        bool greenAvailable = player.greenOrbs.Count > 0 && anyReadyOrbsInScene;

        List<bool> colorAvailability = new() { redAvailable, blueAvailable, yellowAvailable, greenAvailable };

        //if no colors are available, (including purple) return. (must have only yellow/green and no targets)
        if (!redAvailable && !blueAvailable && !yellowAvailable && !greenAvailable && !player.purple.enabled)
        {
            if (debugMode) Debug.Log("Has yellow and/or green but no ready orbs to target");
            return;
        }

        //if only one color is available, (or if purple) choose that color
        Player.AbilityColor onlyAvailableColor = Player.AbilityColor.red; //must assign variable
        bool onlyOneIsAvailable = false;

        if (player.purple.enabled)
        {
            onlyOneIsAvailable = true;
            onlyAvailableColor = Player.AbilityColor.purple;
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                if (colorAvailability[i])
                {
                    //if onlyOneIsAvailable was already set to true, there's more than one available color
                    if (onlyOneIsAvailable)
                    {
                        onlyOneIsAvailable = false;
                        break;
                    }

                    onlyAvailableColor = colorIndex[i];
                    onlyOneIsAvailable = true;
                }
            }
        }

        if (onlyOneIsAvailable)
        {
            if (debugMode) Debug.Log("only has one color: " + onlyAvailableColor);
            PrioritizeColor(onlyAvailableColor, greenAvailable);
            return;
        }

        //else if in danger, choose yellow if available, else blue, else green
        bool isInDanger = false;
        foreach (Orb orb in orbs)
        {
            bool isInExplosionRange = Vector2.Distance(orb.transform.position, transform.position) <= StaticLibrary.playerRadius + StaticLibrary.explosionRadius;
            if (isInExplosionRange && !orb.ready)
            {
                isInDanger = true;
                break;
            }
        }
        if (isInDanger)
        {
            if (debugMode) Debug.Log("in danger!");
            if (yellowAvailable)
            {
                PrioritizeColor(Player.AbilityColor.yellow, greenAvailable);
                return;
            }
            else if (blueAvailable)
            {
                PrioritizeColor(Player.AbilityColor.blue, greenAvailable);
                return;
            }
            else //since there's more than one available, green (and red) must be available
            {
                PrioritizeColor(Player.AbilityColor.green, greenAvailable);
                return;
            }
        }

        //else if red available and player is in range, fire red
        float maxFireRange = StaticLibrary.redBlueRange + StaticLibrary.playerRadius + StaticLibrary.explosionRadius;
        if (redAvailable && Vector2.Distance(transform.position, humanPlayer.position) < maxFireRange)
        {
            if (debugMode) Debug.Log("Firing at player");
            inputRelay.InputRelayServerRpc(Player.AbilityColor.red, humanPlayer.transform.position);
            return;
        }

        //else if has two of at least one available color, randomly choose one of those colors
        List<Player.AbilityColor> maxedOutColors = new();

            //red and blue are always available if their count is greater than 0
        if (player.redOrbs.Count == 2) maxedOutColors.Add(Player.AbilityColor.red);
        if (player.blueOrbs.Count == 2) maxedOutColors.Add(Player.AbilityColor.blue);
        if (player.yellowOrbs.Count == 2 && yellowAvailable) maxedOutColors.Add(Player.AbilityColor.yellow);
        if (player.greenOrbs.Count == 2 && greenAvailable) maxedOutColors.Add(Player.AbilityColor.green);

        if (maxedOutColors.Count > 0)
        {
            if (debugMode) Debug.Log("maxed out on " +  maxedOutColors.Count + " colors");
            Player.AbilityColor randomMaxedOutColor = maxedOutColors[Random.Range(0, maxedOutColors.Count)];
            PrioritizeColor(randomMaxedOutColor, greenAvailable);
            return;
        }

        //else choose a random available color
        List<Player.AbilityColor> availableColors = new();
        for (int i = 0; i < 4; i++)
            if (colorAvailability[i])
                availableColors.Add(colorIndex[i]);

        if (debugMode) Debug.Log("has " + availableColors.Count + " available colors");
        Player.AbilityColor randomAvailableColor = availableColors[Random.Range(0, availableColors.Count)];
        PrioritizeColor(randomAvailableColor, greenAvailable);
    }

    //decide which color orb to attempt to get, if any
    private void PrioritizeColor(Player.AbilityColor chosenColor, bool greenAvailable)
    {
        bool redOrBlue = chosenColor == Player.AbilityColor.red || chosenColor == Player.AbilityColor.blue;

        //determine which orbs are available/in range to get
        float maxGetRange = StaticLibrary.redBlueRange + StaticLibrary.orbRadius + StaticLibrary.explosionRadius;
        List<Orb> availableOrbsToGet = new();
        foreach (Orb orb in orbs)
        {
            if (!orb.ready) continue;
            //if red or blue and out of range, continue
            if (redOrBlue && Vector2.Distance(transform.position, orb.transform.position) > maxGetRange) continue;

            availableOrbsToGet.Add(orb);
        }
        if (debugMode) Debug.Log(availableOrbsToGet.Count + " orbs are available to get");

        //if purple and no orbs are available to target
        if (chosenColor == Player.AbilityColor.purple && availableOrbsToGet.Count == 0)
        {
            if (debugMode) Debug.Log("Is purple, and no orbs in scene are ready");
            return;
        }

        //if red or blue and no ready orbs are in range to get, fire/move in smart direction
        if (redOrBlue && availableOrbsToGet.Count == 0)
        {
            if (debugMode) Debug.Log("Using red/blue and no orbs are available to target");
            inputRelay.InputRelayServerRpc(chosenColor, SmartTarget());
            return;
        }

        //else if blue, has a 1/3 chance to move without prioritizing
        if (chosenColor == Player.AbilityColor.blue && Random.Range(0, 3) == 0)
        {
            if (debugMode) Debug.Log("Moving blue without prioritizing");
            inputRelay.InputRelayServerRpc(chosenColor, SmartTarget());
            return;
        }

        //1/3 chance to use green at this point, if green is available
        if (greenAvailable && Random.Range(0, 3) == 0)
        {
            if (debugMode) Debug.Log("Switching to green");
            chosenColor = Player.AbilityColor.green;
        }

        //if missing any colors that are available, prioritize random available missing color. Else, prioritize random available color
        List<List<Orb>> playerLists = new() { player.redOrbs, player.blueOrbs, player.yellowOrbs, player.greenOrbs };

        List<Player.AbilityColor> colorsAvailableToGet = new();
        foreach (Orb orb in availableOrbsToGet)
            if (!colorsAvailableToGet.Contains(orb.color))
                colorsAvailableToGet.Add(orb.color);

        List<Player.AbilityColor> missingAndAvailableColors = new();
        for (int i = 0; i < 4; i++)
            if (playerLists[i].Count == 0 && colorsAvailableToGet.Contains(colorIndex[i]))
                missingAndAvailableColors.Add(colorIndex[i]);

        Player.AbilityColor prioritizedColor;
        if (missingAndAvailableColors.Count > 0)
        {
            if (debugMode) Debug.Log("prioritizing random available missing color");
            prioritizedColor = missingAndAvailableColors[Random.Range(0, missingAndAvailableColors.Count)];
        }
        else
        {
            if (debugMode) Debug.Log("prioritizing random available color");
            prioritizedColor = colorsAvailableToGet[Random.Range(0, colorsAvailableToGet.Count)];
        }

        ChooseTarget(chosenColor, availableOrbsToGet, prioritizedColor);
    }

    private Vector2 SmartTarget()
    {
        //returns random position between medium and max range towards player if far from player, away from player if close to player

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector2 humanPlayerDirection = (humanPlayer.position - transform.position).normalized;
        bool directionIsTowardHumanPlayer = Vector2.Angle(randomDirection, humanPlayerDirection) < 90;

        bool closeToHumanPlayer = Vector2.Distance(transform.position, humanPlayer.position) < StaticLibrary.redBlueRange;

        if (directionIsTowardHumanPlayer == closeToHumanPlayer) //conditions should be opposite
            randomDirection *= -1;


        float distance = Random.Range(StaticLibrary.redBlueRange / 2, StaticLibrary.redBlueRange);

        return randomDirection * distance;
    }

    //if attempting to get an orb, decide which orb to attempt to get
    private void ChooseTarget(Player.AbilityColor chosenColor, List<Orb> availablOrbsToGet, Player.AbilityColor prioritizedColor)
    {
        //get possible targets
        List<Orb> possibleTargets = new();
        foreach (Orb orb in availablOrbsToGet)
            if (orb.color == prioritizedColor)
                possibleTargets.Add(orb);
        
        //if only 1 possible target, target it
        if (possibleTargets.Count == 1)
        {
            if (debugMode) Debug.Log("Targeting the only possible target of the prioritized color");
            inputRelay.InputRelayServerRpc(chosenColor, possibleTargets[0].transform.position, possibleTargets[0]);
            return;
        }

        //else, there must be two targets. (there's only two of each orb of a given color) Determine which is closer
        float distance0 = Vector2.Distance(transform.position, possibleTargets[0].transform.position);
        float distance1 = Vector2.Distance(transform.position, possibleTargets[1].transform.position);
        Orb closestOrb = distance0 < distance1 ? possibleTargets[0] : possibleTargets[1];
        Orb farthestOrb = closestOrb == possibleTargets[0] ? possibleTargets[1] : possibleTargets[0];

        //if If red or blue, target the farthest orb. If green or yellow, target the closest orb
        Orb targetedOrb;
        if (chosenColor == Player.AbilityColor.red || chosenColor == Player.AbilityColor.blue)
        {
            if (debugMode) Debug.Log("Targeting the farthest possible target of the prioritized color");
            targetedOrb = farthestOrb;
        }
        else
        {
            if (debugMode) Debug.Log("Targeting the closest possible target of the prioritized color");
            targetedOrb = closestOrb;
        }

        inputRelay.InputRelayServerRpc(chosenColor, targetedOrb.transform.position, targetedOrb);
    }
}