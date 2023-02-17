using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class SimpsonsIntroScript : MonoBehaviour
{
    public GameManager manager;
    public VideoPlayer videoPlayer;
    public GameObject blackPanel;
    // Start is called before the first frame update
    void Start()
    {
        videoPlayer.started += VideoPlayer_started;
        blackPanel.SetActive(true);
    }

    private void VideoPlayer_started(VideoPlayer source)
    {
        manager.PlaySong(Enums.MusicState.Normal, manager._mainMusic);
        blackPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
