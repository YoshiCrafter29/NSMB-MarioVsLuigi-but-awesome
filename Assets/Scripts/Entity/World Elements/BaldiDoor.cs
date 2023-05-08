using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaldiDoor : MonoBehaviour
{
    public DoorManager animManager;
    public bool doorOpened = false;

    public AudioClip doorOpenedSFX, doorClosedSFX;
    public Sprite doorClosedSpr, doorOpenedSpr;

    public AudioSource source;
    public SpriteRenderer sprRenderer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (doorOpened != (doorOpened = Mathf.Abs(animManager.Model.transform.rotation.eulerAngles.y) > 1f))
        {
            source.PlayOneShot(doorOpened ? doorOpenedSFX : doorClosedSFX);
            sprRenderer.sprite = doorOpened ? doorOpenedSpr : doorClosedSpr;
        }
    }
}
