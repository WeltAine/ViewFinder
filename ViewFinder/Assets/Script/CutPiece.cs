using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//һ������Ŀ��¼��
public class CutPiece : MonoBehaviour
{
    public List<GameObject> chunks = new List<GameObject>();

    public void Add(GameObject aim)
    {
        chunks.Add(aim);
    }

}
