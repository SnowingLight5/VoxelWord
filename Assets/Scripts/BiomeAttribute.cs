using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttribute", menuName = "MinecraftTutorial/Biome Attribute")]
public class BiomeAttribute : ScriptableObject {
    
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;
    public Lode[] lodes;

}