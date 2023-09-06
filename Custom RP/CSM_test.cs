using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����ű�������ϲ���
[ExecuteAlways]
public class ShadowCameraDebug : MonoBehaviour {
    
    CSM csm;

    void Update() {
        Camera mainCam = Camera.main;

        // ��ȡ��Դ��Ϣ
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // ���� shadowmap
        if (csm == null) csm = new CSM();
        csm.Update(mainCam, lightDir);
        csm.DebugDraw();
    }
}