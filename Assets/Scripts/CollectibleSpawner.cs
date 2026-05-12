using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Collectible Prefab")]
    [SerializeField] private GameObject collectiblePrefab;

    [Header("Spawn Points")]
    [Tooltip("Assign empty GameObjects as spawn positions. Leave empty to use children of this object.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [Tooltip("If true, picks random spawn points each day. If false, uses all points.")]
    [SerializeField] private bool randomizeSpawnPoints = true;
    [Tooltip("Only used if randomizeSpawnPoints is true — must be <= spawnPoints count")]
    [SerializeField] private bool preventDuplicateSpots = true;

    // Pool of spawned collectibles, cleared and refilled each day
    private List<GameObject> activeCollectibles = new List<GameObject>();

    private void Start()
    {
        // Auto-populate spawnPoints from children if none assigned
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                spawnPoints[i] = transform.GetChild(i);
        }

        // Subscribe here instead of OnEnable — GameManager is guaranteed ready
        GameManager.Instance.onDayChanged.AddListener(OnDayChanged);

        // Spawn for day 1 on start
        SpawnForDay(GameManager.Instance.GetTargetForToday());
    }

    // ── Day change listener ──────────────────────────────────────────────────

    private void OnDayChanged(int newDay)
    {
        ClearCollectibles();
        SpawnForDay(GameManager.Instance.GetTargetForToday());
    }

    // ── Core spawn logic ─────────────────────────────────────────────────────

    private void SpawnForDay(int count)
    {
        if (collectiblePrefab == null)
        {
            Debug.LogError("[CollectibleSpawner] No collectible prefab assigned!");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[CollectibleSpawner] No spawn points found!");
            return;
        }

        // Clamp count to available spawn points
        int spawnCount = Mathf.Min(count, spawnPoints.Length);

        List<Transform> chosenPoints = PickSpawnPoints(spawnCount);

        foreach (Transform point in chosenPoints)
        {
            GameObject obj = Instantiate(collectiblePrefab, point.position, point.rotation);
            obj.SetActive(true);
            activeCollectibles.Add(obj);
        }

        Debug.Log($"[CollectibleSpawner] Spawned {spawnCount} collectibles for Day {GameManager.Instance.GetCurrentDay()}.");
    }

    private List<Transform> PickSpawnPoints(int count)
    {
        List<Transform> available = new List<Transform>(spawnPoints);
        List<Transform> chosen = new List<Transform>();

        if (!randomizeSpawnPoints)
        {
            // Use the first N points in order
            for (int i = 0; i < count && i < available.Count; i++)
                chosen.Add(available[i]);
        }
        else
        {
            // Fisher-Yates shuffle then take first N
            for (int i = available.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (available[i], available[j]) = (available[j], available[i]);
            }

            for (int i = 0; i < count; i++)
                chosen.Add(available[i]);
        }

        return chosen;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private void ClearCollectibles()
    {
        foreach (GameObject obj in activeCollectibles)
        {
            if (obj != null)
                Destroy(obj);
        }
        activeCollectibles.Clear();
        Debug.Log("[CollectibleSpawner] Cleared all collectibles.");
    }

    // ── Optional: manually trigger respawn from outside ──────────────────────
    public void RespawnNow() 
    {
        ClearCollectibles();
        SpawnForDay(GameManager.Instance.GetTargetForToday());
    }
}