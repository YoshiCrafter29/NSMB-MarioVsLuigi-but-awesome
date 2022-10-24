using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Enums;

[CreateAssetMenu(fileName = "ChangeableTile", menuName = "ScriptableObjects/Tiles/ChangeableTile", order = 2)]
public class ChangeableTile : AnimatedTile
{
    public TileType tileType = TileType.None;
    public void RefreshTile()
    {
        switch(tileType)
        {
            case TileType.Coin:
                Debug.Log(GameManager.Instance.customCoinSprites.Length);
                if (GameManager.Instance.customCoinSprites != null && GameManager.Instance.customCoinSprites.Length > 0)
                {
                    this.m_AnimatedSprites = GameManager.Instance.customCoinSprites;
                }
                break;
            default:
                break;
        }
    }
}
