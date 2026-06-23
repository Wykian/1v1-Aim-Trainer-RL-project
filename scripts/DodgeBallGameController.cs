using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Random = UnityEngine.Random;
using TMPro;

public class DodgeBallGameController : MonoBehaviour
{
    public enum SceneType
    {
        Game,
        Training,
        Movie,
    }
    public SceneType CurrentSceneType = SceneType.Training;

    public bool ShouldPlayEffects
    {
        get
        {
            return CurrentSceneType != SceneType.Training;
        }
    }

    public enum GameModeType
    {
        Elimination,
        CaptureTheFlag,
    }
    public GameModeType GameMode = GameModeType.Elimination;

    [Header("HUMAN PLAYER")] public GameObject PlayerGameObject;

    [Header("BALLS")] public GameObject BallPrefab;
    public float BallSpawnRadius = 3;
    public List<Transform> BallSpawnPositions;
    public int NumberOfBallsToSpawn = 10;
    public int PlayerMaxHitPoints = 5;

    [Header("CTF SETTINGS")]
    public bool DropFlagImmediately = true;
    public bool FlagMustBeHomeToScore = true;
    public bool FlagCarrierKnockback = false;
    public float CTFHitBonus = 0.02f;

    [Header("ELIMINATION SETTINGS")]
    public float EliminationHitBonus = 0.1f;

    [Header("Visual Effects")]
    public bool usePoofParticlesOnElimination;
    public List<GameObject> poofParticlesList;

    [Header("LOSER PLATFORM")]
    public List<GameObject> blueLosersList;
    public List<GameObject> purpleLosersList;

    [Header("UI Audio")]
    public AudioClip FlagHitClip;
    public AudioClip BallImpactClip1;
    public AudioClip BallImpactClip2;
    public AudioClip BallPickupClip;
    public AudioClip TauntVoiceAudioClip;
    public AudioClip HurtVoiceAudioClip;
    public AudioClip CountdownClip;
    public AudioClip WinSoundFX1;
    public AudioClip WinSoundFX2;
    public AudioClip LoseSoundFX1;
    public AudioClip LoseSoundFX2;
    private AudioSource m_audioSource;

    [Header("UI")]
    public GameObject BlueTeamWonUI;
    public GameObject PurpleTeamWonUI;
    public TMP_Text CountDownText;

    private int m_NumberOfBluePlayersRemaining = 1;
    private int m_NumberOfPurplePlayersRemaining = 1;
    private SimpleMultiAgentGroup m_Team0AgentGroup;
    private SimpleMultiAgentGroup m_Team1AgentGroup;

    // ============================================================
    // SKILL ADAPTATION
    // ============================================================
    [Header("SKILL ADAPTATION")]
    public PlayerMetricsTracker purpleAgentTracker;

    [Header("HUMAN PLAYER METRICS")]
    public PlayerMetricsTracker humanPlayerMetrics;
    // ============================================================

    [Serializable]
    public class PlayerInfo
    {
        public DodgeBallAgent Agent;
        public int HitPointsRemaining;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
        [HideInInspector]
        public int TeamID;
    }

    private bool m_Initialized;
    public List<PlayerInfo> Team0Players;
    public List<PlayerInfo> Team1Players;

    public GameObject Team0Flag;
    public GameObject Team1Flag;
    public GameObject Team0Base;
    public GameObject Team1Base;
    public List<DodgeBall> AllBallsList;

    private int m_ResetTimer;
    private float m_TimeBonus = 1.0f;
    private float m_ReturnOwnFlagBonus = 0.0f;
    private List<bool> m_FlagsAtBase = new List<bool>() { true, true };
    private EnvironmentParameters m_EnvParameters;
    private StatsRecorder m_StatsRecorder;
    private int m_NumFlagDrops = 0;

    public int MaxEnvironmentSteps = 5000;

    void Start()
    {
        if (ShouldPlayEffects)
        {
            ResetPlayerUI();
        }
    }

    void Initialize()
    {
        m_audioSource = gameObject.AddComponent<AudioSource>();
        m_StatsRecorder = Academy.Instance.StatsRecorder;
        m_EnvParameters = Academy.Instance.EnvironmentParameters;
        GameMode = getCurrentGameMode();
        m_Team0AgentGroup = new SimpleMultiAgentGroup();
        m_Team1AgentGroup = new SimpleMultiAgentGroup();
        InstantiateBalls();

        foreach (var item in Team0Players)
        {
            item.Agent.Initialize();
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.m_BehaviorParameters.TeamId = 0;
            item.TeamID = 0;
            item.Agent.NumberOfTimesPlayerCanBeHit = PlayerMaxHitPoints;
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.Initialize();
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.m_BehaviorParameters.TeamId = 1;
            item.TeamID = 1;
            item.Agent.NumberOfTimesPlayerCanBeHit = PlayerMaxHitPoints;
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

        SetActiveLosers(blueLosersList, 0);
        SetActiveLosers(purpleLosersList, 0);

        if (usePoofParticlesOnElimination)
        {
            foreach (var item in poofParticlesList)
            {
                item.SetActive(false);
            }
        }
        m_Initialized = true;
        ResetScene();
    }

    void InstantiateBalls()
    {
        foreach (var ballPos in BallSpawnPositions)
        {
            for (int i = 0; i < NumberOfBallsToSpawn; i++)
            {
                var spawnPosition = ballPos.position + Random.insideUnitSphere * BallSpawnRadius;
                GameObject g = Instantiate(BallPrefab, spawnPosition, Quaternion.identity);
                DodgeBall db = g.GetComponent<DodgeBall>();
                AllBallsList.Add(db);
                g.transform.SetParent(transform);
                g.SetActive(true);
                db.SetResetPosition(spawnPosition);
            }
        }
    }

    void FixedUpdate()
    {
        if (!m_Initialized) return;

        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps)
        {
            m_Team0AgentGroup.GroupEpisodeInterrupted();
            m_Team1AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    IEnumerator GameCountdown()
    {
        Time.timeScale = 0;
        if (ShouldPlayEffects)
        {
            m_audioSource.PlayOneShot(CountdownClip, .25f);
        }

        CountDownText.text = "3";
        CountDownText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(1);
        CountDownText.text = "2";
        yield return new WaitForSecondsRealtime(1);
        CountDownText.text = "1";
        yield return new WaitForSecondsRealtime(1);
        CountDownText.text = "Go!";
        yield return new WaitForSecondsRealtime(1);
        Time.timeScale = 1;
        CountDownText.gameObject.SetActive(false);
    }

    private GameModeType getCurrentGameMode()
    {
        float isCTFparam = m_EnvParameters.GetWithDefault("is_capture_the_flag", (float)GameMode);
        GameModeType newGameMode = isCTFparam > 0.5f ? GameModeType.CaptureTheFlag : GameModeType.Elimination;
        return newGameMode;
    }

    void SetActiveLosers(List<GameObject> list, int numOfLosers)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].SetActive(i < numOfLosers);
        }
    }

    void IncrementActiveLosers(List<GameObject> list)
    {
        int numLosers = 0;
        foreach (var loser in list)
        {
            if (loser.gameObject.activeInHierarchy)
            {
                numLosers++;
            }
        }
        SetActiveLosers(list, Math.Min(numLosers + 1, 3));
    }

    private void dropFlagIfHas(DodgeBallAgent hit, DodgeBallAgent thrower)
    {
        if (hit.HasEnemyFlag)
        {
            if (hit.teamID == 1)
            {
                copyXandZ(Team0Flag.transform, hit.transform, -0.6f, 0.6f);
                Team0Flag.gameObject.SetActive(true);
            }
            else
            {
                copyXandZ(Team1Flag.transform, hit.transform, -0.6f, 0.6f);
                Team1Flag.gameObject.SetActive(true);
            }
            hit.HasEnemyFlag = false;
            m_NumFlagDrops += 1;
            Debug.Log($"Team {hit.teamID} Dropped The Flag");
        }
    }

    public void ReturnFlag(DodgeBallAgent agent)
    {
        if (!m_FlagsAtBase[agent.teamID])
        {
            if (agent.teamID == 1)
            {
                resetTeam1Flag();
            }
            else
            {
                resetTeam0Flag();
            }
            agent.AddReward(m_ReturnOwnFlagBonus);
            Debug.Log($"Team {agent.teamID}'s flag was returned to base.");
        }
    }

    public void PlayParticleAtPosition(Vector3 pos)
    {
        foreach (var item in poofParticlesList)
        {
            if (!item.activeInHierarchy)
            {
                item.transform.position = pos;
                item.SetActive(true);
                break;
            }
        }
    }

    private bool m_GameEnded = false;
    public void ShowWinScreen(int winningTeam, float delaySeconds)
    {
        if (m_GameEnded) return;
        m_GameEnded = true;
        StartCoroutine(ShowWinScreenThenReset(winningTeam, delaySeconds));
    }

    public void EndGame(int winningTeam, float delaySeconds = 1.0f)
    {
        // ============================================================
        // TRACK HUMAN WIN/LOSS - NEW
        // ============================================================
        if (humanPlayerMetrics != null)
        {
            bool humanWon = winningTeam == 0; // team 0 = human Blue
            humanPlayerMetrics.RegisterGameResult(humanWon);

            // Sync to purple agent tracker so AI can read it
            if (purpleAgentTracker != null)
            {
                purpleAgentTracker.currentWinRate = humanPlayerMetrics.currentWinRate;
                purpleAgentTracker.currentHitAccuracy = humanPlayerMetrics.currentHitAccuracy;
            }

            float skillScore = (humanPlayerMetrics.currentWinRate +
                               humanPlayerMetrics.currentHitAccuracy) / 2f;
            string level = skillScore < 0.33f ? "Newbie" :
                          skillScore < 0.66f ? "Medium" : "Pro";

            Debug.Log($"Human won: {humanWon} | WinRate: {humanPlayerMetrics.currentWinRate:F2} | Accuracy: {humanPlayerMetrics.currentHitAccuracy:F2} | AI sees player as: {level}");
        }
        // ============================================================

        if (ShouldPlayEffects)
        {
            ShowWinScreen(winningTeam, delaySeconds);
        }
        else
        {
            ResetScene();
        }
    }

    public IEnumerator ShowWinScreenThenReset(int winningTeam, float delaySeconds)
    {
        GameObject winTextGO = winningTeam == 0 ? BlueTeamWonUI : PurpleTeamWonUI;
        AudioClip clipToUse1 = winningTeam == 0 ? WinSoundFX1 : LoseSoundFX1;
        AudioClip clipToUse2 = winningTeam == 0 ? WinSoundFX2 : LoseSoundFX2;
        yield return new WaitForSeconds(delaySeconds);
        winTextGO.SetActive(true);
        if (ShouldPlayEffects)
        {
            m_audioSource.PlayOneShot(clipToUse1, .05f);
            m_audioSource.PlayOneShot(clipToUse2, .05f);
        }

        float totalTimeSpent = 0f;
        if (winningTeam == 0)
        {
            foreach (var item in Team0Players)
            {
                if (!item.Agent.Stunned && CurrentSceneType == SceneType.Movie)
                {
                    item.Agent.Dancing = true;
                    yield return new WaitForSeconds(0.2f);
                    totalTimeSpent += 0.2f;
                }
            }
            foreach (var item in Team1Players)
            {
                item.Agent.Stunned = true;
            }
        }
        else
        {
            foreach (var item in Team1Players)
            {
                if (!item.Agent.Stunned && CurrentSceneType == SceneType.Movie)
                {
                    item.Agent.Dancing = true;
                    yield return new WaitForSeconds(0.2f);
                    totalTimeSpent += 0.2f;
                }
            }
            foreach (var item in Team0Players)
            {
                item.Agent.Stunned = true;
            }
        }
        yield return new WaitForSeconds(2f - totalTimeSpent);

        winTextGO.SetActive(false);
        ResetScene();
    }

    void ResetPlayerUI()
    {
        if (BlueTeamWonUI)
        {
            BlueTeamWonUI.SetActive(false);
        }
        if (PurpleTeamWonUI)
        {
            PurpleTeamWonUI.SetActive(false);
        }
    }

    public IEnumerator TumbleThenPoof(DodgeBallAgent agent, bool shouldPoof = true)
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        agent.Stunned = true;
        agent.AgentRb.constraints = RigidbodyConstraints.None;
        agent.AgentRb.linearDamping = .5f;
        agent.AgentRb.angularDamping = 0;
        agent.PlayStunnedVoice();
        yield return new WaitForSeconds(2f);
        if (shouldPoof)
        {
            agent.gameObject.SetActive(false);
            if (usePoofParticlesOnElimination)
            {
                PlayParticleAtPosition(agent.transform.position);
            }
            if (agent.teamID == 0)
            {
                IncrementActiveLosers(blueLosersList);
            }
            else
            {
                IncrementActiveLosers(purpleLosersList);
            }
        }
    }

    public void PlayerWasHit(DodgeBallAgent hit, DodgeBallAgent thrower)
    {
        int hitTeamID = hit.teamID;
        int throwTeamID = thrower.teamID;
        var HitAgentGroup = hitTeamID == 1 ? m_Team1AgentGroup : m_Team0AgentGroup;
        var ThrowAgentGroup = hitTeamID == 1 ? m_Team0AgentGroup : m_Team1AgentGroup;
        float hitBonus = GameMode == GameModeType.Elimination ? EliminationHitBonus : CTFHitBonus;

        // ============================================================
        // TRACK HUMAN ACCURACY - NEW
        // ============================================================
        if (humanPlayerMetrics != null)
        {
            // Human (team 0) hit the AI (team 1)
            if (thrower.teamID == 0 && hit.teamID == 1)
            {
                humanPlayerMetrics.RegisterShot(true);
                Debug.Log($"Human landed a hit! Accuracy: {humanPlayerMetrics.currentHitAccuracy:F2}");
            }
            // AI (team 1) hit the human (team 0) - human missed/failed to dodge
            else if (thrower.teamID == 1 && hit.teamID == 0)
            {
                humanPlayerMetrics.RegisterShot(false);
            }
        }
        // ============================================================

        if (DropFlagImmediately)
        {
            dropFlagIfHas(hit, thrower);
        }

        if (hit.HitPointsRemaining == 1)
        {
            if (GameMode == GameModeType.CaptureTheFlag)
            {
                hit.StunAndReset();
                dropFlagIfHas(hit, thrower);
            }
            else if (GameMode == GameModeType.Elimination)
            {
                m_NumberOfBluePlayersRemaining -= hitTeamID == 0 ? 1 : 0;
                m_NumberOfPurplePlayersRemaining -= hitTeamID == 1 ? 1 : 0;
                if (m_NumberOfBluePlayersRemaining == 0 || m_NumberOfPurplePlayersRemaining == 0 || hit.gameObject == PlayerGameObject)
                {
                    ThrowAgentGroup.AddGroupReward(2.0f - m_TimeBonus * (m_ResetTimer / MaxEnvironmentSteps));
                    HitAgentGroup.AddGroupReward(-1.0f);
                    ThrowAgentGroup.EndGroupEpisode();
                    HitAgentGroup.EndGroupEpisode();
                    print($"Team {throwTeamID} Won");
                    hit.DropAllBalls();
                    if (ShouldPlayEffects)
                    {
                        StartCoroutine(TumbleThenPoof(hit, false));
                    }
                    EndGame(throwTeamID);
                }
                else
                {
                    if (ShouldPlayEffects)
                    {
                        StartCoroutine(TumbleThenPoof(hit));
                    }
                    else
                    {
                        hit.gameObject.SetActive(false);
                    }
                    hit.DropAllBalls();
                }
            }
        }
        else
        {
            hit.HitPointsRemaining--;
            thrower.AddReward(hitBonus);
        }
    }

    public void FlagWasTaken(DodgeBallAgent agent)
    {
        if (!m_GameEnded)
        {
            if (agent.teamID == 1)
            {
                Team0Flag.gameObject.SetActive(false);
                if (FlagMustBeHomeToScore)
                {
                    Team0Base.gameObject.SetActive(false);
                }
                m_FlagsAtBase[0] = false;
            }
            else
            {
                Team1Flag.gameObject.SetActive(false);
                if (FlagMustBeHomeToScore)
                {
                    Team1Base.gameObject.SetActive(false);
                }
                m_FlagsAtBase[1] = false;
            }
            agent.HasEnemyFlag = true;
            Debug.Log($"Team {agent.teamID} Stole The Flag");
        }
    }

    private void copyXandZ(Transform targetTransform, Transform sourceTransform, float Xoffset = 0.0f, float Zoffset = 0.0f)
    {
        var localOffset = new Vector3(Xoffset, 0.0f, Zoffset);
        var globalOffset = sourceTransform.TransformDirection(localOffset);
        targetTransform.position = new Vector3(sourceTransform.position.x, targetTransform.position.y, sourceTransform.position.z) + globalOffset;
        targetTransform.rotation = sourceTransform.rotation;
    }

    private void resetTeam0Flag()
    {
        Team0Flag.gameObject.SetActive(true);
        Team0Base.gameObject.SetActive(true);
        copyXandZ(Team0Flag.transform, Team0Base.transform);
        m_FlagsAtBase[0] = true;
    }

    private void resetTeam1Flag()
    {
        Team1Flag.gameObject.SetActive(true);
        Team1Base.gameObject.SetActive(true);
        copyXandZ(Team1Flag.transform, Team1Base.transform);
        m_FlagsAtBase[1] = true;
    }

    public void FlagWasBroughtHome(DodgeBallAgent agent)
    {
        if (m_FlagsAtBase[agent.teamID] || !FlagMustBeHomeToScore)
        {
            var WinAgentGroup = agent.teamID == 1 ? m_Team1AgentGroup : m_Team0AgentGroup;
            var LoseAgentGroup = agent.teamID == 1 ? m_Team0AgentGroup : m_Team1AgentGroup;
            WinAgentGroup.AddGroupReward(2.0f - m_TimeBonus * (float)m_ResetTimer / MaxEnvironmentSteps);
            LoseAgentGroup.AddGroupReward(-1.0f);
            WinAgentGroup.EndGroupEpisode();
            LoseAgentGroup.EndGroupEpisode();
            print($"Team {agent.teamID} Won");
            m_StatsRecorder.Add("Environment/Flag Drops Per Ep", m_NumFlagDrops);
            if (ShouldPlayEffects)
            {
                var winningBase = agent.teamID == 1 ? Team1Base : Team0Base;
                var particles = winningBase.GetComponentInChildren<ParticleSystem>();
                if (particles != null)
                {
                    particles.Play();
                }
            }
            EndGame(agent.teamID, 0.0f);
        }
    }

    private void GetAllParameters()
    {
        float defaultTimeBonus = GameMode == GameModeType.CaptureTheFlag ? 0.0f : 1.0f;
        m_TimeBonus = m_EnvParameters.GetWithDefault("time_bonus_scale", defaultTimeBonus);
        m_ReturnOwnFlagBonus = m_EnvParameters.GetWithDefault("return_flag_bonus", 0.0f);
        CTFHitBonus = m_EnvParameters.GetWithDefault("ctf_hit_reward", CTFHitBonus);
        EliminationHitBonus = m_EnvParameters.GetWithDefault("elimination_hit_reward", EliminationHitBonus);
    }

    // ============================================================
    // SKILL ADAPTATION - STAGE 3
    // Uses continuous random values for infinite skill levels
    // ============================================================
    public void RandomizeOpponentSkillProfile()
    {
        if (purpleAgentTracker == null) return;

        // Only randomize during training - not during actual gameplay
        if (CurrentSceneType == SceneType.Training)
        {
            purpleAgentTracker.currentWinRate = Random.Range(0.0f, 1.0f);
            purpleAgentTracker.currentHitAccuracy = Random.Range(0.0f, 1.0f);

            float skillScore = (purpleAgentTracker.currentWinRate +
                               purpleAgentTracker.currentHitAccuracy) / 2f;

            if (skillScore < 0.33f)
                Debug.Log($"Simulating Newbie - WinRate: {purpleAgentTracker.currentWinRate:F2}, Accuracy: {purpleAgentTracker.currentHitAccuracy:F2}");
            else if (skillScore < 0.66f)
                Debug.Log($"Simulating Medium - WinRate: {purpleAgentTracker.currentWinRate:F2}, Accuracy: {purpleAgentTracker.currentHitAccuracy:F2}");
            else
                Debug.Log($"Simulating Pro - WinRate: {purpleAgentTracker.currentWinRate:F2}, Accuracy: {purpleAgentTracker.currentHitAccuracy:F2}");
        }
    }
    // ============================================================

    void ResetScene()
    {
        StopAllCoroutines();

        if (ShouldPlayEffects)
        {
            ResetPlayerUI();
            if (CurrentSceneType == SceneType.Game)
            {
                StartCoroutine(GameCountdown());
            }
        }
        m_NumberOfBluePlayersRemaining = 1;
        m_NumberOfPurplePlayersRemaining = 1;

        m_GameEnded = false;
        m_NumFlagDrops = 0;
        m_ResetTimer = 0;
        GameMode = getCurrentGameMode();

        GetAllParameters();

        // Only randomize in training mode
        RandomizeOpponentSkillProfile();

        print($"Resetting {gameObject.name}");
        int ballSpawnNum = 0;
        int ballSpawnedInPosition = 0;
        for (int ballNum = 0; ballNum < AllBallsList.Count; ballNum++)
        {
            var item = AllBallsList[ballNum];
            item.BallIsInPlay(false);
            item.rb.linearVelocity = Vector3.zero;
            item.gameObject.SetActive(true);
            var spawnPosition = BallSpawnPositions[ballSpawnNum].position + Random.insideUnitSphere * BallSpawnRadius;
            item.transform.position = spawnPosition;
            item.SetResetPosition(spawnPosition);

            ballSpawnedInPosition++;

            if (ballSpawnedInPosition >= NumberOfBallsToSpawn)
            {
                ballSpawnNum++;
                ballSpawnedInPosition = 0;
            }
        }

        foreach (var item in Team0Players)
        {
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            m_Team0AgentGroup.RegisterAgent(item.Agent);
        }
        foreach (var item in Team1Players)
        {
            item.Agent.HitPointsRemaining = PlayerMaxHitPoints;
            item.Agent.gameObject.SetActive(true);
            item.Agent.ResetAgent();
            m_Team1AgentGroup.RegisterAgent(item.Agent);
        }

        if (GameMode == GameModeType.CaptureTheFlag)
        {
            resetTeam1Flag();
            resetTeam0Flag();
        }
        else
        {
            Team0Flag.gameObject.SetActive(false);
            Team1Flag.gameObject.SetActive(false);
        }

        SetActiveLosers(blueLosersList, 0);
        SetActiveLosers(purpleLosersList, 0);
    }

    void Update()
    {
        if (!m_Initialized)
        {
            Initialize();
        }
    }
}
