using System;
using System.Collections.Generic;
using System.Linq;
using SchoolTeamName.Audio;
using SchoolTeamName.Enemies;
using UnityEngine;
using BeaconPatch.SpawnSystem;
using BeaconPatch.GameObjectPoolSystem;
using BeaconPatch.HealthSystem;
using SchoolTeamName.Utility;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace BeaconPatch.EnemyManager
{
    public class EnemyManager : MonoBehaviour, ISpawnDispatcher
    {
        #region Events and Delegates
        public delegate void WaveStartedDelegate(WaveConfig waveConfig);
        public WaveStartedDelegate waveStarted;
        
        public delegate void WaveCompletedDelegate(WaveConfig completedWaveConfig, WaveResult waveResult);
        public WaveCompletedDelegate waveCompleted;
        #endregion Events and Delegates
        
        #region Parameters
        [SerializeField] [Tooltip("Determine an interval at which the EnemyManager will attempt dispatching enemies" +
                                  "to be spawned at SpawnPoints.")]
        private float spawnAttemptInterval = 5.0f;
        
        [Header("Wave Information")]
        [SerializeField] [Tooltip("Determine the WaveConfig to use from the start to try spawning enemies. " +
                                  "It's recommended to avoid using this parameters and instead use the public " +
                                  "EnemyManager::StartWave() method instead.")]
        private WaveConfig initialWaveConfig;
        #endregion
        
        #region References
        public static EnemyManager instance { get; private set; }
        
        private GameObjectPoolManager _gameObjectPoolManager;
        private SpawnElector _spawnElector;
        private Coroutine _spawnAttemptCoroutine;
        private WaveConfig _activeWaveConfig = null;
        private List<GameObject> _spawnQueue = new List<GameObject>();
        private List<GameObject> _spawned = new List<GameObject>();
        #endregion Reference
        
        #region Flags and data
        private uint _spawnScore = 0;
        #endregion
        
        
        #region MonoBehaviour
        void Awake()
        {
            if (TryInitializeSingleton())
            {
                GetReferences();
            }
        }
        
        
        private void Start()
        {
            if (initialWaveConfig) StartWave(initialWaveConfig);
        }
        
        
        private void OnDisable()
        {
            ClearSpawnQueue();
            CancelInvoke(nameof(TrySpawningEnemies));
        }
        #endregion MonoBehaviour
        
        #region ISpawnManager
        public void OnSpawnAbandoned(GameObject objectToSpawn, SpawnPoint spawnPoint)
        {
            _gameObjectPoolManager.ReturnObject(objectToSpawn);
        }
        #endregion ISpawnManager
        
        
        #region Initialization
        private bool TryInitializeSingleton()
        {
            if (instance != null)
            {
                Debug.LogError(this + "(" + gameObject + ")" + " tried to register itself as the EnemyManager " +
                               "singleton, but it was already defined. Destroying and aborting initialization.");
                gameObject.name = "! " + gameObject.name;
                Destroy(this);
                return false;
            }
            
            instance = this;
            return true;
        }
        
        
        private void GetReferences()
        {
            _spawnElector = FindAnyObjectByType<SpawnElector>();
            if (_spawnElector == null)
            {
                Debug.LogError(this + " failed to find a SpawnElector in the scene, which is required to work. Disabling.");
                gameObject.SetActive(false);
            }
            
            _gameObjectPoolManager = FindAnyObjectByType<GameObjectPoolManager>();
            if (_gameObjectPoolManager == null)
            {
                Debug.LogError(this + " failed to find a GameObjectPoolManager in the scene, which is required to work. Disabling.");
                gameObject.SetActive(false);
            }
        }
        #endregion Initialization

        #region Wave
        /// <summary>
        /// Return true if the EnemyManager is active and a WaveConfig is currently in use. This may return true even
        /// if no enemies have yet to be spawned.
        /// </summary>
        /// <returns></returns>
        public bool IsWaveActive()
        {
            return enabled && _activeWaveConfig;
        }
        
        
        /// <summary>
        /// Start a new wave of enemy using a provided WaveConfig.
        /// </summary>
        /// <param name="newWaveConfig">The new WaveConfig asset to use for this new wave.</param>
        /// <param name="clearEnemies">If true, enemies already in play from a previous wave will be killed.</param>
        public void StartWave(WaveConfig newWaveConfig, bool clearEnemies = false)
        {
            bool playStartWaveStinger = true;
            if (_activeWaveConfig)
            {
                playStartWaveStinger = false;
                InterruptWave(clearEnemies);
            }

            _activeWaveConfig = newWaveConfig;
            
            if (!_activeWaveConfig) return;
            
            if (playStartWaveStinger)
                AudioManager.instance.PlaySFX(_activeWaveConfig.GetWaveStartStingerName());
            
            AudioManager.instance.PlayMusic(_activeWaveConfig.GetWaveMusicName(), 2.0f);
            
            MakeSpawnQueue();
            InvokeRepeating(nameof(TrySpawningEnemies), 0.0f, spawnAttemptInterval);
            
            waveStarted?.Invoke(_activeWaveConfig);
        }


        /// <summary>
        /// Stop the current wave in progress with the given WaveResult.
        /// </summary>
        /// <param name="waveResult"></param>
        /// <param name="clearEnemies">If true, enemies already in play from a previous wave will be killed.</param>
        public void StopWave(WaveResult waveResult, bool clearEnemies = true)
        {
            switch (waveResult)
            {
                case WaveResult.Interrupted:
                case WaveResult.Failed:
                    if (_activeWaveConfig.GetWaveFailedStingerName() != "")
                        AudioManager.instance.PlaySFX(_activeWaveConfig.GetWaveFailedStingerName());
                    break;
                case WaveResult.Succeeded:
                    if (_activeWaveConfig.GetWaveSuccessStingerName() != "" && _activeWaveConfig.GetNextWave() == null)
                        AudioManager.instance.PlaySFX(_activeWaveConfig.GetWaveSuccessStingerName());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(waveResult), waveResult, null);
            }
            
            WaveConfig nextWave = _activeWaveConfig.GetNextWave();
            
            waveCompleted(_activeWaveConfig, waveResult);
            InterruptWave(clearEnemies);
            
            if (nextWave != null)
            {
                StartWave(nextWave);
            }
            else
            {
                AudioManager.instance.StopMusic();
            }
        }
        
        
        private void InterruptWave(bool clearEnemies)
        {
            CancelInvoke(nameof(TrySpawningEnemies));
            
            ClearSpawnQueue();
            if (clearEnemies)
            {
                DamageData damage = new DamageData(
                    100000.0f,
                    DamageType.Unknown,
                    null,
                    Vector3.zero,
                    Vector3.zero);
                foreach (GameObject enemy in _spawned)
                {
                    StopTrackingEnemyStateChanges(enemy);
                    enemy.GetComponent<IDamageable>().TryDamaging(damage);
                }
            }
            
            _spawned.Clear();

            _activeWaveConfig = null;
        }
        #endregion Wave
        
        #region Spawn Queue
        private void MakeSpawnQueue()
        {
            if (!_activeWaveConfig) return;
            
            foreach (EnemyEntry enemyEntry in _activeWaveConfig.GetEnemies())
            {
                GameObject enemy = enemyEntry.GetEnemyToSpawn();
                for (uint i = 0; i < enemyEntry.GetQtsToSpawn(); i++)
                {
                    _spawnQueue.Add(enemy);
                }
            }
            
            if (_activeWaveConfig.GetSpawningOrder() == SpawnOrder.Shuffled)
            {
                _spawnQueue.Shuffle();
            }
        }


        private void ClearSpawnQueue()
        {
            _spawnQueue.Clear();
        }
        
        
        private void TrySpawningEnemies()
        {
            List<SpawnPoint> availableSpawnPoints = _spawnElector.GetAvailableSpawnPoints();
            
            if (availableSpawnPoints.Count == 0) return;
            
            foreach (GameObject enemy in _spawnQueue.ToList())
            {
                if (availableSpawnPoints.Count == 0) break;
                
                if (!CanIncreaseSpawnScore(GetEnemyEntry(enemy))) continue;
                
                // Try getting the highest election score.
                SpawnPoint bestCandidate = null;
                foreach (SpawnPoint spawnPoint in availableSpawnPoints)
                {
                    if (bestCandidate == null || bestCandidate.GetElectionScore() < spawnPoint.GetElectionScore())
                        bestCandidate = spawnPoint;
                }
                if (!bestCandidate) return;
                
                // Try getting an enemy GameObject from a GameObjectPool to distribute to a spawn point.
                GameObject enemyToSpawn = _gameObjectPoolManager.RequestObject(enemy);
                
                // Try distributing the retrieved enemy to the elected SpawnPoint.
                if (enemyToSpawn &&
                    bestCandidate.RequestReservationForGameObject(enemyToSpawn, this))
                {
                    availableSpawnPoints.Remove(bestCandidate);
                    SpawnFromSpawnQueue(enemyToSpawn);
                }
            }
        }
        #endregion Spawn Queue
        
        #region Spawned Enemy Tracking
        private void OnEnemyDespawned(GameObject enemy)
        {
            ReturnToSpawnQueue(enemy);
        }
        
        
        private void SpawnFromSpawnQueue(GameObject enemy)
        {
            EnemyEntry enemyEntry = GetEnemyEntry(enemy);
            IncreaseSpawnScore(enemyEntry);
            
            _spawned.Add(enemy);
            StartTrackingEnemyStateChanges(enemy);
            
            _spawnQueue.Remove(enemyEntry.GetEnemyToSpawn()); // Where the issue occurs
        }


        private void ReturnToSpawnQueue(GameObject enemy)
        {
            EnemyEntry enemyEntry = GetEnemyEntry(enemy);
            DecreaseSpawnScore(enemyEntry);
            InsertInSpawnQueue(enemyEntry);
            
            StopTrackingEnemyStateChanges(enemy);
            _spawned.Remove(enemy);
        }
        
        
        private void InsertInSpawnQueue(EnemyEntry enemyEntry)
        {
            if (!_activeWaveConfig) return;

            switch (_activeWaveConfig.GetReturnToQueuePlacement())
            {
                case ReturnToQueuePlacement.AtStartOfQueue:
                {
                    _spawnQueue.Insert(0, enemyEntry.GetEnemyToSpawn());
                    break;
                }
                case ReturnToQueuePlacement.AtEndOfQueue:
                {
                    _spawnQueue.Add(enemyEntry.GetEnemyToSpawn());
                    break;
                }
                case ReturnToQueuePlacement.Random:
                {
                    int index = Random.Range(0, _spawnQueue.Count + 1);
                    _spawnQueue.Insert(index, enemyEntry.GetEnemyToSpawn());
                    break;
                }
                default:
                    Debug.LogError("Unknown ReturnToQueuePlacement value in " + _activeWaveConfig);
                    break;
            }
        }
        
        
        private void StartTrackingEnemyStateChanges(GameObject enemy)
        {
            if (enemy.TryGetComponent<HealthComponent>(out HealthComponent healthComponent))
            {
                healthComponent.killed += OnKilled;
            }
            
            if (enemy.gameObject.TryGetComponent<EnemySpawnResponse>(out EnemySpawnResponse enemySpawnResponse))
            {
                enemySpawnResponse.enemyDespawned += OnEnemyDespawned;
            }
        }
        
        
        private void StopTrackingEnemyStateChanges(GameObject enemy)
        {
            if (enemy.TryGetComponent<HealthComponent>(out HealthComponent healthComponent))
            {
                healthComponent.killed -= OnKilled;
            }
            
            if (enemy.gameObject.TryGetComponent<EnemySpawnResponse>(out EnemySpawnResponse enemySpawnResponse))
            {
                enemySpawnResponse.enemyDespawned -= OnEnemyDespawned;
            }
        }
        
        
        private void OnKilled(GameObject killedGameObject, DamageData data)
        {
            StopTrackingEnemyStateChanges(killedGameObject);
            
            DecreaseSpawnScore(GetEnemyEntry(killedGameObject));
            _spawned.Remove(killedGameObject);
            
            if (_spawned.Count == 0 && _spawnQueue.Count == 0)
            {
                StopWave(WaveResult.Succeeded);
            }
        }
        #endregion Spawned Enemy Tracking
        
        #region SpawnScore
        private bool CanIncreaseSpawnScore(EnemyEntry enemyEntry)
        {
            if (_activeWaveConfig.GetIsSpawnScoreHardLimit())
            {
                uint theoreticalScore = _spawnScore + GetSpawnScore(enemyEntry);
                return theoreticalScore <= _activeWaveConfig.GetSpawnScoreLimit();
            }

            return _spawnScore < _activeWaveConfig.GetSpawnScoreLimit();
        }
        
        
        private void IncreaseSpawnScore(EnemyEntry enemyEntry)
        {
            _spawnScore += GetSpawnScore(enemyEntry);
        }
        
        
        private void DecreaseSpawnScore(EnemyEntry enemyEntry)
        {
            _spawnScore -= GetSpawnScore(enemyEntry);
        }
        
        
        private uint GetSpawnScore(EnemyEntry enemyEntry)
        {
            uint score = uint.MaxValue;
            foreach (EnemyEntry entry in _activeWaveConfig.GetEnemies())
            {
                if (enemyEntry.GetEnemyToSpawn().name.StartsWith(entry.GetEnemyToSpawn().name))
                    score = Math.Min(entry.GetSpawnScore(), score);
            }
            return score;
        }
        #endregion SpawnScore


        #region Getters
        private EnemyEntry GetEnemyEntry(GameObject enemy)
        {
            EnemyEntry entry = new EnemyEntry();
            foreach (EnemyEntry enemyEntry in _activeWaveConfig.GetEnemies())
            {
                if (enemy.name.StartsWith(enemyEntry.GetEnemyToSpawn().name))
                    entry = enemyEntry;
            }
            return entry;
        }
        #endregion
    }
}