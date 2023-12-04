using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class VolumetricLightFeature : ScriptableRendererFeature
{
    [SerializeField]
    RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    [SerializeField]
    private Shader volumetricLightShader;

    private Material volumetricLightMaterial;

    private VolumetricLightRenderPass volumetricLightRenderPass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }

        if (volumetricLightRenderPass == null)
        {
            return;
        }

        VolumeStack stack = VolumeManager.instance.stack;
        VolumetricEffectComponent volumetricEffect = stack.GetComponent<VolumetricEffectComponent>();
        if (volumetricEffect == null || !volumetricEffect.IsActive())
        {
            Debug.Log("Volumetric Light Component is not active in volume stack");
            return;
        }

        renderer.EnqueuePass(volumetricLightRenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }
        volumetricLightRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
        volumetricLightRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
        volumetricLightRenderPass.SetTarget(renderer.cameraColorTargetHandle);

    }

    public override void Create()
    {
        if (volumetricLightShader == null)
        {
            Debug.LogError("Shader is null");
            return;
        }
        volumetricLightMaterial = CoreUtils.CreateEngineMaterial(volumetricLightShader);
        volumetricLightRenderPass = new VolumetricLightRenderPass(volumetricLightMaterial)
        {
            renderPassEvent = renderPassEvent
        };
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(volumetricLightMaterial);
        volumetricLightRenderPass.Dispose();
    }


    [System.Serializable]
    public class VolumetricLightRenderPass : ScriptableRenderPass
    {
        private Material volumetricLightMaterial;
        private RenderTextureDescriptor descriptor;
        private RTHandle cameraColorTargetHandle;
        private RTHandle raymarchTarget;
        private RTHandle lowResDepthTarget;
        private RTHandle compositeTarget;
        private VolumetricEffectComponent volumetricEffect;
        public VolumetricLightRenderPass(Material volumetricLightMaterial)
        {
            this.volumetricLightMaterial = volumetricLightMaterial;
            raymarchTarget = RTHandles.Alloc(raymarchTarget, name: "Volumetric Light Target");
            lowResDepthTarget = RTHandles.Alloc(lowResDepthTarget, name: "Low Res Depth Target");
            compositeTarget = RTHandles.Alloc(compositeTarget, name: "Composite Target");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            descriptor = renderingData.cameraData.cameraTargetDescriptor;
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     ConfigureTarget(cameraColorTargetHandle);
        // }

        public void SetTarget(RTHandle cameraColorTargetHandle)
        {
            this.cameraColorTargetHandle = cameraColorTargetHandle;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            VolumeStack stack = VolumeManager.instance.stack;
            volumetricEffect = stack.GetComponent<VolumetricEffectComponent>();

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("Volumetric Light")))
            {
                volumetricLightMaterial.SetFloat("_Intensity", volumetricEffect.intensity.value);
                volumetricLightMaterial.SetFloat("_Scattering", volumetricEffect.scattering.value);
                volumetricLightMaterial.SetInt("_Steps", volumetricEffect.marchSteps.value);
                volumetricLightMaterial.SetFloat("_MaxDistance", volumetricEffect.maxDistance.value);
                volumetricLightMaterial.SetFloat("_JitterVolumetric", volumetricEffect.jitter.value);
                volumetricLightMaterial.SetFloat("_GaussAmount", volumetricEffect.gaussBlurAmount.value);
                volumetricLightMaterial.SetInt("_GaussSamples", volumetricEffect.gaussBlurSamples.value);

                VolumetricPass(cmd, cameraColorTargetHandle);

            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private void VolumetricPass(CommandBuffer cmd, RTHandle source)
        {
            RenderTextureDescriptor original = GetCompatibleDescriptor(descriptor, descriptor.width, descriptor.height, descriptor.graphicsFormat);
            RenderTextureDescriptor singleChannel = GetCompatibleDescriptor(descriptor, descriptor.width, descriptor.height, descriptor.graphicsFormat, DepthBits.None);
            singleChannel.colorFormat = RenderTextureFormat.R16;
            // var singleChannel = new RenderTextureDescriptor(original.width, original.height, RenderTextureFormat.R16, 0);
            RenderingUtils.ReAllocateIfNeeded(ref raymarchTarget, singleChannel, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Volumetric Light Target");
            RenderingUtils.ReAllocateIfNeeded(ref lowResDepthTarget, singleChannel, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Low Res Depth Target");
            RenderingUtils.ReAllocateIfNeeded(ref compositeTarget, original, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "Composite Target");

            // raymarch depth
            Blitter.BlitCameraTexture(cmd, source, raymarchTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, volumetricLightMaterial, 0);

            // bilateral blur X
            Blitter.BlitCameraTexture(cmd, raymarchTarget, lowResDepthTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, volumetricLightMaterial, 1);

            // bilateral blur Y
            Blitter.BlitCameraTexture(cmd, lowResDepthTarget, raymarchTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, volumetricLightMaterial, 2);
            cmd.SetGlobalTexture("_VolumetricTexture", raymarchTarget);

            // downsample depth
            Blitter.BlitCameraTexture(cmd, source, lowResDepthTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, volumetricLightMaterial, 4);
            cmd.SetGlobalTexture("_DepthTexture", lowResDepthTarget);

            // composite
            Blitter.BlitCameraTexture(cmd, source, compositeTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, volumetricLightMaterial, 3);
            Blitter.BlitCameraTexture(cmd, compositeTarget, source);
        }

        public void Dispose()
        {
            RTHandles.Release(raymarchTarget);
            RTHandles.Release(lowResDepthTarget);
            RTHandles.Release(compositeTarget);
        }


        RenderTextureDescriptor GetCompatibleDescriptor()
                    => GetCompatibleDescriptor(descriptor.width, descriptor.height, descriptor.graphicsFormat);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
            => GetCompatibleDescriptor(descriptor, width, height, format, depthBufferBits);
        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
        {
            desc.depthBufferBits = (int)depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}