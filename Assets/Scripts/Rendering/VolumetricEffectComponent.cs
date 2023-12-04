using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[Serializable, VolumeComponentMenuForRenderPipeline("Custom/Volumetric Light", typeof(UniversalRenderPipeline))]
public class VolumetricEffectComponent : VolumeComponent, IPostProcessComponent
{

    [Header("Raymarch Settings")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedFloatParameter scattering = new ClampedFloatParameter(0f, -1f, 1f);
    public IntParameter marchSteps = new IntParameter(32);
    public FloatParameter maxDistance = new FloatParameter(75f);
    public FloatParameter jitter = new FloatParameter(250f);
    public FloatParameter gaussBlurAmount = new FloatParameter(1f);
    public IntParameter gaussBlurSamples = new IntParameter(5);

    public bool IsActive()
    {
        return intensity.value > 0f;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}