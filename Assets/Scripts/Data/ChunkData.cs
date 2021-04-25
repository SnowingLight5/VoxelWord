using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData {

    int x;
    int z;

    public Vector2Int position {
        get { return new Vector2Int(x, z); }
        set {
            x = value.x;
            z = value.y;
        }
    }

    public ChunkData(Vector2Int pos) {
        position = pos;
    }
    public ChunkData(int x, int z) {
        this.x = x;
        this.z = z;
    }

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public void Populate() {
        for (int y = 0; y < VoxelData.chunkHeight; y++) {
            for (int x = 0; x < VoxelData.chunkWidth; x++) {
                for (int z = 0; z < VoxelData.chunkWidth; z++) {
                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)));
                }
            }
        }

        World.Instance.worldData.AddToModifiedChunkList(this);
    }
}
