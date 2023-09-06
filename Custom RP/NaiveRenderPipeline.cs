using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using static UnityEditor.Timeline.TimelinePlaybackControls;
using System.Drawing;
using System;

public class NaiveRenderPipeline : RenderPipeline {

    // 低差异序列，范围[0, 1)
    float[] hammersleySequenceX = new float[16];
    float[] hammersleySequenceY = new float[16];
    float[] haltonSequenceX = new float[16];
    float[] haltonSequenceY = new float[16];

    // temporary depth RT
    static int tmpDepthRTID = Shader.PropertyToID("_tmpDepthRT");

    // full-screen mesh for blit
    Mesh blitMesh;

    // gbuffer
    RenderTexture gdepth;                                               
    RenderTexture[] gbuffers = new RenderTexture[4];                    
    RenderTargetIdentifier gdepthID;
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4];

    // IBL
    public Cubemap specularIBL;
    public Cubemap diffuseIBL;
    public Texture brdfLut;
    public float IBLIntensity;

    // shadow
    public int shadowMapResolution = 1024;
    CSM csm;
    RenderTexture[] shadowTextures = new RenderTexture[4];
    public float PCSSKernelRadiusCoefficient;
    public float PCSSLightSize;
    public float PCFDepthBias;

    // light pass
    RenderTexture sceneColor;

    // TAA
    public bool enableTAA;
    int TAASampleIndex;
    static int TAAOutputID = Shader.PropertyToID("_TAAOutput");
    RenderTexture historyFrameBuffer;
    public float TAABlendWeight;
    Matrix4x4 previousViewMatrix;
    Matrix4x4 previousProjectionMatrix;

    // SSAO
    public float SSAOSampleRadius;
    public float SSAOStrength;
    public float SSAOSampleWeightVariability;
    public float SSAODeltaDepthFade;

    // GTAO
    public float GTAODistanceFade;
    public float GTAOSampleRadius;
    public float GTAOSpacialFilterSize;
    public float GTAOSpacialFilterVariance;
    public float GTAOStrength;
    static int GTAOResolvedID = Shader.PropertyToID("_GTAOResolved");
    static int GTAOVerticalFilteredID = Shader.PropertyToID("_GTAOVerticalFiltered");
    static int GTAOSpacialFilteredID = Shader.PropertyToID("_GTAOSpacialFiltered");

    // SSR
    public float SSROriginPosDepthBias;
    public float SSRObjectThickness;
    public float SSRSpacialFilterSize;
    public float SSRColorSpacialFilterVariance;
    public float SSROccupancySpacialFilterVariance;
    public bool SSRTemporalFilter;
    public bool SSRSampleH;
    static int SSRDataID = Shader.PropertyToID("_SSRData");
    static int SSRHID = Shader.PropertyToID("_SSRH");
    static int SSRPDFID = Shader.PropertyToID("_SSRPDF");
    static int SSRResolvedRadianceID = Shader.PropertyToID("_SSRResolvedRadiance");
    static int SSRFilteredRadianceID = Shader.PropertyToID("_SSRFilteredRadiance");
    static int SSRVerticalFilteredRadianceID = Shader.PropertyToID("_SSRVerticalFilteredRadiance");
    static int SSRTemporalFilteredRadianceID = Shader.PropertyToID("_SSRTemporalFilteredRadiance");
    RenderTexture SSRHistory;

    // Bloom
    public bool enableBloom;
    public float bloomIntensity;
    public float bloomVariance;
    static int BloomInitialSceneColorID = Shader.PropertyToID("_BloomInitialSceneColor");
    static int BloomSelectedSceneColor = Shader.PropertyToID("_BloomSelectedSceneColor");
    static int[] BloomDownSampleRTID = new int[7];
    static int[] BloomUpSampleRTID = new int[6];
    public bool bloomEnableComputeShader;
    public ComputeShader bloomComputeDownSample;

    // Debug
    public ComputeShader computeShader1;
    public ComputeShader computeShader2;
    public ComputeShader computeShader3;
    public float tmp;



    public NaiveRenderPipeline() {

        // precompute random sequences
        for (int index = 0; index < 16; index++) {
            hammersleySequenceX[index] = RandomSequence.Hammersley(0, index, 16);
            hammersleySequenceY[index] = RandomSequence.Hammersley(1, index, 16);
            haltonSequenceX[index] = RandomSequence.Halton(0, index);
            haltonSequenceY[index] = RandomSequence.Halton(1, index);
        }

        // set full-screen mesh
        blitMesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];
        vertices[0] = new Vector3(-1, -1, 0.5f);
        vertices[1] = new Vector3(1, -1, 0.5f);
        vertices[2] = new Vector3(-1, 1, 0.5f);
        vertices[3] = new Vector3(1, 1, 0.5f);
        uv[0] = new Vector2(0, 1);
        uv[1] = new Vector2(1, 1);
        uv[2] = new Vector2(0, 0);
        uv[3] = new Vector2(1, 0);
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;
        blitMesh.vertices = vertices;
        blitMesh.uv = uv;
        blitMesh.triangles = triangles;

        // gbuffer
        gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear); // depth
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear); // albedo
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear); // normal 10+10+10+2
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear); // motion vector, roughness, metallic
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); // emission, occlusion
        gdepthID = gdepth;
        for (int i = 0; i < 4; i++)
            gbufferID[i] = gbuffers[i];

        // shadow map
        for (int i = 0; i < 4; i++)
            shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        csm = new CSM();

        // light pass
        sceneColor = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        sceneColor.useMipMap = true;


        // TAA
        enableTAA = false;
        TAASampleIndex = 0;
        historyFrameBuffer = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);

        // SSR
        SSRHistory = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);

        // Bloom
        for (int i = 0; i < 7; i++) {
            BloomDownSampleRTID[i] = Shader.PropertyToID("_BloomDownSampleRT" + i);
        }
        for (int i = 0; i < 6; i++) {
            BloomUpSampleRTID[i] = Shader.PropertyToID("_BloomUpSampleRT" + i);
        }
    }


    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

        Camera camera = cameras[0];

        ShadowPass(context, camera);

        SetGlobalVariables(context, camera);

        GbufferPass(context, camera);

        GTAOPass(context, camera);

        LightPass(context, camera);

        SSRPass(context, camera);

        PostProcessPass(context, camera);

        SkyBoxPass(context, camera);

        if (Application.isPlaying && enableTAA) {
            TAAPass(context, camera);
        }

        if (Application.isPlaying && enableBloom) {
            BloomPass(context, camera);
        }

        DebugPass(context, camera);


    }


    void SetGlobalVariables(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "set_variables";
        

        TAASampleIndex = (TAASampleIndex + 1) % 16;
        cmd.SetGlobalInt("_TAASampleIndex", TAASampleIndex);


        // screen
        cmd.SetGlobalFloat("_screenPixelWidth", camera.pixelWidth);
        cmd.SetGlobalFloat("_screenPixelHeight", camera.pixelHeight);
        cmd.SetGlobalFloat("_screenPixelWidthInv", 1.0f / camera.pixelWidth);
        cmd.SetGlobalFloat("_screenPixelHeightInv", 1.0f / camera.pixelHeight);


        // camera
        cmd.SetGlobalVector("_cameraUp", Camera.main.transform.up);


        // light
        Light light = RenderSettings.sun;
        Vector3 lightDir = -light.transform.forward;
        cmd.SetGlobalVector("_LightDirection", lightDir);
        UnityEngine.Color lightColor = light.color;
        cmd.SetGlobalColor("_LightColor", lightColor);
        float lightIntensity = light.intensity;
        cmd.SetGlobalFloat("_LightIntensity", lightIntensity);


        // gbuffer
        cmd.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
            Shader.SetGlobalTexture("_GT" + i, gbuffers[i]);


        // IBL texture
        cmd.SetGlobalTexture("_SpecularIBL", specularIBL);
        cmd.SetGlobalTexture("_DiffuseIBL", diffuseIBL);
        cmd.SetGlobalTexture("_BrdfLut", brdfLut);
        cmd.SetGlobalFloat("_IBLIntensity", IBLIntensity);

        

        // TAA (1/2)
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        Matrix4x4 jitteredProjectionMatrix = projectionMatrix;

        if (enableTAA) {
            float jitterX = hammersleySequenceX[TAASampleIndex];
            float jitterY = hammersleySequenceY[TAASampleIndex];
            jitteredProjectionMatrix.m02 += (jitterX * 2 - 1) / camera.pixelWidth;
            jitteredProjectionMatrix.m12 += (jitterY * 2 - 1) / camera.pixelHeight;
        }
        cmd.SetGlobalMatrix("_jitteredProjectionMatrix", jitteredProjectionMatrix);
        cmd.SetGlobalMatrix("_previousViewMatrix", previousViewMatrix);
        cmd.SetGlobalMatrix("_previousProjectionMatrix", previousProjectionMatrix);
        
        previousViewMatrix = viewMatrix;
    

        // LightPass
        Matrix4x4 vpMatrix;
        vpMatrix = jitteredProjectionMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        cmd.SetGlobalMatrix("_vpMatrix", vpMatrix);
        cmd.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
        cmd.SetGlobalMatrix("_pMatrix", jitteredProjectionMatrix);
        cmd.SetGlobalMatrix("_pMatrixInv", jitteredProjectionMatrix.inverse);
        cmd.SetGlobalMatrix("_vMatrix", viewMatrix);


        // TAA (2/2)
        // previousProjectionMatrix使用这一帧的投影参数，但是使用下一帧的jitter
        jitteredProjectionMatrix = projectionMatrix;
        if (enableTAA) {
            float jitterX = hammersleySequenceX[(TAASampleIndex + 1) % 16];
            float jitterY = hammersleySequenceY[(TAASampleIndex + 1) % 16];
            jitteredProjectionMatrix.m02 += (jitterX * 2 - 1) / camera.pixelWidth;   ///////// 问题原因
            jitteredProjectionMatrix.m12 += (jitterY * 2 - 1) / camera.pixelHeight;
        }
        previousProjectionMatrix = jitteredProjectionMatrix;


        // SSAO
        cmd.SetGlobalFloat("_SSAOSampleRadius", SSAOSampleRadius);
        cmd.SetGlobalFloat("_SSAOStrength", SSAOStrength);
        cmd.SetGlobalFloat("_SSAOSampleWeightVariability", SSAOSampleWeightVariability);
        cmd.SetGlobalFloat("_SSAODeltaDepthFade", SSAODeltaDepthFade);


        // GTAO
        cmd.SetGlobalFloat("_GTAODistanceFade", GTAODistanceFade);
        cmd.SetGlobalFloat("_GTAOSampleRadius", GTAOSampleRadius);
        cmd.SetGlobalFloat("_GTAOSpacialFilterSize", GTAOSpacialFilterSize);
        cmd.SetGlobalFloat("_GTAOSpacialFilterVariance", GTAOSpacialFilterVariance);
        cmd.SetGlobalFloat("_GTAOStrength", GTAOStrength);


        // SSR
        cmd.SetGlobalFloat("_SSROriginPosDepthBias", SSROriginPosDepthBias);
        cmd.SetGlobalFloat("_SSRObjectThickness", SSRObjectThickness);
        cmd.SetGlobalFloat("_SSRSpacialFilterSize", SSRSpacialFilterSize);
        cmd.SetGlobalFloat("_SSRColorSpacialFilterVariance", SSRColorSpacialFilterVariance);
        cmd.SetGlobalFloat("_SSROccupancySpacialFilterVariance", SSROccupancySpacialFilterVariance);
        cmd.SetGlobalInt("_SSRSampleH", SSRSampleH ? 1 : 0);


        // shadow
        cmd.SetGlobalFloat("_PCSSKernelRadiusCoefficient", PCSSKernelRadiusCoefficient);
        cmd.SetGlobalFloat("_PCSSLightSize", PCSSLightSize);
        cmd.SetGlobalFloat("_PCFDepthBias", PCFDepthBias);


        // Bloom
        cmd.SetGlobalFloat("_bloomIntensity", bloomIntensity);
        cmd.SetGlobalFloat("_bloomVariance", bloomVariance);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }


    void GbufferPass(ScriptableRenderContext context, Camera camera) {

        UnityEngine.Profiling.Profiler.BeginSample("gbufferDraw");

        // 设置相机
        context.SetupCameraProperties(camera);

        // 初始化 command buffer
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        // 将gbuffer设为render target
        cmd.SetRenderTarget(gbufferID, gdepthID);

        // 清屏
        cmd.ClearRenderTarget(true, true, UnityEngine.Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        // 绘制到gbuffer
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.Submit();

        UnityEngine.Profiling.Profiler.EndSample();

    }


    void ShadowPass(ScriptableRenderContext context, Camera camera) {
        
        // 获取光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // 计算各级cascade包围盒信息
        csm.Update(camera, lightDir);

        // 保存当前相机状态
        csm.SaveMainCameraSettings(ref camera);

        // 绘制shadow map
        for (int level = 0; level < 4; level++) {

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "shadowmap" + level;

            // 将相机移到光源方向
            csm.ConfigCameraToShadowSpace(ref camera, lightDir, level, 500.0f);

            // 绘制前准备
            context.SetupCameraProperties(camera);
            cmd.SetRenderTarget(shadowTextures[level]);
            cmd.ClearRenderTarget(true, true, UnityEngine.Color.clear);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 剔除
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);
            ShaderTagId shaderTagId = new ShaderTagId("depthonly");
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 绘制
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            // 设置shader变量
            cmd.SetGlobalTexture("_shadowtex" + level, shadowTextures[level]);
            cmd.SetGlobalFloat("_split" + level, csm.splits[level]);
            Matrix4x4 v = camera.worldToCameraMatrix;
            Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            cmd.SetGlobalMatrix("_shadowVpMatrix" + level, p * v);
            cmd.SetGlobalVector("_shadowMapWorldSize" + level, new Vector2(csm.shadowMapWorldSizeX[level], csm.shadowMapWorldSizeY[level]));

            context.ExecuteCommandBuffer(cmd);

            context.Submit();
        }

        // 还原相机状态
        csm.RevertMainCameraSettings(ref camera);
        context.SetupCameraProperties(camera);
        context.Submit();
    }


    void SSRPass(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "SSR";

        cmd.GetTemporaryRT(tmpDepthRTID, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.R8);

        // compute SSR
        Material mat = new Material(Shader.Find("NaiveRP/SSRpass"));
        RenderTargetIdentifier[] SSRHalfVector_SSRRadiance = new RenderTargetIdentifier[3] { SSRDataID, SSRHID, SSRPDFID };
        cmd.GetTemporaryRT(SSRDataID, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.ARGB64);
        cmd.GetTemporaryRT(SSRHID, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.ARGB64);
        cmd.GetTemporaryRT(SSRPDFID, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.RFloat);
        cmd.SetRenderTarget(SSRHalfVector_SSRRadiance, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat);

        // resolve SSR
        Material mat2 = new Material(Shader.Find("NaiveRP/SSRpass2"));
        cmd.GetTemporaryRT(SSRResolvedRadianceID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(SSRResolvedRadianceID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat2);

        // filter SSR (spacial, vertical)
        Material mat3_1 = new Material(Shader.Find("NaiveRP/SSRpass3_1"));
        cmd.GetTemporaryRT(SSRVerticalFilteredRadianceID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(SSRVerticalFilteredRadianceID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat3_1);

        // filter SSR (spacial, horizontal)
        Material mat3_2 = new Material(Shader.Find("NaiveRP/SSRpass3_2"));
        cmd.GetTemporaryRT(SSRFilteredRadianceID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(SSRFilteredRadianceID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat3_2);

        // filter SSR (temporal)
        if (SSRTemporalFilter) {
            cmd.SetGlobalTexture("_SSRHistory", SSRHistory);
            Material mat4 = new Material(Shader.Find("NaiveRP/SSRpass4"));
            cmd.GetTemporaryRT(SSRTemporalFilteredRadianceID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
            cmd.SetRenderTarget(SSRTemporalFilteredRadianceID, tmpDepthRTID);
            cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat4);
            cmd.Blit(SSRTemporalFilteredRadianceID, SSRHistory);
            cmd.Blit(SSRTemporalFilteredRadianceID, SSRFilteredRadianceID);
        }

        context.ExecuteCommandBuffer(cmd);
        context.Submit();

    }


    void GTAOPass(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GTAO";

        cmd.GetTemporaryRT(tmpDepthRTID, Screen.width, Screen.height, 0, FilterMode.Point, RenderTextureFormat.R8);

        Material mat1 = new Material(Shader.Find("NaiveRP/GTAOpass"));
        cmd.GetTemporaryRT(GTAOResolvedID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(GTAOResolvedID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat1);

        Material mat2 = new Material(Shader.Find("NaiveRP/GTAOpass2"));
        cmd.GetTemporaryRT(GTAOVerticalFilteredID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(GTAOVerticalFilteredID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat2);

        Material mat3 = new Material(Shader.Find("NaiveRP/GTAOpass3"));
        cmd.GetTemporaryRT(GTAOSpacialFilteredID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);
        cmd.SetRenderTarget(GTAOSpacialFilteredID, tmpDepthRTID);
        cmd.DrawMesh(blitMesh, Matrix4x4.identity, mat3);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();

    }


    void LightPass(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        // draw
        Material mat = new Material(Shader.Find("NaiveRP/lightpass"));
        cmd.Blit(gbuffers[0], sceneColor, mat);
        cmd.SetGlobalTexture("_sceneColor", sceneColor);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }


    void PostProcessPass(ScriptableRenderContext context, Camera camera) {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "postprocess";

        // draw
        Material mat = new Material(Shader.Find("NaiveRP/postprocesspass"));
        cmd.Blit(sceneColor, BuiltinRenderTextureType.CameraTarget, mat);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }


    void TAAPass(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "TAA";

        cmd.GetTemporaryRT(TAAOutputID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64);

        cmd.SetGlobalTexture("_HistoryFrameBuffer", historyFrameBuffer);
        cmd.SetGlobalFloat("_TAABlendWeight", TAABlendWeight);

        Material matTAA = new Material(Shader.Find("NaiveRP/TAA"));
        Material matFlip = new Material(Shader.Find("NaiveRP/verticalflip"));

        cmd.Blit(BuiltinRenderTextureType.CameraTarget, TAAOutputID, matTAA);
        cmd.Blit(TAAOutputID, historyFrameBuffer);
        cmd.Blit(TAAOutputID, BuiltinRenderTextureType.CameraTarget, matFlip);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();

    }

    void BloomPass(ScriptableRenderContext context, Camera camera) {
        
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Bloom";

        Material matFlip = new Material(Shader.Find("NaiveRP/verticalflip"));
        cmd.GetTemporaryRT(BloomInitialSceneColorID, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear, 1, true);
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, BloomInitialSceneColorID, matFlip);

        Material matSelect = new Material(Shader.Find("NaiveRP/BloomSelect"));
        cmd.GetTemporaryRT(BloomSelectedSceneColor, Screen.width, Screen.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear, 1, true);
        cmd.Blit(BloomInitialSceneColorID, BloomSelectedSceneColor, matSelect);

        int downSampleFactor = 1;
        for (int i = 0; i < 7; i++) {
            downSampleFactor *= 2;
            cmd.GetTemporaryRT(BloomDownSampleRTID[i], Screen.width / downSampleFactor, Screen.height / downSampleFactor, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);
        }
        downSampleFactor = 1;
        for (int i = 5; i >= 0; i--) {
            downSampleFactor *= 2;
            cmd.GetTemporaryRT(BloomUpSampleRTID[i], Screen.width / downSampleFactor, Screen.height / downSampleFactor, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, 1, true);
        }

        if (bloomEnableComputeShader) {
            int downSampleKernel = bloomComputeDownSample.FindKernel("BlurDownSample");
            cmd.SetComputeTextureParam(bloomComputeDownSample, downSampleKernel, "gInput", BloomSelectedSceneColor);
            cmd.SetComputeTextureParam(bloomComputeDownSample, downSampleKernel, "gOutput", BloomDownSampleRTID[0]);
            cmd.DispatchCompute(bloomComputeDownSample, downSampleKernel, (Screen.width / 2 + 15) / 16, (Screen.height / 2 + 15) / 16, 1);
            downSampleFactor = 2;
            for (int sourceIndex = 0; sourceIndex <= 5; sourceIndex++) {
                downSampleFactor *= 2;
                cmd.SetComputeTextureParam(bloomComputeDownSample, downSampleKernel, "gInput", BloomDownSampleRTID[sourceIndex]);
                cmd.SetComputeTextureParam(bloomComputeDownSample, downSampleKernel, "gOutput", BloomDownSampleRTID[sourceIndex + 1]);
                cmd.DispatchCompute(bloomComputeDownSample, downSampleKernel, Math.Max((Screen.width / downSampleFactor + 15) / 16, 1), Math.Max((Screen.height / downSampleFactor + 15) / 16, 1), 1);
            }
        }
        else {
            Material matDownSample = new Material(Shader.Find("NaiveRP/BloomDownSample"));
            cmd.Blit(BloomSelectedSceneColor, BloomDownSampleRTID[0], matDownSample);
            cmd.Blit(BloomDownSampleRTID[0], BloomDownSampleRTID[1], matDownSample);
            cmd.Blit(BloomDownSampleRTID[1], BloomDownSampleRTID[2], matDownSample);
            cmd.Blit(BloomDownSampleRTID[2], BloomDownSampleRTID[3], matDownSample);
            cmd.Blit(BloomDownSampleRTID[3], BloomDownSampleRTID[4], matDownSample);
            cmd.Blit(BloomDownSampleRTID[4], BloomDownSampleRTID[5], matDownSample);
            cmd.Blit(BloomDownSampleRTID[5], BloomDownSampleRTID[6], matDownSample);
        }

        Material matUpSample = new Material(Shader.Find("NaiveRP/BloomUpSample"));
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[5]);
        cmd.Blit(BloomDownSampleRTID[6], BloomUpSampleRTID[0], matUpSample);
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[4]);
        cmd.Blit(BloomUpSampleRTID[0], BloomUpSampleRTID[1], matUpSample);
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[3]);
        cmd.Blit(BloomUpSampleRTID[1], BloomUpSampleRTID[2], matUpSample);
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[2]);
        cmd.Blit(BloomUpSampleRTID[2], BloomUpSampleRTID[3], matUpSample);
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[1]);
        cmd.Blit(BloomUpSampleRTID[3], BloomUpSampleRTID[4], matUpSample);
        cmd.SetGlobalTexture("_downSampleTexture", BloomDownSampleRTID[0]);
        cmd.Blit(BloomUpSampleRTID[4], BloomUpSampleRTID[5], matUpSample);

        Material matApply = new Material(Shader.Find("NaiveRP/BloomApply"));
        cmd.SetGlobalTexture("_bloomTex", BloomUpSampleRTID[5]);
        cmd.Blit(BloomInitialSceneColorID, BuiltinRenderTextureType.CameraTarget, matApply);

        cmd.ReleaseTemporaryRT(BloomInitialSceneColorID);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    void SkyBoxPass(ScriptableRenderContext context, Camera camera) {
        // skybox and Gizmos
        context.SetupCameraProperties(camera);
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos()) {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
        context.Submit();
    }

    void DebugPass(ScriptableRenderContext context, Camera camera) {

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Debug";

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        context.Submit();
    }

}
