using System.Collections.Generic;
using UnityEngine;

/*
As mentioned in the video, this is a custom implementation based on Kristin Lague's project. 
If you've downloaded this repo for the cutting part, I suggest starting with this repo: https://github.com/KristinLague/Mesh-Cutting
and this video: https://www.youtube.com/watch?v=1UsuZsaUUng&t=7s 
*/

public class Cutter : MonoBehaviour
{
    private static bool isBusy;
    private static Mesh originalMesh;

    public static GameObject Cut(GameObject originalGameObject, Vector3 contactPoint, Vector3 cutNormal)
    {
        if(isBusy)
            return null;

        isBusy = true;

        Plane cutPlane = new Plane(originalGameObject.transform.InverseTransformDirection(-cutNormal), originalGameObject.transform.InverseTransformPoint(contactPoint));
        originalMesh = originalGameObject.GetComponent<MeshFilter>().mesh;

        if (originalMesh == null)
        {
            Debug.LogError("Need mesh to cut");
            return null;
        }

        List<Vector3> addedVertices = new List<Vector3>();
        GeneratedMesh leftMesh = new GeneratedMesh();
        GeneratedMesh rightMesh = new GeneratedMesh();
        
        //只切割网格
        SeparateMeshes(leftMesh,rightMesh,cutPlane,addedVertices);

        //缝合切口
        FillCut(addedVertices, cutPlane, leftMesh, rightMesh);

        Mesh finishedLeftMesh = leftMesh.GetGeneratedMesh();
        Mesh finishedRightMesh = rightMesh.GetGeneratedMesh();

        //删除原有碰撞
        var originalCols = originalGameObject.GetComponents<Collider>();
        foreach (var col in originalCols)
            Destroy(col);

        //设置网格并添加新碰撞
        originalGameObject.GetComponent<MeshFilter>().mesh = finishedLeftMesh;
        var collider = originalGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = finishedLeftMesh;
        collider.convex = true;
        //collider.convex = false;
        
        var mat = originalGameObject.GetComponent<MeshRenderer>().material;

        Material[] mats = new Material[finishedLeftMesh.subMeshCount];
        for (int i = 0; i < finishedLeftMesh.subMeshCount; i++)
		{
            mats[i] = mat;
            //mats[i] = originalGameObject.GetComponent<MeshRenderer>().materials[i];
        }
        originalGameObject.GetComponent<MeshRenderer>().materials = mats;

        //切割产生的另一个部分，它的生命周期归场景所管控
        GameObject right = new GameObject();
        right.transform.position = originalGameObject.transform.position;
        right.transform.rotation = originalGameObject.transform.rotation;
        right.transform.localScale = originalGameObject.transform.localScale;
        right.AddComponent<MeshRenderer>();
        
        mats = new Material[finishedRightMesh.subMeshCount];
        for (int i = 0; i < finishedRightMesh.subMeshCount; i++)
		{
            mats[i] = mat;
            //mats[i] = originalGameObject.GetComponent<MeshRenderer>().materials[i];
        }
        right.GetComponent<MeshRenderer>().materials = mats;
        right.AddComponent<MeshFilter>().mesh = finishedRightMesh;
        
        right.AddComponent<MeshCollider>().sharedMesh = finishedRightMesh;
        var cols = right.GetComponents<MeshCollider>();
        foreach (var col in cols)
        {
            col.convex = true;
            //col.convex = false;
        }
        
        var rightRigidbody = right.AddComponent<Rigidbody>();
        rightRigidbody.isKinematic = true;

        right.name = originalGameObject.name + Random.Range(0, 9999);

        isBusy = false;

        //right.layer = LayerMask.NameToLayer("Cuttable");
        //这里一点点小魔改
        right.layer = originalGameObject.layer;
        right.tag = originalGameObject.tag;

        return right;
    }




    /// <summary>
    /// Iterates over all the triangles of all the submeshes of the original mesh to separate the left
    /// and right side of the plane into individual meshes.
    /// 逐子网格，逐三角面做分割处理
    /// </summary>
    /// <param name="leftMesh"></param>
    /// <param name="rightMesh"></param>
    /// <param name="plane"></param>
    /// <param name="addedVertices"></param>
    private static void SeparateMeshes(GeneratedMesh leftMesh,GeneratedMesh rightMesh, Plane plane, List<Vector3> addedVertices)
    {
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            var subMeshIndices = originalMesh.GetTriangles(i);

            //We are now going through the submesh indices as triangles to determine on what side of the mesh they are.
            for (int j = 0; j < subMeshIndices.Length; j+=3)
            {
                var triangleIndexA = subMeshIndices[j];
                var triangleIndexB = subMeshIndices[j + 1];
                var triangleIndexC = subMeshIndices[j + 2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA,triangleIndexB,triangleIndexC,i);

                //We are now using the plane.getside function to see on which side of the cut our trianle is situated 
                //or if it might be cut through
                bool triangleALeftSide = plane.GetSide(originalMesh.vertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexC]);

                switch (triangleALeftSide)
                {
                    //All three vertices are on the left side of the plane, so they need to be added to the left
                    //mesh
                    case true when triangleBLeftSide && triangleCLeftSide:
                        leftMesh.AddTriangle(currentTriangle);
                        break;
                    //All three vertices are on the right side of the mesh.
                    case false when !triangleBLeftSide && !triangleCLeftSide:
                        rightMesh.AddTriangle(currentTriangle);
                        break;
                    default:
                        CutTriangle(plane,currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide,leftMesh,rightMesh,addedVertices);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Returns the tree vertices of a triangle as one MeshTriangle to keep code more readable
    /// </summary>
    /// <param name="_triangleIndexA"></param>
    /// <param name="_triangleIndexB"></param>
    /// <param name="_triangleIndexC"></param>
    /// <param name="_submeshIndex"></param>
    /// <returns></returns>
    private static MeshTriangle GetTriangle(int _triangleIndexA, int _triangleIndexB, int _triangleIndexC, int _submeshIndex)
    {
        //Adding the Vertices at the triangleIndex
        Vector3[] verticesToAdd = {
            originalMesh.vertices[_triangleIndexA],
            originalMesh.vertices[_triangleIndexB],
            originalMesh.vertices[_triangleIndexC]
        };

        //Adding the normals at the triangle index
        Vector3[] normalsToAdd = {
            originalMesh.normals[_triangleIndexA],
            originalMesh.normals[_triangleIndexB],
            originalMesh.normals[_triangleIndexC]
        };

        //adding the uvs at the triangleIndex
        Vector2[] uvsToAdd = {
            originalMesh.uv[_triangleIndexA],
            originalMesh.uv[_triangleIndexB],
            originalMesh.uv[_triangleIndexC]
        };

        return new MeshTriangle(verticesToAdd, normalsToAdd, uvsToAdd, _submeshIndex);
    }

    /// <summary>
    /// Cuts a triangle that exists between both sides of the cut apart adding additional vertices
    /// where needed to create intact triangles on both sides.
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="triangle"></param>
    /// <param name="triangleALeftSide"></param>
    /// <param name="triangleBLeftSide"></param>
    /// <param name="triangleCLeftSide"></param>
    /// <param name="leftMesh"></param>
    /// <param name="rightMesh"></param>
    /// <param name="addedVertices"></param>
    private static void CutTriangle(Plane plane,MeshTriangle triangle, bool triangleALeftSide, bool triangleBLeftSide, bool triangleCLeftSide,
    GeneratedMesh leftMesh, GeneratedMesh rightMesh, List<Vector3> addedVertices)
    {
        //走到这个函数，说明三角的三个顶点在平面两侧都存在


        List<bool> leftSide = new List<bool>();
        leftSide.Add(triangleALeftSide);
        leftSide.Add(triangleBLeftSide);
        leftSide.Add(triangleCLeftSide);

        //对三角的顶点进行拆分，分到左侧和右侧的顶点都不可能超过2
        MeshTriangle leftMeshTriangle = new MeshTriangle(new Vector3[2],new Vector3[2],new Vector2[2],triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], triangle.SubmeshIndex);

        bool left = false;
        bool right = false;

        for (int i = 0; i < 3; i++)
        {
            if(leftSide[i])
            {
                if (!left)
                {
                    //第一次装填
                    left = true;

                    leftMeshTriangle.Vertices[0] = triangle.Vertices[i];
                    leftMeshTriangle.Vertices[1] = leftMeshTriangle.Vertices[0];

                    leftMeshTriangle.UVs[0] = triangle.UVs[i];
                    leftMeshTriangle.UVs[1] = leftMeshTriangle.UVs[0];

                    leftMeshTriangle.Normals[0] = triangle.Normals[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Normals[0];
                }
                else
                {
                    //第二次装填
                    leftMeshTriangle.Vertices[1] = triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = triangle.Normals[i];
                    leftMeshTriangle.UVs[1] = triangle.UVs[i];
                }
            }
            else
            {
                if(!right)
                {
                    right = true;

                    rightMeshTriangle.Vertices[0] = triangle.Vertices[i];
                    rightMeshTriangle.Vertices[1] = rightMeshTriangle.Vertices[0];

                    rightMeshTriangle.UVs[0] = triangle.UVs[i];
                    rightMeshTriangle.UVs[1] = rightMeshTriangle.UVs[0];

                    rightMeshTriangle.Normals[0] = triangle.Normals[i];
                    rightMeshTriangle.Normals[1] = rightMeshTriangle.Normals[0];

                }
                else
                {
                    rightMeshTriangle.Vertices[1] = triangle.Vertices[i];
                    rightMeshTriangle.Normals[1] = triangle.Normals[i];
                    rightMeshTriangle.UVs[1] = triangle.UVs[i];
                }
            }
        }

        float normalizedDistance;//比例
        float distance;
        plane.Raycast(new Ray(leftMeshTriangle.Vertices[0], (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).normalized), out distance);

        //断点坐标
        normalizedDistance = distance / (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).magnitude;
        Vector3 vertLeft = Vector3.Lerp(leftMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[0], normalizedDistance);
        addedVertices.Add(vertLeft);
        //断点法线和UV
        Vector3 normalLeft = Vector3.Lerp(leftMeshTriangle.Normals[0], rightMeshTriangle.Normals[0], normalizedDistance);
        Vector2 uvLeft = Vector2.Lerp(leftMeshTriangle.UVs[0], rightMeshTriangle.UVs[0], normalizedDistance);
        
        //同上
        plane.Raycast(new Ray(leftMeshTriangle.Vertices[1], (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).normalized), out distance);

        normalizedDistance = distance / (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).magnitude;
        Vector3 vertRight = Vector3.Lerp(leftMeshTriangle.Vertices[1], rightMeshTriangle.Vertices[1], normalizedDistance);
        addedVertices.Add(vertRight);

        Vector3 normalRight = Vector3.Lerp(leftMeshTriangle.Normals[1], rightMeshTriangle.Normals[1], normalizedDistance);
        Vector2 uvRight = Vector2.Lerp(leftMeshTriangle.UVs[1], rightMeshTriangle.UVs[1], normalizedDistance);


        //之后这里一共做了四次处理
        //先尝试将左侧拆为两个三角，并生成网格三角，添加到对应网格中
        //再尝试将右侧拆为两个三角，并生成网格三角，添加到对应网格中
        //过程中会基于法线对三角进行翻转，该检测公式的另一个左右就是排除非但三角面，因为非三角面运行该公式会报错
        //同时四个中也必然有一个不是三角形

        //TESTING OUR FIRST TRIANGLE
        MeshTriangle currentTriangle;
        Vector3[] updatedVertices = { leftMeshTriangle.Vertices[0], vertLeft, vertRight };
        Vector3[] updatedNormals = { leftMeshTriangle.Normals[0], normalLeft, normalRight };
        Vector2[] updatedUVs = { leftMeshTriangle.UVs[0], uvLeft, uvRight };
        
       currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);

        //If our vertices ant the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        //SECOND TRIANGLE 
        updatedVertices = new Vector3[] { leftMeshTriangle.Vertices[0], leftMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { leftMeshTriangle.Normals[0], leftMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { leftMeshTriangle.UVs[0],leftMeshTriangle.UVs[1], uvRight };


        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        //THIRD TRIANGLE 
        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], vertLeft, vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], normalLeft, normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }

        //FOURTH TRIANGLE 
        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], rightMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],rightMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        //If our vertices arent the same
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }
    }

    private static void FlipTriangel(MeshTriangle _triangle)
    {
        Vector3 temp = _triangle.Vertices[2];
        _triangle.Vertices[2] = _triangle.Vertices[0];
        _triangle.Vertices[0] = temp;

        temp = _triangle.Normals[2];
		_triangle.Normals[2] = _triangle.Normals[0];
		_triangle.Normals[0] = temp;

		(_triangle.UVs[2], _triangle.UVs[0]) = (_triangle.UVs[0], _triangle.UVs[2]);
    }

    public static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> polygon = new List<Vector3>();

        for (int i = 0; i < _addedVertices.Count - 1; i++)
        {
            if(!vertices.Contains(_addedVertices[i]))
            {
                polygon.Clear();
                polygon.Add(_addedVertices[i]);
                polygon.Add(_addedVertices[i + 1]);

                vertices.Add(_addedVertices[i]);
                vertices.Add(_addedVertices[i + 1]);

                EvaluatePairs(_addedVertices, vertices, polygon);
                Fill(polygon, _plane, _leftMesh, _rightMesh);
            }
        }
    }

    public static void EvaluatePairs(List<Vector3> _addedVertices,List<Vector3> _vertices, List<Vector3> _polygone)
    {
        bool isDone = false;
        while(!isDone)
        {
            isDone = true;//假定已经所有的断点已经串起来

            for (int i = 0; i < _addedVertices.Count; i+=2)
            {
                //第一个条件是为了仙界，第二个条件是为了确保这是一个新链
                //仔细解释一下addedVertices在填充时一直是一对一对加的，且它们无降重，你可以把它们想象成车轮的链条，添加时的没一个对都是一个关节零件
                //遗憾的是，addedVertices还不是链条，只是一堆散乱的零件，我们要将其串起来（也就是将切面的那个伤口缝起来）
                //所以接下来就是每次找一个几何上可以拼接上的下一个关机零件，就这么一直串下去（看完后你也会意识到这里可以递归，而不是while，不过没什么大的区别）
                if (_addedVertices[i] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i + 1]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i + 1]);
                    _vertices.Add(_addedVertices[i + 1]);
                } 
                else if (_addedVertices[i + 1] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i]);
                    _vertices.Add(_addedVertices[i]);
                }
            }
        }
    }

    private static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        //Firstly we need the center we do this by adding up all the vertices and then calculating the average
        Vector3 centerPosition = Vector3.zero;
        for (int i = 0; i < _vertices.Count; i++)
        {
            centerPosition += _vertices[i];
        }
        centerPosition /= _vertices.Count;

        //We now need an Upward Axis we use the plane we cut the mesh with for that
        //这是在干嘛？
        Vector3 up = new Vector3()
        {
            x = _plane.normal.x,
            y = _plane.normal.y,
            z = _plane.normal.z
        };

        Vector3 left = Vector3.Cross(_plane.normal, up);

        Vector3 displacement = Vector3.zero;
        Vector2 uv1 = Vector2.zero;
        Vector2 uv2 = Vector2.zero;


        //两个断点和一个中电构成一个三角形
        for (int i = 0; i < _vertices.Count; i++)
        {
            //这里是假定中心的uv是0.5， 0.5，然后。。。我觉得这里算错了
            displacement = _vertices[i] - centerPosition;
            uv1 = new Vector2()
            {
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            displacement = _vertices[(i + 1) % _vertices.Count] - centerPosition;
            uv2 = new Vector2()
            { 
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };



            Vector3[] vertices = {_vertices[i], _vertices[(i+1) % _vertices.Count], centerPosition};
			Vector3[] normals = {-_plane.normal, -_plane.normal, -_plane.normal};
			Vector2[] uvs   = {uv1, uv2, new(0.5f, 0.5f)};

            MeshTriangle currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _leftMesh.AddTriangle(currentTriangle);



            normals = new[] { _plane.normal, _plane.normal, _plane.normal };//调整法线
            currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _rightMesh.AddTriangle(currentTriangle);
        
        } 
    }
}
