using UnityEngine;
using UnityEngine.SceneManagement;
// Script to load the scene whose name we put in, scene number also good, but i like naming.
public class LoadScene : MonoBehaviour
{
    public string sceneName;
    public void LoadTheScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
