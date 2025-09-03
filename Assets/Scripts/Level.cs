using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Level : NetworkBehaviour
{
    public static Level Current { get; private set; }
	public enum ScoreMode
	{
		Minigolf, 
		LastPlayerStanding,
		Racing,
        Scoring,
        KingCrown
    }
    public enum LevelGameMode
    {
        NoExtraRules,
        Snake,
        KingCrown,
        Moles
    }
    [SerializeField] private EventTriggerer[] startEvents;
    public string gameModeName;
    public ScoreMode scoreMode = ScoreMode.Minigolf;
    public LevelGameMode gameMode = LevelGameMode.NoExtraRules;
    public List<Transform> SpawnPoints;
    public float spawnHeight = 1f;

    public void TriggerEvents()
    {
        if (startEvents == null) return;

        foreach (EventTriggerer trigger in startEvents)
            trigger.TriggerEvent();
    }

    public static void Load(Level level)
    {
        Unload();

        if (GameManager.Instance.Runner.CanSpawn)
        {
            Level newLevel = GameManager.Instance.Runner.Spawn(ResourcesManager.Instance.levels[GameManager.Instance.CurrentHole]);

            if (level != null) newLevel.scoreMode = level.scoreMode; // Set correct mode
        }
    }

    public static void Unload()
	{
		if (Current)
		{
			GameManager.Instance.Runner.Despawn(Current.Object);
			Current = null;
		}
	}
    public ScoreMode GetScoreMode()
    {
        Debug.Log($"Score rule: {scoreMode}");
        return scoreMode;
    }
    public LevelGameMode GetGameMode()
    {
        Debug.Log($"Game mode rule: {gameMode}");
        return gameMode;
    }

    public override void Spawned()
	{
		Current = this;
		GameManager.Instance.Rpc_LoadDone();
	}

    public Vector3 GetSpawnPosition(int index)
    {
        // Check if there are defined spawn points
        if (SpawnPoints != null && SpawnPoints.Count > 0)
        {
            // Calculate the spawn index using modulo to loop around
            int spawnIndex = index % SpawnPoints.Count;
            Transform spawnPoint = SpawnPoints[spawnIndex];

            if (spawnPoint != null)
            {
                Vector3 spawnPos = spawnPoint.position;
                spawnPos.y += spawnHeight; // Adjust to spawn height if needed
                return spawnPos;
            }
        }

        // Fallback to the existing random spawn logic
        Vector2 p = Random.insideUnitCircle * 0.15f;
        return new Vector3(p.x, spawnHeight, p.y);
    }


    private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(Vector3.up * spawnHeight, 0.03f);
	}
}
