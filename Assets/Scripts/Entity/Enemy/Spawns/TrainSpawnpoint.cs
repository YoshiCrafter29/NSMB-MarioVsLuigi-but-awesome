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


    public float time = 0f;
    public int step = 0;
    public TrainWalk train;
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (photonView.IsMine)
        {
            if (time > 30f && step < 1)
            {
                step = 1;
                photonView.RPC("sncf", RpcTarget.All);
            } else if (time > 35f && step < 2)
            {
                step = 2;
                train = PhotonNetwork.InstantiateRoomObject(prefab, transform.position, transform.rotation).GetComponent<TrainWalk>();
            } else if (time > 50f && step < 3)
            {
                step = 3;
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
            train.photonView.RPC("Kill", RpcTarget.All);
            train = null;
        }
    }

    [PunRPC]
    public void sncf()
    {
        Debug.Log("SNCF!!");
    }
}
