using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
using Helpers.Linq;
using static Level;

public class GameManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static GameState State => Instance._gameState;
    public static ScoreMode ScoreGameMode = ScoreMode.Minigolf; // Set dynamically based on level
    public static LevelGameMode CurrentGameMode = LevelGameMode.NoExtraRules; // Set dynamically based on level
    [SerializeField] private GameState _gameState;

    // Session Config
    [Networked, OnChangedRender(nameof(OnSessionConfigChanged))]
    public int CourseLengthIndex { get; set; }
    [Networked, OnChangedRender(nameof(OnSessionConfigChanged))]
    public int MaxTimeIndex { get; set; }
    [Networked, OnChangedRender(nameof(OnSessionConfigChanged))]
    public int MaxStrokesIndex { get; set; }
    [Networked, OnChangedRender(nameof(OnSessionConfigChanged))]
    public bool DoCollisions { get; set; }

    public static int CourseLength => SessionSetup._lengths[Instance.CourseLengthIndex];
    public static float MaxTime => SessionSetup._times[Instance.MaxTimeIndex].v;
    public static int MaxStrokes => SessionSetup._shots[Instance.MaxStrokesIndex].v;
    public static float ModifiedMaxTime { get; set; } = 0f; // Default to 0, meaning it won't override
    // Gameplay Data
    [Networked]
    public int CurrentHole { get; set; }
    [Networked]
    public int TickStarted { get; set; }
    public static float Time => Instance?.Object?.IsValid == true
        ? (Instance.TickStarted == 0
            ? 0
            : (Instance.Runner.Tick - Instance.TickStarted) * Instance.Runner.DeltaTime)
        : 0;

    public static GameManager Instance { get; private set; }

    void OnSessionConfigChanged()
    {
        InterfaceManager.Instance.sessionScreen.UpdateSessionConfig();
    }

    public override void Spawned()
    {
        Instance = this;
        Runner.AddCallbacks(this);
        if (Runner.IsServer)
        {
            CourseLengthIndex = SessionSetup.courseLength;
            MaxTimeIndex = SessionSetup.maxTime;
            MaxStrokesIndex = SessionSetup.maxShots;
            DoCollisions = SessionSetup.doCollisions;
            if (Runner.SessionInfo.IsVisible != !SessionSetup.isPrivate)
                Runner.SessionInfo.IsVisible = !SessionSetup.isPrivate;
        }

        if (State.Current < GameState.EGameState.Loading)
        {
            UIScreen.Focus(InterfaceManager.Instance.sessionScreen.screen);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Instance = null;
        InterfaceManager.Instance.resultsScreen.Clear();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.SimulationUnityScene.name == "Game")
        {
            Level.Load(ResourcesManager.Instance.levels[CurrentHole]);
            ScoreGameMode = Level.Current.GetScoreMode();
            CurrentGameMode = Level.Current.GetGameMode();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_LoadDone(RpcInfo info = default)
    {
        PlayerRegistry.GetPlayer(info.Source).IsLoaded = true;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_LoadGameMode()
    {
        CurrentGameMode = Level.Current.GetGameMode();
    }

    public static void PlayerDNF(PlayerObject player)
    {
        if (ScoreGameMode == ScoreMode.Minigolf)
        {
            player.TimeTaken = PlayerObject.TIME_DNF;
            player.Strokes = int.MaxValue;
        }
        else if (ScoreGameMode == ScoreMode.KingCrown)
        {
            player.TimeTaken = player.StackedTime;
        }
        else
        {
            player.TimeTaken = (Instance.Runner.Tick - Instance.TickStarted) * Instance.Runner.DeltaTime;
        }

        FinishPlayerState(player.Controller, dnf: true);
    }
    public static void CalculateScores()
    {
        ScoreGameMode = Level.Current.GetScoreMode();

        switch (ScoreGameMode)
        {
            case ScoreMode.Minigolf:
                CalculateMinigolfScores();
                break;
            case ScoreMode.LastPlayerStanding:
                CalculateLastPlayerStandingScores();
                break;
            case ScoreMode.KingCrown:
                CalculateKingCrownScores();
                break;
            case ScoreMode.Scoring:
                CalculateGameScoringScores();
                break;
        }
    }
    private static void CalculateMinigolfScores()
    {
        // Order players by strokes in descending order (higher strokes = worse rank).
        // If strokes are tied, sort by time in descending order (slower = worse rank).
        PlayerObject[] sortedPlayers = PlayerRegistry.OrderDesc(p => p.Strokes, p => !p.IsSpectator)
                                                     .ThenByDescending(p => p.TimeTaken)
                                                     .ToArray();

        Dictionary<PlayerObject, byte> scores = new Dictionary<PlayerObject, byte>();

        int totalPlayers = sortedPlayers.Length;
        int currentRank = 1; // Start ranking from the worst player
        int playersInCurrentRank = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerObject player = sortedPlayers[i];

            // If it's not the first player, check if the current one differs from the previous
            if (i > 0 &&
                (player.Strokes != sortedPlayers[i - 1].Strokes || player.TimeTaken != sortedPlayers[i - 1].TimeTaken))
            {
                // Move rank up based on the number of players in the previous rank
                currentRank += playersInCurrentRank;
                playersInCurrentRank = 1;
            }
            else
            {
                // If tied, stay in the same rank and increase the count
                playersInCurrentRank++;
            }

            // Assign score based on rank
            scores[player] = (byte)currentRank;
        }

        // Assign scores to players
        foreach (KeyValuePair<PlayerObject, byte> kvp in scores)
        {
            kvp.Key.Scores.Set(Instance.CurrentHole, kvp.Value);
        }

        // Notify UI
        InterfaceManager.Instance.resultsScreen.SetRoundScores();

        // Sort the Time taken and Strokes to then update the UI
        SortTimeandStrokes(sortedPlayers);
    }

    private static void CalculateLastPlayerStandingScores()
    {
        // Separate winners and non-winners
        List<PlayerObject> winners = new List<PlayerObject>();
        List<PlayerObject> nonWinners = new List<PlayerObject>();

        foreach (var player in PlayerRegistry.Players)
        {
            if (!player.IsSpectator)
            {
                if (player.Victory)
                    winners.Add(player);
                else
                    nonWinners.Add(player);
            }
        }

        // Sort non-winners: Lives (Ascending), then Survival Time (Ascending)
        nonWinners = nonWinners.OrderBy(p => p.Lives)
                               .ThenBy(p => p.TimeTaken)
                               .ToList();

        // Sort winners: Survival Time (Descending)
        winners = winners.OrderByDescending(p => p.TimeTaken).ToList();

        // Combine lists: Winners go at the end (higher rank)
        List<PlayerObject> combinedList = nonWinners.Concat(winners).ToList();

        PlayerObject[] sortedPlayers = combinedList.ToArray();

        Dictionary<PlayerObject, byte> scores = new Dictionary<PlayerObject, byte>();
        int totalPlayers = sortedPlayers.Length;
        int currentRank = 1; // Start from lowest position
        int playersInCurrentRank = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerObject player = sortedPlayers[i];

            // Check if current player differs from the previous one in relevant criteria
            if (i > 0)
            {
                bool differentCategory = player.Victory != sortedPlayers[i - 1].Victory;
                bool differentNonWinnerRank = !player.Victory &&
                                              (player.Lives != sortedPlayers[i - 1].Lives ||
                                               player.TimeTaken != sortedPlayers[i - 1].TimeTaken);
                bool differentWinnerRank = player.Victory &&
                                           (player.TimeTaken != sortedPlayers[i - 1].TimeTaken);

                if (differentCategory || differentNonWinnerRank || differentWinnerRank)
                {
                    // Move the rank forward by the number of players in the previous rank
                    currentRank += playersInCurrentRank;
                    playersInCurrentRank = 1;
                }
                else
                {
                    // If tied, stay in the same rank and count the tie
                    playersInCurrentRank++;
                }
            }
            else
            {
                playersInCurrentRank = 1;
            }

            // Assign rank
            scores[player] = (byte)currentRank;
        }

        // Assign scores to players
        foreach (KeyValuePair<PlayerObject, byte> kvp in scores)
        {
            kvp.Key.Scores.Set(Instance.CurrentHole, kvp.Value);
        }

        // Notify UI
        InterfaceManager.Instance.resultsScreen.SetRoundScores();

        // Convert list to array and sort it properly
        SortTimeandStrokes(sortedPlayers);
    }
    private static void CalculateGameScoringScores()
    {
        // Order players by score in ascending order (higher scores = better rank).
        PlayerObject[] sortedPlayers = PlayerRegistry.OrderAsc(p => p.GameScore, p => !p.IsSpectator)
                                                     .ToArray();

        Dictionary<PlayerObject, byte> scores = new Dictionary<PlayerObject, byte>();

        int totalPlayers = sortedPlayers.Length;
        int currentRank = 1; // Start ranking from the worst player
        int playersInCurrentRank = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerObject player = sortedPlayers[i];

            // If it's not the first player, check if the current one differs from the previous
            if (i > 0 &&
                (player.GameScore != sortedPlayers[i - 1].GameScore))
            {
                // Move rank up based on the number of players in the previous rank
                currentRank += playersInCurrentRank;
                playersInCurrentRank = 1;
            }
            else
            {
                // If tied, stay in the same rank and increase the count
                playersInCurrentRank++;
            }

            // Assign score based on rank
            scores[player] = (byte)currentRank;
        }

        // Assign scores to players
        foreach (KeyValuePair<PlayerObject, byte> kvp in scores)
        {
            kvp.Key.Scores.Set(Instance.CurrentHole, kvp.Value);
        }

        // Notify UI
        InterfaceManager.Instance.resultsScreen.SetRoundScores();

        // Sort the Time taken and Strokes to then update the UI
        SortTimeandStrokes(sortedPlayers);
    }
    private static void CalculateKingCrownScores()
    {
        // Order players by score in ascending order (higher scores = better rank, whoever has the crown has 1 score).
        // If scores are tied, sort by time in ascending order (more time = better rank).
        PlayerObject[] sortedPlayers = PlayerRegistry.OrderAsc(p => p.GameScore, p => !p.IsSpectator)
                                                     .ThenBy(p => p.TimeTaken)
                                                     .ToArray();

        Dictionary<PlayerObject, byte> scores = new Dictionary<PlayerObject, byte>();

        int totalPlayers = sortedPlayers.Length;
        int currentRank = 1; // Start ranking from the worst player
        int playersInCurrentRank = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerObject player = sortedPlayers[i];

            // If it's not the first player, check if the current one differs from the previous
            if (i > 0 &&
                (player.GameScore != sortedPlayers[i - 1].GameScore || player.TimeTaken != sortedPlayers[i - 1].TimeTaken))
            {
                // Move rank up based on the number of players in the previous rank
                currentRank += playersInCurrentRank;
                playersInCurrentRank = 1;
            }
            else
            {
                // If tied, stay in the same rank and increase the count
                playersInCurrentRank++;
            }

            // Assign score based on rank
            scores[player] = (byte)currentRank;
        }

        // Assign scores to players
        foreach (KeyValuePair<PlayerObject, byte> kvp in scores)
        {
            kvp.Key.Scores.Set(Instance.CurrentHole, kvp.Value);
        }

        // Notify UI
        InterfaceManager.Instance.resultsScreen.SetRoundScores();

        // Sort the Time taken and Strokes to then update the UI
        SortTimeandStrokes(sortedPlayers);
    }


    private static void SortTimeandStrokes(PlayerObject[] sortedPlayers)
    {
        byte[] strokeRanks = new byte[PlayerRegistry.CountPlayers];
        byte[] timeRanks = new byte[PlayerRegistry.CountPlayers]; // Store rankings for time

        List<int> storedStrokes = new List<int>(); // Stores ordered strokes for later UI update
        List<float> storedTimes = new List<float>(); // Stores ordered time taken for later UI update

        // Collect strokes and time in separate lists for ranking
        List<int> strokeList = sortedPlayers.Select(p => p.Strokes).ToList();
        List<float> timeList = sortedPlayers.Select(p => p.TimeTaken).ToList();

        // Sort strokes from lowest to highest for ranking
        List<int> sortedStrokeList = strokeList.OrderBy(stroke => stroke).ToList();
        // Sort times from lowest to highest for ranking
        List<float> sortedTimeList = timeList.OrderBy(time => time).ToList();

        // Rank the players strokes and time
        int totalPlayers = sortedPlayers.Length;

        // Rank strokes
        Dictionary<int, byte> strokeRankDict = new Dictionary<int, byte>();
        byte currentStrokeRank = 1;
        for (int i = 0; i < sortedStrokeList.Count; i++)
        {
            if (sortedStrokeList[i] != int.MaxValue)  // Skip DNF players
            {
                if (!strokeRankDict.ContainsKey(sortedStrokeList[i]))
                {
                    strokeRankDict[sortedStrokeList[i]] = currentStrokeRank;
                    currentStrokeRank++;
                }
            }
            else
            {
                // DNF players get a rank last, we assign a very high rank value
                strokeRankDict[sortedStrokeList[i]] = (byte)(totalPlayers + 1); // Put DNF at the end
            }
        }

        // Rank time
        Dictionary<float, byte> timeRankDict = new Dictionary<float, byte>();
        byte currentTimeRank = 1;
        for (int i = 0; i < sortedTimeList.Count; i++)
        {
            if (sortedTimeList[i] != PlayerObject.TIME_DNF)  // Skip DNF players
            {
                if (!timeRankDict.ContainsKey(sortedTimeList[i]))
                {
                    timeRankDict[sortedTimeList[i]] = currentTimeRank;
                    currentTimeRank++;
                }
            }
            else
            {
                // DNF players get a rank last, we assign a very high rank value
                timeRankDict[sortedTimeList[i]] = (byte)(totalPlayers + 1); // Put DNF at the end
            }
        }

        // Assign ranks to players
        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerObject player = sortedPlayers[i];

            // Assign stroke rank based on sorted stroke list
            strokeRanks[i] = strokeRankDict[player.Strokes];
            // Assign time rank based on sorted time list
            timeRanks[i] = timeRankDict[player.TimeTaken];

            // Store the strokes and time for UI updates later
            storedStrokes.Add(player.Strokes);
            storedTimes.Add(player.TimeTaken);
        }

        // Update local player UI with stored stroke and time data
        UpdateLocalPlayerUI(sortedPlayers, strokeRanks, timeRanks, storedStrokes, storedTimes);
    }

    public static void FinishPlayerState(Putter player, bool victory = false, bool instaLose = false, bool dnf = false)
    {
        var playerObj = player.PlayerObj;

        // If the player is eliminated
        if (instaLose || dnf)
        {
            playerObj.Lives = 0;
            Debug.Log($"{playerObj.Nickname} has been eliminated!");
        }
        else if (victory) // If the player won the level
        {
            player.PlayerObj.Victory = true; // Considered that won the round by reaching the end
            Debug.Log($"{playerObj.Nickname} has reached the end!");
        }

        // If the player reaches a state where they have to despawn
        if (instaLose || playerObj.Lives <= 0 || victory)
        {
            if (!dnf)
                player.PlayerObj.TimeTaken = (Instance.Runner.Tick - Instance.TickStarted) * Instance.Runner.DeltaTime;

            Instance.Runner.Despawn(player.Object);
        }

        // Check game-ending conditions
        ScoreGameMode = Level.Current.scoreMode;
        if (ScoreGameMode == ScoreMode.LastPlayerStanding && PlayerRegistry.CountPlayers > 1)
        {
            // Get the count of players who still are standing
            var remainingPlayers = PlayerRegistry.CountWhere(p => !p.HasFinished);

            if (remainingPlayers == 1) // If only one player remains, declare them the winner
            {
                PlayerRegistry.ForEachWhere(p => p.Controller, p => LastPlayerVictory(p, victory));
                State.Server_SetState(GameState.EGameState.Outro); // End the game round
            }
        }
        else
        {
            // Normal condition: End round when all players have finished
            if (PlayerRegistry.All(p => p.HasFinished))
            {
                State.Server_SetState(GameState.EGameState.Outro);
            }
        }
    }

    private static void LastPlayerVictory(PlayerObject playerObject, bool playerWon)
    {
        playerObject.Victory = !playerWon;
        playerObject.TimeTaken = (Instance.Runner.Tick - Instance.TickStarted) * Instance.Runner.DeltaTime;
        Instance.Runner.Despawn(playerObject.Controller.Object);
        Debug.Log($"{playerObject.Nickname} is the last player standing!");
    }

    public void StartGameRules()
    {
        ScoreGameMode = Level.Current.GetScoreMode();
        CurrentGameMode = Level.Current.GetGameMode();

        if (CurrentGameMode == LevelGameMode.Moles || CurrentGameMode == LevelGameMode.KingCrown)
        {
            float halfTime = Mathf.Ceil(MaxTime / 2f); // Divide by 2 and round up
            ModifiedMaxTime = Mathf.Max(halfTime, 30f); // Ensure it's at least 30 seconds
        }
        else ModifiedMaxTime = 0;

        if (CurrentGameMode == LevelGameMode.KingCrown) AssignCrownRandomly();
        else RemoveCrown();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_AddPoints(Putter player, int points)
    {
        // Add points to the player local scoring
        player.PlayerObj.GameScore += points;
        Debug.Log(player.PlayerObj.GameScore);
    }

    private CrownHolder currentCrown = null; // Stores the currently active crown
    private Putter currentTarget = null; // The player currently wearing the crown

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_AssignCrown(NetworkObject playerObject)
    {
        Putter player = playerObject.GetComponent<Putter>();
        if (player == null) return;

        // Set the new crown holder (but don't spawn the crown itself)
        currentTarget = player;
        player.PlayerObj.GameScore = 1;

        // Now tell each client to update their UI crowns
        UpdateCrownUI();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_RemoveCrown()
    {
        if (currentTarget != null)
        {
            currentTarget.PlayerObj.GameScore = 0;
            currentTarget = null;
        }

        if (currentCrown != null)
        {
            Destroy(currentCrown.gameObject);
            currentCrown = null; // Fix: Properly nullify it
        }

        UpdateCrownUI();
    }

    public void AssignCrown(Putter player)
    {
        if (player == null || !player.HasStateAuthority) return;

        Rpc_AssignCrown(player.Object);
    }

    public void RemoveCrown()
    {
        if (!HasStateAuthority) return;

        Rpc_RemoveCrown();
    }

    public void AssignCrownRandomly()
    {
        if (!HasStateAuthority) return;

        List<Putter> players = new List<Putter>();
        PlayerRegistry.ForEachWhere(p => p.Controller, p => players.Add(p.Controller));

        if (players.Count == 0) return;

        Putter randomPlayer = players[UnityEngine.Random.Range(0, players.Count)];
        Rpc_AssignCrown(randomPlayer.Object);
    }

    private void UpdateCrownUI()
    {
        // If we have a target, show the crown on them
        if (currentTarget != null) ShowCrownUI(currentTarget);
        else HideCrownUI();
    }

    private void ShowCrownUI(Putter player)
    {
        if (currentCrown != null) Destroy(currentCrown.gameObject); // Remove old one

        // Spawn UI crown in the player's world canvas
        currentCrown = Instantiate(ResourcesManager.Instance.crownPrefab, InterfaceManager.Instance.worldCanvas.transform);
        currentCrown.SetTarget(player);
    }

    private void HideCrownUI()
    {
        if (currentCrown != null)
        {
            Destroy(currentCrown.gameObject);
            currentCrown = null;
        }
    }

    public Putter GetCrownHolder()
    {
        return currentCrown != null ? currentCrown.putter : null;
    }

    public bool HasCrown(Putter player)
    {
        return currentTarget == player;
    }

    private static void UpdateLocalPlayerUI(PlayerObject[] sortedPlayers, byte[] strokeRanks, byte[] timeRanks, List<int> storedStrokes, List<float> storedTimes)
    {
        if (!PlayerObject.Local.IsSpectator)
        {
            // Find the index of the local player in the sorted list
            int playerIndex = Array.IndexOf(sortedPlayers, PlayerObject.Local);

            if (playerIndex != -1)
            {
                // Ensure playerIndex is within valid bounds
                if (playerIndex < strokeRanks.Length && playerIndex < timeRanks.Length && playerIndex < storedStrokes.Count && playerIndex < storedTimes.Count)
                {
                    int strokeRank = strokeRanks[playerIndex];
                    int strokes = storedStrokes[playerIndex];
                    int timeRank = timeRanks[playerIndex];
                    float timeTaken = storedTimes[playerIndex];

                    // Update the player's performance display with their actual strokes & time
                    InterfaceManager.Instance.performance.SetTimesText(timeTaken, timeRank);
                    InterfaceManager.Instance.performance.SetStrokesText(strokes, strokeRank);
                }
                else
                {
                    // Handle case where the playerIndex is invalid or arrays are not properly synchronized
                    Debug.LogWarning("Player index is out of bounds for the arrays.");
                }
            }
            else
            {
                // Handle case where local player is not found in the sorted players
                Debug.LogWarning("Local player not found in sorted player list.");
            }
        }
    }


    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason != ShutdownReason.Ok)
            DisconnectUI.OnShutdown(shutdownReason);
    }

    #region INetworkRunnerCallbacks
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}
