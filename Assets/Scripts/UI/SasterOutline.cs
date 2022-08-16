using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SasterOutline : MonoBehaviour
{

    void Awake()
    {
        TextMeshPro textmeshPro = this.transform.Find("New HUD").Find("WinText").gameObject.GetComponent<TextMeshPro>();
        textmeshPro.outlineWidth = 0.4f;
        textmeshPro.outlineColor = new Color32(0, 0, 0, 255);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
