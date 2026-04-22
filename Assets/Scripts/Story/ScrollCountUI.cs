using TMPro;
using UnityEngine;

public class ScrollCountUI : MonoBehaviour
{
    public TMP_Text countText;

    void Update()
    {
        if (countText == null) return;

        if (GameSceneManager.Instance == null)
        {
            countText.text = "GameSceneManager NULL";
            return;
        }

        countText.text = ": " + GameSceneManager.Instance.ReadScrollCount;
    }
}