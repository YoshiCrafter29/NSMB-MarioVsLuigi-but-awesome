using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using static Enums;
using System.Collections.Generic;

public class SleepyBus : KillableEntity
{
    [SerializeField] public float speed, deathTimer = -1, terminalVelocity = -8;
    [SerializeField] public bool sleeping = true;
    public float maxPlayerSpeedBeforeWakeUp = 2f;
    public SpriteRenderer spriteRenderer;
    public AudioSource audioSource;
    public Sprite sleepingSprite, slightlyAwakeSprite, awakeSprite;
    public AudioClip sleepingSound, awakeSound;
    public PlayerController focusedPlayer = null;
    public float angryTimer, imMadTime = 0f;
    private List<PowerupState> LoudPowerups = new List<PowerupState>()
    {
        PowerupState.MegaMushroom,
        PowerupState.Gigachad
    };


    public new void Start()
    {
        base.Start();
        body.velocity = new Vector2(speed * (left ? -1 : 1), body.velocity.y);
        SetSleeping(sleeping);
    }

    public new void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject)
        {
            PlayerController controller;
            if ((controller = collision.gameObject.GetComponent<PlayerController>()) == null) return;
            if (controller.photonView.IsMine)
                controller.kill();
        }
    }

    [PunRPC]
    public void SetSleeping(bool sleeping)
    {
        this.sleeping = sleeping;
        audioSource.Stop();
        if (sleeping)
        {
            audioSource.clip = sleepingSound;
        }
        else
        {
            audioSource.clip = awakeSound;
        }
        audioSource.Play();
    }

    public void UpdateSleeping()
    {
        //if (!) return;

        bool wasSleeping = sleeping;

        if (focusedPlayer != null && (!focusedPlayer.Active || focusedPlayer.dead))
        {
            focusedPlayer = null;
            angryTimer = 0f;
        }
        if (focusedPlayer != null || imMadTime > 0f)
        {
            sleeping = false;
            if (focusedPlayer != null)
                left = (focusedPlayer.transform.position.x - body.position.x) < 0;
            else
                imMadTime -= Time.deltaTime;
        } else
        {
            sleeping = true;
            bool angried = false;
            foreach (var hit in Physics2D.OverlapAreaAll(new Vector2(-1000f, -1000f), new Vector2(1000f, 1000f)))
            {
                var cont = hit.GetComponent<PlayerController>();

                if (!cont)
                    continue;
                bool isLoud = LoudPowerups.Contains(cont.state);
                if(cont.dead || cont.hitInvincibilityCounter > 0 || !cont.onGround || cont.state == PowerupState.MiniMushroom || (!isLoud && (cont.transform.position - transform.position).magnitude > 10f))
                    continue;

                Rigidbody2D rigidBody = hit.GetComponent<Rigidbody2D>();
                float speed = Mathf.Abs(rigidBody.velocity.x);
                if (rigidBody != null && (isLoud || speed > maxPlayerSpeedBeforeWakeUp))
                {
                    angried = true;
                    angryTimer = Mathf.Clamp(angryTimer + (Time.deltaTime * 3f * (speed / maxPlayerSpeedBeforeWakeUp)), 0, 1);
                    if (angryTimer >= 1f)
                    {
                        sleeping = false;
                        focusedPlayer = cont;
                        imMadTime = 2f;
                    }
                    break;
                }
            }
            if (!angried)
                angryTimer = Mathf.Clamp(angryTimer - (Time.deltaTime / 3f), 0, 1);
        }

        if (photonView.IsMineOrLocal() && wasSleeping != sleeping)
        {
            photonView.RPC(nameof(SetSleeping), RpcTarget.All, sleeping);
        }
    }

    public new void FixedUpdate()
    {
        if (GameManager.Instance && GameManager.Instance.gameover)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }

        base.FixedUpdate();

        if (dead)
        {
            if (deathTimer >= 0 && (photonView?.IsMine ?? true))
            {
                Utils.TickTimer(ref deathTimer, 0, Time.fixedDeltaTime);
                if (deathTimer == 0)
                    PhotonNetwork.Destroy(gameObject);
            }
            return;
        }


        physics.UpdateCollisions();
        if (physics.hitLeft || physics.hitRight)
        {
            left = physics.hitRight;
        }
        body.velocity = new Vector2(sleeping ? 0f : (speed * (left ? -1 : 1)), Mathf.Max(terminalVelocity, body.velocity.y));
        sRenderer.flipX = !left;

        UpdateSleeping();

        spriteRenderer.sprite = (sleeping ? (angryTimer >= 0.2f ? slightlyAwakeSprite : sleepingSprite) : awakeSprite);
    }

    [PunRPC]
    public override void Kill()
    {
        body.velocity = Vector2.zero;
        body.isKinematic = true;
        speed = 0;
        dead = true;
        deathTimer = 0.5f;
        hitbox.enabled = false;
    }
}