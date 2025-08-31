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
        //�첽���س�ʼ����Level0���ر������ײ
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
        //���س���
        yield return SceneManager.LoadSceneAsync("Level0", LoadSceneMode.Additive);

        //���ý�ɫ����ɼ��ԣ��Լ���ײ
        playCamera.cullingMask = LayerMask.GetMask("Default", "Level0", "UI");
        


    }
}
