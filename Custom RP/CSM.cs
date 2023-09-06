using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CSM {

    // 每一级cascade所占的深度比例
    public float[] splits = { 0.07f, 0.13f, 0.25f, 0.55f };

    // 主相机视锥体
    Vector3[] farCorners = new Vector3[4];
    Vector3[] nearCorners = new Vector3[4];

    // 每一级cascade的视锥体
    Vector3[] f0_near = new Vector3[4];
    Vector3[] f0_far = new Vector3[4];
    Vector3[] f1_near = new Vector3[4];
    Vector3[] f1_far = new Vector3[4];
    Vector3[] f2_near = new Vector3[4];
    Vector3[] f2_far = new Vector3[4];
    Vector3[] f3_near = new Vector3[4];
    Vector3[] f3_far = new Vector3[4];

    // 每一级cascade的包围盒
    Vector3[] box0 = new Vector3[8];
    Vector3[] box1 = new Vector3[8];
    Vector3[] box2 = new Vector3[8];
    Vector3[] box3 = new Vector3[8];

    // 每一级shadowmap的世界空间大小
    public float[] shadowMapWorldSizeX = new float[4];
    public float[] shadowMapWorldSizeY = new float[4];

    // 相机参数，用于在绘制shadow map时保存相机原先的状态
    struct MainCameraSettings {
        public Vector3 position;
        public Quaternion rotation;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
    };
    MainCameraSettings settings;


    // 计算各级cascade的包围盒
    public void Update(Camera mainCam, Vector3 lightDir) {
        
        // 获取主相机视锥体
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);

        // 视锥体顶点转世界坐标
        for (int i = 0; i < 4; i++) {
            farCorners[i] = mainCam.transform.TransformVector(farCorners[i]) + mainCam.transform.position;
            nearCorners[i] = mainCam.transform.TransformVector(nearCorners[i]) + mainCam.transform.position;
        }

        // 按照比例划分相机视锥体
        for (int i = 0; i < 4; i++) {
            Vector3 dir = farCorners[i] - nearCorners[i];

            f0_near[i] = nearCorners[i];
            f0_far[i] = f0_near[i] + dir * splits[0];

            f1_near[i] = f0_far[i];
            f1_far[i] = f1_near[i] + dir * splits[1];

            f2_near[i] = f1_far[i];
            f2_far[i] = f2_near[i] + dir * splits[2];

            f3_near[i] = f2_far[i];
            f3_far[i] = f3_near[i] + dir * splits[3];
        }

        // 计算包围盒
        box0 = LightSpaceAABB(f0_near, f0_far, lightDir, ref shadowMapWorldSizeX[0], ref shadowMapWorldSizeY[0]);
        box1 = LightSpaceAABB(f1_near, f1_far, lightDir, ref shadowMapWorldSizeX[1], ref shadowMapWorldSizeY[1]);
        box2 = LightSpaceAABB(f2_near, f2_far, lightDir, ref shadowMapWorldSizeX[2], ref shadowMapWorldSizeY[2]);
        box3 = LightSpaceAABB(f3_near, f3_far, lightDir, ref shadowMapWorldSizeX[3], ref shadowMapWorldSizeY[3]);
    }


    // 计算单个棱台的包围盒，与光源的坐标系对齐
    Vector3[] LightSpaceAABB(Vector3[] nearCorners, Vector3[] farCorners, Vector3 lightDir, ref float xSize, ref float ySize) {
        
        // 光源的V-1和V矩阵
        Matrix4x4 toShadowViewInv = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        Matrix4x4 toShadowView = toShadowViewInv.inverse;

        // 视锥体顶点从世界坐标系转到光源坐标系
        for (int i = 0; i < 4; i++) {
            farCorners[i] = matTransform(toShadowView, farCorners[i], 1.0f);
            nearCorners[i] = matTransform(toShadowView, nearCorners[i], 1.0f);
        }

        // 计算包围盒
        float[] x = new float[8];
        float[] y = new float[8];
        float[] z = new float[8];
        for (int i = 0; i < 4; i++) {
            x[i] = nearCorners[i].x; x[i + 4] = farCorners[i].x;
            y[i] = nearCorners[i].y; y[i + 4] = farCorners[i].y;
            z[i] = nearCorners[i].z; z[i + 4] = farCorners[i].z;
        }
        float xmin = Mathf.Min(x), xmax = Mathf.Max(x);
        float ymin = Mathf.Min(y), ymax = Mathf.Max(y);
        float zmin = Mathf.Min(z), zmax = Mathf.Max(z);

        // 计算世界坐标下的shadowmap大小
        xSize = xmax - xmin;
        ySize = ymax - ymin;

        // 包围盒顶点转回世界坐标
        Vector3[] points = {
            new Vector3(xmin, ymin, zmin),
            new Vector3(xmin, ymin, zmax),
            new Vector3(xmin, ymax, zmin),
            new Vector3(xmin, ymax, zmax),
            new Vector3(xmax, ymin, zmin),
            new Vector3(xmax, ymin, zmax),
            new Vector3(xmax, ymax, zmin),
            new Vector3(xmax, ymax, zmax)
        };
        for (int i = 0; i < 8; i++)
            points[i] = matTransform(toShadowViewInv, points[i], 1.0f);

        // 视锥体顶点转回世界坐标
        for (int i = 0; i < 4; i++) {
            farCorners[i] = matTransform(toShadowViewInv, farCorners[i], 1.0f);
            nearCorners[i] = matTransform(toShadowViewInv, nearCorners[i], 1.0f);
        }

        return points;
    }


    // 将相机参数调整为绘制shadow map的情况
    public void ConfigCameraToShadowSpace(ref Camera camera, Vector3 lightDir, int level, float distance) {
        // 选择第 level 级视锥划分
        var box = new Vector3[8];
        if (level == 0) box = box0; if (level == 1) box = box1;
        if (level == 2) box = box2; if (level == 3) box = box3;

        // 计算 Box 中点, 宽高比
        Vector3 center = (box[3] + box[4]) / 2;
        float w = Vector3.Magnitude(box[0] - box[4]);
        float h = Vector3.Magnitude(box[0] - box[2]);

        // 配置相机
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = center;
        camera.nearClipPlane = -distance;
        camera.farClipPlane = distance;
        camera.aspect = w / h;
        camera.orthographicSize = h * 0.5f;
    }


    // 保存相机参数, 更改为正交投影
    public void SaveMainCameraSettings(ref Camera camera) {
        settings.position = camera.transform.position;
        settings.rotation = camera.transform.rotation;
        settings.farClipPlane = camera.farClipPlane;
        settings.nearClipPlane = camera.nearClipPlane;
        settings.aspect = camera.aspect;
        camera.orthographic = true;
    }

    // 还原相机参数, 更改为透视投影
    public void RevertMainCameraSettings(ref Camera camera) {
        camera.transform.position = settings.position;
        camera.transform.rotation = settings.rotation;
        camera.farClipPlane = settings.farClipPlane;
        camera.nearClipPlane = settings.nearClipPlane;
        camera.aspect = settings.aspect;
        camera.orthographic = false;
    }

    Vector3 matTransform(Matrix4x4 m, Vector3 v, float w) {
        Vector4 v4 = new Vector4(v.x, v.y, v.z, w);
        v4 = m * v4;
        return new Vector3(v4.x, v4.y, v4.z);
    }


    public void DebugDraw() {
        DrawFrustum(nearCorners, farCorners, Color.white);
        DrawAABB(box0, Color.yellow);
        DrawAABB(box1, Color.magenta);
        DrawAABB(box2, Color.green);
        DrawAABB(box3, Color.cyan);
        DrawNearFarPlane(f0_near, Color.blue);
        DrawNearFarPlane(f1_near, Color.blue);
        DrawNearFarPlane(f2_near, Color.blue);
        DrawNearFarPlane(f3_near, Color.blue);
        DrawNearFarPlane(f3_far, Color.blue);
    }

    // 画相机视锥体
    void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color) {
        for (int i = 0; i < 4; i++)
            Debug.DrawLine(nearCorners[i], farCorners[i], color);

        Debug.DrawLine(farCorners[0], farCorners[1], color);
        Debug.DrawLine(farCorners[0], farCorners[3], color);
        Debug.DrawLine(farCorners[2], farCorners[1], color);
        Debug.DrawLine(farCorners[2], farCorners[3], color);
        Debug.DrawLine(nearCorners[0], nearCorners[1], color);
        Debug.DrawLine(nearCorners[0], nearCorners[3], color);
        Debug.DrawLine(nearCorners[2], nearCorners[1], color);
        Debug.DrawLine(nearCorners[2], nearCorners[3], color);
    }

    // 画各级cascade分割平面
    void DrawNearFarPlane(Vector3[] fn_near_far, Color color) {

        Debug.DrawLine(fn_near_far[0], fn_near_far[1], color);
        Debug.DrawLine(fn_near_far[0], fn_near_far[3], color);
        Debug.DrawLine(fn_near_far[2], fn_near_far[1], color);
        Debug.DrawLine(fn_near_far[2], fn_near_far[3], color);
    }

    // 画光源方向的 AABB 包围盒
    void DrawAABB(Vector3[] points, Color color) {
        // 画线
        Debug.DrawLine(points[0], points[1], color);
        Debug.DrawLine(points[0], points[2], color);
        Debug.DrawLine(points[0], points[4], color);

        Debug.DrawLine(points[6], points[2], color);
        Debug.DrawLine(points[6], points[7], color);
        Debug.DrawLine(points[6], points[4], color);

        Debug.DrawLine(points[5], points[1], color);
        Debug.DrawLine(points[5], points[7], color);
        Debug.DrawLine(points[5], points[4], color);

        Debug.DrawLine(points[3], points[1], color);
        Debug.DrawLine(points[3], points[2], color);
        Debug.DrawLine(points[3], points[7], color);
    }
}
