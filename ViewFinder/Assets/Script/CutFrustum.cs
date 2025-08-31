using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

public class CutFrustum : MonoBehaviour
{

    public float customOffset = 0.1f;
    public Camera captureCamera;//��ȡ�����ӵ�
    public Transform filmSpacePoint;
    public float frustumAspect = 1;//��߱�
    private Vector3 capturePoint, leftUp, leftDown, rightUp, rightDown;//��׶��5������

    private GameObject left, right, top, bottom, frustum;//�Դ�MF��MC��MR
    private Plane leftPlane, rightPlane, topPlane, bottomPlane;//planeֻ��һ�����θ����api��û������ʲô��
    private MeshFilter leftPlaneMF, rightPlaneMF, topPlaneMF, bottomPlaneMF, frustumExistMF;//ÿ��GO��MF
    private MeshCollider leftPlaneMC, rightPlaneMC, topPlaneMC, bottomPlaneMC, frustumExistMC;//ÿ��GOMC


    private List<GameObject> leftCutList, rightCutList, topCutList, bottomCutList, frustumExistList;//������Ҫ���и�����


    PolaroidFilm activeFilm;//��Ƭ�ռ�



    //��ʼ��������GO������MF��MC�󶨵����ϣ�MC����Ϊ�����������ر���ײ��⣬MR�ر�
    void Start()
    {
        frustumAspect = captureCamera.aspect;//���ÿ�߱�
        capturePoint = captureCamera.transform.position;

        //�и����ü�������
        left = GameObject.CreatePrimitive(PrimitiveType.Plane);
        left.name = "leftZone";
        var leftCC = left.AddComponent<CollisionChecker>();
        leftCC.aim = this;
        leftCC.side = 0;
        leftPlaneMF = left.GetComponent<MeshFilter>();
        leftPlaneMC = left.GetComponent<MeshCollider>();
        leftPlaneMC.convex = true;
        leftPlaneMC.isTrigger = true;
        leftPlaneMC.enabled = false;
        left.GetComponent<MeshRenderer>().enabled = false;


        right = GameObject.CreatePrimitive(PrimitiveType.Plane);
        right.name = "rightZone";
        var rightCC = right.AddComponent<CollisionChecker>();
        rightCC.aim = this;
        rightCC.side = 1;
        rightPlaneMF = right.GetComponent<MeshFilter>();
        rightPlaneMC = right.GetComponent<MeshCollider>();
        rightPlaneMC.convex = true;
        rightPlaneMC.isTrigger = true;
        rightPlaneMC.enabled = false;
        right.GetComponent<MeshRenderer>().enabled = false;

        top = GameObject.CreatePrimitive(PrimitiveType.Plane);
        top.name = "topZone";
        var topCC = top.AddComponent<CollisionChecker>();
        topCC.aim = this;
        topCC.side = 2;
        topPlaneMF = top.GetComponent<MeshFilter>();
        topPlaneMC = top.GetComponent<MeshCollider>();
        topPlaneMC.convex = true;
        topPlaneMC.isTrigger = true;
        topPlaneMC.enabled = false;
        top.GetComponent<MeshRenderer>().enabled = false;

        bottom = GameObject.CreatePrimitive(PrimitiveType.Plane);
        bottom.name = "bottomZone";
        var bottomCC = bottom.AddComponent<CollisionChecker>();
        bottomCC.aim = this;
        bottomCC.side = 3;
        bottomPlaneMF = bottom.GetComponent<MeshFilter>();
        bottomPlaneMC = bottom.GetComponent<MeshCollider>();
        bottomPlaneMC.convex = true;
        bottomPlaneMC.isTrigger = true;
        bottomPlaneMC.enabled = false;
        bottom.GetComponent<MeshRenderer>().enabled = false;

        frustum = GameObject.CreatePrimitive(PrimitiveType.Plane);
        frustum.name = "frustumZone";
        var frustumCC = frustum.AddComponent<CollisionChecker>();
        frustumCC.aim = this;
        frustumCC.side = 4;
        frustumExistMF = frustum.GetComponent<MeshFilter>();
        frustumExistMC = frustum.GetComponent<MeshCollider>();
        frustumExistMC.convex = true;
        frustumExistMC.isTrigger = true;
        frustumExistMC.enabled = false;
        frustum.GetComponent<MeshRenderer>().enabled = false;



        //�и��б�׼��
        leftCutList = new List<GameObject>();
        rightCutList = new List<GameObject>();
        topCutList = new List<GameObject>();
        bottomCutList = new List<GameObject>();
        frustumExistList = new List<GameObject>();


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //�����и����õ��ĸ��棨���Ƿ�������ռ䣩����������ײ��⣬Э�̿����и�
    public void GenericFrustumAndCut(bool isPress)
    {
        
        //�����и�׶���5������(local�ռ�)
        float _helfHeight = (captureCamera.farClipPlane * Mathf.Tan(captureCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        float _helfWidth = _helfHeight * frustumAspect;

        capturePoint = captureCamera.transform.position;//׶�嶥��
        //׶�������ĸ�����
        leftUp = new(-_helfWidth, _helfHeight, captureCamera.farClipPlane);
        leftDown = new(-_helfWidth, -_helfHeight, captureCamera.farClipPlane);
        rightUp = new(_helfWidth, _helfHeight, captureCamera.farClipPlane);
        rightDown = new(_helfWidth, -_helfHeight, captureCamera.farClipPlane);


        //local�ռ�ת��Ϊworld�ռ�
        leftUp = captureCamera.transform.TransformPoint(leftUp);
        leftDown = captureCamera.transform.TransformPoint(leftDown);
        rightUp = captureCamera.transform.TransformPoint(rightUp);
        rightDown = captureCamera.transform.TransformPoint(rightDown);


        //����PlaneΪ�и������׼��
        leftPlane = new Plane(capturePoint, leftUp, leftDown);
        rightPlane = new Plane(capturePoint, rightDown, rightUp);
        topPlane = new Plane(capturePoint, rightUp, leftUp);
        bottomPlane = new Plane(capturePoint, leftDown, rightDown);



        //�������񣬲�����ΪGO����ʹ��mesh��set����Ҳ�ᵼ��ָ��sharedmesh��Ϊmesh�����ã�һЩ��������²��ᣩ��
        Vector3 leftOffset = -1 * leftPlane.normal * customOffset;
        leftPlaneMF.mesh = GenericCustomBoxCollider(capturePoint + leftOffset, leftDown + leftOffset, ((leftDown + leftUp) / 2) + leftOffset, leftUp + leftOffset,
                                               capturePoint, leftDown, (leftDown + leftUp) / 2, leftUp);

        Vector3 rightOffset = -1 * rightPlane.normal * customOffset;
        rightPlaneMF.mesh = GenericCustomBoxCollider(capturePoint + rightOffset, rightUp + rightOffset, ((rightDown + rightUp) / 2) + rightOffset, rightDown + rightOffset,
                                               capturePoint, rightUp, (rightDown + rightUp) / 2, rightDown);

        Vector3 topOffset = -1 * topPlane.normal * customOffset;
        topPlaneMF.mesh = GenericCustomBoxCollider(capturePoint + topOffset, leftUp + topOffset, ((rightUp + leftUp) / 2) + topOffset, rightUp + topOffset,
                                               capturePoint, leftUp, (rightUp + leftUp) / 2, rightUp);

        Vector3 bottomOffset = -1 * bottomPlane.normal * customOffset;
        bottomPlaneMF.mesh = GenericCustomBoxCollider(capturePoint + bottomOffset, rightDown + bottomOffset, ((rightDown + leftDown) / 2) + bottomOffset, leftDown + bottomOffset,
                                               capturePoint, rightDown, (rightDown + leftDown) / 2, leftDown);

        //��ʹ��triggerEntryʱҪ����һЩƫ�ƣ����������Ŵ�������������������
        Vector3 farCenter = capturePoint + captureCamera.transform.forward * captureCamera.farClipPlane;
        frustumExistMF.mesh = GenericCustomFrustumCollider(capturePoint + (captureCamera.transform.forward * 0.1f), leftUp + ((farCenter - leftUp).normalized * 5.0f), leftDown + ((farCenter - leftDown).normalized * 5.0f), rightUp + ((farCenter - rightUp).normalized * 5.0f), rightDown + ((farCenter - rightDown).normalized * 5.0f));




        //����GO��������ײ��
        leftPlaneMC.sharedMesh = leftPlaneMF.mesh;
        rightPlaneMC.sharedMesh = rightPlaneMF.mesh;
        topPlaneMC.sharedMesh = topPlaneMF.mesh;
        bottomPlaneMC.sharedMesh = bottomPlaneMF.mesh;
        frustumExistMC.sharedMesh = frustumExistMF.mesh;


        leftCutList.Clear();
        rightCutList.Clear();
        topCutList.Clear();
        bottomCutList.Clear();
        frustumExistList.Clear();

        //���ô�����ײ
        leftPlaneMC.enabled = true;
        rightPlaneMC.enabled = true;
        topPlaneMC.enabled = true;
        bottomPlaneMC.enabled = true;
        frustumExistMC.enabled = false;



        StartCoroutine(CutAimList(isPress));
    }




    //��ӵ�������У�����side������
    public void AddObjectToCut(GameObject aim, int side)
    {
        switch (side)
        {
            case 0:
                {
                    if(!leftCutList.Contains(aim))
                        leftCutList.Add(aim);
                    break;
                };
            case 1:
                {   if(!rightCutList.Contains(aim))
                        rightCutList.Add(aim);
                    break;
                };
            case 2:
                {
                    if(!topCutList.Contains(aim))
                        topCutList.Add(aim);
                    break;
                };
            case 3:
                {
                    if(!bottomCutList.Contains(aim))
                        bottomCutList.Add(aim);
                    break;
                };
            case 4:
                {
                    if (!frustumExistList.Contains(aim))
                        frustumExistList.Add(aim);
                    break;
                }

        }

    }


    //��ʼ�и�
    private IEnumerator CutAimList(bool isPress)
    {

        //����Э�̣���������һ���첽����ʵ�ֵȴ���֡��Ч������֤��ɼ��
        yield return null;
        yield return null;
        yield return null;


        //��ʱ�رռ�⣬����֮������Բ������и���ٲ�����ײ
        leftPlaneMC.enabled = false;
        rightPlaneMC.enabled = false;
        topPlaneMC.enabled = false;
        bottomPlaneMC.enabled = false;


        List<GameObject> completeObjects = new List<GameObject>();//�����ı��帱��
        List<GameObject> allChunks = new List<GameObject>();//�и�����в��������п�

        foreach (var aim in leftCutList)
        {

            


            if (isPress)
            {
                string initialName = aim.name;
                //GameObject original = GameObject.Instantiate(aim);
                GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                aim.name = aim.name + "/cut";
                original.name = initialName;
                original.SetActive(false);//�رո���������֮����׶���
                completeObjects.Add(original);
            }


            allChunks.Add(aim);


            CutPiece cutPiece = aim.GetComponent<CutPiece>();
            if(cutPiece == null)
            {
                aim.AddComponent<CutPiece>();
                cutPiece = aim.GetComponent<CutPiece>();
                cutPiece.Add(aim);
            }


            GameObject newChunk = Cutter.Cut(aim, leftPlane.ClosestPointOnPlane(Vector3.zero), leftPlane.normal);//aim���뷨�߷����෴

            newChunk.transform.SetParent(aim.transform.parent);
            cutPiece.Add(newChunk);
            allChunks.Add(newChunk);
        }


        foreach (var aim in rightCutList)
        {
            

            if (isPress)
            {
                if (aim.name.Split("/").Length == 1)//����Ѿ��������и������׼�������ı��帱��
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//�رո���
                    completeObjects.Add(original);
                    
                }
            }


            if (!allChunks.Contains(aim))
            {
                allChunks.Add(aim);
            }


            CutPiece cutPiece = aim.GetComponent<CutPiece>();

            if(cutPiece == null)
            {
                aim.AddComponent<CutPiece>();
                cutPiece = aim.GetComponent <CutPiece>();
                cutPiece.Add(aim);
            }


            int count = cutPiece.chunks.Count;

            for (int i = 0; i < count; i++)
            {
                GameObject newChunk = Cutter.Cut(cutPiece.chunks[i], rightPlane.ClosestPointOnPlane(Vector3.zero), rightPlane.normal);

                newChunk.transform.SetParent(cutPiece.transform.parent);
                cutPiece.Add(newChunk);
                allChunks.Add(newChunk);
            }

        }


        foreach (var aim in topCutList)
        {

            if (isPress)
            {
                if (aim.name.Split("/").Length == 1)//����Ѿ��������и������׼�������ı��帱��
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//�رո���
                    completeObjects.Add(original);
                }
            }


            if (!allChunks.Contains(aim))
            {
                allChunks.Add(aim);
            }


            CutPiece cutPiece = aim.GetComponent<CutPiece>();

            if (cutPiece == null)
            {
                aim.AddComponent<CutPiece>();
                cutPiece = aim.GetComponent<CutPiece>();
                cutPiece.Add(aim);
            }


            int count = cutPiece.chunks.Count;

            for (int i = 0; i < count; i++)
            {
                GameObject newChunk = Cutter.Cut(cutPiece.chunks[i], topPlane.ClosestPointOnPlane(Vector3.zero), topPlane.normal);

                newChunk.transform.SetParent(cutPiece.transform.parent);
                cutPiece.Add(newChunk);
                allChunks.Add(newChunk);
            }

        }


        foreach (var aim in bottomCutList)
        {
            if (isPress)
            {
                if (aim.name.Split("/").Length == 1)//����Ѿ��������и������׼�������ı��帱��
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//�رո���
                    completeObjects.Add(original);
                }
            }


            if (!allChunks.Contains(aim))
            {
                allChunks.Add(aim);
            }


            CutPiece cutPiece = aim.GetComponent<CutPiece>();

            if (cutPiece == null)
            {
                aim.AddComponent<CutPiece>();
                cutPiece = aim.GetComponent<CutPiece>();
                cutPiece.Add(aim);
            }


            int count = cutPiece.chunks.Count;

            for (int i = 0; i < count; i++)
            {
                GameObject newChunk = Cutter.Cut(cutPiece.chunks[i], bottomPlane.ClosestPointOnPlane(Vector3.zero), bottomPlane.normal);

                newChunk.transform.SetParent(cutPiece.transform.parent);
                cutPiece.Add(newChunk);
                allChunks.Add(newChunk);
            }

        }


        //�رմ�����ײ
        leftPlaneMC.enabled = false;
        rightPlaneMC.enabled = false;
        topPlaneMC.enabled = false;
        bottomPlaneMC.enabled = false;


        frustumExistMC.enabled = true;
        //�ȴ����
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        frustumExistMC.enabled = false;


        if (isPress)
        {
            //ʹ�����ʱ�����ɾ����������������
            activeFilm = new PolaroidFilm(frustumExistList, filmSpacePoint, this.GetComponent<PlayController>().currentSceneRef);

            foreach (var aim in completeObjects)
            {
                aim.SetActive(true);
            }

            foreach (var aim in allChunks)
            {
                Debug.Log($"ɾ���飺{aim.name}");
                Destroy(aim);
            }
        }
        else
        {
            //ʹ����Ƭʱ����׶�����ɾ������������ɾ��
            int count = allChunks.Count;
            for (int i = 0; i < count; i++)
            {
                Destroy(allChunks[i].GetComponent<CutPiece>());
                allChunks[i].name = allChunks[i].name.Split('/')[0] + $"({i})";
                allChunks[i].SetActive(true);
            }

            foreach (var aim in frustumExistList)
            {
                Debug.Log($"ʧȥ��׶�����壺{aim.name}");
                Destroy(aim);
            }


            foreach (var aim in completeObjects)
            {
                Debug.Log($"ʧȥ�����������壺{aim.name}");

                Destroy(aim);
            }

            activeFilm.Use();

        }

    }



    //��������
    public Mesh GenericCustomBoxCollider(params Vector3[] vertexs)
    {
        Mesh customBox = new Mesh();
        customBox.vertices = vertexs;
        customBox.triangles = new int[] {0, 1, 2,
                                         0, 2, 3,
                                         4, 6, 5,
                                         4, 7, 6,
                                         0, 5, 1,
                                         0, 4, 5,
                                         0, 3, 7,
                                         0, 7, 4,
                                         6, 7, 3,
                                         6, 3, 2,
                                         5, 6, 2,
                                         5, 2, 1
                                        };

        return customBox;
    }

    public Mesh GenericCustomFrustumCollider(params Vector3[] vertexs)//�У����ϣ����£����ϣ�����
    {
        Mesh customFrustum = new Mesh();
        customFrustum.vertices = vertexs;
        customFrustum.triangles = new int[] {0, 2, 1,
                                         0, 1, 3,
                                         0, 3, 4,
                                         0, 4, 2,
                                         1, 2, 3,
                                         4, 3, 2
                                        };

        return customFrustum;
    }


    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(capturePoint, leftUp);
        //Gizmos.DrawLine(capturePoint, leftDown);
        //Gizmos.DrawLine(capturePoint, rightUp);
        //Gizmos.DrawLine(capturePoint, rightDown);
        //Gizmos.DrawLine(leftDown, rightDown);
        //Gizmos.DrawLine(leftUp, rightUp);
        //Gizmos.DrawLine(leftUp, leftDown);
        //Gizmos.DrawLine(rightUp, rightDown);

    }


    private void OnDrawGizmos()
    {

        //Gizmos.color = Color.yellow;
        //Vector3 leftOffset = -1 * leftPlane.normal * customOffset;
        //Gizmos.DrawLineList(new Vector3[]
        //{
        //    capturePoint + leftOffset, leftDown + leftOffset,
        //    leftDown + leftOffset, ((leftDown + leftUp) / 2) + leftOffset,
        //    ((leftDown + leftUp) / 2) + leftOffset, leftUp + leftOffset,
        //    leftUp + leftOffset, capturePoint + leftOffset,

        //    capturePoint, leftDown,
        //    leftDown, (leftDown + leftUp) / 2,
        //    (leftDown + leftUp) / 2, leftUp,
        //    leftUp, capturePoint,

        //    capturePoint + leftOffset, capturePoint,
        //    leftDown + leftOffset, leftDown,
        //    ((leftDown + leftUp) / 2) + leftOffset, ((leftDown + leftUp) / 2),
        //    leftUp + leftOffset, leftUp
        //});


        //Gizmos.color = Color.gray;
        //Vector3 rightOffset = -1 * rightPlane.normal * customOffset;
        //Gizmos.DrawLineList(new Vector3[]
        //{
        //    capturePoint + rightOffset, rightUp + rightOffset,
        //    rightUp + rightOffset, ((rightDown + rightUp) / 2) + rightOffset,
        //    ((rightDown + rightUp) / 2) + rightOffset, rightDown + rightOffset,
        //    rightDown + rightOffset, capturePoint + rightOffset,

        //    capturePoint, rightUp,
        //    rightUp, (rightDown + rightUp) / 2,
        //    (rightDown + rightUp) / 2, rightDown,
        //    rightDown, capturePoint,

        //    capturePoint + rightOffset, capturePoint,
        //    rightUp + rightOffset, rightUp,
        //    ((rightDown + rightUp) / 2) + rightOffset, ((rightDown + rightUp) / 2),
        //    rightDown + rightOffset, rightDown
        //});


        //Gizmos.color = Color.blue;
        //Vector3 topOffset = -1 * topPlane.normal * customOffset;
        //Gizmos.DrawLineList(new Vector3[]
        //{
        //    capturePoint + topOffset, leftUp + topOffset,
        //    leftUp + topOffset, ((rightUp + leftUp) / 2) + topOffset,
        //    ((rightUp + leftUp) / 2) + topOffset, rightUp + topOffset,
        //    rightUp + topOffset, capturePoint + topOffset,

        //    capturePoint, leftUp,
        //    leftUp, ((rightUp + leftUp) / 2),
        //    ((rightUp + leftUp) / 2), rightUp,
        //    rightUp, capturePoint,

        //    capturePoint + topOffset, capturePoint,
        //    leftUp + topOffset, leftUp,
        //    ((rightUp + leftUp) / 2) + topOffset, ((rightUp + leftUp) / 2),
        //    rightUp + topOffset, rightUp
        //});


        //Gizmos.color = Color.green;
        //Vector3 bottomOffset = -1 * bottomPlane.normal * customOffset;
        //Gizmos.DrawLineList(new Vector3[]
        //{
        //    capturePoint + bottomOffset, rightDown + bottomOffset,
        //    rightDown + bottomOffset, ((rightDown + leftDown) / 2) + bottomOffset,
        //    ((rightDown + leftDown) / 2) + bottomOffset, leftDown + bottomOffset,
        //    leftDown + bottomOffset, capturePoint + bottomOffset,

        //    capturePoint, rightDown,
        //    rightDown, ((rightDown + leftDown) / 2),
        //    ((rightDown + leftDown) / 2), leftDown,
        //    leftDown, capturePoint,

        //    capturePoint + bottomOffset, capturePoint,
        //    rightDown + bottomOffset, rightDown,
        //    ((rightDown + leftDown) / 2) + bottomOffset, ((rightDown + leftDown) / 2),
        //    leftDown + bottomOffset, leftDown,
        //});


        Gizmos.color = Color.red;
        Gizmos.DrawLine(capturePoint + (captureCamera.transform.forward * 1.0f), leftUp + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(capturePoint + (captureCamera.transform.forward * 1.0f), leftDown + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(capturePoint + (captureCamera.transform.forward * 1.0f), rightUp + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(capturePoint + (captureCamera.transform.forward * 1.0f), rightDown + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(leftDown + (captureCamera.transform.forward * 1.0f), rightDown + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(leftUp + (captureCamera.transform.forward * 1.0f), rightUp + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(leftUp + (captureCamera.transform.forward * 1.0f), leftDown + (captureCamera.transform.forward * 1.0f));
        Gizmos.DrawLine(rightUp + (captureCamera.transform.forward * 1.0f), rightDown + (captureCamera.transform.forward * 1.0f));


    }


}

public class PolaroidFilm
{
    private List<GameObject> placeHolder;//��Ƭ�ռ��е�����
    private SceneReference currentSceneRef;

    public PolaroidFilm(List<GameObject> aims, Transform parent, SceneReference currentSceneRef)
    {

        placeHolder = new List<GameObject>();

        foreach (var aim in aims)
        {
            GameObject copy = GameObject.Instantiate(aim, aim.transform.parent);
            copy.transform.parent = parent;
            copy.SetActive(false);
            placeHolder.Add(copy);
        }

        this.currentSceneRef = currentSceneRef;
    }


    public void Use()
    {
        Scene aimScene = SceneManager.GetSceneByName(currentSceneRef.sceneName);

        
        GameObject aimLevel = aimScene.GetRootGameObjects()[1];//��Ҫ���ǵĳ����еڶ�����һ����Level
        

        foreach (var aim in placeHolder)
        {
            Debug.Log(currentSceneRef.sceneName);

            aim.transform.SetParent(aimLevel.transform, true);

            aim.layer = LayerMask.NameToLayer(currentSceneRef.sceneName);
            aim.SetActive(true);
        }

    }
}