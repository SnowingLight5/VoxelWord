using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    World world;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public bool isActive{
        get { return chunkObject.activeSelf; }
        set { chunkObject.SetActive(value); }
    }

    public Vector3 position{
        get { return chunkObject.transform.position; }
    }

    public Chunk(ChunkCoord chunkCoord, World world){
        coord = chunkCoord;
        this.world = world;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);
        chunkObject.name = "Chunk: " + coord.x + ", " + coord.z;

        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }

    void PopulateVoxelMap(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    voxelMap[x,y,z] = world.GetVoxel(new Vector3(x,y,z) + position);
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

    bool CheckVoxel(Vector3 pos){
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!isVoxelInChunk(x, y, z)){
            return world.blockTypes[world.GetVoxel(pos + position)].isSolid;
        }

        return world.blockTypes[voxelMap[x,y,z]].isSolid;
    }

    bool isVoxelInChunk(int x, int y, int z){
        if(x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1){
            return false;
        } else{
            return true;
        }
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

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.normalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y + VoxelData.normalizedBlockTextureSize));

    }
}
