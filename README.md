# Wave Manager
## A system made for Unity

### Requirements:
The Wave Manager couldn't be use alone, perhaps a flaw it its design. it instead required two the systems that I've also developed, a GameObjectPool system and a SpawnSystem.

### Few changes:
For the purpose of this repository, I've...
... replaced the project namespace with my own.

### What would I change:
Originally, the SpawnSystem was ment to be used for both collectible items and enemies. It was a generic system that could be used by multiple other systems, including the WaveManager. However, while I appreciate this abstraction of spawn point, I believe in this case an improved version of the WaveManager would be best to include its own set of spawning tools.
1. Specialized SpawnPoints which can be limited/fixed to a set NavMeshAgent type (ie, regular humanoid and the alike could only use a SpawnPoint only if it match its agent type's parameters such has eight, radius, and NavMesh underneath). I believe making specialized SpawnPoint would help level designers better understand and find the SpawnPoints once placed and defined and separate them from other SpawnPoint used for things other than enemies.
2. Specialized SpawnAreas and SpawnRestriction volumes, similar to what Halo offers where enemies may spawn freely in a given area or SpawnPoints in a area be limited or not considered when rivals are within bound or in sight, helping to prevent "spawn kill" and the like.
3. More complex and flexible WaveConfiguration with tools to make their usage and configuration more intuitive.
4. The current implementation of the WaveManager is a "weak" (perhaps naive) implementation of a singleton pattern. I would like to rectify that so that multiple WaveConfig could be ran simultaneously, such as in an open world context.
