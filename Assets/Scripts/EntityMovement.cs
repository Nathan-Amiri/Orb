using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    [SerializeField] private bool isPlayer; //false = orb

    private bool hasTarget;
    private Vector2 target;
    [NonSerialized] public Transform playerTarget; //used for green and purple orbs

    private readonly Vector2 maxPlayerPosition = new(7.95f, 4.125f);
    private readonly Vector2 maxOrbPosition = new(8.125f, 4.3f);

    private Vector2 maxPosition;

    private readonly float speed = 10;

    private void Start()
    {
        maxPosition = isPlayer ? maxPlayerPosition : maxOrbPosition;
    }

    private void Update()
    {
        if (hasTarget)
        {
            Vector2 direction = (target - (Vector2)transform.position).normalized;

            //if out of x bounds
            if (Mathf.Abs(transform.position.x) >= maxPosition.x)
            {
                //snap back to slightly inside bounds
                transform.position = new Vector2((maxPosition.x - .1f) * Mathf.Sign(transform.position.x), transform.position.y);
                Reset();
                return;
            }
            //if out of y bounds
            if (Mathf.Abs(transform.position.y) >= maxPosition.y)
            {
                //snap back to slightly inside bounds
                transform.position = new Vector2(transform.position.x, (maxPosition.y - .1f) * Mathf.Sign(transform.position.y));
                Reset();
                return;
            }

            transform.Translate(speed * Time.deltaTime * direction);
        }
        else if (playerTarget != null) //playerTarget reset manually
        {
            Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
            transform.Translate(speed * Time.deltaTime * direction);
        }
    }

    public void NewTarget(Vector2 newTarget)
    {
        target = newTarget;
        hasTarget = true;

        StopAllCoroutines();
        StartCoroutine(SnapToTarget());
    }

    private IEnumerator SnapToTarget()
    {
        float distance = Vector2.Distance(transform.position, target);
        float duration = distance / speed;
        yield return new WaitForSeconds(duration);

        Reset();
    }

    public void Reset()
    {
        hasTarget = false;
        playerTarget = null;
    }
}