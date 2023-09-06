[TOC]

#### 1. Deferred rendering

5张RT：

1. depth
2. albedo, ARGB32
3. normal, ARGB1010102
4. motion vector, roughness, metallic, ARGB64
5. emission, AO

#### 2. CSM

根据到camera的linear depth，将视锥体分为4四个部分，每一部分分别计算bounding box（轴与平行光方向对齐）来确定光源的VP矩阵。

CSM绘制完成后，在shader中根据shading point到camera的linear depth计算它属于的cascade，然后将它的世界坐标投影到光源的NDC空间，与shadow map深度比较。

#### 3. PCF

在光源的NDC空间中，将shadow map中周围像素的深度与shading point的深度进行比较。采样可以使用泊松圆盘或者低差异序列（hammersley）。

#### 4. PCSS

计算平均遮挡物距离，然后使用比例线段来计算PCF半径。与遮挡物平均距离越大，且设定的光源大小越大，PCF半径越大。

遮挡物距离：在shading point周围采样shadow map，计算平均深度（只有当采样点离光源更近时才参与计算）。

为了防止由于shadow map精度有限而将shading point周围的像素误判为遮挡物，需要增加一个bias：

```hlsl
if (d_sample <= d_shading_point + _PCFDepthBias) shadow += 1.0;
```

如果使用16个sample来计算遮挡物距离以及PCF，那么会有较大的噪声。但是使用了TAA之后发现这些噪声基本被去除了，所以没有使用其他滤波方法。

另外shadow map参数需要精调，否则效果会比较奇怪。

#### 5. PBR

FDG

计算时roughness可以clamp一下，防止高光太小而看不见

```
roughness = max(roughness, 0.05);
```

#### 5. IBL

需要两个cube map，一个是diffuse图：预积分irradiance，使用其他软件生成）；一个是specular图，将不同roughness下的入射光积分存在各级mipmap中，采样时使用三线性插值：

```hlsl
float roughness_lod = roughness_clamp * (1.7 - 0.7 * roughness_clamp);
float lod = 6.0 * roughness_lod;  // Unity mipmap: [0, 1, 2, 3, 4, 5, 6]
float3 specular_light_IBL = texCUBElod(_SpecularIBL, float4(r, lod)).rgb;
```

另外由于菲涅尔项也拆成了两部分，所以还需要一个brdfLut来计算specular brdf。

不计算G项。

#### 6. SSAO

参照shading point的真实法线，在上半球面进行采样计算visibility。这里假设采样点到shading point的visibility就是采样点到camera的visibility，所以有一张深度图就可以算出来了。

AO基于假设：环境光照是常数，所以积分是cos-weighted的。

另外，可以给更接近shading point的采样点更大的权重。

就算使用了很多个采样点（比如64）结果的噪声也会比较大，所以需要输出到RT然后模糊。

#### 7. GTAO

将上半球面分成数个slice，每个slice计算往相反两个方向看的未被遮挡的角度（与真实法线的夹角h），然后就可以在平面上积分得到每个slice的visibility，然后不同slice的visibility求平均就是上半球面的visibility。

<img src="img/GTAO1.png" alt="GTAO1" style="zoom: 67%;" />

在view space中做，首先计算真实法线，根据真实法线构建局部坐标系，切出slice。

对于每一个slice，在屏幕空间沿uv坐标步进，结合深度图就可以得到采样点的view space坐标，从而计算出该采样点对应的夹角h。根据采样点的h计算slice的h最简单的方法就是取最大值，但是为了防止当采样点与shading point深度差距过大时产生误遮挡（因为物体的深度是有限的，一般都会假定一个深度，当深度差大于物体深度时倾向于认为实际上没有遮挡），可以根据采样点与shading point之间的距离进行lerp：

```hlsl
float2 falloff = saturate(sample12LengthSquared.xy * (2 / pow(2.0f, _GTAODistanceFade)));
float2 sample12h = float2(dot(sample1Vec, viewDir), dot(sample2Vec, viewDir)) * sample12LengthInv;
h.xy = (sample12h.xy > h.xy) ? lerp(sample12h, h, falloff) : lerp(sample12h.xy, h.xy, thickness);
```

这样就可以使得，当新的采样点太远的时候，倾向于认为它的结果的可靠性较低，分配更低的权重。

计算出slice的两个h之后，套上图中的公式就可以算出visibility。

发现使用2个slice（计算4个h）就可以有比较好的效果，配合高斯模糊基本看不出噪声。这里的高斯模糊可以使用双边滤波：权重中引入亮度差从而保留锐利边界。

![GTAO2](img\GTAO2.png)

#### 8. TAA

修改投影矩阵，让每次的投影结果在uv平面上产生微小平移（这里使用低差异序列），然后将历史帧和当前帧在时域混合。

```
if (enableTAA) {
	float jitterX = hammersleySequenceX[TAASampleIndex];
	float jitterY = hammersleySequenceY[TAASampleIndex];
	jitteredProjectionMatrix.m02 += (jitterX * 2 - 1) / camera.pixelWidth;
	jitteredProjectionMatrix.m12 += (jitterY * 2 - 1) / camera.pixelHeight;
}
```

每一帧渲染完成之后，额外blit到historyFrameBuffer。

为了解决相机移动的问题，需要存上一帧的V和P矩阵。这里需要注意，经过实验发现，相邻两帧的P矩阵虽然投影参数可能不同，但是抖动量需要相同，此时才能混合出正确的结果，否则画面会变糊。也就是说，当设置变量previousProjectionMatrix时，需要将其抖动量设置为下一帧的情况。

重投影：先用当前的vpMatrixInv转换到世界坐标，然后再用上一帧的VP矩阵得到上一帧的屏幕空间坐标。

考虑到像素在上一帧并不一定可以得到参考，所以采样得到historyColor时需要进行处理，防止它和currentColor差距过大。这里的方法是将historyColor在颜色空间中clip到currentColor周围：采样当前像素周围的3x3个像素，得到局部颜色在亮度色度（YCoCg）空间的bounding box，然后让historyColor沿着向bounding box中心的方向移动到bounding box边界。

#### 9. SSR

由于gbuffer中拿到了深度图，所以可以比较容易地求交。这里使用的是固定步长，在世界坐标中每次前进固定距离，然后投影到屏幕空间计算深度，如果深度大于场景深度，那么就说明光线与场景相交了。为了增加求交的精度，当发现相交之后，回退到上一个采样点，并将步长减半继续步进，直到每次步进在屏幕空间的移动距离小于一个像素的宽度。

为了防止产生拖影，得到交点之后，计算交点深度和场景深度的距离，如果该距离大于预先设定的物体厚度，则丢弃这个结果。

最简单的方法，反射光直接由入射光反射得到：

```
float3 R = normalize(reflect(-V, H));
```

在求得交点后，为了模拟glossy表面特性，采样sceneColor的mipmap来得到模糊后的结果。如何确定mip级别？首先根据表面roughness计算brdf lobe的胖瘦：

```
float coneTangent = lerp(0, roughness * (1 - 0.5), n_dot_v * sqrt(roughness));
```

然后结合反射光传播的距离就可以算出brdf lobe打到场景上区域的大小：

```
float coneIntersectionCircleRadius = coneTangent * length(sampleHitUV - sampleUV);
```

然后把这个大小换算成屏幕空间的半径，就可以知道mip级别。

屏幕空间反射结果写入一个RT，最后和sceneColor混合。为了确定混合系数，用一个通道来存储反射光是否与场景有交点。

#### 10. StochasticSSR

反射光不直接由入射光反射得到，而是通过GGX重要性采样生成。此外，需要输出到多个RT，存储交点UV，交点深度，交点半程向量，采样PDF以及是否有交点。然后在resolveSSR环节根据蒙特卡洛积分计算着色结果。此外，为了增加SPP，相邻的像素可以互相共用采样光线，因为可以认为空间位置接近的像素具有差不多的反射情况。

这个方法得到的SSR结果噪声非常大，需要进行空间域和时域的滤波。

#### 11. Bloom

Bloom的过程可以用一张图描述：

![Bloom](img\Bloom.png)

每次下采样使用5x5的高斯卷积核，每次上采样也使用5x5的高斯卷积核。

上采样以及Blur and Add操作是为了减少Bloom结果的方形图案（由线性插值导致）。

为了降低开销，考虑到高斯模糊的过程只使用到邻域的颜色信息，所以下采样过程使用compute shader完成，这样可以利用线程组的shared memory来降低采样纹理的开销。compute shader使用4x4的高斯卷积核从而方便对齐。使用compute shader进行高斯模糊主要分为两个阶段：1. 初始化共享内存，2. 计算卷积。

在初始化共享内存阶段，每个线程将自己对应位置的纹理颜色复制到共享内存。此时，因为计算卷积还需要一些线程组所处位置之外的颜色信息，所以需要让部分线程将这些像素值也复制到共享内存。

在计算卷积部分，就是简单的加权求和，权重全部预计算。
