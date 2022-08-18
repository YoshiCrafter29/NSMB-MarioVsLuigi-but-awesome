using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomAutoFollow : MonoBehaviour
{
    MovingPowerup move;
    Rigidbody2D body;
    public GameObject playerWhoSpawnedIt;
    // Start is called before the first frame update
    void Start()
    {
        move = GetComponent<MovingPowerup>();
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!move.photonView.IsMine) return;

        if (playerWhoSpawnedIt != null)
            move.despawnCounter = 15.0f; // never despawning;
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
            move.right = (closestPosition.x - body.position.x) > 0;
    }
}
