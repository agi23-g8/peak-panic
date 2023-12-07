using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    // this script should transition to main scene if any key is pressed
    private void Update()
    {
        if (Input.anyKeyDown)
        {
            // leave if it was from the mouse
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                return;
            }

            // go to main scene
            SceneManager.LoadScene("Main");
        }
    }
}
