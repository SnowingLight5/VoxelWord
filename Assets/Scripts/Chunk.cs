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
    List<int> transparentTriangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    Material[] materials = new Material[2];

    public Vector3 position;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];
    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private bool isVoxelMapPopulated = false;
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
            if(!isVoxelMapPopulated){
                return false;
            } else{
                return true;
            }
        }
    }

    public Chunk(ChunkCoord chunkCoord, World world){
        coord = chunkCoord;
        this.world = world;
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

        PopulateVoxelMap();
        
    }

    void PopulateVoxelMap(){
        for(int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){
                    voxelMap[x,y,z] = new VoxelState(world.GetVoxel(new Vector3(x,y,z) + position));
                }
            }
        }
        isVoxelMapPopulated = true;

        lock(world.chunkUpdateThreadLock){
            world.chunksToUpdate.Add(this);
        }

        if (world.settings.enableAnimatedChunks)
        {
            chunkObject.AddComponent<ChunkLoadAnimation>();
        }
    }

    public void UpdateChunk(){

        while(modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position - position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;
        }


        ClearMeshData();
        
        CalculateLight();

        for (int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){

                    if(world.blockTypes[voxelMap[x,y,z].id].isSolid){
                        UpdateMeshData(new Vector3(x,y,z));
                    }
                }
            }
        }

        lock(world.chunksToDraw) {
            world.chunksToDraw.Enqueue(this);
        }

    }

    void CalculateLight(){

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for(int x = 0; x < VoxelData.chunkWidth; x++){
            for(int z = 0; z < VoxelData.chunkWidth; z++){

                float lightRay = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; y--){

                    VoxelState thisVoxel = voxelMap[x,y,z];

                    if(thisVoxel.id > 0 && world.blockTypes[thisVoxel.id].transparency < lightRay){
                        lightRay = world.blockTypes[thisVoxel.id].transparency;
                    }
                    thisVoxel.globalLightPercent = lightRay;

                    voxelMap[x, y, z] = thisVoxel;

                    if(lightRay > VoxelData.lightFalloff){
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        while(litVoxels.Count > 0){

            Vector3Int v = litVoxels.Dequeue();

            for(int p = 0; p < 6; p++){
                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbor = new Vector3Int((int) currentVoxel.x, (int) currentVoxel.y, (int) currentVoxel.z);

                if(IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z)) {

                    if(voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if(voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff){
                            litVoxels.Enqueue(neighbor);
                        }
                    }
                }
            }

        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    VoxelState CheckVoxel(Vector3 pos){
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!IsVoxelInChunk(x, y, z)){
            return world.GetVoxelState(pos + position);
        }

        return voxelMap[x,y,z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos){
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return voxelMap[xCheck, yCheck, zCheck];
        
    }

    bool IsVoxelInChunk(int x, int y, int z){
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

        voxelMap[xCheck, yCheck, zCheck].id = newBlockId;

        lock(world.chunkUpdateThreadLock){
            world.chunksToUpdate.Insert(0, this);
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
        }
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));
            }
        }
    }

    void UpdateMeshData(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        byte blockId = voxelMap[x, y, z].id;

        for (int p = 0; p < 6; p++){
            
            VoxelState neighbor = CheckVoxel(position + VoxelData.faceChecks[p]);

            if (neighbor != null && world.blockTypes[neighbor.id].renderNeighborFaces)
            {
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for(int i = 0; i < 4; i++)
                {
                    normals.Add(VoxelData.faceChecks[p]);
                }

                AddTexture(world.blockTypes[blockId].GetTextureId(p));

                float lightLevel = neighbor.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));


                if (world.blockTypes[blockId].renderNeighborFaces)
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
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

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

public class VoxelState{

    public byte id;
    public float globalLightPercent;


    public VoxelState(){
        this.id = 0;
        this.globalLightPercent = 0f;
    }

    public VoxelState(byte id){
        this.id = id;
        this.globalLightPercent = 0f;
    }
}
