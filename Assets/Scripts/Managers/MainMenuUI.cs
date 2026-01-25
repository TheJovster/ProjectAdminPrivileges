using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TestLevel-TwinStick");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
