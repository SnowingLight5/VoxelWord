using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    
    public bool isGrounded;
    public bool isSprinting;

    public float mouseSensibility = 1f;
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.3f;

    private Transform cam;
    private World world;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    public Transform highlightBlock;
    public Transform placeBlock;
    public GameObject debugScreen;

    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Toolbar toolbar;


    private void Start() {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        world.inUi = false;
    }

    private void FixedUpdate() {

        if(world.inUi){
            return;
        }

        CalculateVelocity();
        if(jumpRequest){
            Jump();
        }

        transform.Translate(velocity, Space.World);
    }

    private void Update() {

        if (Input.GetKeyDown(KeyCode.I)){
            world.inUi = !world.inUi;
        }

        if(world.inUi){
            return;
        }

        GetPlayerInputs();
        PlaceCursorBlocks();


        transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivy);
        cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivy);
    }

    void Jump(){

        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;

    }

    private void CalculateVelocity () {

        if(verticalMomentum > gravity){
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        if(isSprinting){
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        } else {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        }

        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;
        
        if((velocity.z > 0 && front) || (velocity.z < 0 && back)){
            velocity.z = 0;
        }

        if((velocity.x > 0 && right) || (velocity.x < 0 && left)){
            velocity.x = 0;
        }

        if(velocity.y < 0){
            velocity.y = CheckDownSpeed(velocity.y);
        } else if (velocity.y > 0){
            velocity.y = CheckUpSpeed(velocity.y);
        }
    }

    void GetPlayerInputs(){

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if(Input.GetButtonDown("Sprint")){
            isSprinting = true;
        }
        if(Input.GetButtonUp("Sprint")){
            isSprinting = false;
        }

        if(isGrounded && Input.GetButtonDown("Jump")){
            jumpRequest = true;
        }

        if(Input.GetKeyDown(KeyCode.F3)){
            debugScreen.SetActive(!debugScreen.activeSelf);
            highlightBlock.gameObject.SetActive(!highlightBlock.gameObject.activeSelf);
            placeBlock.gameObject.SetActive(!placeBlock.gameObject.activeSelf);
        }

        
        if (Input.GetMouseButtonDown(0) && highlightBlock.position.x != 0 && highlightBlock.position.y != 0 && highlightBlock.position.z != 0){
            world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
        }

        if (Input.GetMouseButtonDown(1) && placeBlock.position.x != 0 && placeBlock.position.y != 0 && placeBlock.position.z != 0)
        {
            if(toolbar.slots[toolbar.slotIndex].hasItem){
                world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
            }
        }
        

    }

    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while(step < reach)
        {
            Vector3 pos = cam.position + (cam.forward * step);

            if (world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                return;
            }
            else
            {
                lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                step += checkIncrement;
            }
        }

        highlightBlock.position = new Vector3(0, 0, 0);
        placeBlock.position = new Vector3(0, 0, 0);

    }

    private float CheckDownSpeed(float downSpeed){
        
        if(world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))){
            isGrounded = true;
            return 0;
        }else{
            isGrounded = false;
            return downSpeed;
        }
    }

    private float CheckUpSpeed(float upSpeed){
        
        if(world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))
        || world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))){
            return 0;
        }else{
            return upSpeed;
        }
    }

    public bool front {
        get { 
            if(world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth))
            || world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))){
                return true;
            } else {
                return false;
            }
        }
    }

    public bool back {
        get { 
            if(world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth))
            || world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))){
                return true;
            } else {
                return false;
            }
        }
    }

    public bool left {
        get { 
            if(world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z))
            || world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))){
                return true;
            } else {
                return false;
            }
        }
    }

    public bool right {
        get { 
            if(world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z))
            || world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))){
                return true;
            } else {
                return false;
            }
        }
    }

}
