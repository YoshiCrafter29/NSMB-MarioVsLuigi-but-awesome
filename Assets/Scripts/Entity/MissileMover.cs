using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEngine.Tilemaps;

public class MissileMover : MonoBehaviourPun
{
    public float speed = 3f, bounceHeight = 4.5f, terminalVelocity = 6.25f;
    public bool left;
    public bool isIceball;
    private Rigidbody2D body;
    private PhysicsEntity physics;
    bool breakOnImpact;
    public GameObject explosion;
    public PlayerController playerController;
    public PlayerController playerToFocus;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        physics = GetComponent<PhysicsEntity>();

        object[] data = photonView.InstantiationData;
        left = (bool)data[0];
        if (data.Length > 1 && isIceball)
            speed += Mathf.Abs((float)data[1] / 3f);

        body.velocity = new Vector2(speed * (left ? -1 : 1), -speed);
    }
    void FixedUpdate()
    {
        HandleCollision();

        if (playerToFocus == null)
        {
            Collider2D closest = null;
            Vector2 closestPosition = Vector2.zero;
            float distance = float.MaxValue;
            foreach (var hit in Physics2D.OverlapCircleAll(body.position, 15f))
            {
                if (!hit.CompareTag("Player"))
                    continue;
                if (playerController != null && hit.attachedRigidbody.gameObject == playerController)
                    continue;
                Vector2 actualPosition = hit.attachedRigidbody.position + hit.offset;
                float tempDistance = Vector2.Distance(actualPosition, body.position);
                if (tempDistance > distance)
                    continue;
                distance = tempDistance;
                closest = hit;
                closestPosition = actualPosition;
            }
            playerToFocus = closest?.GetComponent<PlayerController>();
        }
        if (playerToFocus != null)
        {
            Quaternion rot = transform.rotation;
            transform.LookAt(new Vector2(playerToFocus.transform.position.x, playerToFocus.transform.position.y));
            Quaternion nRot = transform.rotation;
            transform.rotation = Quaternion.Lerp(rot, nRot, Mathf.Clamp(Time.deltaTime * 0.125f * 60, 0f, 1f));
        }

        body.velocity = transform.forward * speed;
    }
    void HandleCollision()
    {
        physics.UpdateCollisions();
        bool breaking = physics.hitLeft || physics.hitRight || physics.hitRoof || (physics.onGround && breakOnImpact && body.velocity.y <= 0);
        if (breaking)
            yesRicoKaboom();
    }

    void yesRicoKaboom()
    {
        if (!photonView) return;
        if (photonView.IsMine)
        {
            photonView.RPC("Detonate", RpcTarget.All);
        }
        else
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == playerController.gameObject || other.gameObject == this)
            return;

        Transform e = playerController.transform;
        while (e != null)
        {
            if (e.gameObject == other.gameObject || e.gameObject == this)
                return;
            e = e.parent;
        }
        yesRicoKaboom();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == null) return;
        if (collision.gameObject == playerController.gameObject || collision.gameObject == this)
            return;

        Transform e = playerController.transform;
        while (e != null)
        {
            if (e.gameObject == collision.gameObject || e.gameObject == this)
                return;
            e = e.parent;
        }
        yesRicoKaboom();
    }


    [PunRPC]
    public void Detonate()
    {

        Instantiate(explosion, transform.position, Quaternion.identity);

        if (!photonView.IsMine)
            return;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position + new Vector3(0, 0.5f), 1.2f, Vector2.zero);
        foreach (RaycastHit2D hit in hits)
        {
            GameObject obj = hit.collider.gameObject;

            if (obj == gameObject)
                continue;

            if (obj.GetComponent<KillableEntity>() is KillableEntity en)
            {
                en.photonView.RPC("SpecialKill", RpcTarget.All, transform.position.x < obj.transform.position.x, false, 0);
                continue;
            }

            switch (hit.collider.tag)
            {
                case "Player":
                    {
                        obj.GetPhotonView().RPC("Powerdown", RpcTarget.All, false);
                        break;
                    }
            }
        }

        Vector3Int tileLocation = Utils.WorldToTilemapPosition(body.position);
        Tilemap tm = GameManager.Instance.tilemap;
        int explosionTileSize = 2;
        for (int x = -explosionTileSize; x <= explosionTileSize; x++)
        {
            for (int y = -explosionTileSize; y <= explosionTileSize; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > explosionTileSize) continue;
                Vector3Int ourLocation = tileLocation + new Vector3Int(x, y, 0);
                Utils.WrapTileLocation(ref ourLocation);

                TileBase tile = tm.GetTile(ourLocation);
                if (tile is InteractableTile iTile)
                {
                    iTile.Interact(this, InteractableTile.InteractionDirection.Up, Utils.TilemapToWorldPosition(ourLocation));
                }
            }
        }
        PhotonNetwork.Destroy(gameObject);
    }

    void OnDestroy()
    {
        Instantiate(Resources.Load("Prefabs/Particle/" + (isIceball ? "IceballWall" : "FireballWall")), transform.position, Quaternion.identity);
    }

    [PunRPC]
    protected void Kill()
    {
        if (photonView.IsMine)
            PhotonNetwork.Destroy(photonView);
    }

}
