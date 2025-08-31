using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Controls;

//胶片操控类
public class FilmContriol : MonoBehaviour
{

    public Transform LookInFilmPoint;//观察相片时的位置
    public Transform HandheldFilmPoint;//film手持点
    public Transform FrustumPoint;//切割视锥的顶点
    

    public float rotateSpead = -10.0f;//旋转速度

    public PlayerInput polaroidInput;
    public PlayerInput thisInput;
    public UnityEvent<bool> cutHolder;


    private void Start()
    {
        this.GetComponent<PlayerInput>().actions.Disable();
    }

    //观察照片时的回调
    public void LookInFilm(InputAction.CallbackContext callbackContext)
    {

        if (callbackContext.performed)
        {
            this.GetComponentInParent<PlayController>().PlayerState(false);

            this.GetComponentInParent<PlayController>().isSwitchScene = true;

            //移动到指定位置
            this.transform.DOMove(LookInFilmPoint.position, 1.2f, false);
            this.transform.DORotate(LookInFilmPoint.rotation.eulerAngles, 1.2f, RotateMode.Fast).OnComplete(() => this.GetComponentInParent<PlayController>().PlayerState(true));

            FrustumPoint.Rotate(Vector3.zero, Space.Self);

            Debug.Log($"LookInFilm, {callbackContext.phase}, {this.name}");

        }
    }


    //移开照片时的回调
    public void LookOutFilm(InputAction.CallbackContext callbackContext)
    {
        int x = 0;

        if (callbackContext.performed)
        {

            this.GetComponentInParent<PlayController>().PlayerState(false);

            //
            this.transform.DOMove(HandheldFilmPoint.position, 1.2f, false);
            this.transform.DORotate(HandheldFilmPoint.rotation.eulerAngles, 1.2f, RotateMode.Fast).OnComplete(RecoverAnimatorAndPlayer);


            void RecoverAnimatorAndPlayer()
            {
                //this.GetComponentInParent<Animator>().speed = 1.0f;
                this.GetComponentInParent<PlayController>().PlayerState(true);

                Debug.Log(x++);
            }

            Debug.Log("LookOutFilm");
        }

    }

    //旋转照片时的回调
    public void RotateFilm(InputAction.CallbackContext callbackContext)
    {
        Debug.Log("RotateFilm");

        float dir = callbackContext.ReadValue<float>();
        if( dir > 0 )
            //绕一个轴（根据第三个参数决定是在局部空间轴还是世界空间轴）旋转一定角度
            FrustumPoint.Rotate(Vector3.forward, rotateSpead * Time.deltaTime, Space.Self);
        else if(dir < 0)
            FrustumPoint.Rotate(Vector3.forward, -1 * rotateSpead * Time.deltaTime, Space.Self);

    }


    //使用照片时的回调
    public void UseFilm(InputAction.CallbackContext callbackContext)
    {

        if (!callbackContext.performed) return;

        if (this.name == "Film(Clone)")
        {
            this.cutHolder?.Invoke(false);//调用切割方法

            //在Polaroid类中我阐述了当下我在操作切换中遇到的问题，这是目前可行的方案
            polaroidInput.actions.Enable();
            this.GetComponent<PlayerInput>().actions.Disable();

            Debug.Log($"UseFilm, {this.name}");
            Destroy(this.gameObject);
        }

    }


}
