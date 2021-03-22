using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
public class AtlasPacker : EditorWindow
{

    int blockSize = 512; //block size in pixels.
    int atlasSizeInBlocks = 16;
    int atlasSize;

    Object[] rawTextures = new Object[256];
    List<Texture2D> sortedTextures = new List<Texture2D>();
    Texture2D atlas;

    [MenuItem ("VoxelWorld/Atlas Packer")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;

        GUILayout.Label("Voxel world texture atlas packer", EditorStyles.boldLabel);

        blockSize = EditorGUILayout.IntField("Block size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas size in blocks", atlasSizeInBlocks);

        if(GUILayout.Button("Load Textures"))
        {
            LoadTextures();
            PackAtlas();
            Debug.Log("Atlas Packer: Textures loaded");
        }

        if (GUILayout.Button("Clear Textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);
            Debug.Log("Atlas Packer: Textures cleared");
        }

        if(GUILayout.Button("Save Atlas"))
        {
            byte[] bytes = atlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Textures/PackedAtlas.png", bytes);
                Debug.Log("Atlas Packer: Textures saved");
            }
            catch
            {
                Debug.Log("Atlas Packer: Couldn't save atlas to file.");
            }
        }

        GUILayout.Label(atlas);

    }

    void LoadTextures()
    {
        sortedTextures.Clear();
        rawTextures = new Object[blockSize * atlasSizeInBlocks];
        rawTextures = Resources.LoadAll("AtlasPacker/textures", typeof(Texture2D));

        int index = 0;

        foreach(Object texture in rawTextures)
        {
            Texture2D t = (Texture2D) texture;
            if (t.width == blockSize && t.height == blockSize)
            {
                if (texture.name.EndsWith("_n"))
                {
                    Debug.Log("Ignoring n textures");
                }
                else if (texture.name.EndsWith("_s")) {
                    Debug.Log("Ignoring s textures");
                }
                else if (texture.name.Contains("glass_pane"))
                {
                    Debug.Log("Ignoring glass pane textures");
                }
                else if (texture.name.Contains("torch"))
                {
                    Debug.Log("Ignoring torch textures");
                }
                else
                {
                    Debug.Log(texture.name);
                    sortedTextures.Add(t);
                }
            }else
            {
                Debug.Log("Asset Packer: " + texture.name + " incorrect size. Texture not loaded");
            }

            index++;
        }

        Debug.Log("Atlas Packer: " + sortedTextures.Count);
    }

    void PackAtlas()
    {
        atlas = new Texture2D(atlasSize, atlasSize);
        Color[] pixels = new Color[atlasSize * atlasSize];

        for(int x = 0; x < atlasSize; x++)
        {
            for (int y = 0; y < atlasSize; y++)
            {
                int currentBlockX = x / blockSize;
                int currentBlockY = y / blockSize;

                int index = currentBlockY * atlasSizeInBlocks + currentBlockX;

                int currentPixelX = x - (currentBlockX * blockSize);
                int currentPixelY = y - (currentBlockY * blockSize);

                if(index < sortedTextures.Count)
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sortedTextures[index].GetPixel(x, blockSize - y - 1);
                }
                else
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);         
                }
            }

        }

        atlas.SetPixels(pixels);
        atlas.Apply();
    }
}
