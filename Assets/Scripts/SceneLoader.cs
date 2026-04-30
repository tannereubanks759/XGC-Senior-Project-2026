using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void Start()
    {
        Destroy(GameObject.FindAnyObjectByType<FirstPersonController>().gameObject);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }
}
