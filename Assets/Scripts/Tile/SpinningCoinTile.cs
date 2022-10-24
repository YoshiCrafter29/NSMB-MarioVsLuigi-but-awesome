using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Enums;

[CreateAssetMenu(fileName = "SpinningCoinTile", menuName = "ScriptableObjects/Tiles/SpinningCoinTile", order = 2)]
public class SpinningCoinTile : AnimatedTile
{
    public int coins = 1;
    /*
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
    */
}
