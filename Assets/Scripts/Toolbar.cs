using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
    
    public UIItemSlot[] slots;
    public int slotIndex = 0;

    public Player player;
    public Sprite selectedBackground;
    public Sprite unselectedBackground;

    private void Start(){

        byte index = 1;

        foreach(UIItemSlot s in slots){
            ItemStack stack = new ItemStack(index, Random.Range(2, 65));
            ItemSlot slot = new ItemSlot(s, stack);
            index++;
        }
    }

    private void Update(){

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if(scroll != 0){

            slots[slotIndex].slotImage.sprite = unselectedBackground;


            if(scroll > 0){
                slotIndex--;
            } else {
                slotIndex ++;
            }

            if(slotIndex > slots.Length - 1){
                slotIndex = 0;
            } else if (slotIndex < 0){
                slotIndex = slots.Length - 1;
            }

            slots[slotIndex].slotImage.sprite = selectedBackground;
        }

    }

}
