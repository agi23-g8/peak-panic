using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearTexturesOnQuit  : MonoBehaviour
{
    public List<RenderTexture> texturesToClear = new List<RenderTexture>();

    void OnDestroy()
    {
        foreach (RenderTexture rt in texturesToClear)
        {
            // Clear the RenderTexture to black
            Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);
        }
    }
}
