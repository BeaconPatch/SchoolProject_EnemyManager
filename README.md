# Wave Manager
_A system made in Unity for my video game developer program's final project._

## In short
The EnemyManager is a singleton-like MOnoBehaviour object that would be saved to a prefab and instantiated once into a scene. With it, you could provide a WaveConfig ScriptableObject that would give it direction of what enemies you wanted to spawn, how many could be spawned at once and in total, what music you wanted to play while the wave was ongoing, what wave to start once ended, etc.

The EnemyManager couldn't be used alone, a flaw it its design to some degree. It instead required two the systems that I've also developed alongside, a GameObjectPool system and a SpawnSystem.

The EnemyManager first created itself a spawn queue. Then, a an interval, it would request an instance of the enemy at the front of the queue from the GameObjectPool system. If it received one back, then it would submit it to the SpawnSystem, which would be in charge of finding which SpawnPoint to use. If no SpawnPoint was available of the elected SpawnPoint would fail, then the EnemyManger would get notified and return the enemy it requested to spawn back to its spawn queue (the WaveConfig allowed to establish where to put it back in the spawn queue).

The EnemyManager was also in charge of monitoring spawned enemies so that, once killed or disabled due to level streaming or any reasons, they would be put back in the spawn queue as well to be respawned later.

## Few changes for this repository
For the purpose of this repository, I've...  
... replaced the project namespace with my own when I made the file.
... removed a few work in progress notes and left overs TODOs.  
The rest remains as is, albeit outside of its full context.

## What would I change in the future
Originally, the SpawnSystem was ment to be used for both collectible items and enemies. It was a generic system that could be used by multiple other systems, including the EnemyManager. However, while I appreciate this abstraction of SpawnPoint, I believe in this case an improved version of the EnemyManager would be best to include its own set of spawning tools.

When come to the EnemyManager itself and of its components, I would change the followings:
1. Specialized SpawnPoints which can be limited/fixed to a set NavMeshAgent type (ie, regular humanoid and the alike could only use a SpawnPoint only if it match its agent type's parameters such has eight, radius, and NavMesh underneath). I believe making specialized SpawnPoint would help level designers better understand and find the SpawnPoints once placed and defined and separate them from other SpawnPoint used for things other than enemies.
2. Specialized SpawnAreas and SpawnRestriction volumes, similar to what Halo offers where enemies may spawn freely in a given area or SpawnPoints in a area be limited or not considered when rivals are within bound or in sight, helping to prevent "spawn kill" and the like.
3. More complex and flexible WaveConfiguration with tools to make their usage and configuration more intuitive.
4. The current implementation of the WaveManager is a "weak" (perhaps naive) implementation of a singleton pattern. I would like to rectify that so that multiple WaveConfig could be ran simultaneously, such as in an open world context.
5. Make audio request event that an AudioManager could use rather than directly asking said AudioManager to play audio.
6. Originally though as a general purpose manager for enemies, it ended up being used specifically for Waves of enemies. To reflect that, I would change its name to WaveManager.
