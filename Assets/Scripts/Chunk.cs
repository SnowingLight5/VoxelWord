﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    World world;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    void Start()
    {

        world = GameObject.Find("World").GetComponent<World>();

        PopulateVoxelMap();
        
        CreateMeshData();

        CreateMesh();        
    }

    void PopulateVoxelMap(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    if(y < 1){
                        voxelMap[x,y,z] = 0;
                    }else if(y == VoxelData.chunkHeight - 1){
                        voxelMap[x,y,z] = 3;
                    }else{
                        voxelMap[x,y,z] = 1;
                    }
                }
            }
        }
    }

    void CreateMeshData(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    AddVoxelDataToChunk(new Vector3(x,y,z));
                }
            }
        }
    }

    bool CheckVoxel(Vector3 position){
        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        if(x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1){
            return false;
        }

        return world.blockTypes[voxelMap[x,y,z]].isSolid;
    }

    void AddVoxelDataToChunk(Vector3 position) {
        for(int p = 0; p < 6; p++){

            if(CheckVoxel(position + VoxelData.faceChecks[p])){
                continue;
            }

            byte blockId = voxelMap[(int)position.x, (int)position.y, (int)position.z];

            vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p,0]]);
            vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p,1]]);
            vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p,2]]);
            vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p,3]]);
            
            AddTexture(world.blockTypes[blockId].GetTextureId(p));

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex+1);
            triangles.Add(vertexIndex+2);
            triangles.Add(vertexIndex+2);
            triangles.Add(vertexIndex+1);
            triangles.Add(vertexIndex+3);

            vertexIndex+=4;
        }
    }

    void CreateMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureId){

        float y = textureId / VoxelData.textureAtlasSizeInBlocks;
        float x = textureId - (y * VoxelData.textureAtlasSizeInBlocks);

        x *= VoxelData.normalizedBlockTextureSize;
        y *= VoxelData.normalizedBlockTextureSize;

        Debug.Log(x +" - "+y);

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.normalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y + VoxelData.normalizedBlockTextureSize));


    }
}
