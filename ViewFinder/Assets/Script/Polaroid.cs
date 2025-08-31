using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Polaroid : MonoBehaviour
{
    public Camera polaroidCamera;
    public Animator polaroidAnimator;
    public int filmNumber = 5;
    public RenderTexture cameraPicture;
    public RenderTexture filmPicture;

    public GameObject film;
    private GameObject filmClone;
    public Transform filmPoint, filmSpacePoint;


    public bool overridePosition = false;

    //public Action<Boolean> cutHolder;//无法序列化
    public UnityEvent<bool> cutHolder;//可以序列化，使用起来也无太大差别（至少一个参数，void返回）

    // Start is called before the first frame update
    void Start()
    {
        polaroidCamera.aspect = 1.0f;

    }

    // Update is called once per frame
    void Update()
    {
        polaroidCamera.cullingMask = this.gameObject.GetComponentInParent<Camera>().cullingMask;

    }

    public void ProduceFilm_Copy()
    {
        Graphics.CopyTexture(cameraPicture, filmPicture);//复制画面到胶卷中

        filmClone = GameObject.Instantiate(film);//产生一个副本，但动画中运行中的仍是本体，我们之后调整一下位置
        filmClone.transform.localScale = new Vector3(0.0125f, 1.0f, 0.0125f);
        filmClone.transform.SetParent(filmSpacePoint);
        
    }

    //在按下快门动画（PressTheShutter）的末尾执行film本体与副本交换位置
    public void Film_Switch()
    {
        filmClone.transform.SetPositionAndRotation(film.transform.position, film.transform.rotation);
        film.transform.SetPositionAndRotation(filmPoint.position, filmPoint.rotation);

        filmClone.GetComponent<PlayerInput>().actions.Enable() ;
        this.GetComponent<PlayerInput>().actions.Disable();


    }

    public void PlayerState(int isExercise)
    {
        this.GetComponentInParent<PlayController>().PlayerState((isExercise > 0));
    }


    //下面为一系列绑定到playerinput的回调方法

    public void PressShutter(InputAction.CallbackContext callbackContext)
    {

        if (polaroidAnimator.GetCurrentAnimatorStateInfo(0).IsName("Look") && filmNumber > 0 && callbackContext.performed)
        {
            Debug.Log("produce Film");
            this.GetComponentInParent<PlayController>().isSwitchScene = true;
            polaroidAnimator.SetTrigger("Press");
            cutHolder?.Invoke(true);//开始切割
            this.GetComponentInParent<PlayController>().isSwitchScene = false;

        }
    }


    public void LookOut(InputAction.CallbackContext callbackContext)
    {
        if (polaroidAnimator.GetCurrentAnimatorStateInfo(0).IsName("Look"))
        {
            polaroidAnimator.SetTrigger("LookOut");
            polaroidAnimator.ResetTrigger("LookIn");

        }
    }


    public void LookIn(InputAction.CallbackContext callbackContext)
    {
        if (!callbackContext.performed) return;

        if (polaroidAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idel"))
        {
            polaroidAnimator.ResetTrigger("LookOut");
            polaroidAnimator.ResetTrigger("Press");
            polaroidAnimator.SetTrigger("LookIn");
        }
    }
}
