using UnityEngine;
using UnityEngine.UI;

public class DetectionMeterUI : MonoBehaviour
{
    public Image meterFill;

    public void SetValue(float value)
    {
        meterFill.fillAmount = value;
    }

    public void ResetMeter()
    {
        meterFill.fillAmount = 0;
    }
}