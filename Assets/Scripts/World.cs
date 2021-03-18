using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{

    [Header("World generation values")]    
    public int seed;
    public BiomeAttribute biome;

    [Header("Performance")]
    public bool enableThreading;

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
    List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    private bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    public GameObject creativeInventoryWindow;
    public GameObject cursor;

    public bool _inUi = false;

    public bool inUi {
        get { return _inUi;}
        set { 
            _inUi = value;
            if(_inUi){
                Cursor.lockState = CursorLockMode.None;
                creativeInventoryWindow.SetActive(true);
                cursor.SetActive(true);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                creativeInventoryWindow.SetActive(false);
                cursor.SetActive(false);
            }
        }
    }

    private void Start() {

        Random.InitState(seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);
        
        spawnPosition = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight + 2f -50f, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }

    private void Update() {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        Shader.SetGlobalFloat("globalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);

        if(!playerChunkCoord.Equals(playerLastChunkCoord)){
            CheckViewDistance();
            playerLastChunkCoord = playerChunkCoord;
        }

        if(!applyingModifications)
        {
            ApplyModifications();
        }

        if(chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if(chunksToUpdate.Count > 0)
        {
            UpdateChunks();
        }

        if(chunksToDraw.Count > 0){
            lock(chunksToDraw){
                if(chunksToDraw.Peek().isEditable){
                    chunksToDraw.Dequeue().CreateMesh();
                }
            }
        }
    }

    void GenerateWorld(){
        for(int x = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; x < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; x++){
            for(int z = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; z < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; z++){
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        activeChunks.Add(c);
        chunks[c.x, c.z].Init();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].isEditable)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(0);
                updated = true;
            }
            else
            {
                index++;
            }
        }
    }

    void ApplyModifications()
    {
        applyingModifications = true;

        while(modifications.Count > 0)
        {

            Queue<VoxelMod> queue = modifications.Dequeue();

            while(queue.Count > 0){

                VoxelMod v = queue.Dequeue();

                if(v == null){
                    continue;
                }

                ChunkCoord c = GetChunkCoordFromVector3(v.position);

                if (chunks[c.x, c.z] == null)
                {
                    chunks[c.x, c.z] = new Chunk(c, this, true);
                    activeChunks.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);

                if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
                {
                    chunksToUpdate.Add(chunks[c.x, c.z]);
                }
            }
        }

        applyingModifications = false;
    }

    public ChunkCoord GetChunkCoordFromVector3(Vector3 pos){

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoord(x,z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return chunks[x, z];
    }


    void CheckViewDistance(){
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        // Loop through all chunks currently within view distance of the player.
        for (int x = coord.x - VoxelData.viewDistanceInChunks; x < coord.x + VoxelData.viewDistanceInChunks; x++) {
            for (int z = coord.z - VoxelData.viewDistanceInChunks; z < coord.z + VoxelData.viewDistanceInChunks; z++) {

                // If the current chunk is in the world...
                if (IsChunkInWorld (new ChunkCoord (x, z))) {

                    // Check if it active, if not, activate it.
                    if (chunks[x, z] == null) {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }  else if (!chunks[x, z].isActive) {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                // Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
                for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z))){
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them.
        foreach (ChunkCoord c in previouslyActiveChunks){
            chunks[c.x, c.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos){

        ChunkCoord chunkCoord = new ChunkCoord(pos);

        if(!IsChunkInWorld(chunkCoord) || pos.y < 0 || pos.y > VoxelData.chunkHeight){
            return false;
        }

        if(chunks[chunkCoord.x, chunkCoord.z] != null && chunks[chunkCoord.x, chunkCoord.z].isEditable){
            return blockTypes[chunks[chunkCoord.x, chunkCoord.z].GetVoxelFromGlobalVector3(pos).id].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;

        
    }

    public VoxelState GetVoxelState(Vector3 pos)
    {

        ChunkCoord chunkCoord = new ChunkCoord(pos);

        if (!IsChunkInWorld(chunkCoord) || pos.y < 0 || pos.y > VoxelData.chunkHeight)
        {
            return null;
        }

        if (chunks[chunkCoord.x, chunkCoord.z] != null && chunks[chunkCoord.x, chunkCoord.z].isEditable)
        {
            return chunks[chunkCoord.x, chunkCoord.z].GetVoxelFromGlobalVector3(pos);
        }

        return new VoxelState(GetVoxel(pos));


    }

    public byte GetVoxel(Vector3 position){

        int yPos = Mathf.FloorToInt(position.y);

        // if outside of world -> return air
        if(!IsVoxelInWorld(position)){
            return 0;
        }
        // if bottom of world -> return bedrock
        if(yPos == 0){
            return 1;
        }

        /* BASIC TERRAIN PASS*/

        int terrainHeight = Mathf.FloorToInt(Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.terrainScale) * biome.terrainHeight) + biome.solidGroundHeight;
        byte voxelValue = 0;

        if(yPos == terrainHeight){
            voxelValue = 3;
        } else if (yPos < terrainHeight && yPos > terrainHeight - 4){
            voxelValue = 6;
        } else if(yPos > terrainHeight){
            return 0;
        } else {
            voxelValue = 2;
        }

        /* SECOND PASS*/

        if(voxelValue == 2){
            foreach(Lode lode in biome.lodes){
                if (yPos > lode.minHeight && yPos < lode.maxHeight){
                    if(Noise.Get3DPerlin(position, lode.noiseOffset, lode.scale, lode.threshold)){
                        voxelValue = lode.blockId;
                    }
                }
            }
        }

        /* TREE PASS*/

        if(yPos == terrainHeight)
        {
            if(Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
            {
                if(Noise.Get2DPerlin(new Vector2(position.x, position.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold){
                    modifications.Enqueue(Structure.MakeTree(position, biome.minTreeHeight, biome.maxTreeHeight));
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld(ChunkCoord chunkCoord){
        if(chunkCoord.x >= 0 && chunkCoord.x < VoxelData.worldSizeInChunks && chunkCoord.z >= 0 && chunkCoord.z < VoxelData.worldSizeInChunks){
            return true;
        } else {
            return false;
        }
    }

    bool IsVoxelInWorld(Vector3 position){
        if(position.x >= 0 && position.x < VoxelData.worldSizeInVoxels && position.y >= 0 && position.y < VoxelData.chunkHeight && position.z >= 0 && position.z < VoxelData.worldSizeInVoxels ){
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

    [Header("Texture")]
    public int frontFaceTexture;
    public int backFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    public int GetTextureId(int faceIndex){

        switch(faceIndex){
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