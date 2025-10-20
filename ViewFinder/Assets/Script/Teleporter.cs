using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using DG.Tweening;
using System;


//传送门，负责视觉效果所需的纹理，场景加载与对应交互与可见性调整
public class Teleporter : MonoBehaviour
{
    public string loadSence;//要交互的场景名称，传送的终点
    public string unloadSence;//要卸载的场景名称，传送的起点

    private Camera observeCamera;//传送门的场景观测相机
    private bool isObserve = true;
    private Camera playerCamera;//角色相机，用于定位观测相机

    private RenderTexture observeTexture;//透视纹理
    public MeshRenderer screen;//透视窗口

    public Transform calibrationPoint;//传送位置矫正点（传送前调整玩家位置）
 

    // Start is called before the first frame update
    void Start()
    {
        //异步加载对应场景
        SceneManager.LoadSceneAsync(loadSence, LoadSceneMode.Additive);

        
        observeTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        observeTexture.Create();

        //为观察相机设置可见性，输出目标，输出到该纹理
        observeCamera = this.gameObject.GetComponentInChildren<Camera>();
        observeCamera.cullingMask = LayerMask.GetMask(loadSence);
        observeCamera.targetTexture = observeTexture;


        screen.material.SetTexture("_MainTex", observeTexture);


        //将观测相机绑定到角色相机上
        playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        observeCamera.transform.SetParent(playerCamera.transform, false);


        //处理场景的可见性与可交互性
        observeCamera.cullingMask = LayerMask.GetMask(loadSence);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer(loadSence), LayerMask.NameToLayer("Default"), true);

    }

    // Update is called once per frame
    void Update()
    {

    }


    private void OnDestroy()
    {
        observeTexture.Release();//纹理资源需手动销毁
    }

    //传送时对玩家的调整
    public void MovePlayer()
    {
        GameObject player = GameObject.Find("Player");

        player.GetComponent<CharacterController>().enabled = false;//在调整玩家位置时必须关闭角色碰撞
        //因为该组件的皮肤使得实际碰撞检测范围大于胶囊范围，在我们调整角色位置后可能因为皮肤碰撞被弹回原位置

        Vector3 end = new(calibrationPoint.position.x, 1.0f, calibrationPoint.position.z);
        player.transform.DOMove(end, 2.0f);
        player.transform.DODynamicLookAt(end - Vector3.up * 0.5f, 2.0f, AxisConstraint.Y);

        playerCamera.transform.DODynamicLookAt(screen.transform.position, 2.0f, AxisConstraint.Y)
            .OnComplete(SceneSwitch);


        void SceneSwitch()
        {

            isObserve = false;

            screen.transform.DOScale(new Vector3(10, 10, 1), 2.0f)
                .OnComplete(() => { StartCoroutine(Switch()); });

            //交互与可见性设置
            IEnumerator Switch()
            {
                //可交互性
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer(unloadSence), true);
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer(loadSence), false);

                //可见性
                playerCamera.cullingMask = LayerMask.GetMask(loadSence, "Default", "UI");


                player.GetComponent<CharacterController>().enabled = true;
                player.GetComponent<PlayController>().CurrentScene = loadSence;
                screen.transform.localScale = Vector3.zero;
                player.GetComponent<PlayController>().isSwitchScene = false;

                observeCamera.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(observeCamera.gameObject, SceneManager.GetSceneByName(unloadSence));

                //卸载场景
                yield return SceneManager.UnloadSceneAsync(unloadSence);

            }
        }
    }


    
}
