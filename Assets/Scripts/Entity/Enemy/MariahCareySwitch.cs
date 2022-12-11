using NSMB.Utils;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MariahCareySwitch : MonoBehaviourPun
{
    public bool activated = false;
    public float cooldownRangeMin = 3f, cooldownRangeMax = 6f;
    public int giftsNumRangeMin = 1, giftsNumRangeMax = 4;
    public string giftPrefab = "Prefabs/Enemy/Gift";
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

    float elapsed = 0f;
    public float nextTime = 0f;

    private void Update()
    {
        if (!activated)
        {
            earthquakeSFX.volume = 0;
            return;
        }

        float time = AllIWantForChristmasIsYou.time;
        // -9 to 5.75
        mariahCarey.transform.position = new Vector3(mariahCarey.transform.position.x, -9f + Mathf.Clamp((time - 10.550f) / 44f * 14.5f, 0f, 14.5f), mariahCarey.transform.position.z);
        earthquakeSFX.volume = Mathf.Clamp((time - 10.550f) / 2.5f, 0f, 1f) * (time <= 54.550f ? 1f : 0f);
        CameraController.ScreenShake = 0.25f * earthquakeSFX.volume;
        CameraController.OnlyShakeOnGround = false;
        HorizontalCamera.OFFSET = HorizontalCamera.OFFSET_TARGET += Mathf.Clamp(Utils.Map(time, 10.550f, 54.550f, 0f, 4f), 0f, 3f);

        if (time > 175.9f)
            time = AllIWantForChristmasIsYou.time = 60.8f;

        if (!photonView.IsMine || time < 54.550f) return;

        elapsed += Time.deltaTime;
        if (elapsed > nextTime)
        {
            elapsed -= nextTime;

            nextTime = Random.Range(cooldownRangeMin, cooldownRangeMax);

            int giftsAmount = Random.Range(giftsNumRangeMin, giftsNumRangeMax);
            for(int i = 0; i < giftsAmount; i++)
            {
                Debug.Log("SPAWNING GIFT");
                float x = Random.Range(GameManager.Instance.levelMinTileX, GameManager.Instance.levelMinTileX + GameManager.Instance.levelWidthTile);
                PhotonNetwork.InstantiateRoomObject(giftPrefab, new Vector3(x, 20f, 0f), transform.rotation);
            }
        }
    }
}
