using UnityEngine;
using UnityEngine.UI;

public class UIPlayerPositionElement : MonoBehaviour
{
    [SerializeField]
    private Color colorFirst;

    [SerializeField]
    private Color colorDefault;

    [SerializeField]
    private TMPro.TextMeshProUGUI playerName;

    [SerializeField]
    private TMPro.TextMeshProUGUI playerPosition;

    [SerializeField]
    private Image panel;

    public void SetPlayerName(string name)
    {
        playerName.text = name;
    }

    public void SetPlayerPosition(int position)
    {
        playerPosition.text = position.ToString();

        if (position == 1)
        {
            panel.color = colorFirst;
            playerName.color = Color.black;
            playerPosition.color = Color.black;

        }
        else
        {
            panel.color = colorDefault;
            playerName.color = Color.white;
            playerPosition.color = Color.white;
        }
    }
}
