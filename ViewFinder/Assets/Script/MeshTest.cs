using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//了解mesh和shedeMesh
public class MeshTest : MonoBehaviour
{
    Mesh sm1, sm2;
    // Start is called before the first frame update
    void Start()
    {

        Mesh tem_mesh = new Mesh();
        tem_mesh.Clear();
        tem_mesh.name = "tem_mesh";


        tem_mesh.vertices = new Vector3[] { new(10, 10, 0 ), new(0, 10, 0), new(0, 10, 10)};
        tem_mesh.triangles = new int[] { 0, 1, 2};

        GameObject.Find("Cube").GetComponent<MeshFilter>().mesh = tem_mesh;
        //tem_mesh = GameObject.Find("Cube").GetComponent<MeshFilter>().mesh;


        sm1 =  GameObject.Find("Cube").GetComponent<MeshFilter>().sharedMesh;

        sm2 =  GameObject.Find("Cube (1)").GetComponent<MeshFilter>().sharedMesh;
        Debug.Log($"same{sm1 == sm2}");//！！使用set，也会对sharedmesh造成影响
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
