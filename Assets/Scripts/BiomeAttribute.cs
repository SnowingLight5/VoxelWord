using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttribute", menuName = "MinecraftTutorial/Biome Attribute")]
public class BiomeAttribute : ScriptableObject {

    [Header("Biome Information")]
    public string biomeName;
    public int offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Floare")]
    public int majorFloraIndex;
    public float majorFloraScale = 1.3f;
    [Range(0f, 1f)]
    public float majorFloraThreshold = 0.6f;
    public float majorPlacementFloraScale = 15f;
    [Range(0f, 1f)]
    public float majorFloraPlacementThreshold = 0.8f;

    public bool placeMajorFlora = false;

    public int maxMajorFloraHeight = 5;
    public int minMajorFloraHeight = 3;

    public Lode[] lodes;

}