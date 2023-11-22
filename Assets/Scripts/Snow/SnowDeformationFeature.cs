using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SnowDeformationFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CompositingSettings
    {
        public string profilerTag = "Accumulate Deformation";
        public Material compositingMaterial;

        public RenderTexture currentDeformationMap;
        public RenderTexture previousDeformationMap;
    }

    class CompositingPass : ScriptableRenderPass
    {
        string m_profilerTag;
        RTHandle m_rawDeformationMap;
        RenderTexture m_currentDeformationMap;
        RenderTexture m_previousDeformationMap;
        Material m_compositingMaterial;

        public CompositingPass(string _profilerTag, Material _compositingMaterial)
        {
            m_profilerTag = _profilerTag;
            m_compositingMaterial = _compositingMaterial;
        }

        public void SetMultiTargets(RenderTexture _prev, RenderTexture _curr)
        {
            m_previousDeformationMap = _prev;
            m_currentDeformationMap = _curr;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer _cmd, ref RenderingData _renderingData)
        {
            // get the raw deformation map from rendering data (color target)
            m_rawDeformationMap = _renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext _context, ref RenderingData _renderingData)
        {
            // fetch a command buffer to use
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            // set the raw deformation map
            Shader.SetGlobalTexture("_RawSnowDeformationMap", m_rawDeformationMap.rt);
            Shader.SetGlobalFloat("_SnowDeformationAreaPixels", m_rawDeformationMap.rt.width);

            // copy previous deformation map
            cmd.Blit(m_currentDeformationMap, m_previousDeformationMap);
            Shader.SetGlobalTexture("_PrevSnowDeformationMap", m_previousDeformationMap);

            // accumulate raw and previous deformation map
            cmd.Blit(null, m_currentDeformationMap, m_compositingMaterial);
            Shader.SetGlobalTexture("_CurSnowDeformationMap", m_currentDeformationMap);

            // tell ScriptableRenderContext to execute the commands
            _context.ExecuteCommandBuffer(cmd);

            // tidy up after ourselves
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer _cmd)
        {
        }
    }

    // Settings, must be named "settings" to be shown in the Render Features inspector
    public CompositingSettings settings = new CompositingSettings();

    // The actual rendering pass where rendering commands happen
    CompositingPass m_compositingPass;

    // The material used to produce the final deformation map
    Material m_compositingMaterial;

    public override void Create()
    {
        m_compositingPass = new CompositingPass(settings.profilerTag, settings.compositingMaterial);

        // Configures where the render pass should be injected.
        m_compositingPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer _renderer, ref RenderingData _renderingData)
    {
        m_compositingPass.SetMultiTargets(settings.previousDeformationMap, settings.currentDeformationMap);
        _renderer.EnqueuePass(m_compositingPass);
    }
}