using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//测试静态碰撞体，动态碰撞体和运动碰撞体
public class ColliderText : MonoBehaviour
{

    public Rigidbody rb;

    public bool isRbSleep = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRbSleep)
        {
            rb.Sleep();
        }
    }



}
