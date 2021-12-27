using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Core;
using AdvancedSceneManager.Utility;
using Lazy.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoLoadingScreen : LoadingScreen
{
    /**
     *  Video clip in this example is set from the scene before to make it more dynamic. 
     *  VideoLoadingScreen.videoClip = videoClip;
     */
    public VideoClip defaultVideoClip;
    public static VideoClip videoClip;
    public CanvasGroup group;
    public RawImage VideoRenderer;
    public float fadeDuration;


    [Header("VideoClip is static, Apply it before loading")]
    public VideoPlayer videoPlayer;


    bool videoFinished;
    public override IEnumerator OnOpen(ISceneOperation operation)
    {
        yield return group.Fade(1, fadeDuration);
        SetupVideo();
    }
    public override IEnumerator OnClose(ISceneOperation operation)
    {
        // Unitys Coroutine does not support this, so make use of our, coroutine().StartCoroutine()
        // Lets Wait til video is done before we continue
        yield return WaitUntil().StartCoroutine();
        yield return group.Fade(0, fadeDuration);
    }

    private void SetupVideo()
    {
        videoPlayer.clip = videoClip ? videoClip : defaultVideoClip;
        videoPlayer.loopPointReached += EndReached;
        videoPlayer.Play();
    }

    private void EndReached(UnityEngine.Video.VideoPlayer source)
    {
        videoFinished = true;
        videoPlayer.Stop();
        VideoRenderer.enabled = false;
    }

    private IEnumerator WaitUntil()
    {
        yield return new WaitUntil(() => videoFinished);
    }
}
