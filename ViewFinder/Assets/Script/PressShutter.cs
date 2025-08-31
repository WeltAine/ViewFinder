using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//没有使用
public class PressShutter : StateMachineBehaviour//该接口用于对Animator做出更详细的控制，它主要由一系列回调函数构成，分别象征着一系列的生命周期等等，状态机的state可以使用该脚本
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
