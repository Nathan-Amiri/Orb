using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    [SerializeField] private bool isPlayer; //false = orb

    [NonSerialized] public Vector2 target;
    [NonSerialized] public Transform playerTarget; //used for green and purple orbs

    private readonly float threshhold = .2f;
    private readonly float speed = 10;

    private readonly Vector2 maxPlayerPosition = new(7.95f, 4.125f);
    private readonly Vector2 maxOrbPosition = new(8.125f, 4.3f);

    private Vector2 maxPosition;

    private void Start()
    {
        maxPosition = isPlayer ? maxPlayerPosition : maxOrbPosition;
    }

    private void Update()
    {        
        if (target != default)
        {
            Vector2 direction = (target - (Vector2)transform.position).normalized;

            //if out of x bounds
            if (Mathf.Abs(transform.position.x) >= maxPosition.x)
                //if trying to move farther out of bounds
                if (Mathf.Sign(direction.x) == Mathf.Sign(transform.position.x))
                {
                    Reset();
                    return;
                }
            //if out of y bounds
            if (Mathf.Abs(transform.position.y) >= maxPosition.y)
                //if trying to move farther out of bounds
                if (Mathf.Sign(direction.y) == Mathf.Sign(transform.position.y))
                {
                    Reset();
                    return;
                }

            //if close enough to the target
            if (Vector2.Distance(transform.position, target) < threshhold)
            {
                transform.position = target;
                target = default;
                return;
            }

            //move
            transform.Translate(speed * Time.deltaTime * direction);
        }
        else if (playerTarget != null) //playerTarget reset manually
        {
            Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
            transform.Translate(speed * Time.deltaTime * direction);
        }
    }

    private void Reset()
    {
        target = default;
        playerTarget = null;
    }
}