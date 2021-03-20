using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure {
    

    public static Queue<VoxelMod> GenerateMajorFlora(int index, Vector3 groundPosition, int minTrunkHeight, int maxTrunkHeight)
    {
        switch (index)
        {
            case 0:
               return MakeTree(groundPosition, minTrunkHeight, maxTrunkHeight);
            case 1:
                return MakeCactus(groundPosition, minTrunkHeight, maxTrunkHeight);
        }

        return new Queue<VoxelMod>();
    }

    public static Queue<VoxelMod> MakeTree(Vector3 groundPosition, int minTrunkHeight, int maxTrunkHeight)
    {

        Vector3 position = new Vector3(groundPosition.x, groundPosition.y + 1, groundPosition.z);

        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        // Generate trunk
        int height = minTrunkHeight +  (int)((maxTrunkHeight - minTrunkHeight) * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f));

        for(int i = 0; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 7));
        }

        //Generate Leaves
        for(int x = -2; x < 3; x++)
        {
            for (int y = -3; y < 1; y++)
            {
                for (int z = -2; z < 3; z++)
                {
                    if(y < 0 && x == 0 && z == 0){
                        continue;
                    }

                    if(y < -1){
                        if(Mathf.Abs(x) + Mathf.Abs(z) > 3){
                            continue;
                        }
                    }else if(y == -1){
                        if(Mathf.Abs(x) > 1 || Mathf.Abs(z) > 1){
                            continue;
                        }
                    }else if (y == 0){
                        if(Mathf.Abs(x) + Mathf.Abs(z) > 1){
                            continue;
                        }
                    }

                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), 11));
                }
            }

        }

        return queue;
    }

    public static Queue<VoxelMod> MakeCactus(Vector3 groundPosition, int minTrunkHeight, int maxTrunkHeight)
    {

        Vector3 position = new Vector3(groundPosition.x, groundPosition.y + 1, groundPosition.z);

        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 23456f, 2f));
        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        for (int i = 0; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 13));
        }

        return queue;
    }
}
