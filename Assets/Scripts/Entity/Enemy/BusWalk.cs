using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using System.Collections.Generic;

public class BusWalk : KillableEntity {
    [SerializeField] float speed, deathTimer = -1, terminalVelocity = -8;

    public new void Start() {
        base.Start();
        body.velocity = new Vector2(speed * (left ? -1 : 1), body.velocity.y);
        animator.SetBool("dead", false);
    }

    Dictionary<int, Vector3> offsets = new Dictionary<int, Vector3>();
    Dictionary<int, Vector3> lastPosition = new Dictionary<int, Vector3>();
    public void OnCollisionStay2D(Collision2D collision)
    {

        PlayerController controller;
        if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
        if (controller.photonView.IsMine)
        {
            if (lastPosition.ContainsKey(controller.playerId))
            {
                offsets[controller.playerId] += collision.transform.position - lastPosition[controller.playerId];
            }
            Vector3 offset = offsets[controller.playerId];
            collision.transform.position = transform.position + offset;
            lastPosition[controller.playerId] = collision.transform.position;
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController controller;
        if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
        if (controller.photonView.IsMine)
        {
            if (offsets.ContainsKey(controller.playerId))
                offsets.Remove(controller.playerId);
            offsets.Add(controller.playerId, controller.transform.position - transform.position);

            OnCollisionStay2D(collision);
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController controller;
        if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
        if (controller.photonView.IsMine)
        {
            offsets.Remove(controller.playerId);
            lastPosition.Remove(controller.playerId);
        }
    }

    public new void FixedUpdate() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }

        base.FixedUpdate();
        if (dead) {
            if (deathTimer >= 0 && (photonView?.IsMine ?? true)) {
                Utils.TickTimer(ref deathTimer, 0, Time.fixedDeltaTime);
                if (deathTimer == 0)
                    PhotonNetwork.Destroy(gameObject);
            }
            return;
        }


        physics.UpdateCollisions();
        if (physics.hitLeft || physics.hitRight) {
            left = physics.hitRight;
        }
        body.velocity = new Vector2(speed * (left ? -1 : 1), Mathf.Max(terminalVelocity, body.velocity.y));
        sRenderer.flipX = !left;
    }

}