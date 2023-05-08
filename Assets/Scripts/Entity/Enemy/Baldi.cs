using NSMB.Utils;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baldi : KillableEntity
{
    public bool angry = false;

    public List<Sprite> slapSprites = new List<Sprite>();

    public AudioSource source;

    public AudioClip rulerSlap, baldiWelcome;

    public SpriteRenderer spr;

    public float slapTime = 0f;

    public float slapSpeed = 30f;

    public float minSpeed = 1.75f;
    public float maxSpeed = 0.45f;

    public Rigidbody2D rb;

    // focused player
    PlayerController player = null;

    bool chaosChecked = false;
    // Update is called once per frame
    void Update()
    {
        if (angry)
        {
            if (!chaosChecked)
            {
                Utils.GetCustomProperty(Enums.NetRoomProperties.ChaosMode, out bool chaos);
                if (chaos)
                {
                    minSpeed = 1.35f;
                    maxSpeed = 0.20f;
                }
                chaosChecked = true;
            }
            if (GameManager.Instance.music != null)
                GameManager.Instance.music.volume = 0f;

            float dist = float.MaxValue;
            bool left = false;

            int starsCount = 0;
            foreach(PlayerController p in GameManager.Instance.allPlayers)
            {
                if (p.stars > starsCount)
                    starsCount = p.stars;
                float d = (p.transform.position - transform.position).magnitude;
                RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 10f, p.transform.position - transform.position);

                if (d < dist)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider.gameObject.GetComponent<PlayerController>() == p)
                        {
                            dist = d;
                            player = p;
                            break;
                        }
                    }
                }
                
            }

            if (player != null && player.hitInvincibilityCounter <= 0f)
            {
                DoorManager door = null;
                Transform targetTransform = player.transform;

                int playerZone = getZone(player.transform.position);
                int baldiZone = getZone(transform.position);

                if (playerZone != baldiZone)
                {
                    int zoneToGoThrough = getZonePath(baldiZone, playerZone);
                    door = GameManager.Instance.doors[getZoneDoor(zoneToGoThrough, baldiZone)];
                    targetTransform = door.transform;
                }
                float distance = targetTransform.position.x - transform.position.x;
                if (distance < -GameManager.Instance.levelWidthTile * 0.25f)
                    distance += GameManager.Instance.levelWidthTile * 0.5f;
                if (distance > GameManager.Instance.levelWidthTile * 0.25f)
                    distance -= GameManager.Instance.levelWidthTile * 0.5f;
                left = distance < 0;

                if (door != null && Mathf.Abs((door.transform.position - transform.position).x) < 0.5f)
                {
                    // tp baldi to the other side
                    transform.position = door.otherDoor.spawnPos.position;
                    photonView.RPC("OpenDoor", RpcTarget.All, door.doorID);
                    photonView.RPC("OpenDoor", RpcTarget.All, door.otherDoor.doorID);
                }
            }

            slapTime += Time.deltaTime;
            if (photonView.IsMine)
            {
                float speed = Mathf.Lerp(minSpeed, maxSpeed, ((float)starsCount) / ((float)GameManager.Instance.starRequirement));
                // Debug.Log(((float)starsCount) / ((float)GameManager.Instance.starRequirement));
                // Debug.Log(speed);
                if (slapTime > speed)
                {
                    photonView.RPC("Slap", RpcTarget.All, left ? -15f : 15f);
                }
            }
            if (slapTime > 0.125f)
            {
                rb.velocity = new Vector2();
            }
            // animation update
            spr.sprite = slapSprites[Mathf.FloorToInt(Mathf.Clamp(slapTime * 15f, 0, slapSprites.Count - 1))];
        }
    }

    [PunRPC]
    public void OpenDoor(int id)
    {
        GameManager.Instance.doors[id].OpenDoor();
    }

    public int getZoneDoor(int zone, int from)
    {
        switch(zone)
        {
            case 0:
                switch(from)
                {
                    case 1:
                        return 7;
                    case 2:
                        return 5;
                    case 3:
                        return 3;
                    case 4:
                        return 1;
                    case 5:
                        return 9;
                }
                break;
            case 1:
                return 6;
            case 2:
                return from == 0 ? 4 : 9;
            case 3:
                return 2;
            case 4:
                return 0;
            case 5:
                return 8;
        }
        return -1;
    }

    [PunRPC]
    public override void Freeze(int cube)
    {

    }

    [PunRPC]
    public override void Unfreeze(byte reasonByte)
    {

    }

    public int getZonePath(int zone1, int zone2)
    {
        if (zone1 == zone2) return zone2;
        switch(zone1)
        {
            case 0:
                if (zone2 >= 5)
                    return 2; // facility, go through room 2
                return zone2; // since in hallway, can access every room
            case 1 | 2 | 3 | 4:
                return 0; // hallway
            case 5:
                return zone2;
        }
        return zone2;
    }

    public int getZone(Vector3 pos)
    {
        if (pos.y < 5) // hallway
            return 0;
        else if (pos.y < 12)
        {
            if (pos.x < -8) // first room
                return 1;
            if (pos.x < 0) // second room
                return 2;
            if (pos.x < 8) // third room
                return 3;
            return 4; // fourth room
        }

        return 5; // school facility room
    }

    public void GetMad()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("GetsMad", RpcTarget.All);
        }
    }

    [PunRPC]
    public void GetsMad()
    {
        angry = true;
        GameManager.Instance.music.volume = 0f;
    }

    public void Start()
    {
        base.Start();
        collide = false;
        source.PlayOneShot(baldiWelcome);
    }

    [PunRPC]
    public void Slap(float speed)
    {
        source.PlayOneShot(rulerSlap);
        slapTime = 0f;
        rb.velocity = new Vector2(speed, 0f);
    }

    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        // do nothing: baldi is invincible
        dead = false;
    }

    public override void InteractWithPlayer(PlayerController player)
    {
        if (angry && !player.dead && player.hitInvincibilityCounter <= 0f)
            player.kill();
    }
}
