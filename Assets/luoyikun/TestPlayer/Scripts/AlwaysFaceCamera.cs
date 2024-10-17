using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlwaysFaceCamera : MonoBehaviour
{
    public Camera cameraToLookAt;

    private void Start()
    {
        if (cameraToLookAt == null)
        {
            cameraToLookAt = Camera.main;
        }
    }
    void Update()
    {
        // 若cameraToLookAt为空，则自动选择主摄像机
        if (cameraToLookAt == null)
            cameraToLookAt = Camera.main;


        //使用  Vector3.ProjectOnPlane （投影向量，投影平面法向量）用于计算某个向量在某个平面上的投影向量  
        //Vector3 lookPoint = Vector3.ProjectOnPlane(this.transform.position - cameraToLookAt.transform.position, cameraToLookAt.transform.forward);
        //this.transform.LookAt((cameraToLookAt.transform.position + lookPoint));
        //transform.rotation = Quaternion.LookRotation(transform.position - cameraToLookAt.transform.position);
        this.transform.forward = cameraToLookAt.transform.forward;
    }
}
