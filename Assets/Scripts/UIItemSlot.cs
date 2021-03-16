using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIItemSlot : MonoBehaviour
{
    
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public Text slotAmount;

    World world;

    private void Awake(){
        world = GameObject.Find("World").GetComponent<World>();
    }

    public bool hasItem{
        get { 
            if(itemSlot == null){
                return false;
            } else {
                return itemSlot.hasItem;
            }
        }
    }

    public void Link(ItemSlot itemSlot){
        this.itemSlot = itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void Unlink(){
        itemSlot.UnlinkUISlot();
        this.itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot(){
        if(itemSlot != null && itemSlot.hasItem){

            slotIcon.sprite = world.blockTypes[itemSlot.stack.id].icon;
            slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;

        } else {
            Clear();
        }
    }

    public void Clear(){
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy(){
        if(isLinked){
            itemSlot.UnlinkUISlot();
        }
    }
}

public class ItemSlot {

    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public ItemSlot(UIItemSlot uiItemSlot){
        this.uiItemSlot = uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot uiItemSlot, ItemStack stack){
        this.stack = stack;
        this.uiItemSlot = uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiSlot){
        uiItemSlot = uiSlot;
    }

    public void UnlinkUISlot(){
        uiItemSlot = null;
    }

    public void EmptySlot(){
        stack = null;
        if(uiItemSlot != null){
            uiItemSlot.UpdateSlot();
        }
    }

    public int Take(int amount){
        if(amount > stack.amount){
            int amt = stack.amount;
            EmptySlot();
            return amt;
        } else if(amount < stack.amount){
            stack.amount -= amount;
            uiItemSlot.UpdateSlot();
            return amount;
        } else{
            EmptySlot();
            return amount;
        }


    }

    public bool hasItem {
        get {
            if(stack != null){
                return true;
            } else {
                return false;
            }
        }
    }
}
