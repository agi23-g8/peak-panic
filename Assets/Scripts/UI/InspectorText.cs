using UnityEngine;

[AddComponentMenu("Miscellaneous/README Info Note")]
public class InspectorText : MonoBehaviour
{
    [TextArea(10, 1000)]
    public string Comment = "Information Here.";
}
