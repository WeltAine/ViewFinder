using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//切割视锥中5各mesh所用的触发函数
public class CollisionChecker : MonoBehaviour
{

    public CutFrustum aim;
    public int side;



    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Cuttable")
        {//!!！我们以tag来区分什么是可以切割的，用layer来判定什么是可见可交互的
            Debug.Log($"{this.gameObject.name} is trigged， aim: {other.name}");
            aim.AddObjectToCut(other.gameObject, side);
        }
    }

}
