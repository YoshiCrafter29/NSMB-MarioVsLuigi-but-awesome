using UnityEngine;

[CreateAssetMenu(fileName = "MarioLevel", menuName = "ScriptableObjects/MarioLevel")]
public class MarioLevel : ScriptableObject
{
    public string levelName = "My Level";
    public Sprite levelIcon = null;
    public string chatMessage = "";
}