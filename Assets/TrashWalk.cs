using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashWalk : KillableEntity
{
    public float speed = 0f;

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;

        if (collision.attachedRigidbody != null && (collision.GetComponent<PlayerController>() != null || (collision.GetComponent<KillableEntity>() != null) && collision.GetComponent<TrashWalk>() == null))
        {
            float speed = Mathf.Abs(collision.attachedRigidbody.velocity.x);

            if (speed > this.speed)
            {
                if (photonView.IsMine)
                {
                    photonView.RPC(nameof(UpdateSpeed), Photon.Pun.RpcTarget.All, speed, collision.attachedRigidbody.velocity.x < 0f);
                }
            } else
            {
                if (!left && collision.transform.position.y < transform.position.y)
                {
                    body.velocity = new Vector2(body.velocity.x, 15f);
                }
                if (left && collision.transform.position.x > transform.position.x) return;
                if (!left && collision.transform.position.x < transform.position.x) return;

            }
        }
    }

    [PunRPC]
    public void UpdateSpeed(float speed, bool left)
    {
        this.speed = speed;
        SetLeft(left);
    }

    public override void InteractWithPlayer(PlayerController player)
    {
        // doesnt do anything
    }
    public new void FixedUpdate()
    {
        base.FixedUpdate();
        body.velocity = new Vector2(speed * (left ? -1f : 1f), body.velocity.y);
    }
}
