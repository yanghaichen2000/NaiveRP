using System;
using UnityEngine;

public class CaptureScreenshot : MonoBehaviour {
    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            string screenshotFileName = "C:\\Users\\23954\\Desktop\\screenshot\\unity_screenshot_" + 
                DateTime.Now.ToString("yyyyMMddHHmmss") + ".png"; // ͼ���ļ����������޸�
            ScreenCapture.CaptureScreenshot(screenshotFileName);
            Debug.Log("Screenshot captured: " + screenshotFileName);
        }
    }
}