using System.Collections;
using System.Collections.Generic;
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

        public int Total => amount * multiplier;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Animation")]
    [SerializeField] private float lineDelay = 0.35f;
    [SerializeField] private float totalCountDuration = 1.2f;
    [SerializeField] private string totalPrefix = "Total Score: ";

    private void Start()
    {
        PlayResults();
    }

    public void PlayResults()
    {
        StopAllCoroutines();
        StartCoroutine(PlayResultsRoutine(BuildResultsFromGameManager()));
    }

    private List<ResultLine> BuildResultsFromGameManager()
    {
        List<ResultLine> lines = new List<ResultLine>();

        int enemiesKilled = 0;
        int scrollsRead = 0;

        if (GameSceneManager.Instance != null)
        {
            enemiesKilled = GameSceneManager.Instance.StealthKillCount;
            scrollsRead = GameSceneManager.Instance.ReadScrollCount;
        }
        else
        {
            enemiesKilled = PlayerPrefs.GetInt("StealthKillCount", 0);
            scrollsRead = PlayerPrefs.GetInt("ScrollReadCount", 0);
        }

        lines.Add(new ResultLine
        {
            label = "Enemies Killed",
            amount = enemiesKilled,
            multiplier = 100
        });

        lines.Add(new ResultLine
        {
            label = "Scrolls Read",
            amount = scrollsRead,
            multiplier = 50
        });

        return lines;
    }

    private IEnumerator PlayResultsRoutine(List<ResultLine> resultLines)
    {
        if (resultsText != null)
            resultsText.text = "";

        if (totalScoreText != null)
            totalScoreText.text = totalPrefix + "0";

        int totalScore = 0;

        for (int i = 0; i < resultLines.Count; i++)
        {
            ResultLine line = resultLines[i];
            int lineScore = line.Total;
            totalScore += lineScore;

            if (resultsText != null)
                resultsText.text += $"{line.label} x{line.amount}  +{lineScore}\n";

            yield return new WaitForSecondsRealtime(lineDelay);
        }

        yield return StartCoroutine(AnimateTotalScore(totalScore));
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
            totalScoreText.text = totalPrefix + currentScore;

            yield return null;
        }

        totalScoreText.text = totalPrefix + finalScore;
    }
}