using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomAutoFollow : MonoBehaviour
{
    MovingPowerup move;
    // Start is called before the first frame update
    void Start()
    {
        move = GetComponent<MovingPowerup>();
    }

    // Update is called once per frame
    void Update()
    {
        PlayerController closestPlayer = null;
        float lastDist = 0;

        foreach(PlayerController p in GameManager.Instance.allPlayers)
        {
            if (closestPlayer == null)
            {
                closestPlayer = p;
            }

            float dist = (gameObject.transform.position.x - (p.transform.position.x));
            while(dist < 0)
                dist += GameManager.Instance.levelWidthTile * 0.5f;
            dist %= GameManager.Instance.levelWidthTile * 0.5f;
            if (dist > GameManager.Instance.levelWidthTile * 0.25f)
                dist -= GameManager.Instance.levelWidthTile * 0.5f;

            if (Mathf.Abs(lastDist) > Mathf.Abs(dist))
            {
                lastDist = dist;
                closestPlayer = p;
            }
        }

        if (closestPlayer != null)
            move.right = lastDist > 0;
    }
}
