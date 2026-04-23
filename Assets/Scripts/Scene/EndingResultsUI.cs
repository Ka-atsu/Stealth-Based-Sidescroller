using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EndingResultsUI : MonoBehaviour
{
    [System.Serializable]
    public class ResultLine
    {
        public string label;
        public int amount;
        public int multiplier;
        public bool showMultiplier = true;

        public int Total => amount * multiplier;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private TMP_Text totalScoreText;
    [SerializeField] private TMP_Text rankText;

    [Header("Animation")]
    [SerializeField] private float lineDelay = 0.35f;
    [SerializeField] private float totalCountDuration = 1.2f;
    [SerializeField] private string totalPrefix = "Total Score: ";

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] private float backgroundMusicVolume = 0.8f;
    [SerializeField] private bool backgroundMusicLoop = true;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool stopMusicOnDisable = false;

    [Header("Fallback Values")]
    [SerializeField] private int fallbackKillMultiplier = 100;
    [SerializeField] private int fallbackScrollMultiplier = 50;

    private void Start()
    {
        PlayResults();

        if (playMusicOnStart)
            PlayBackgroundMusic();
    }

    private void OnDisable()
    {
        if (stopMusicOnDisable)
            StopBackgroundMusic();
    }

    public void PlayResults()
    {
        StopAllCoroutines();
        StartCoroutine(PlayResultsRoutine(BuildResultsFromGameManager()));
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic == null)
            return;

        NinjaAudioManager manager = NinjaAudioManager.Instance != null
            ? NinjaAudioManager.Instance
            : Object.FindFirstObjectByType<NinjaAudioManager>();

        if (manager == null)
            return;

        manager.PlayBackgroundMusic(backgroundMusic, backgroundMusicVolume, backgroundMusicLoop);
    }

    public void StopBackgroundMusic()
    {
        NinjaAudioManager manager = NinjaAudioManager.Instance != null
            ? NinjaAudioManager.Instance
            : Object.FindFirstObjectByType<NinjaAudioManager>();

        if (manager == null)
            return;

        manager.StopBackgroundMusic();
    }

    private List<ResultLine> BuildResultsFromGameManager()
    {
        List<ResultLine> lines = new List<ResultLine>();

        int missionScore;
        int enemiesKilled;
        int scrollsRead;
        int killMultiplier;
        int scrollMultiplier;

        if (GameSceneManager.Instance != null)
        {
            missionScore = GameSceneManager.Instance.CurrentScore;
            enemiesKilled = GameSceneManager.Instance.StealthKillCount; // currently using stealth kills
            scrollsRead = GameSceneManager.Instance.ReadScrollCount;
            killMultiplier = GameSceneManager.Instance.EnemyKillMultiplier;
            scrollMultiplier = GameSceneManager.Instance.ScrollReadMultiplier;
        }
        else
        {
            missionScore = PlayerPrefs.GetInt("PlayerScore", 0);
            enemiesKilled = PlayerPrefs.GetInt("StealthKillCount", 0);
            scrollsRead = PlayerPrefs.GetInt("ScrollReadCount", 0);
            killMultiplier = fallbackKillMultiplier;
            scrollMultiplier = fallbackScrollMultiplier;
        }

        // Base score already earned during gameplay
        lines.Add(new ResultLine
        {
            label = "Mission Score",
            amount = missionScore,
            multiplier = 1,
            showMultiplier = false
        });

        // Bonus from kills
        lines.Add(new ResultLine
        {
            label = "Enemies Killed",
            amount = enemiesKilled,
            multiplier = killMultiplier,
            showMultiplier = true
        });

        // Bonus from scrolls
        lines.Add(new ResultLine
        {
            label = "Scrolls Read",
            amount = scrollsRead,
            multiplier = scrollMultiplier,
            showMultiplier = true
        });

        return lines;
    }

    private IEnumerator PlayResultsRoutine(List<ResultLine> resultLines)
    {
        if (resultsText != null)
            resultsText.text = "";

        if (totalScoreText != null)
            totalScoreText.text = totalPrefix + "0";

        if (rankText != null)
            rankText.text = "";

        int totalScore = 0;
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < resultLines.Count; i++)
        {
            ResultLine line = resultLines[i];
            int lineScore = line.Total;
            totalScore += lineScore;

            if (line.showMultiplier)
            {
                sb.AppendLine($"{line.label}: {line.amount} x {line.multiplier}   =   +{lineScore:N0}");
            }
            else
            {
                sb.AppendLine($"{line.label}: +{lineScore:N0}");
            }

            if (resultsText != null)
                resultsText.text = sb.ToString();

            yield return new WaitForSecondsRealtime(lineDelay);
        }

        yield return StartCoroutine(AnimateTotalScore(totalScore));

        if (rankText != null)
            rankText.text = GetRank(totalScore);
    }

    private IEnumerator AnimateTotalScore(int finalScore)
    {
        if (totalScoreText == null)
            yield break;

        float time = 0f;

        while (time < totalCountDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / totalCountDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            int currentScore = Mathf.RoundToInt(Mathf.Lerp(0, finalScore, eased));
            totalScoreText.text = totalPrefix + currentScore.ToString("N0");

            yield return null;
        }

        totalScoreText.text = totalPrefix + finalScore.ToString("N0");
    }

    private string GetRank(int totalScore)
    {
        if (totalScore >= 3000) return "Rank: Shadow Legend";
        if (totalScore >= 2000) return "Rank: Silent Reaper";
        if (totalScore >= 1200) return "Rank: Hidden Blade";
        if (totalScore >= 600) return "Rank: Survivor";
        return "Rank: Escaped";
    }
}