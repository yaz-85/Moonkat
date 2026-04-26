using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages event-driven enemy waves based on the number of enemies defeated.
/// Allows spawning different enemy types and boss encounters at specific defeat thresholds.
/// </summary>
public class WaveManager : MonoBehaviour
{
    /// <summary>
    /// Defines a single wave of enemies to spawn at a specific condition
    /// </summary>
    [System.Serializable]
    public class Wave
    {
        [Header("Wave Trigger")]
        [Tooltip("This wave activates when the player has defeated this many enemies")]
        public int triggeredAtEnemyCount = 0;

        [Tooltip("If true, this wave only spawns once. If false, spawns continuously every spawnDelay seconds after being triggered")]
        public bool spawnOnce = true;

        [Tooltip("If true, the previous wave will stop spawning new enemies when this wave is activated")]
        public bool disablePreviousWaveOnActivation = false;

        [Header("Enemy Configuration")]
        [Tooltip("The enemy prefab to spawn for this wave. Leave empty if using 'Enemy To Activate' instead")]
        public GameObject enemyPrefab = null;

        [Tooltip("Alternatively: Drag a pre-placed INACTIVE enemy from the scene to activate it when the wave triggers. Takes priority over spawning if assigned")]
        public GameObject enemyToActivate = null;

        [Tooltip("How many enemies to spawn per cycle (only used for spawning, ignored if Enemy To Activate is assigned)")]
        [Min(1)]
        public int spawnCount = 1;

        [Tooltip("Optional: Maximum total number of enemies to spawn from this wave. Set to 0 for unlimited. Useful for preventing infinite waves")]
        [Min(0)]
        public int maxEnemiesPerWave = 0;

        [Tooltip("Delay in seconds between each spawn in this wave")]
        [Min(0.1f)]
        public float spawnDelay = 1.0f;

        [Tooltip("Optional: Delay in seconds before the FIRST spawn of this wave after it's triggered (useful for boss intros or story moments)")]
        [Min(0f)]
        public float initialSpawnDelay = 0f;

        [Header("Spawn Location")]
        [Tooltip("If assigned, enemies spawn at this specific location. If not assigned, spawns at WaveManager location")]
        public Transform spawnLocation = null;

        [Tooltip("If true and no spawn location is assigned, enemies spawn at a random offset from WaveManager")]
        public bool useRandomOffset = false;

        [Tooltip("Random spawn offset range (only used if useRandomOffset is true)")]
        public Vector2 randomOffsetRange = new Vector2(5f, 5f);

        [Tooltip("Optional: The player or target for enemies to follow")]
        public Transform targetForEnemies = null;

        [Tooltip("Optional: Parent transform for spawned enemy projectiles (for scene organization)")]
        public Transform projectileHolder = null;

        // Internal tracking
        [HideInInspector]
        public bool hasBeenTriggered = false;

        [HideInInspector]
        public float lastSpawnTime = Mathf.NegativeInfinity;

        [HideInInspector]
        public bool isFirstSpawn = true;

        [HideInInspector]
        public bool isDisabled = false;

        [HideInInspector]
        public int enemiesSpawnedFromThisWave = 0;
    }

    [Tooltip("List of waves to spawn during the game")]
    public List<Wave> waves = new List<Wave>();

    [Header("General Settings")]
    [Tooltip("Enable debug logging to monitor wave triggering")]
    public bool enableDebugLogging = true;

    // Tracks the index of the currently active wave
    private int currentActiveWaveIndex = -1;

    /// <summary>
    /// Description:
    /// Standard Unity function called every frame
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void Update()
    {
        CheckAndTriggerWaves();
    }

    /// <summary>
    /// Description:
    /// Checks if any waves should be triggered or spawned based on current enemy defeat count
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    private void CheckAndTriggerWaves()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        int currentEnemiesDefeated = GameManager.instance.EnemiesDefeated;

        for (int i = 0; i < waves.Count; i++)
        {
            Wave wave = waves[i];

            // Check if this wave's trigger condition is met
            if (currentEnemiesDefeated >= wave.triggeredAtEnemyCount && !wave.hasBeenTriggered)
            {
                wave.hasBeenTriggered = true;

                // If this wave should disable the previous wave, do so
                if (wave.disablePreviousWaveOnActivation && currentActiveWaveIndex >= 0)
                {
                    DisableWave(currentActiveWaveIndex);
                    if (enableDebugLogging)
                    {
                        Debug.Log($"[WaveManager] Wave {currentActiveWaveIndex} disabled by Wave {i}");
                    }
                }

                // Update current active wave
                currentActiveWaveIndex = i;

                if (enableDebugLogging)
                {
                    Debug.Log($"[WaveManager] Wave {i} triggered at {wave.triggeredAtEnemyCount} enemies defeated!");
                }
            }

            // If wave is triggered, handle spawning
            if (wave.hasBeenTriggered)
            {
                HandleWaveSpawning(wave);
            }
        }
    }

    /// <summary>
    /// Description:
    /// Handles the spawning logic for a specific wave
    /// Inputs: 
    /// Wave wave
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="wave">The wave to handle spawning for</param>
    private void HandleWaveSpawning(Wave wave)
    {
        // Skip spawning if this wave has been disabled
        if (wave.isDisabled)
        {
            return;
        }

        // Check if this wave has reached its max enemy limit
        if (wave.maxEnemiesPerWave > 0 && wave.enemiesSpawnedFromThisWave >= wave.maxEnemiesPerWave)
        {
            return;
        }

        // Determine which delay to use (initial delay for first spawn, regular delay for subsequent)
        float delayToUse = wave.isFirstSpawn ? wave.initialSpawnDelay : wave.spawnDelay;

        // Check if it's time to spawn for this wave
        if (Time.timeSinceLevelLoad > wave.lastSpawnTime + delayToUse)
        {
            // If this wave uses pre-placed activation mode
            if (wave.enemyToActivate != null)
            {
                ActivateWaveEnemy(wave);
            }
            // Otherwise, spawn new enemies
            else
            {
                for (int i = 0; i < wave.spawnCount; i++)
                {
                    // Check if we've reached the max before spawning each enemy
                    if (wave.maxEnemiesPerWave > 0 && wave.enemiesSpawnedFromThisWave >= wave.maxEnemiesPerWave)
                    {
                        break;
                    }
                    SpawnWaveEnemy(wave);
                    wave.enemiesSpawnedFromThisWave++;
                }
            }

            wave.lastSpawnTime = Time.timeSinceLevelLoad;
            wave.isFirstSpawn = false;

            if (enableDebugLogging)
            {
                string action = wave.enemyToActivate != null ? "Activated" : $"Spawned {wave.spawnCount} enemy(ies) from";
                Debug.Log($"[WaveManager] {action} wave");
            }

            // If this wave only spawns once, mark it as complete by setting spawn delay to infinity
            if (wave.spawnOnce)
            {
                wave.spawnDelay = Mathf.Infinity;
            }
        }
    }

    /// <summary>
    /// Description:
    /// Spawns a single enemy for a wave
    /// Inputs: 
    /// Wave wave
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="wave">The wave configuration to use for spawning</param>
    private void SpawnWaveEnemy(Wave wave)
    {
        if (wave.enemyPrefab == null)
        {
            Debug.LogError("[WaveManager] Enemy prefab is null for a wave! Cannot spawn.");
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition = GetWaveSpawnPosition(wave);

        // Instantiate the enemy
        GameObject spawnedEnemy = Instantiate(wave.enemyPrefab, spawnPosition, wave.enemyPrefab.transform.rotation, null);

        // Configure the spawned enemy
        Enemy enemyComponent = spawnedEnemy.GetComponent<Enemy>();
        if (enemyComponent != null && wave.targetForEnemies != null)
        {
            enemyComponent.followTarget = wave.targetForEnemies;
        }

        // Configure projectile holders for all guns
        ShootingController[] shootingControllers = spawnedEnemy.GetComponentsInChildren<ShootingController>();
        if (wave.projectileHolder != null)
        {
            foreach (ShootingController gun in shootingControllers)
            {
                gun.projectileHolder = wave.projectileHolder;
            }
        }
    }

    /// <summary>
    /// Description:
    /// Activates a pre-placed inactive enemy from the scene
    /// Inputs: 
    /// Wave wave
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="wave">The wave configuration to use for activation</param>
    private void ActivateWaveEnemy(Wave wave)
    {
        if (wave.enemyToActivate == null)
        {
            Debug.LogError("[WaveManager] Enemy to activate is null for a wave! Cannot activate.");
            return;
        }

        // Activate the pre-placed enemy
        wave.enemyToActivate.SetActive(true);

        // Configure the activated enemy if it has the Enemy component
        Enemy enemyComponent = wave.enemyToActivate.GetComponent<Enemy>();
        if (enemyComponent != null && wave.targetForEnemies != null)
        {
            enemyComponent.followTarget = wave.targetForEnemies;
        }

        // Configure projectile holders for all guns if needed
        if (wave.projectileHolder != null)
        {
            ShootingController[] shootingControllers = wave.enemyToActivate.GetComponentsInChildren<ShootingController>();
            foreach (ShootingController gun in shootingControllers)
            {
                gun.projectileHolder = wave.projectileHolder;
            }
        }
    }

    /// <summary>
    /// Description:
    /// Determines the spawn position for a wave enemy based on wave configuration
    /// Inputs: 
    /// Wave wave
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The position where the enemy should spawn</returns>
    private Vector3 GetWaveSpawnPosition(Wave wave)
    {
        // If a specific spawn location is assigned, use it
        if (wave.spawnLocation != null)
        {
            return wave.spawnLocation.position;
        }

        // If random offset is enabled, spawn near WaveManager with random offset
        if (wave.useRandomOffset)
        {
            float randomX = Random.Range(-wave.randomOffsetRange.x, wave.randomOffsetRange.x);
            float randomY = Random.Range(-wave.randomOffsetRange.y, wave.randomOffsetRange.y);
            return transform.position + new Vector3(randomX, randomY, 0);
        }

        // Default: spawn at WaveManager location
        return transform.position;
    }

    /// <summary>
    /// Description:
    /// Public method to manually reset all waves (useful for testing or level resets)
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void ResetAllWaves()
    {
        foreach (Wave wave in waves)
        {
            wave.hasBeenTriggered = false;
            wave.lastSpawnTime = Mathf.NegativeInfinity;
            wave.isFirstSpawn = true;
            wave.isDisabled = false;
            wave.enemiesSpawnedFromThisWave = 0;
        }
        currentActiveWaveIndex = -1;
        if (enableDebugLogging)
        {
            Debug.Log("[WaveManager] All waves reset.");
        }
    }

    /// <summary>
    /// Description:
    /// Public method to manually trigger a specific wave by index
    /// Inputs: 
    /// int waveIndex
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="waveIndex">The index of the wave to trigger</param>
    public void TriggerWaveManually(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogError($"[WaveManager] Invalid wave index: {waveIndex}");
            return;
        }

        waves[waveIndex].hasBeenTriggered = true;
        if (enableDebugLogging)
        {
            Debug.Log($"[WaveManager] Wave {waveIndex} manually triggered!");
        }
    }

    /// <summary>
    /// Description:
    /// Disables a specific wave, preventing it from spawning new enemies
    /// Existing enemies from this wave will remain alive
    /// Inputs: 
    /// int waveIndex
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="waveIndex">The index of the wave to disable</param>
    public void DisableWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogError($"[WaveManager] Invalid wave index: {waveIndex}");
            return;
        }

        waves[waveIndex].isDisabled = true;
        if (enableDebugLogging)
        {
            Debug.Log($"[WaveManager] Wave {waveIndex} disabled. No new enemies will spawn from this wave.");
        }
    }

    /// <summary>
    /// Description:
    /// Re-enables a previously disabled wave
    /// Inputs: 
    /// int waveIndex
    /// Returns: 
    /// void (no return)
    /// </summary>
    /// <param name="waveIndex">The index of the wave to re-enable</param>
    public void EnableWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogError($"[WaveManager] Invalid wave index: {waveIndex}");
            return;
        }

        waves[waveIndex].isDisabled = false;
        if (enableDebugLogging)
        {
            Debug.Log($"[WaveManager] Wave {waveIndex} re-enabled.");
        }
    }
}
