using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using System.Collections.Generic;

public class MioHonda : KillableEntity {
    [SerializeField] public float speed, deathTimer = -1, terminalVelocity = -8;

    public float timer = 0f;
    public float timerMul = 0.5f;
    public bool firstStep = false;

    public AudioSource source;
    public List<Sprite> sprites;
    public Sprite jumpSprite;
    public SpriteRenderer spriteRenderer;

    public bool firstJump = true;

    public new void Start() {
        base.Start();
        body.velocity = new Vector2(0f, 5f);
        source = GetComponent<AudioSource>();
    }

    public new void FixedUpdate() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
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
        if (Mathf.Abs(body.velocity.y) > 0.25f)
            spriteRenderer.sprite = jumpSprite;
        else
        {
            firstJump = false;
            timer += Time.fixedDeltaTime * Mathf.Abs(body.velocity.x) * timerMul;

            if (!firstStep && timer >= 1f)
            {
                photonView.RPC(nameof(Step), RpcTarget.All, timer, body.velocity.x, left);
                firstStep = true;
            }
            else if (timer > 2f)
            {
                timer -= 2f;
                photonView.RPC(nameof(Step), RpcTarget.All, timer, body.velocity.x, left);
                firstStep = false;
            }

            spriteRenderer.sprite = sprites[(int)(timer * 2f)];

            if (physics.hitLeft || physics.hitRight)
            {
                photonView.RPC(nameof(Jump), RpcTarget.All, timer, body.velocity.x, left);
            }
        }
        if (!firstJump)
            body.velocity = new Vector2(Mathf.Lerp(body.velocity.x, speed * (left ? -1 : 1), 0.25f * 60f * Time.fixedDeltaTime), Mathf.Max(terminalVelocity, body.velocity.y));
        sRenderer.flipX = !left;
    }

    [PunRPC]
    public void Step(float timer, float velocity, bool left)
    {
        UpdateValues(timer, velocity, left);
        // step
        CameraController.ScreenShake = 0.15f;
        source.PlayOneShot(Enums.Sounds.Powerup_MegaMushroom_Walk.GetClip(null, (byte)(timer >= 1f ? 1 : 2)), 1f);
    }
    [PunRPC]
    public void Jump(float timer, float velocity, bool left)
    {
        UpdateValues(timer, velocity, left);
        // step
        this.body.velocity = new Vector2(velocity, 15f);
    }

    void UpdateValues(float timer, float velocity, bool left)
    {
        this.body.velocity = new Vector2(velocity, this.body.velocity.y);
        this.left = left;
        this.timer = timer;
    }

    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        return;
    }
}