using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    
    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    HashSet<ChunkCoord> activeChunks = new HashSet<ChunkCoord>();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    private void Start() {
        
        spawnPosition = new Vector3((VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f, VoxelData.chunkHeight + 2f, (VoxelData.worldSizeInChunks * VoxelData.chunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }

    private void Update() {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if(!playerChunkCoord.Equals(playerLastChunkCoord)){
            CheckViewDistance();
            playerLastChunkCoord = playerChunkCoord;
        }
    }

    void GenerateWorld(){
        for(int x = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; x < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; x++){
            for(int z = (VoxelData.worldSizeInChunks / 2) - VoxelData.viewDistanceInChunks; z < (VoxelData.worldSizeInChunks / 2) + VoxelData.viewDistanceInChunks; z++){
                CreateNewChunk(x,z);
            }
        }

        player.position = spawnPosition;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos){

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        return new ChunkCoord(x,z);
    }


    void CheckViewDistance(){
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);

        HashSet<ChunkCoord> visibleChunks = new HashSet<ChunkCoord>();

        for(int x = coord.x - VoxelData.viewDistanceInChunks; x < coord.x + VoxelData.viewDistanceInChunks; x++){
            for(int z = coord.z - VoxelData.viewDistanceInChunks; z < coord.z + VoxelData.viewDistanceInChunks; z++){
                if(IsChunkInWorld(new ChunkCoord(x, z))){
                    if(chunks[x, z] == null){
                        CreateNewChunk(x, z);
                    } else if (!chunks[x,z].isActive) {
                        chunks[x,z].isActive = true;
                        activeChunks.Add(chunks[x,z].coord);
                    }

                    visibleChunks.Add(chunks[x,z].coord);
                }
            } 
        }

        HashSet<ChunkCoord> inactiveChunks = new HashSet<ChunkCoord>(activeChunks);
        inactiveChunks.ExceptWith(visibleChunks);

        foreach (ChunkCoord c in inactiveChunks){
            chunks[c.x, c.z].isActive = false;
            activeChunks.Remove(c);
        }
    }

    public byte GetVoxel(Vector3 position){

        if(!isVoxelInWorld(position)){
            return 0;
        }

        if(position.y < 1){
            return 1;
        } else if (position.y == VoxelData.chunkHeight - 1){
            return 3;
        } else {
            return 2;
        }
    }

    void CreateNewChunk(int x, int z){
        chunks[x,z] = new Chunk(new ChunkCoord(x,z), this);

        activeChunks.Add(new ChunkCoord(x,z));
    }

    bool IsChunkInWorld(ChunkCoord chunkCoord){
        if(chunkCoord.x > 0 && chunkCoord.x < VoxelData.worldSizeInChunks -1 && chunkCoord.z > 0 && chunkCoord.z < VoxelData.worldSizeInChunks -1){
            return true;
        } else {
            return false;
        }
    }

    bool isVoxelInWorld(Vector3 position){
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