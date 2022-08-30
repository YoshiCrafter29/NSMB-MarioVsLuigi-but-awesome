using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadSpriteChange : MonoBehaviour
{
    GoombaWalk walk;
    public Sprite newSprite;
    SpriteRenderer renderer;
    // Start is called before the first frame update
    void Start()
    {
        walk = GetComponent<GoombaWalk>();
        renderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (walk.dead)
            renderer.sprite = newSprite;
    }
}
