using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class Chunk {

    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    World world;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    Material[] materials = new Material[2];

    public Vector3 position;

    public byte[,,] voxelMap = new byte[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];
    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private bool isVoxelMapPopulated = false;
    private bool threadLocked = false;
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

    public bool isEditable {
        get { 
            if(!isVoxelMapPopulated || threadLocked){
                return false;
            } else{
                return true;
            }
        }
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

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);
        chunkObject.name = "Chunk: " + coord.x + ", " + coord.z;

        position = chunkObject.transform.position;

        Thread thread = new Thread(new ThreadStart(PopulateVoxelMap));
        thread.Start();
    }

    void PopulateVoxelMap(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    voxelMap[x,y,z] = world.GetVoxel(new Vector3(x,y,z) + position);
                }
            }
        }
        PrivateUpdateChunk();
        isVoxelMapPopulated = true;
    }

    public void UpdateChunk(){

        Thread thread = new Thread(new ThreadStart(PrivateUpdateChunk));

        thread.Start();

    }

    private void PrivateUpdateChunk(){

        threadLocked = true;

        while(modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position - position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = v.id;
        }


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

        lock(world.chunksToDraw) {
            world.chunksToDraw.Enqueue(this);
        }

        threadLocked = false;

    }


    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
    }

    bool CheckIfVoxelIsTransparent(Vector3 pos){
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!isVoxelInChunk(x, y, z)){
            return world.CheckIfVoxelIsTransparent(pos + position);
        }

        return world.blockTypes[voxelMap[x,y,z]].isTransparent;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos){
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

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

        PrivateUpdateChunk();
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

        byte blockId = voxelMap[(int)position.x, (int)position.y, (int)position.z];
        bool isVoxelTransparent = world.blockTypes[blockId].isTransparent;

        for (int p = 0; p < 6; p++){

            if (CheckIfVoxelIsTransparent(position + VoxelData.faceChecks[p]))
            {
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                AddTexture(world.blockTypes[blockId].GetTextureId(p));

                if (isVoxelTransparent)
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }
                else
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    public void CreateMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureId){

        float y = textureId / VoxelData.textureAtlasSizeInBlocks;
        float x = textureId - (y * VoxelData.textureAtlasSizeInBlocks);

        x *= VoxelData.normalizedBlockTextureSize;
        y *= VoxelData.normalizedBlockTextureSize;

        y = 1f - (y + VoxelData.normalizedBlockTextureSize);

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.normalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.normalizedBlockTextureSize, y + VoxelData.normalizedBlockTextureSize));

    }
}
