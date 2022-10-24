using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainSpawnpoint : MonoBehaviourPun
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public string prefab;

    [Header("Settings")]
    public float interval = 35f;
    public float announcementTime = 3f;
    public float trainTime = 7.5f;
    public AudioSource sncfClip;

    [Header("Advanced stuff (DO NOT TOUCH!!)")]
    public float time = 0f;
    public int step = 0;
    public TrainWalk train;
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (photonView.IsMine)
        {
            if (time > interval && step < 1)
            {
                step = 1;
                photonView.RPC("sncf", RpcTarget.All);
            } else if (time > interval + announcementTime && step < 2)
            {
                step = 2;
                train = PhotonNetwork.InstantiateRoomObject(prefab, transform.position, transform.rotation).GetComponent<TrainWalk>();
            } else if (time > interval + announcementTime + trainTime && step < 3)
            {
                step = 3;
                PhotonNetwork.Destroy(train.GetComponent<PhotonView>());
                photonView.RPC("ResetTrain", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void ResetTrain()
    {
        step = 0;
        time = 0f;
        if (train != null)
        {
            train = null;
        }
    }

    [PunRPC]
    public void sncf()
    {
        sncfClip.Play();
    }
}
