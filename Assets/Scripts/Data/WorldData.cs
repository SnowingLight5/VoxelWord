using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData {

    public string worldName = "Prototype";
    public int seed;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();
    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>(); 

    public void AddToModifiedChunkList(ChunkData chunk) {
        if (!modifiedChunks.Contains(chunk)) {
            modifiedChunks.Add(chunk);
        }
    }

    public WorldData(string worldName, int seed) {
        this.worldName = worldName;
        this.seed = seed;
    }

    public WorldData(WorldData worldData) {
        worldName = worldData.worldName;
        seed = worldData.seed;
    }

    public ChunkData RequestChunk(Vector2Int coord, bool create) {

        ChunkData c;

        lock (World.Instance.chunkListThreadLock) {

            if (chunks.ContainsKey(coord)) {
                c = chunks[coord];
            } else if (!create) {
                c = null;
            } else {
                LoadChunk(coord);
                c = chunks[coord];
            }
        }

        return c;

    }

    public void LoadChunk(Vector2Int coord) {

        if (chunks.ContainsKey(coord)) {
            return;
        }

        ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);
        if(chunk != null) {
            chunks.Add(coord, chunk);
            return;
        }

        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();

    }

    bool IsVoxelInWorld(Vector3 position) {
        if (position.x >= 0 && position.x < VoxelData.worldSizeInVoxels && position.y >= 0 && position.y < VoxelData.chunkHeight && position.z >= 0 && position.z < VoxelData.worldSizeInVoxels) {
            return true;
        } else {
            return false;
        }
    }

    public void SetVoxel(Vector3 pos, byte value) {
        if (!IsVoxelInWorld(pos)) {
            return;
        }

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int((int) (pos.x - x), (int) pos.y, (int) (pos.z - z));

        chunk.map[voxel.x, voxel.y, voxel.z].id = value;
        AddToModifiedChunkList(chunk);
    }

    public VoxelState GetVoxel(Vector3 pos) {
        if (!IsVoxelInWorld(pos)) {
            return null;
        }

        int x = Mathf.FloorToInt(pos.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.chunkWidth);

        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int((int) (pos.x - x), (int) pos.y, (int) (pos.z - z));

        return chunk.map[voxel.x, voxel.y, voxel.z];
    }

}
