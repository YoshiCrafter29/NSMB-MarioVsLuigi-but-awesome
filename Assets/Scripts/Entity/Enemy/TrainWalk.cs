using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using System.Collections.Generic;

public class TrainWalk : KillableEntity {
    [SerializeField] public float speed, deathTimer = -1, terminalVelocity = -8;
    [SerializeField] BoxCollider2D ride;
    Vector3 basePos;

    public List<TrainWalk> followingCars = new List<TrainWalk>();
    public TrainWalk parent;
    public int id = 0;

    private float trainWidth = 35.0548f * 0.15f;

    public new void Start() {
        base.Start();
        body.velocity = new Vector2(speed * (left ? -1 : 1), body.velocity.y);
        animator.SetBool("dead", false);
        basePos = transform.position;

        if (photonView.IsMineOrLocal())
        {
            // 5% chance of being supersonic
            photonView.RPC(nameof(SetSpeed), RpcTarget.All, speed * ((Random.Range(0f, 100f) >= 95f) ? 5f : (1f + (Random.Range(-12f, 12f) / 100f))));
        }
    }

    [PunRPC]
    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    Dictionary<int, Vector3> offsets = new Dictionary<int, Vector3>();
    Dictionary<int, Vector3> lastPosition = new Dictionary<int, Vector3>();
    public void OnTriggerStay2D(Collider2D collision)
    {

    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController controller;
        if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
        if (controller.photonView.IsMine)
        {
            /*
            if (offsets.ContainsKey(controller.playerId))
                offsets.Remove(controller.playerId);
            offsets.Add(controller.playerId, controller.transform.position - transform.position);
            */

            controller.kill();
            //OnTriggerStay2D(collision);
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController controller;
        if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
        if (controller.photonView.IsMine)
        {
            OnTriggerStay2D(collision);
            offsets.Remove(controller.playerId);
            lastPosition.Remove(controller.playerId);
        }
    }

    public override void InteractWithPlayer(PlayerController p)
    {
        if (!ride.IsTouching(p.MainHitbox))
        {
            base.InteractWithPlayer(p);
            return;
        }
    }

    public void Update()
    {

    }
    public new void FixedUpdate() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }

        if (transform.position.y < -1000.0f)
        {
            dead = false;

            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.gravityScale = 1f;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            transform.position = basePos;
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