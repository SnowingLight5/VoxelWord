using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttribute", menuName = "MinecraftTutorial/Biome Attribute")]
public class BiomeAttribute : ScriptableObject {
    
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 1.3f;
    [Range(0f, 1f)]
    public float treeZoneThreshold = 0.6f;
    public float treePlacementScale = 15f;
    [Range(0f, 1f)]
    public float treePlacementThreshold = 0.8f;

    public int maxTreeHeight = 5;
    public int minTreeHeight = 3;

    public Lode[] lodes;

}