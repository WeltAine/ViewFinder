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

    //public Action<Boolean> cutHolder;//�޷����л�
    public UnityEvent<bool> cutHolder;//�������л���ʹ������Ҳ��̫��������һ��������void���أ�

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
        Graphics.CopyTexture(cameraPicture, filmPicture);//���ƻ��浽������

        filmClone = GameObject.Instantiate(film);//����һ���������������������е����Ǳ��壬����֮�����һ��λ��
        filmClone.transform.localScale = new Vector3(0.0125f, 1.0f, 0.0125f);
        filmClone.transform.SetParent(filmSpacePoint);
        
    }

    //�ڰ��¿��Ŷ�����PressTheShutter����ĩβִ��film�����븱������λ��
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


    //����Ϊһϵ�а󶨵�playerinput�Ļص�����

    public void PressShutter(InputAction.CallbackContext callbackContext)
    {

        if (polaroidAnimator.GetCurrentAnimatorStateInfo(0).IsName("Look") && filmNumber > 0 && callbackContext.performed)
        {
            Debug.Log("produce Film");
            this.GetComponentInParent<PlayController>().isSwitchScene = true;
            polaroidAnimator.SetTrigger("Press");
            cutHolder?.Invoke(true);//��ʼ�и�
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
