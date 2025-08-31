using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
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

    public Camera observeCamera;//传送门的场景观测相机
    public bool isObserve = true;
    public Camera playerCamera;//角色相机，主要用于定位

    public RenderTexture observeTexture;//观测纹理
    public RawImage cameraRT;//传送门上的UI

    public Transform calibrationPoint;//传送位置矫正点



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


        //为UI设置使用的纹理，使用该纹理，实现视觉效果
        cameraRT = this.gameObject.GetComponentInChildren<RawImage>();
        cameraRT.texture = observeTexture;


        //aimMask = this.transform.Find("Mask");

        //将传送门相机定位到角色身上
        //先找到角色相机
        playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        //设置父对象
        observeCamera.transform.SetParent(playerCamera.transform, false);//第二个参数是世界空间保持，这样设置父对象时不会改变物体在空间中的呈现效果，但我们选择的时false


        //处理场景
        //可见性处理
        observeCamera.cullingMask = LayerMask.GetMask(loadSence);
        //可交互性处理
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer(loadSence), LayerMask.NameToLayer("Default"), true);

    }

    // Update is called once per frame
    void Update()
    {

        if (isObserve)
        {

            //调整传送门观测相机朝向
            observeCamera.transform.LookAt(cameraRT.transform);

        }

    }


    private void OnDestroy()
    {
        observeTexture.Release();//手动销毁资源

    }


    public void MovePlayer()
    {
        GameObject player = GameObject.Find("Player");

        player.GetComponent<CharacterController>().enabled = false;//必须开这个
        //因为该组件的皮肤使得实际碰撞检测范围大于胶囊范围，在我们调整角色位置后可能因为皮肤碰撞被弹回原位置

        Vector3 end = new(calibrationPoint.position.x, 1.0f, calibrationPoint.position.z);
        player.transform.DOMove(end, 2.0f);
        player.transform.DODynamicLookAt(end - Vector3.up * 0.5f, 2.0f, AxisConstraint.Y);
        playerCamera.transform.DODynamicLookAt(cameraRT.transform.position, 2.0f, AxisConstraint.Y)
            .OnComplete(SceneSwitch);


        void SceneSwitch()
        {

            isObserve = false;

            Canvas canvas = this.GetComponentInChildren<Canvas>();
            //canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = playerCamera;//确保正确的观测相机

            RectTransform maskRectTransform = this.GetComponentInChildren<Mask>().rectTransform;
            maskRectTransform.DOSizeDelta(new(3000f, 3000f), 2.0f)
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
                maskRectTransform.sizeDelta = Vector2.zero;
                player.GetComponent<PlayController>().isSwitchScene = false;

                //卸载场景
                yield return SceneManager.UnloadSceneAsync(unloadSence);


            }
        }
    }


    
}
