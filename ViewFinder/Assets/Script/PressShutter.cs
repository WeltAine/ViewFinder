using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//û��ʹ��
public class PressShutter : StateMachineBehaviour//�ýӿ����ڶ�Animator��������ϸ�Ŀ��ƣ�����Ҫ��һϵ�лص��������ɣ��ֱ�������һϵ�е��������ڵȵȣ�״̬����state����ʹ�øýű�
{

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //animator.GetComponent<Polaroid>().UseFilmClone();
        //Debug.Log("!!");

        //animator.GetComponent<Polaroid>().overridePosition = true;


        animator.Update(0);
        animator.GetComponent<Polaroid>().Film_Switch();
        animator.SetLayerWeight(layerIndex, 0);
        Debug.Log("!!");


    }

}
