using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static void LoadLobby()
    {
        Debug.Log("[SceneLoader] Loading Lobby scene...");
        SceneManager.LoadScene("Lobby");
    }

    public static void LoadInGame()
    {
        Debug.Log("[SceneLoader] Loading InGame scene...");
        SceneManager.LoadScene("InGame");
    }

    public static void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[SceneLoader] Reloading scene: {currentScene.name}");
        SceneManager.LoadScene(currentScene.name);
    }

    public static void QuitGame()
    {
        Debug.Log("[SceneLoader] Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
