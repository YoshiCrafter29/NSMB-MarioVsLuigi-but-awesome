using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{
    public GameObject Model;
    public DoorManager otherDoor;
    public Transform spawnPos;
    public float entryTime = 0f;
    public int doorID = 0;

    public float localEntryTime = 0f;

    public void OpenDoor()
    {
        entryTime = 1.5f;
    }

    // Start is called before the first frame update
    void Start()
    {
        GameManager.Instance.doors[doorID] = this;
    }

    // Update is called once per frame
    void Update()
    {
        entryTime = Mathf.Max(entryTime - (Time.deltaTime * 2.5f), 0f);
        Model.transform.rotation = Quaternion.Euler(0f, Mathf.Pow(Mathf.Min(1f, entryTime), 2) * 90f, 0f);
    }
}
