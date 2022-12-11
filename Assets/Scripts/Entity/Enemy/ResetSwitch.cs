using ExitGames.Client.Photon;
using NSMB.Utils;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetSwitch : MonoBehaviourPun
{
    public bool activated = false;
    public AudioSource audioSource;
    public Transform switchModel;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;

        PlayerController c = collision.gameObject.GetComponent<PlayerController>();
        if (c != null)
        {
            photonView.RPC(nameof(ActivateSwitch), RpcTarget.All);
        }
    }

    [PunRPC]
    public void ActivateSwitch()
    {
        if (activated) return;
        activated = true;

        audioSource.PlayOneShot(Enums.Sounds.Player_Sound_PowerupReserveStore.GetClip());

        switchModel.localScale = new Vector3(switchModel.localScale.x, switchModel.localScale.y * 0.25f, switchModel.localScale.z);

        if (!photonView.IsMine) return;
        GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.ResetTiles, null, SendOptions.SendReliable);

        elapsed = 0f;
    }

    float elapsed = 0f;

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (!activated) return;

        elapsed += Time.deltaTime;

        if (elapsed > 20f)
        {
            activated = false;
            switchModel.localScale = new Vector3(switchModel.localScale.x, switchModel.localScale.x, switchModel.localScale.z);
        }
    }
}
