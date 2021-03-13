using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toolbar : MonoBehaviour
{

    public World world;
    public Player player;

    public ItemSlot[] itemSlots;

    public Sprite backgroundImage;
    public Sprite highlightBackgroundImage;

    int slotIndex = 0;

    void Start()
    {
        foreach (ItemSlot itemSlot in itemSlots)
        {
            itemSlot.icon.sprite = world.blockTypes[itemSlot.itemId].icon;
            itemSlot.icon.enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if(scroll != 0)
        {
            itemSlots[slotIndex].backgroundIcon.sprite = backgroundImage;

            if (scroll < 0)
            {
                slotIndex++;
            }else
            {
                slotIndex--;
            }

            if(slotIndex > itemSlots.Length - 1)
            {
                slotIndex = 0;
            }
            if(slotIndex < 0)
            {
                slotIndex = itemSlots.Length - 1;
            }

            itemSlots[slotIndex].backgroundIcon.sprite = highlightBackgroundImage;
            player.selectedBlockIndex = itemSlots[slotIndex].itemId;
        }
    }
}

[System.Serializable]
public class ItemSlot
{
    public byte itemId;
    public Image icon;
    public Image backgroundIcon;

}
