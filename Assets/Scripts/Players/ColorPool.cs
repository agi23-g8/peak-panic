using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ColorPool", menuName = "Skin/ColorPool", order = 1)]
public class ColorPool : ScriptableObject
{
    //________________________________________________________________
    // User-defined colors

    [SerializeField]
    private List<Color> m_colors = new List<Color>();


    //________________________________________________________________
    // Internal pool structure
    private List<Color> m_availableColors = new List<Color>();
    private List<Color> m_usedColors = new List<Color>();


    //________________________________________________________________
    // Pool allocation logic

    /// @brief Reset the pool state.
    public void ResetPool()
    {
        m_usedColors.Clear();
        m_availableColors.Clear();
        m_availableColors.AddRange(m_colors);
    }

    // @brief Return the first available color from the pool.
    public Color PullColor()
    {
        if (m_availableColors.Count == 0)
        {
            Debug.LogWarning("Color pool is empty and will be resetted.");
            ResetPool();
        }

        // Get the first available color
        Color allocatedColor = m_availableColors[0];

        // Move the color from available to used list
        m_availableColors.RemoveAt(0);
        m_usedColors.Add(allocatedColor);

        return allocatedColor;
    }


    //________________________________________________________________
    // Color distribution helpers

    /// @brief Auto-generate pure random color values.
    public void RandomColorDistribution()
    {
        for (int i = 0; i < m_colors.Count; i++)
        {
            m_colors[i] = new Color(Random.value, Random.value, Random.value);
        }

        ShuffleColors();
        ResetPool();
    }

    /// @brief Auto-generate saturated colors, evenly distributed across the color wheel.
    public void SaturatedColorDistribution()
    {
        float hueStep = 1.0f / m_colors.Count;
        float baseHue = Random.value;

        for (int i = 0; i < m_colors.Count; i++)
        {
            // Evenly spaced hues
            float hue = (baseHue + i * hueStep) % 1.0f;
            // Full saturation
            float saturation = 1f;
            // Full value
            float value = 1f;

            m_colors[i] = Color.HSVToRGB(hue, saturation, value);
        }

        ShuffleColors();
        ResetPool();
    }

    /// @brief Auto-generate pastel colors, evenly distributed across the color wheel.
    public void PastelColorDistribution()
    {
        float hueStep = 1.0f / m_colors.Count;
        float baseHue = Random.value;

        for (int i = 0; i < m_colors.Count; i++)
        {
            // Evenly spaced hues
            float hue = (baseHue + i * hueStep) % 1.0f;
            // Random saturation between 0.3 and 0.7
            float saturation = 0.3f + Random.value * 0.4f;
            // Random value between 0.8 and 1.0
            float value = 0.8f + Random.value * 0.2f;

            m_colors[i] = Color.HSVToRGB(hue, saturation, value);
        }

        ShuffleColors();
        ResetPool();
    }

    /// @brief Shuffle the color array using Fisher-Yates unbiased shuffling algorithm.
    private void ShuffleColors()
    {
        int n = m_colors.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = m_colors[i];
            m_colors[i] = m_colors[j];
            m_colors[j] = temp;
        }
    }
}


#if UNITY_EDITOR
    [CustomEditor(typeof(ColorPool))]
    public class ColorPoolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);

            ColorPool colorPool = target as ColorPool;

            if (GUILayout.Button("Random Colors"))
            {
                colorPool.RandomColorDistribution();
                EditorUtility.SetDirty(colorPool);
            }

            if (GUILayout.Button("Saturated Colors"))
            {
                colorPool.SaturatedColorDistribution();
                EditorUtility.SetDirty(colorPool);
            }

            if (GUILayout.Button("Pastel Colors"))
            {
                colorPool.PastelColorDistribution();
                EditorUtility.SetDirty(colorPool);
            }
        }

        private void OnEnable()
        {
            // Reset the ColorPool when entering play mode
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            // Reset the ColorPool when leaving play mode
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ColorPool colorPool = target as ColorPool;
                colorPool.ResetPool();

                EditorUtility.SetDirty(colorPool);
            }
        }
    }
#endif
