using UnityEngine;

public class VoxelMod {
    public Vector3 position;
    public byte id;


    public VoxelMod() {
        this.position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 position, byte id) {
        this.position = position;
        this.id = id;
    }
}
