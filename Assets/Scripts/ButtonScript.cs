using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    
    private Scene currentScene;
    private Scene[] scenes;
    public Button[] buttons = new Button[3];

    public void Start()
    {
        currentScene = SceneManager.GetActiveScene();
        buttons[0].onClick.AddListener(RestartCurrentScene);
        buttons[1].onClick.AddListener(StartNewScene);
        buttons[2].onClick.AddListener(QuitGame);
    }
    
    private void RestartCurrentScene()
    {
        SceneManager.LoadScene(currentScene.name);
    }

    private void StartNewScene()
    {
        SceneInformation.currentMap = Maps.GetRandomMap();
        SceneManager.LoadScene(currentScene.name);
    }
    
    private void QuitGame()
    {
        Application.Quit();
    }

}