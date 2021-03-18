﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData {

    public static readonly int chunkWidth = 16;
    public static readonly int chunkHeight = 128;
    public static readonly int worldSizeInChunks = 100;

    public static int worldSizeInVoxels {
        get { return worldSizeInChunks * chunkWidth; }
    }
    
    public static readonly int viewDistanceInChunks = 5;

    public static readonly int textureAtlasSizeInBlocks = 16;
    public static float normalizedBlockTextureSize{
        get { return 1f / textureAtlasSizeInBlocks; }
    }

    //Lighting
    public static float minLightLevel = 0.15f;
    public static float maxLightLevel = 0.8f;
    public static float lightFalloff = 0.08f;

    public static readonly Vector3[] voxelVerts = new Vector3[8]{
        new Vector3(0f,0f,0f),
        new Vector3(1f,0f,0f),
        new Vector3(1f,1f,0f),
        new Vector3(0f,1f,0f),
        new Vector3(0f,0f,1f),
        new Vector3(1f,0f,1f),
        new Vector3(1f,1f,1f),
        new Vector3(0f,1f,1f)
    };

    public static readonly Vector3[] faceChecks = new Vector3[6] {
        new Vector3(0f,0f,-1f),
        new Vector3(0f,0f,1f),
        new Vector3(0f,1f,0f),
        new Vector3(0f,-1f,0f),
        new Vector3(-1f,0f,0f),
        new Vector3(1f,0f,0f)
    };

    public static readonly int[,] voxelTris = new int[6,4] {

        {0,3,1,2}, //Front face
        {5,6,4,7}, //Back face
        {3,7,2,6}, //Top face
        {4,0,5,1}, //Bottom face
        {4,7,0,3}, //Left face
        {1,2,5,6}  //Righ face
    };

    public static readonly Vector2[] voxelUvs = new Vector2[4] {
        new Vector2(0f,0f),
        new Vector2(0f,1f),
        new Vector2(1f,0f),
        new Vector2(1f,1f)
    };
}
