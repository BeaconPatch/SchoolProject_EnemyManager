using System;
using UnityEngine;

namespace BeaconPatch.EnemyManager
{
    [Serializable]
    public struct EnemyEntry
    {
        [SerializeField] [Tooltip("Determine the model of enemy to spawn.")]
        private GameObject enemyToSpawn;
        [SerializeField] [Tooltip("The quantity of enemy to spawn.")]
        private uint qtsToSpawn;
        [SerializeField] [Tooltip("The spawn score attributed to this enemy. If multiple entries for this enemy is " +
                                  "provided, the lowest spawn score between entries will be used.")]
        private uint spawnScore;
        
        
        public EnemyEntry(GameObject enemy, uint qts = 1, uint score = 10)
        {
            enemyToSpawn = enemy;
            qtsToSpawn = qts;
            spawnScore = score;
        }

        public GameObject GetEnemyToSpawn() { return enemyToSpawn; }
        public uint GetQtsToSpawn() { return qtsToSpawn; }
        public uint GetSpawnScore() { return spawnScore; }
    }
}
