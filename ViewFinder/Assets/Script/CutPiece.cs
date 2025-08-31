using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//一个物体的块记录类
public class CutPiece : MonoBehaviour
{
    public List<GameObject> chunks = new List<GameObject>();

    public void Add(GameObject aim)
    {
        chunks.Add(aim);
    }

}
