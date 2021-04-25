using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour {

    public Settings settings;

    [Header("World generation values")]
    public BiomeAttribute[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    HashSet<ChunkCoord> activeChunks = new HashSet<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    public GameObject creativeInventoryWindow;
    public GameObject cursor;

    Thread chunkUpdateThread;
    public object chunkUpdateThreadLock = new object();
    public object chunkListThreadLock = new object();

    public Clouds clouds;

    private static World _instance;
    public static World Instance {
        get { return _instance; }
    }

    public WorldData worldData;

    public string appPath;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }

        appPath = Application.persistentDataPath;
    }

    public bool _inUi = false;
    public bool inUi {
        get { return _inUi; }
        set {
            _inUi = value;
            if (_inUi) {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursor.SetActive(true);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursor.SetActive(false);
            }
        }
    }

    private void Start() {

        Debug.Log("Generating new world using seed " + VoxelData.seed);

        worldData = SaveSystem.LoadWorld("Testing");

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.json");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        Debug.Log(settings.clouds);

        Random.InitState(VoxelData.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        if (settings.enableThreading) {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            chunkUpdateThread.Start();
        }

        SetGlobalLightValue();

        spawnPosition = new Vector3(VoxelData.worldCenter, VoxelData.chunkHeight + 2f - 50f, VoxelData.worldCenter);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    public void SetGlobalLightValue() {
        Shader.SetGlobalFloat("globalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    private void Update() {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if (!playerChunkCoord.Equals(playerLastChunkCoord)) {
            CheckViewDistance();
            playerLastChunkCoord = playerChunkCoord;
        }



        if (chunksToCreate.Count > 0) {
            CreateChunk();
        }



        if (chunksToDraw.Count > 0) {
            chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableThreading) {
            if (!applyingModifications) {
                ApplyModifications();
            }
            if (chunksToUpdate.Count > 0) {
                UpdateChunks();
            }
        }

        if (Input.GetKeyDown(KeyCode.F1)) {
            SaveSystem.SaveWorld(worldData);
        }
    }

    void LoadWorld() {
        for (int x = (VoxelData.worldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.worldSizeInChunks / 2) + settings.loadDistance; x++) {
            for (int z = (VoxelData.worldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.worldSizeInChunks / 2) + settings.loadDistance; z++) {

                worldData.LoadChunk(new Vector2Int(x, z));
            }
        }
    }

    void GenerateWorld() {
        for (int x = (VoxelData.worldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.worldSizeInChunks / 2) + settings.viewDistance; x++) {
            for (int z = (VoxelData.worldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.worldSizeInChunks / 2) + settings.viewDistance; z++) {

                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunk);
                chunksToCreate.Add(newChunk);
            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }

    void CreateChunk() {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks() {

        lock (chunkUpdateThreadLock) {
            chunksToUpdate[0].UpdateChunk();
            if (activeChunks.Contains(chunksToUpdate[0].coord)) {
                activeChunks.Add(chunksToUpdate[0].coord);
            }
            chunksToUpdate.RemoveAt(0);
        }
    }

    void ThreadedUpdate() {
        while (true) {
            if (!applyingModifications) {
                ApplyModifications();
            }
            if (chunksToUpdate.Count > 0) {
                UpdateChunks();
            }
        }
    }

    private void OnDisable() {
        if (settings.enableThreading) {
            chunkUpdateThread.Abort();
        }
    }

    void ApplyModifications() {
        applyingModifications = true;

        while (modifications.Count > 0) {

            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0) {

                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id);
            }
        }

        applyingModifications = false;
    }

    public ChunkCoord GetChunkCoordFromVector3(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return chunks[x, z];
    }


    void CheckViewDistance() {

        clouds.UpdateClouds();

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        activeChunks.Clear();

        // Loop through all chunks currently within view distance of the player.
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++) {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++) {

                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord)) {

                    // Check if it active, if not, activate it.
                    if (chunks[x, z] == null) {
                        chunks[x, z] = new Chunk(thisChunkCoord);
                        chunksToCreate.Add(thisChunkCoord);
                    } else if (!chunks[x, z].isActive) {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(thisChunkCoord);
                }

                // Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
                for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                    if (previouslyActiveChunks[i].Equals(thisChunkCoord)) {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them.
        foreach (ChunkCoord c in previouslyActiveChunks) {
            chunks[c.x, c.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos) {
        VoxelState voxel = worldData.GetVoxel(pos);

        if (blockTypes[voxel.id].isSolid) {
            return true;
        } else {
            return false;
        }
    }

    public VoxelState GetVoxelState(Vector3 pos) {
        return worldData.GetVoxel(pos);
    }

    public byte GetVoxel(Vector3 position) {

        int yPos = Mathf.FloorToInt(position.y);

        // if outside of world -> return air
        if (!IsVoxelInWorld(position)) {
            return 0;
        }
        // if bottom of world -> return bedrock
        if (yPos == 0) {
            return 1;
        }

        /* BIOME SELECTION PASS */

        int solidGroundHeight = 42;
        float sumOfHeigths = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestbiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++) {
            float weight = Noise.Get2DPerlin(new Vector2(position.x, position.z), biomes[i].offset, biomes[i].scale);

            if (weight > strongestWeight) {
                strongestWeight = weight;
                strongestbiomeIndex = i;
            }

            float height = Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biomes[i].terrainScale) * biomes[i].terrainHeight * weight;

            if (height > 0) {
                sumOfHeigths += height;
                count++;
            }
        }

        BiomeAttribute biome = biomes[strongestbiomeIndex];

        int terrainHeight = Mathf.FloorToInt((sumOfHeigths / count) + solidGroundHeight);

        /* BASIC TERRAIN PASS*/

        byte voxelValue = 0;

        if (yPos == terrainHeight) {
            voxelValue = biome.surfaceBlock;
        } else if (yPos < terrainHeight && yPos > terrainHeight - 4) {
            voxelValue = biome.subSurfaceBlock;
        } else if (yPos > terrainHeight) {
            return 0;
        } else {
            voxelValue = 2;
        }

        /* SECOND PASS*/

        if (voxelValue == 2) {
            foreach (Lode lode in biome.lodes) {
                if (yPos > lode.minHeight && yPos < lode.maxHeight) {
                    if (Noise.Get3DPerlin(position, lode.noiseOffset, lode.scale, lode.threshold)) {
                        voxelValue = lode.blockId;
                    }
                }
            }
        }

        /* MAJOR FLORA PASS*/

        if (yPos == terrainHeight && biome.placeMajorFlora) {
            if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.majorFloraScale) > biome.majorFloraThreshold) {
                if (Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.majorPlacementFloraScale) > biome.majorFloraPlacementThreshold) {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex, position, biome.minMajorFloraHeight, biome.maxMajorFloraHeight));
                }
            }
        }

        return voxelValue;
    }

    public int GetBiomeIndex(Vector3 position) {
        float strongestWeight = 0f;
        int strongestbiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++) {
            float weight = Noise.Get2DPerlin(new Vector2(position.x, position.z), biomes[i].offset, biomes[i].scale);

            if (weight > strongestWeight) {
                strongestWeight = weight;
                strongestbiomeIndex = i;
            }
        }

        return strongestbiomeIndex;
    }

    bool IsChunkInWorld(ChunkCoord chunkCoord) {
        if (chunkCoord.x >= 0 && chunkCoord.x < VoxelData.worldSizeInChunks && chunkCoord.z >= 0 && chunkCoord.z < VoxelData.worldSizeInChunks) {
            return true;
        } else {
            return false;
        }
    }

    bool IsVoxelInWorld(Vector3 position) {
        if (position.x >= 0 && position.x < VoxelData.worldSizeInVoxels && position.y >= 0 && position.y < VoxelData.chunkHeight && position.z >= 0 && position.z < VoxelData.worldSizeInVoxels) {
            return true;
        } else {
            return false;
        }
    }

}

[System.Serializable]
public class BlockType {

    public string name;
    public bool isSolid;
    public bool renderNeighborFaces;
    public float transparency;
    public Sprite icon;

    public bool toBeColorized;

    [Header("Texture")]
    public int frontFaceTexture;
    public int backFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureId(int faceIndex) {

        switch (faceIndex) {
            case 0:
                return frontFaceTexture;
            case 1:
                return backFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureId. Invalid face index");
                return frontFaceTexture;
        }

    }

}

[System.Serializable]
public class Settings {
    [Header("Game Data")]
    public string version = "0.0.1";


    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public bool enableThreading = true;
    public bool enableAnimatedChunks = false;
    public CloudStyle clouds = CloudStyle.Fancy;
    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivy = 1f;

}