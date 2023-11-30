using System.Collections;
using TMPro;
using UnityEngine;

public class UIMessagePopup : MonoBehaviour
{
    public TextMeshProUGUI messageText;

    public void Show(string text, float seconds)
    {
        gameObject.SetActive(true);
        messageText.text = text;
        StartCoroutine(HideAfterSeconds(seconds));
    }

    public void HideEarly()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
