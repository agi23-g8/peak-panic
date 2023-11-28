using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class UICountdown : MonoBehaviour
{
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI countdownTextShadow;

    /// <summary>
    /// Creates a new countdown UI element that counts down from the given number of seconds.
    /// </summary>
    /// <param name="seconds"></param>
    public void NewCountDown(int seconds, Action onFinishedCallback)
    {
        StartCoroutine(Countdown(seconds, onFinishedCallback));
    }

    public void ResetCountdown()
    {
        countdownText.text = "";
        countdownTextShadow.text = "";
    }

    IEnumerator Countdown(int seconds, Action onFinishedCallback)
    {
        int counter = seconds;
        while (counter > 0)
        {
            Debug.Log(counter);
            countdownText.text = counter.ToString();
            countdownTextShadow.text = counter.ToString();
            yield return new WaitForSeconds(1);
            counter--;
        }

        countdownText.text = "";
        countdownTextShadow.text = "";
        onFinishedCallback();
    }
}
