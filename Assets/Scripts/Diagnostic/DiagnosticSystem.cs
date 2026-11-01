using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using Unity.MLAgents;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using UnityEngine.TestTools;

public class DiagnosticSystem { 

    #region RewardsByType
    private struct RewardStats
    {
        public int count;
        public float total;

        public void Register(float reward)
        {
            count++; // increment count at each call
            total += reward;
        }
    }

    private static Dictionary<REWARDS_TYPE, RewardStats> rewardsByType = new Dictionary<REWARDS_TYPE, RewardStats>();
    #endregion

    #region RewardsByAgent
    private class AgentStats
    {
        public float sessionTotal;
        public int rewardCount;

        // queue of each reward in session + total : to get average
        public Queue<float> recentEpisodes = new Queue<float>();
        public float recentEpisodeTotal;
    }

    private static Dictionary<FlipperAgent, AgentStats> rewardsByAgent = new Dictionary<FlipperAgent, AgentStats>();

    private const int AverageWindow = 20;

    #endregion

    #region Global

    // Global
    private static float globalTotal;
    private static int globalRewardCount;
    private static int globalEpisodes = 0;

    private static Queue<float> globalRecentEpisodes = new Queue<float>();
    private static float globalRecentEpisodeTotal;
    private static int globalEpisodesSinceReport;
    private static int globalDrainsSinceReport;
    private static int globalTargetsSinceReport;
    private const int GlobalReportWindow = 20;
    #endregion

    #region LongPlay
    static int totalTargetHits = 0;
    static int totalLongPlayEpisodes = 0;

    #endregion

    public static void AddReward(FlipperAgent agent, REWARDS_TYPE type)
    {
        if (agent == null) return;

        float value = 0;
        switch (type)
        {
            case REWARDS_TYPE.DRAIN:
                value = RewardConfig.Drain;
                globalDrainsSinceReport++;
                break;
            case REWARDS_TYPE.TARGET:
                value = RewardConfig.Target;
                globalTargetsSinceReport++;
                break;
            default:
                Debug.LogWarning("Reward type not founded : " + type);
                return;
        }

        // give reward to this agent
        agent.AddReward(value);

        // update stats by reward type 
        UpdateStatsByType(type, value);

        // update agent stat
        UpdateAgentStats(agent, value);

        // update global stats
        globalRewardCount++;
        globalTotal += value;

    }

    public static void AddRelativeReward(FlipperAgent agent, REWARDS_TYPE type, float relativeValue)
    {
        if (agent == null) return;

        float value = 0;
        switch (type)
        {
            // add reward relative to min target distance (more it is away from the target, more it receives a significant negative reward)
            case REWARDS_TYPE.MIN_TARGET_DISTANCE:
                value = Mathf.Lerp(0, RewardConfig.MinTargetDistance, relativeValue); // if min distance == 0 (reached target) => no reward
                Debug.Log("Normalized minDistance: " + relativeValue + " | reward value: " + value);
                break;
            case REWARDS_TYPE.BAD_DIRECTION:
                value = Mathf.Lerp(0f, RewardConfig.BadDirection, relativeValue);
                Debug.Log("Bad direction: " + relativeValue + " | reward value: " + value);
                break;
            default:
                Debug.LogWarning("Reward type not founded : " + type);
                return;
        }

        // give reward to this agent
        agent.AddReward(value);

        // update stats by reward type 
        UpdateStatsByType(type, value);

        // update agent stat
        UpdateAgentStats(agent, value);

        // update global stats
        globalRewardCount++;
        globalTotal += value;

    }
         
    private static void UpdateStatsByType(REWARDS_TYPE type, float value)
    {
        // Update stats (cumul value && nb) by reward type
        if (!rewardsByType.ContainsKey(type))  rewardsByType[type] = new RewardStats();

        RewardStats typeStats = rewardsByType[type];
        typeStats.Register(value);
        rewardsByType[type] = typeStats;
    }

    private static void UpdateAgentStats(FlipperAgent agent, float value)
    {
        // if agent isn't registered => create a new key for it
        if (!rewardsByAgent.ContainsKey(agent))  rewardsByAgent[agent] = new AgentStats();

        AgentStats agentStats = rewardsByAgent[agent];
        agentStats.sessionTotal += value;
        agentStats.rewardCount++; // increment count at each call
    }


    // Called at the end of each episode
    public static void RegisterEpisodeEnd(FlipperAgent agent, float episodeReward)
    {
        // -- AGENT STATS -- 
        if (agent == null) return;

        // no agent registered => create a new key for it
        if (!rewardsByAgent.ContainsKey(agent))
            rewardsByAgent[agent] = new AgentStats();

        AgentStats stats = rewardsByAgent[agent]; // stats for this agent

        stats.recentEpisodes.Enqueue(episodeReward); // add rewards of the episode in list
        stats.recentEpisodeTotal += episodeReward;   // add rewards to the total 

        //keep max the 100 last episodes
        while (stats.recentEpisodes.Count > AverageWindow)
        {
            stats.recentEpisodeTotal -= stats.recentEpisodes.Dequeue();
        }

        // -- DEBUG AVERAGE PER AGENT --
        float average = stats.recentEpisodeTotal / stats.recentEpisodes.Count;

        Debug.Log(
            "EPISODE ENDED | AGENT DIAG \n" +
            "Cumulative rewards for episode : " + episodeReward + 
            " | Average " + stats.recentEpisodes.Count + " last episodes: " + average + 
            " | Cumul session: " + stats.sessionTotal);

        // -- AVERAGE GLOBAL --
        globalEpisodes++;

        globalRecentEpisodes.Enqueue(episodeReward);
        globalRecentEpisodeTotal += episodeReward;

        while (globalRecentEpisodes.Count > AverageWindow)
        {
            globalRecentEpisodeTotal -= globalRecentEpisodes.Dequeue();
        }

        globalEpisodesSinceReport++;

        // display global diag every 20 global episodes
        if (globalEpisodesSinceReport >= GlobalReportWindow)
        {
            float globalAverage = globalRecentEpisodeTotal / globalRecentEpisodes.Count;

            Debug.Log(
                "FREQUENT DIAGNOSTIC | Achieved episodes : " + globalEpisodesSinceReport +
                " | Average of " + globalRecentEpisodes.Count + " last episodes : " + globalAverage +
                " | DRAINS : " + globalDrainsSinceReport +
                " | TARGETS : " + globalTargetsSinceReport
            );

            DisplayGlobalDiagnostic();

            globalEpisodesSinceReport = 0;
            globalDrainsSinceReport = 0;
            globalTargetsSinceReport = 0;
        }
    }

    // average per agent
    public static float GetRecentAverage(FlipperAgent agent)
    {
        // if no agent or not registered => average = 0
        if (agent == null || !rewardsByAgent.ContainsKey(agent))
            return 0f;

        AgentStats stats = rewardsByAgent[agent];

        if (stats.recentEpisodes.Count == 0)
            return 0f;

        // average
        return stats.recentEpisodeTotal / stats.recentEpisodes.Count;
    }

    // global diag at the end of the session
    private static void DisplayGlobalDiagnostic()
    {
        // display cumul reward (global && count)
        Debug.Log("GLOBAL DIAGNOSTIC \n Reward count : " + globalRewardCount + " | Cumul: " + globalTotal);
        float average = ((float)rewardsByType[REWARDS_TYPE.TARGET].count / (float)globalEpisodes)*100;
        Debug.Log("AVERAGE \n : " +  average.ToString("#.00") + "% | " + rewardsByType[REWARDS_TYPE.TARGET].count + "/" + globalEpisodes);

        // display cumul reward per type
        foreach (var stats in rewardsByType)
        {
            // Display
            Debug.Log(stats.Key + " | Count : " + stats.Value.count +
                                  " | Cumul value : " + stats.Value.total);
        }
    }

    // for long play episodes
    public static void RegisterTargetHits(int targetHits)
    {
        // pinball area stats
        Debug.Log("Target hits for pinball area registered in this episode : " + targetHits);

        // -- global --
        totalTargetHits += targetHits;
        totalLongPlayEpisodes++;

        float average = (float)totalTargetHits / totalLongPlayEpisodes;

        Debug.Log("AVERAGE \n : " + average.ToString("#.00") + " target.s touched per episode");
    }
}
