using UnityEngine;

public class PlayerMetricsTracker : MonoBehaviour
{
    [Header("Player Performance Metrics")]
    public float currentWinRate = 0.5f;
    public float currentHitAccuracy = 0.5f;

    private int totalShots = 0;
    private int totalHits = 0;
    private int totalWins = 0;
    private int totalGames = 0;

    public void RegisterShot(bool wasHit)
    {
        totalShots++;
        if (wasHit) totalHits++;
        if (totalShots > 0)
            currentHitAccuracy = (float)totalHits / totalShots;
    }

    public void RegisterGameResult(bool playerWon)
    {
        totalGames++;
        if (playerWon) totalWins++;
        if (totalGames > 0)
            currentWinRate = (float)totalWins / totalGames;
    }

    public void Reset()
    {
        totalShots = 0;
        totalHits = 0;
        totalWins = 0;
        totalGames = 0;
        currentWinRate = 0.5f;
        currentHitAccuracy = 0.5f;
    }
}
