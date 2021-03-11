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

    public bool Equals(ChunkCoord other){
        if(other == null){
            return false;
        }

        return other.x == x && other.z == z;
    }
}
