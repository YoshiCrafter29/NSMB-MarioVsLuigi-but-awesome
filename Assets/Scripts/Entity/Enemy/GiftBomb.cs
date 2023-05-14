using NSMB.Utils;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GiftBomb : KillableEntity
{
    public GameObject explosion;
    public bool detonated = false;
    public int explosionTileSize = 5;
    public float fallingSpeed = 0.75f;

    public List<Sprite> sprites;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // kaboom!
        photonView.RPC("Kaboom", RpcTarget.All);
    }

    public override void InteractWithPlayer(PlayerController player)
    {
        // kaboom!
        photonView.RPC("Kaboom", RpcTarget.All);
    }

    private new void Start()
    {
        base.Start();
        GetComponent<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Count-1)];
    }

    private void Update()
    {
        body.velocity = new Vector2(0f, -fallingSpeed);
    }


    [PunRPC]
    public void Kaboom()
    {
        sRenderer.enabled = false;
        hitbox.enabled = false;
        detonated = true;

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
                        obj.GetPhotonView().RPC("Death", RpcTarget.All, false, false);
                        break;
                    }
            }
        }

        Vector3Int tileLocation = Utils.WorldToTilemapPosition(body.position);
        Tilemap tm = GameManager.Instance.tilemap;
        for (int x = -explosionTileSize; x <= explosionTileSize; x++)
        {
            for (int y = -explosionTileSize; y <= explosionTileSize; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > explosionTileSize) continue;
                Vector3Int ourLocation = tileLocation + new Vector3Int(x, y, 0);
                Utils.WrapTileLocation(ref ourLocation);

                TileBase tile = tm.GetTile(ourLocation);
                if (tile is BreakableBrickTile iBrick)
                {
                    iBrick.Break(this, Utils.TilemapToWorldPosition(ourLocation), Enums.Sounds.World_Block_Break);
                } else if (tile is InteractableTile iTile)
                {
                    iTile.Interact(this, InteractableTile.InteractionDirection.Up, Utils.TilemapToWorldPosition(ourLocation));
                }
            }
        }
        PhotonNetwork.Destroy(gameObject);
    }
}
