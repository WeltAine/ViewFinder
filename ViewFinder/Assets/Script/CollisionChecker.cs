using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//�и���׶��5��mesh���õĴ�������
public class CollisionChecker : MonoBehaviour
{

    public CutFrustum aim;
    public int side;



    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Cuttable")
        {//!!��������tag������ʲô�ǿ����и�ģ���layer���ж�ʲô�ǿɼ��ɽ�����
            Debug.Log($"{this.gameObject.name} is trigged�� aim: {other.name}");
            aim.AddObjectToCut(other.gameObject, side);
        }
    }

}
