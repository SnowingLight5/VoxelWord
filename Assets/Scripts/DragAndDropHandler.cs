using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour
{
    [SerializeField]
    private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField]
    private GraphicRaycaster raycaster = null;
    private PointerEventData pointerEventData;
    [SerializeField]
    private EventSystem eventSystem = null;

    private World world;

    private void Start() {
        world = GameObject.Find("World").GetComponent<World>();
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update() {
        if(!world.inUi){
            return;
        }

        cursorSlot.transform.position = Input.mousePosition;

        if(Input.GetMouseButtonDown(0)){
            HandleSlotClick(CheckForSlot());
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot){
        if(clickedSlot == null){
            return;
        }

        if(!cursorSlot.hasItem && !clickedSlot.hasItem){
            return;
        }

        if(clickedSlot.itemSlot.isCreative){
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if(!cursorSlot.hasItem && clickedSlot.hasItem){
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if(cursorSlot.hasItem && !clickedSlot.hasItem){
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if(cursorSlot.hasItem && clickedSlot.hasItem){
            if(cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id){
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll(); 
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
            } else {
                //merge stack here
            } 

        }

    }

    private UIItemSlot CheckForSlot(){
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach(RaycastResult result in results){
            if(result.gameObject.tag == "UIItemSlot"){
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
