using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using DG.Tweening;
using System;


//�����ţ������Ӿ�Ч����������������������Ӧ������ɼ��Ե���
public class Teleporter : MonoBehaviour
{
    public string loadSence;//Ҫ�����ĳ������ƣ����͵��յ�
    public string unloadSence;//Ҫж�صĳ������ƣ����͵����

    public Camera observeCamera;//�����ŵĳ����۲����
    public bool isObserve = true;
    public Camera playerCamera;//��ɫ�������Ҫ���ڶ�λ

    public RenderTexture observeTexture;//�۲�����
    public RawImage cameraRT;//�������ϵ�UI

    public Transform calibrationPoint;//����λ�ý�����



    // Start is called before the first frame update
    void Start()
    {
        //�첽���ض�Ӧ����
        SceneManager.LoadSceneAsync(loadSence, LoadSceneMode.Additive);

        
        observeTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        observeTexture.Create();

        //Ϊ�۲�������ÿɼ��ԣ����Ŀ�꣬�����������
        observeCamera = this.gameObject.GetComponentInChildren<Camera>();
        observeCamera.cullingMask = LayerMask.GetMask(loadSence);
        observeCamera.targetTexture = observeTexture;


        //ΪUI����ʹ�õ�����ʹ�ø�����ʵ���Ӿ�Ч��
        cameraRT = this.gameObject.GetComponentInChildren<RawImage>();
        cameraRT.texture = observeTexture;


        //aimMask = this.transform.Find("Mask");

        //�������������λ����ɫ����
        //���ҵ���ɫ���
        playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
        //���ø�����
        observeCamera.transform.SetParent(playerCamera.transform, false);//�ڶ�������������ռ䱣�֣��������ø�����ʱ����ı������ڿռ��еĳ���Ч����������ѡ���ʱfalse


        //������
        //�ɼ��Դ���
        observeCamera.cullingMask = LayerMask.GetMask(loadSence);
        //�ɽ����Դ���
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer(loadSence), LayerMask.NameToLayer("Default"), true);

    }

    // Update is called once per frame
    void Update()
    {

        if (isObserve)
        {

            //���������Ź۲��������
            observeCamera.transform.LookAt(cameraRT.transform);

        }

    }


    private void OnDestroy()
    {
        observeTexture.Release();//�ֶ�������Դ

    }


    public void MovePlayer()
    {
        GameObject player = GameObject.Find("Player");

        player.GetComponent<CharacterController>().enabled = false;//���뿪���
        //��Ϊ�������Ƥ��ʹ��ʵ����ײ��ⷶΧ���ڽ��ҷ�Χ�������ǵ�����ɫλ�ú������ΪƤ����ײ������ԭλ��

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
            canvas.worldCamera = playerCamera;//ȷ����ȷ�Ĺ۲����

            RectTransform maskRectTransform = this.GetComponentInChildren<Mask>().rectTransform;
            maskRectTransform.DOSizeDelta(new(3000f, 3000f), 2.0f)
                .OnComplete(() => { StartCoroutine(Switch()); });


            //������ɼ�������
            IEnumerator Switch()
            {
                //�ɽ�����
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer(unloadSence), true);
                Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer(loadSence), false);

                //�ɼ���
                playerCamera.cullingMask = LayerMask.GetMask(loadSence, "Default", "UI");


                player.GetComponent<CharacterController>().enabled = true;
                player.GetComponent<PlayController>().CurrentScene = loadSence;
                maskRectTransform.sizeDelta = Vector2.zero;
                player.GetComponent<PlayController>().isSwitchScene = false;

                //ж�س���
                yield return SceneManager.UnloadSceneAsync(unloadSence);


            }
        }
    }


    
}
