using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{

    World world;
    Text text;

    float frameRate;
    float timer;

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        string debugText = "Debug Screen.";

        debugText += "\n ";
        debugText += frameRate + " fps";
        debugText += "\n\n";
        debugText += "XYZ: " + Mathf.FloorToInt(world.player.transform.position.x) + " / " 
                             + Mathf.FloorToInt(world.player.transform.position.y) + " / " 
                             + Mathf.FloorToInt(world.player.transform.position.z);
        debugText += "\n";
        debugText += "Chunk: " + world.playerChunkCoord.x + "/" + world.playerChunkCoord.z;
    

        text.text = debugText;

        if(timer > 1f){
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }else{
            timer += Time.deltaTime;
        }
    }
}
