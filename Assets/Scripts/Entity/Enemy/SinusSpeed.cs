using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinusSpeed : MonoBehaviourPun
{
    GoombaWalk walk;
    public float min;
    public float max;
    public float speed;
    private float cur = 0;
    // Start is called before the first frame update
    void Start()
    {
        walk = GetComponent<GoombaWalk>();
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            cur = (cur + (Time.deltaTime * speed)) % 1;
            walk.speed = Mathf.Lerp(min, max, 0.5f + (0.5f * Mathf.Sin(cur * Mathf.PI)));
        }
    }
}
