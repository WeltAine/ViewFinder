using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

public class CutFrustum : MonoBehaviour
{

    public float customOffset = 0.1f;
    public Camera captureCamera;//截取所用视点
    public Transform filmSpacePoint;
    public float frustumAspect = 1;//宽高比
    private Vector3 capturePoint, leftUp, leftDown, rightUp, rightDown;//视锥的5个顶点

    private GameObject left, right, top, bottom, frustum;//自带MF，MC，MR
    private Plane leftPlane, rightPlane, topPlane, bottomPlane;//plane只是一个几何概念的api，没有网格什么的
    private MeshFilter leftPlaneMF, rightPlaneMF, topPlaneMF, bottomPlaneMF, frustumExistMF;//每个GO的MF
    private MeshCollider leftPlaneMC, rightPlaneMC, topPlaneMC, bottomPlaneMC, frustumExistMC;//每个GOMC


    private List<GameObject> leftCutList, rightCutList, topCutList, bottomCutList, frustumExistList;//储存需要被切割物体


    PolaroidFilm activeFilm;//胶片空间



    //初始化，生成GO，并将MF，MC绑定到其上；MC设置为触发器，并关闭碰撞检测，MR关闭
    void Start()
    {
        frustumAspect = captureCamera.aspect;//设置宽高比
        capturePoint = captureCamera.transform.position;

        //切割所用几何设置
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



        //切割列表准备
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

    //生成切割所用的四个面（它们符合世界空间），并开启碰撞检测，协程开启切割
    public void GenericFrustumAndCut(bool isPress)
    {
        
        //生成切割锥体的5个顶点(local空间)
        float _helfHeight = (captureCamera.farClipPlane * Mathf.Tan(captureCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        float _helfWidth = _helfHeight * frustumAspect;

        capturePoint = captureCamera.transform.position;//锥体顶点
        //锥体底面的四个顶点
        leftUp = new(-_helfWidth, _helfHeight, captureCamera.farClipPlane);
        leftDown = new(-_helfWidth, -_helfHeight, captureCamera.farClipPlane);
        rightUp = new(_helfWidth, _helfHeight, captureCamera.farClipPlane);
        rightDown = new(_helfWidth, -_helfHeight, captureCamera.farClipPlane);


        //local空间转换为world空间
        leftUp = captureCamera.transform.TransformPoint(leftUp);
        leftDown = captureCamera.transform.TransformPoint(leftDown);
        rightUp = captureCamera.transform.TransformPoint(rightUp);
        rightDown = captureCamera.transform.TransformPoint(rightDown);


        //生成Plane为切割触发区做准备
        leftPlane = new Plane(capturePoint, leftUp, leftDown);
        rightPlane = new Plane(capturePoint, rightDown, rightUp);
        topPlane = new Plane(capturePoint, rightUp, leftUp);
        bottomPlane = new Plane(capturePoint, leftDown, rightDown);



        //创建网格，并设置为GO网格（使用mesh的set属性也会导致指向sharedmesh成为mesh的引用（一些特殊情况下不会））
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

        //当使用triggerEntry时要带上一些偏移，以免贴合着触发区域的物体产生触发
        Vector3 farCenter = capturePoint + captureCamera.transform.forward * captureCamera.farClipPlane;
        frustumExistMF.mesh = GenericCustomFrustumCollider(capturePoint + (captureCamera.transform.forward * 0.1f), leftUp + ((farCenter - leftUp).normalized * 5.0f), leftDown + ((farCenter - leftDown).normalized * 5.0f), rightUp + ((farCenter - rightUp).normalized * 5.0f), rightDown + ((farCenter - rightDown).normalized * 5.0f));




        //设置GO的网格碰撞体
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

        //启用触发碰撞
        leftPlaneMC.enabled = true;
        rightPlaneMC.enabled = true;
        topPlaneMC.enabled = true;
        bottomPlaneMC.enabled = true;
        frustumExistMC.enabled = false;



        StartCoroutine(CutAimList(isPress));
    }




    //添加到处理队列（基于side参数）
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


    //开始切割
    private IEnumerator CutAimList(bool isPress)
    {

        //利用协程（本质上是一种异步）来实现等待三帧的效果，保证完成检测
        yield return null;
        yield return null;
        yield return null;


        //及时关闭检测，避免之后继续对产生的切割块再产生碰撞
        leftPlaneMC.enabled = false;
        rightPlaneMC.enabled = false;
        topPlaneMC.enabled = false;
        bottomPlaneMC.enabled = false;


        List<GameObject> completeObjects = new List<GameObject>();//完整的本体副本
        List<GameObject> allChunks = new List<GameObject>();//切割过程中产生的所有块

        foreach (var aim in leftCutList)
        {

            


            if (isPress)
            {
                string initialName = aim.name;
                //GameObject original = GameObject.Instantiate(aim);
                GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                aim.name = aim.name + "/cut";
                original.name = initialName;
                original.SetActive(false);//关闭副本，避免之后被视锥检测
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


            GameObject newChunk = Cutter.Cut(aim, leftPlane.ClosestPointOnPlane(Vector3.zero), leftPlane.normal);//aim会与法线方向相反

            newChunk.transform.SetParent(aim.transform.parent);
            cutPiece.Add(newChunk);
            allChunks.Add(newChunk);
        }


        foreach (var aim in rightCutList)
        {
            

            if (isPress)
            {
                if (aim.name.Split("/").Length == 1)//如果已经经历过切割，则无需准备完整的本体副本
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//关闭副本
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
                if (aim.name.Split("/").Length == 1)//如果已经经历过切割，则无需准备完整的本体副本
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//关闭副本
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
                if (aim.name.Split("/").Length == 1)//如果已经经历过切割，则无需准备完整的本体副本
                {
                    string initialName = aim.name;
                    //GameObject original = GameObject.Instantiate(aim);
                    GameObject original = GameObject.Instantiate(aim, aim.transform.parent);
                    aim.name = aim.name + "/cut";
                    original.name = initialName;
                    original.SetActive(false);//关闭副本
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


        //关闭触发碰撞
        leftPlaneMC.enabled = false;
        rightPlaneMC.enabled = false;
        topPlaneMC.enabled = false;
        bottomPlaneMC.enabled = false;


        frustumExistMC.enabled = true;
        //等待检测
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        frustumExistMC.enabled = false;


        if (isPress)
        {
            //使用相机时，碎块删除，完整副本保留
            activeFilm = new PolaroidFilm(frustumExistList, filmSpacePoint, this.GetComponent<PlayController>().currentSceneRef);

            foreach (var aim in completeObjects)
            {
                aim.SetActive(true);
            }

            foreach (var aim in allChunks)
            {
                Debug.Log($"删除块：{aim.name}");
                Destroy(aim);
            }
        }
        else
        {
            //使用照片时，视锥内碎块删除，完整副本删除
            int count = allChunks.Count;
            for (int i = 0; i < count; i++)
            {
                Destroy(allChunks[i].GetComponent<CutPiece>());
                allChunks[i].name = allChunks[i].name.Split('/')[0] + $"({i})";
                allChunks[i].SetActive(true);
            }

            foreach (var aim in frustumExistList)
            {
                Debug.Log($"失去视锥内物体：{aim.name}");
                Destroy(aim);
            }


            foreach (var aim in completeObjects)
            {
                Debug.Log($"失去完整副本物体：{aim.name}");

                Destroy(aim);
            }

            activeFilm.Use();

        }

    }



    //生成网格
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

    public Mesh GenericCustomFrustumCollider(params Vector3[] vertexs)//中，左上，左下，右上，右下
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
    private List<GameObject> placeHolder;//胶片空间中的物体
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

        GameObject aimLevel = aimScene.GetRootGameObjects()[1];//!!!需要我们的场景中第二个根一定是Level
        
        foreach (var aim in placeHolder)
        {
            Debug.Log(currentSceneRef.sceneName);

            aim.transform.SetParent(aimLevel.transform, true);

            aim.layer = LayerMask.NameToLayer(currentSceneRef.sceneName);
            aim.SetActive(true);
        }
    }
}