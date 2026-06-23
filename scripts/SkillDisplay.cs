using UnityEngine;
using TMPro;

public class SkillDisplay : MonoBehaviour
{
    public PlayerMetricsTracker playerMetrics;
    public TMP_Text displayText;

    void Update()
    {
        if (playerMetrics != null && displayText != null)
        {
            float skillScore = (playerMetrics.currentWinRate +
                               playerMetrics.currentHitAccuracy) / 2f;

            string level = skillScore < 0.33f ? "🟢 Newbie" :
                          skillScore < 0.66f ? "🟡 Medium" : "🔴 Pro";

            displayText.text =
                $"Win Rate: {playerMetrics.currentWinRate:F2}\n" +
                $"Accuracy: {playerMetrics.currentHitAccuracy:F2}\n" +
                $"AI sees you as: {level}";
        }
    }
}
