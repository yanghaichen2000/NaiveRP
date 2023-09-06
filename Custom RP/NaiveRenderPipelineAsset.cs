using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/NaiveRenderPipeline")]
public class NaiveRenderPipelineAsset : RenderPipelineAsset {

    public Cubemap specularIBL;
    public Cubemap diffuseIBL;
    public Texture brdfLut;
    public float IBLIntensity;

    public float PCSSKernelRadiusCoefficient;
    public float PCSSLightSize;
    public float PCFDepthBias;

    public bool enableTAA;
    public float TAABlendWeight;

    public float SSAOSampleRadius;
    public float SSAOStrength;
    public float SSAOSampleWeightVariability;
    public float SSAODeltaDepthFade;

    public float SSROriginPosDepthBias;
    public float SSRObjectThickness;
    public float SSRSpacialFilterSize;
    public float SSRColorSpacialFilterVariance;
    public float SSROccupancySpacialFilterVariance;
    public bool SSRTemporalFilter;
    public bool SSRSampleH;

    public float GTAODistanceFade;
    public float GTAOSampleRadius;
    public float GTAOSpacialFilterSize;
    public float GTAOSpacialFilterVariance;
    public float GTAOStrength;

    public bool enableBloom;
    public float bloomIntensity;
    public float bloomVariance;
    public bool bloomEnableComputeShader;
    public ComputeShader bloomComputeDownSample;

    public float tmp;
    public ComputeShader computeShader1;
    public ComputeShader computeShader2;
    public ComputeShader computeShader3;

    protected override RenderPipeline CreatePipeline() {
        NaiveRenderPipeline rp = new NaiveRenderPipeline();

        rp.specularIBL = specularIBL;
        rp.diffuseIBL = diffuseIBL;
        rp.brdfLut = brdfLut;
        rp.IBLIntensity = IBLIntensity;
        rp.PCSSKernelRadiusCoefficient = PCSSKernelRadiusCoefficient;
        rp.PCSSLightSize = PCSSLightSize;
        rp.PCFDepthBias = PCFDepthBias;
        rp.TAABlendWeight = TAABlendWeight;
        rp.tmp = tmp;
        rp.enableTAA = enableTAA;
        rp.SSAOSampleRadius = SSAOSampleRadius;
        rp.SSAOStrength = SSAOStrength;
        rp.SSAOSampleWeightVariability = SSAOSampleWeightVariability;
        rp.SSAODeltaDepthFade = SSAODeltaDepthFade;
        rp.SSROriginPosDepthBias = SSROriginPosDepthBias;
        rp.SSRObjectThickness = SSRObjectThickness;
        rp.SSRColorSpacialFilterVariance = SSRColorSpacialFilterVariance;
        rp.SSROccupancySpacialFilterVariance = SSROccupancySpacialFilterVariance;
        rp.SSRTemporalFilter = SSRTemporalFilter;
        rp.SSRSpacialFilterSize = SSRSpacialFilterSize;
        rp.GTAODistanceFade = GTAODistanceFade;
        rp.GTAOSpacialFilterSize = GTAOSpacialFilterSize;
        rp.GTAOSpacialFilterVariance = GTAOSpacialFilterVariance;
        rp.GTAOSampleRadius = GTAOSampleRadius;
        rp.GTAOStrength = GTAOStrength;
        rp.SSRSampleH = SSRSampleH;
        rp.enableBloom = enableBloom;
        rp.bloomIntensity = bloomIntensity;
        rp.bloomVariance = bloomVariance;
        rp.bloomEnableComputeShader = bloomEnableComputeShader;
        rp.bloomComputeDownSample = bloomComputeDownSample;

        rp.computeShader1 = computeShader1;
        rp.computeShader2 = computeShader2;
        rp.computeShader3 = computeShader3;

        return rp;
    }
}