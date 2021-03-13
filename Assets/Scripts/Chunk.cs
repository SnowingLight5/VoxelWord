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

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public bool isVoxelMapPopulated = false;
    private bool active;
    public bool isActive{
        get { return active; }
        set { 
                active = value;
                if(chunkObject != null) {
                    chunkObject.SetActive(value);
                } 
            }
    }

    public Vector3 position{
        get { return chunkObject.transform.position; }
    }


    public Chunk(ChunkCoord chunkCoord, World world, bool generateOnLoad){
        coord = chunkCoord;
        this.world = world;
        isActive = true;

        if(generateOnLoad){
            Init();
        }
       
    }

    public void Init(){
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);
        chunkObject.name = "Chunk: " + coord.x + ", " + coord.z;

        PopulateVoxelMap();
        UpdateChunk();
    }

    void PopulateVoxelMap(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    voxelMap[x,y,z] = world.GetVoxel(new Vector3(x,y,z) + position);
                }
            }
        }
        isVoxelMapPopulated = true;
    }

    void UpdateChunk(){

        ClearMeshData();

        for (int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){

                    if(world.blockTypes[voxelMap[x,y,z]].isSolid){
                        UpdateMeshData(new Vector3(x,y,z));
                    }
                }
            }
        }

        CreateMesh();

    }


    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    bool CheckVoxel(Vector3 pos){
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!isVoxelInChunk(x, y, z)){
            return world.CheckForVoxel(pos + position);
        }

        return world.blockTypes[voxelMap[x,y,z]].isSolid;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos){
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        return voxelMap[xCheck, yCheck, zCheck];
        
    }

    bool isVoxelInChunk(int x, int y, int z){
        if(x < 0 || x > VoxelData.chunkWidth - 1 || y < 0 || y > VoxelData.chunkHeight - 1 || z < 0 || z > VoxelData.chunkWidth - 1){
            return false;
        } else{
            return true;
        }
    }

    public void EditVoxel(Vector3 pos, byte newBlockId)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[xCheck, yCheck, zCheck] = newBlockId;

        updateSurroundingVoxels(xCheck, yCheck, zCheck);

        UpdateChunk();
    }

    void updateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!isVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.GetChunkFromVector3(currentVoxel + position).UpdateChunk();
            }
        }
    }

    void UpdateMeshData(Vector3 position) {
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
