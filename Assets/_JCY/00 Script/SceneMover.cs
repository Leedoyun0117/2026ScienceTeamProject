using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMover : MonoBehaviour
{
   [SerializeField] private int sceneNum;

   public void SceneMove()
   {
      SceneManager.LoadScene(sceneNum);
   }

   public void Quit()
   {
      Application.Quit();
   }
}
