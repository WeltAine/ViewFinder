using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public class PlayController : MonoBehaviour
{

    public Camera playerCamera;
    private string currentScene;
    public string CurrentScene { get { return currentScene; } set { this.currentSceneRef.sceneName = value; this.currentScene = value; }  }
    public SceneReference currentSceneRef = new SceneReference();


    public Vector3 playerMove = Vector2.zero;
    public Vector2 playerLook = Vector2.zero;
    public float speed = 3.0f;

    public float mouseXSensitivity = 5.0f;
    public float mouseYSensitivity = 5.0f;

    public float gravity = -9.81f;//重力
    public float jumpHight = 0.8f;

    public CharacterController characterController;

    public bool isSwitchScene = false;

    // Start is called before the first frame update
    void Start()
    {
        characterController = this.GetComponent<CharacterController>();
        playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {

        if (!isSwitchScene)
        {
            //获取输入，并控制玩家

            //玩家位移
            playerMove.x = Input.GetAxis("Horizontal") * speed;
            playerMove.z = Input.GetAxis("Vertical") * speed;


            if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
            {
                playerMove.y = Mathf.Sqrt(-2.0f * jumpHight * gravity);
            }



            //鼠标移动，改变玩家视野
            playerLook.x = Input.GetAxis("Mouse X");
            playerLook.y = Input.GetAxis("Mouse Y");
            //改变transform（转身）和相机朝向（抬头）
            playerCamera.transform.Rotate(-playerLook.y * Vector3.right * mouseYSensitivity, Space.Self);//相机只对Y有反应
            this.transform.Rotate(playerLook.x * Vector3.up * mouseXSensitivity, Space.Self);//身体只对X有反应



            //对着传送器按下M键
            if (Input.GetKeyDown(KeyCode.M))
            {
                Ray cast = Camera.main.ScreenPointToRay(new(Screen.width / 2.0f, Screen.height / 2.0f));
                RaycastHit hit;
                Physics.Raycast(cast, out hit, 2.0f, LayerMask.GetMask(CurrentScene));



                if (hit.transform.tag == "Teleporter")
                {
                    this.isSwitchScene = true;//禁止响应玩家操控
                    hit.transform.GetComponent<Teleporter>().MovePlayer();

                }
            }


            //相机控制（不写在这里，写在polaroid中）


        }
    }


    private void FixedUpdate()
    {

        playerMove.y += Time.fixedDeltaTime * gravity;

        Vector3 direction = this.transform.right * playerMove.x + this.transform.forward * playerMove.z + playerMove.y * Vector3.up ;

        if (characterController.enabled)
        {
            characterController.Move(direction * Time.fixedDeltaTime);
        }



    }


    public void PlayerState(bool isExercise)
    {

        this.isSwitchScene = !isExercise;
        this.playerMove = Vector3.zero;

    }


}


public class SceneReference
{
    public string sceneName;
}
