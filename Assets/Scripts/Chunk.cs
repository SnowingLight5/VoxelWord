using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {

    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    Material[] materials = new Material[2];

    public Vector3 position;

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

    ChunkData chunkData;

    public Chunk(ChunkCoord chunkCoord){
        coord = chunkCoord;
    }

    public void Init(){
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.chunkWidth, 0f, coord.z * VoxelData.chunkWidth);
        chunkObject.name = "Chunk: " + coord.x + ", " + coord.z;

        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int) position.x, (int) position.z), true);

        lock (World.Instance.chunkUpdateThreadLock) {
            World.Instance.chunksToUpdate.Add(this);
        }

        if (World.Instance.settings.enableAnimatedChunks) {
            chunkObject.AddComponent<ChunkLoadAnimation>();
        }
    }



    public void UpdateChunk(){

        ClearMeshData();
        
        CalculateLight();

        for (int y = 0; y < VoxelData.chunkHeight; y++){
            for(int x = 0; x < VoxelData.chunkWidth; x++){
                for(int z = 0; z < VoxelData.chunkWidth; z++){

                    if(World.Instance.blockTypes[chunkData.map[x,y,z].id].isSolid){
                        UpdateMeshData(new Vector3(x,y,z));
                    }
                }
            }
        }

        lock(World.Instance.chunksToDraw) {
            World.Instance.chunksToDraw.Enqueue(this);
        }

    }

    void CalculateLight(){

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for(int x = 0; x < VoxelData.chunkWidth; x++){
            for(int z = 0; z < VoxelData.chunkWidth; z++){

                float lightRay = 1f;

                for (int y = VoxelData.chunkHeight - 1; y >= 0; y--){

                    VoxelState thisVoxel = chunkData.map[x,y,z];

                    if(thisVoxel.id > 0 && World.Instance.blockTypes[thisVoxel.id].transparency < lightRay){
                        lightRay = World.Instance.blockTypes[thisVoxel.id].transparency;
                    }
                    thisVoxel.globalLightPercent = lightRay;

                    chunkData.map[x, y, z] = thisVoxel;

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

                    if(chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if(chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff){
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
            return World.Instance.GetVoxelState(pos + position);
        }

        return chunkData.map[x,y,z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos){
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map[xCheck, yCheck, zCheck];
        
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

        chunkData.map[xCheck, yCheck, zCheck].id = newBlockId;
        World.Instance.worldData.AddToModifiedChunkList(chunkData);

        lock(World.Instance.chunkUpdateThreadLock){
            World.Instance.chunksToUpdate.Insert(0, this);
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
                World.Instance.chunksToUpdate.Insert(0, World.Instance.GetChunkFromVector3(currentVoxel + position));
            }
        }
    }

    void UpdateMeshData(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        byte blockId = chunkData.map[x, y, z].id;

        for (int p = 0; p < 6; p++){
            
            VoxelState neighbor = CheckVoxel(position + VoxelData.faceChecks[p]);

            if (neighbor != null && World.Instance.blockTypes[neighbor.id].renderNeighborFaces)
            {
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(position + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for(int i = 0; i < 4; i++)
                {
                    normals.Add(VoxelData.faceChecks[p]);
                }

                AddTexture(World.Instance.blockTypes[blockId].GetTextureId(p));

                float lightLevel = neighbor.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if (World.Instance.blockTypes[blockId].renderNeighborFaces)
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
        
        textureId = textureId - 1;

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

[System.Serializable]
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
