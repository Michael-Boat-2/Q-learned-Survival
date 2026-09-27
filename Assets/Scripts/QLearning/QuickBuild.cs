using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickBuild : MonoBehaviour
{
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SceneManagerLoad()
    {
        SceneManager.LoadScene(0);
    }

    public void WatchTrain()
    {
        SceneManager.LoadScene("GameEnv");
    }

    public void WatchInfer()
    {
        SceneManager.LoadScene("GameEnvFancy");
    }
    
    
    

    public void Retry()
    {
        SceneManager.LoadScene(1);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
