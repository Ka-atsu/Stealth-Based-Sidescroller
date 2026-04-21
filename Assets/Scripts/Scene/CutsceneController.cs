using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "Level1";

    private void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadSceneAsync(nextSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer == null) return;

        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
    }
}