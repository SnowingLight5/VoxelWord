using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkCoord 
{
    public int x;
    public int z;

    public ChunkCoord(int x, int z){
        this.x = x;
        this.z = z;
    }

    public ChunkCoord() {
        this.x = 0;
        this.z = 0;
    }

    public ChunkCoord(Vector3 pos){
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        this.x = xCheck / VoxelData.chunkWidth;
        this.z = zCheck / VoxelData.chunkWidth;
    }

    public bool Equals(ChunkCoord other){
        if(other == null){
            return false;
        }

        return other.x == x && other.z == z;
    }
}
