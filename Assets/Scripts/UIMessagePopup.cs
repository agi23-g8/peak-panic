using System.Collections;
using TMPro;
using UnityEngine;

public class UIMessagePopup : MonoBehaviour
{
    public TextMeshProUGUI messageText;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Show(string text, float duration)
    {
        gameObject.SetActive(true);
        messageText.text = text;
        StartCoroutine(HideAfterSeconds(duration));
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}
