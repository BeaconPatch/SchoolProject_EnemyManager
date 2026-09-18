using System.Collections.Generic;
using UnityEngine;

namespace BeaconPatch.EnemyManager
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Scriptable Objects/Enemy Manager/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        #region Parameters
        [Header("Spawning")]
        [SerializeField] [Tooltip("Determine the order to spawn enemies.")]
        private SpawnOrder spawningOrder = SpawnOrder.InOrder;
        [SerializeField] [Tooltip("Determine the list of enemy to spawn.")]
        private List<EnemyEntry> enemies = new List<EnemyEntry>();
        [SerializeField] [Tooltip("Determine where to put the enemy in the spawn queue when returned to the wave " +
                                  "manager.")]
        private ReturnToQueuePlacement returnToQueuePlacement = ReturnToQueuePlacement.AtStartOfQueue;
        [SerializeField] [Tooltip("The maximum score of enemies that can be spawned.")]
        private uint spawnScoreLimit = 100;
        [SerializeField] [Tooltip("Determine if the next enemy in the spawn queue should be skipped if it would " +
                                  "cause the total spawn score to exceed the spawn score limit.")]
        private bool isSpawnScoreHardLimit = false;

        [Header("Audio")]
        [SerializeField] [Tooltip("The name of a short SFX to play when the wave begins. " +
                                  "That name must correspond to a SFX registered in the AudioManager.")]
        private string waveStartStingerName;
        [SerializeField] [Tooltip("The name of a looping music to play while the wave is active. " +
                                  "That name must correspond to a music registered in the AudioManager.")]
        private string waveMusicName;
        [SerializeField] [Tooltip("The name of a short SFX sound to play when the wave ends and the player survives. " +
                                  "That name must correspond to a SFX registered in the AudioManager. " +
                                  "If a nextWave is configured, this parameter will be ignored.")]
        private string waveSuccessStingerName;
        [SerializeField] [Tooltip("The name of a short SFX to play when the wave ends with the player dying. " +
                                  "That name must correspond to a SFX registered in the AudioManager.")]
        private string waveFailedStingerName;

        [Header("Wave")]
        [SerializeField] [Tooltip("Determine, if any, what wave to start once the once is completed with a success.")]
        private WaveConfig nextWave;
        #endregion Parameters
        
        
        #region Getters
        public SpawnOrder GetSpawningOrder() { return spawningOrder; }
        public List<EnemyEntry> GetEnemies() { return enemies; }
        public ReturnToQueuePlacement GetReturnToQueuePlacement() { return returnToQueuePlacement; }

        public uint GetSpawnScoreLimit() { return spawnScoreLimit; }
        public bool GetIsSpawnScoreHardLimit() { return isSpawnScoreHardLimit; }

        public string GetWaveStartStingerName() { return waveStartStingerName; }
        public string GetWaveMusicName() { return waveMusicName; }
        public string GetWaveSuccessStingerName() { return waveSuccessStingerName; }
        public string GetWaveFailedStingerName() { return waveFailedStingerName; }

        public WaveConfig GetNextWave() { return nextWave; }
        #endregion Getters
    }
}
