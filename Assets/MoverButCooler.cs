using NSMB.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoverButCooler : MonoBehaviour
{
    public float time = 0f;
    public float length = 2f;

    private float ogY;
    public float offsetY = 5f;

    private void Start()
    {
        ogY = transform.position.y;
        Utils.GetCustomProperty(Enums.NetRoomProperties.ChaosMode, out bool chaos);
        if (chaos)
        {
            length /= 2f;
        }
    }
    // Update is called once per frame
    void Update()
    {
        time = (time + Time.deltaTime) % length;
        float sin = 0.5f + (0.5f * Mathf.Cos(time / length * Mathf.PI * 2));
        transform.position = new Vector3(transform.position.x, ogY + (sin * offsetY), transform.position.z);
    }
}
