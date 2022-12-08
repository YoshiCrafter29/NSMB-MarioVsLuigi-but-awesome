using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClownBus : BusAutoFollow
{
    public AudioSource jumpSFXSource;
    public AudioClip jumpSFX;
    public BoxCollider2D boxColliderThatKillsYouExclamationMark;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (move.dead) return;

        MonoBehaviourPun pun;
        List<ContactPoint2D> contacts = new List<ContactPoint2D>();
        collision.GetContacts(contacts);
        foreach(ContactPoint2D contact in contacts)
        {
            if (contact.rigidbody == boxColliderThatKillsYouExclamationMark || contact.otherRigidbody == boxColliderThatKillsYouExclamationMark)
            {
                KillableEntity entity = collision.GetComponent<KillableEntity>();
                PlayerController controller = collision.GetComponent<PlayerController>();
                if (entity != null)
                {
                    if (controller != null)
                        controller.photonView.RPC("Powerdown", RpcTarget.All, true);
                    else
                        entity.Kill();
                }
                return;
            }
        }
        if (collision.attachedRigidbody != null && (pun = collision.GetComponent<MonoBehaviourPun>()) != null && pun.photonView.IsMine && collision.attachedRigidbody.velocity.y < 15f)
        {
            collision.attachedRigidbody.velocity = new Vector2(collision.attachedRigidbody.velocity.x, 20f);
            photonView.RPC(nameof(PlayJumpSFX), RpcTarget.All);
        }
        
    }

    [PunRPC]
    public void PlayJumpSFX()
    {
        jumpSFXSource.PlayOneShot(jumpSFX);
    }
}
