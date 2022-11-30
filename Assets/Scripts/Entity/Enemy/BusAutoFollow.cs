using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusAutoFollow : MonoBehaviour
{
    BusWalk move;
    KillableEntity moveEntity;
    Rigidbody2D body;
    public GameObject playerWhoSpawnedIt;
    private float __speed, __defaultSpeed;
    // Start is called before the first frame update
    void Start()
    {
        move = GetComponent<BusWalk>();
        body = GetComponent<Rigidbody2D>();
        moveEntity = GetComponent<KillableEntity>();
        __defaultSpeed = __speed = move.speed;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //if (!move.photonView.IsMine) return;

        Collider2D closest = null;
        Vector2 closestPosition = Vector2.zero;
        float distance = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(body.position, 30f))
        {
            if (playerWhoSpawnedIt != null && hit.attachedRigidbody.gameObject == playerWhoSpawnedIt)
                continue;
            if (!hit.CompareTag("Player"))
                continue;
            Vector2 actualPosition = hit.attachedRigidbody.position + hit.offset;
            float tempDistance = Vector2.Distance(actualPosition, body.position);
            if (tempDistance > distance)
                continue;
            distance = tempDistance;
            closest = hit;
            closestPosition = actualPosition;
        }
        if (closest)
        {
            if (moveEntity.left != (moveEntity.left = (closestPosition.x - body.position.x) < 0))
            {
                move.speed = -move.speed;
            }
        }
        move.speed = Mathf.Lerp(move.speed, __speed, 0.125f * 15 * Time.deltaTime);
            
    }
}
