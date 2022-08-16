using UnityEngine;
using static Enums;

public class PipeManager : MonoBehaviour {
    public bool entryAllowed = true, bottom = false, miniOnly = false;
    public PipeManager otherPipe;
    public MusicData destData;
    public MusicState destState;
}
