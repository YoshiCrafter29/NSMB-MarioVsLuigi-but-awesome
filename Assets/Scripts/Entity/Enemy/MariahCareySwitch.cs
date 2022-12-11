using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MariahCareySwitch : MonoBehaviourPun
{
    public bool activated = false;
    public AudioSource audioSource, earthquakeSFX, AllIWantForChristmasIsYou;
    public Transform switchModel;
    public GameObject mariahCarey;
    public MusicData mariahMusicData;
    void Start()
    {
        mariahCarey = GameObject.Find("Mariah Carey");
    }

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
        GameManager.Instance.music.volume = 0;
        AllIWantForChristmasIsYou.Play();

        GameManager.Instance.mainMusic = mariahMusicData;

        switchModel.localScale = new Vector3(switchModel.localScale.x, switchModel.localScale.y * 0.25f, switchModel.localScale.z);
    }

    private void Update()
    {
        if (!activated)
        {
            earthquakeSFX.volume = 0;
            return;
        }

        float time = AllIWantForChristmasIsYou.time;
        // -6 to 6
        mariahCarey.transform.position = new Vector3(mariahCarey.transform.position.x, -6f + Mathf.Clamp((time - 10.550f) / 44f * 12f, 0f, 12f), mariahCarey.transform.position.z);
        earthquakeSFX.volume = Mathf.Clamp((time - 10.550f) / 2.5f, 0f, 1f) * (time <= 54.550f ? 1f : 0f);
        CameraController.ScreenShake = 0.25f * earthquakeSFX.volume;


        if (!photonView.IsMine) return;
    }
}
