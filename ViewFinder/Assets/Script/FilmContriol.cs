using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.Animations;
using UnityEngine.InputSystem.Controls;

//��Ƭ�ٿ���
public class FilmContriol : MonoBehaviour
{

    public Transform LookInFilmPoint;//�۲���Ƭʱ��λ��
    public Transform HandheldFilmPoint;//film�ֳֵ�
    public Transform FrustumPoint;//�и���׶�Ķ���
    

    public float rotateSpead = -10.0f;//��ת�ٶ�

    public PlayerInput polaroidInput;
    public PlayerInput thisInput;
    public UnityEvent<bool> cutHolder;


    private void Start()
    {
        this.GetComponent<PlayerInput>().actions.Disable();
    }

    //�۲���Ƭʱ�Ļص�
    public void LookInFilm(InputAction.CallbackContext callbackContext)
    {

        if (callbackContext.performed)
        {
            this.GetComponentInParent<PlayController>().PlayerState(false);

            this.GetComponentInParent<PlayController>().isSwitchScene = true;

            //�ƶ���ָ��λ��
            this.transform.DOMove(LookInFilmPoint.position, 1.2f, false);
            this.transform.DORotate(LookInFilmPoint.rotation.eulerAngles, 1.2f, RotateMode.Fast).OnComplete(() => this.GetComponentInParent<PlayController>().PlayerState(true));

            FrustumPoint.Rotate(Vector3.zero, Space.Self);

            Debug.Log($"LookInFilm, {callbackContext.phase}, {this.name}");

        }
    }


    //�ƿ���Ƭʱ�Ļص�
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

    //��ת��Ƭʱ�Ļص�
    public void RotateFilm(InputAction.CallbackContext callbackContext)
    {
        Debug.Log("RotateFilm");

        float dir = callbackContext.ReadValue<float>();
        if( dir > 0 )
            //��һ���ᣨ���ݵ����������������ھֲ��ռ��ỹ������ռ��ᣩ��תһ���Ƕ�
            FrustumPoint.Rotate(Vector3.forward, rotateSpead * Time.deltaTime, Space.Self);
        else if(dir < 0)
            FrustumPoint.Rotate(Vector3.forward, -1 * rotateSpead * Time.deltaTime, Space.Self);

    }


    //ʹ����Ƭʱ�Ļص�
    public void UseFilm(InputAction.CallbackContext callbackContext)
    {

        if (!callbackContext.performed) return;

        if (this.name == "Film(Clone)")
        {
            this.cutHolder?.Invoke(false);//�����и��

            //��Polaroid�����Ҳ����˵������ڲ����л������������⣬����Ŀǰ���еķ���
            polaroidInput.actions.Enable();
            this.GetComponent<PlayerInput>().actions.Disable();

            Debug.Log($"UseFilm, {this.name}");
            Destroy(this.gameObject);
        }

    }


}
