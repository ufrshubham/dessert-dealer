using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{

    [Header("Spawn Points")]
    [Tooltip("Assign empty GameObjects as spawn positions. Leave empty to use children of this object.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [Tooltip("If true, picks random spawn points each day. If false, uses all points.")]
    [SerializeField] private bool randomizeSpawnPoints = true;
    [Tooltip("Only used if randomizeSpawnPoints is true — must be <= spawnPoints count")]
    [SerializeField] private bool randomizeDesserts    = true;

    // Pool of spawned collectibles, cleared and refilled each day
    private List<GameObject> allInstances = new List<GameObject>();

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
        PrewarmInstances();

        ActivateForDay(GameManager.Instance.GetTargetForToday());
    }

    // ── Day change listener ──────────────────────────────────────────────────

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.onDayChanged.RemoveListener(OnDayChanged);
    }

    private void OnDayChanged(int newDay)
    {
        DisableAll();
        ActivateForDay(GameManager.Instance.GetTargetForToday());
    }

    private void PrewarmInstances()
    {
        List<GameObject> desserts = DessertManager.Instance.desserts;

        if (desserts == null || desserts.Count == 0)
        {
            Debug.LogError("[CollectibleSpawner] DessertManager.desserts is empty!");
            return;
        }

        // Spawn enough instances to cover the largest possible day target
        int maxNeeded = Mathf.Min(spawnPoints.Length, desserts.Count * 3); // generous pool

        for (int i = 0; i < maxNeeded; i++)
        {
            GameObject prefab = desserts[i % desserts.Count];
            GameObject obj    = Instantiate(prefab);
            obj.SetActive(false);   // start inactive
            allInstances.Add(obj);
        }

        Debug.Log($"[CollectibleSpawner] Pre-warmed {allInstances.Count} instances.");
    }

    // ── Core spawn logic ─────────────────────────────────────────────────────

    // ── Activate N instances at chosen spawn points ───────────────────────────

    private void ActivateForDay(int count)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[CollectibleSpawner] No spawn points!");
            return;
        }

        int spawnCount         = Mathf.Min(count, spawnPoints.Length, allInstances.Count);
        List<Transform> points = PickSpawnPoints(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject obj = allInstances[i];
            obj.transform.position = points[i].position;
            obj.transform.rotation = points[i].rotation;
            obj.SetActive(true);
        }

        Debug.Log($"[CollectibleSpawner] Activated {spawnCount} collectibles for Day {GameManager.Instance.GetCurrentDay()}.");
    }


    private List<Transform> PickSpawnPoints(int count)
    {
        List<Transform> available = new List<Transform>(spawnPoints);

        if (randomizeSpawnPoints)
            Shuffle(available);

        return available.GetRange(0, Mathf.Min(count, available.Count));
    }

    // ── Pick desserts from DessertManager list ───────────────────────────────

    private List<GameObject> PickDesserts(int count, List<GameObject> source)
    {
        List<GameObject> available = new List<GameObject>(source);
        List<GameObject> result    = new List<GameObject>();

        if (randomizeDesserts)
            Shuffle(available);

        // If count > source size, wrap around (repeat desserts)
        for (int i = 0; i < count; i++)
            result.Add(available[i % available.Count]);

        return result;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    private void DisableAll()
    {
        foreach (GameObject obj in allInstances)
            if (obj != null) obj.SetActive(false);
    }

    public void RespawnNow()
    {
        DisableAll();
        ActivateForDay(GameManager.Instance.GetTargetForToday());
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}