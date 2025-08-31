using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public Camera playCamera;


    // Start is called before the first frame update
    void Start()
    {
        //异步加载初始场景Level0，关闭相关碰撞
        playCamera = GameObject.Find("Player").GetComponentInChildren<Camera>();

        StartCoroutine(LoadLevel0());

        GameObject.Find("Player").GetComponent<PlayController>().CurrentScene = "Level0";

    }

    // Update is called once per frame
    void Update()
    {
    }

    private void FixedUpdate()
    {
        
    }


    private  IEnumerator LoadLevel0()
    {
        //加载场景
        yield return SceneManager.LoadSceneAsync("Level0", LoadSceneMode.Additive);

        //设置角色相机可见性，以及碰撞
        playCamera.cullingMask = LayerMask.GetMask("Default", "Level0", "UI");
        


    }
}
