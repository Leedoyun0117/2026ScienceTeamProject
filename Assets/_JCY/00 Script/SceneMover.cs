using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneMover : MonoBehaviour
{
   [SerializeField] private int sceneNum;
   [SerializeField] private GameObject pannel;

   private void Start()
   {
      if (pannel != null)
      {
         pannel.SetActive(false);
      }
   }

   public void SceneMove()
   {
      SceneManager.LoadScene(sceneNum);
   }

   public void Quit()
   {
      Application.Quit();
   }

   private void Update()
   {
      if (Keyboard.current.escapeKey.wasPressedThisFrame)
      {
         DisplayPannel();
      }
      
   }

   public void DisplayPannel()
   {
      if (pannel != null)
      {
         pannel.SetActive(!pannel.activeInHierarchy);
      }
      
   }
}
