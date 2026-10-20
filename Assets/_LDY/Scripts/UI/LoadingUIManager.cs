using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingUIManager : MonoBehaviour
{
    private static LoadingUIManager _instance;

    [SerializeField] private CanvasGroup _loadingCanvas;
    

    private IEnumerator Fade(bool isFadeIn)
    {
        float timer = 0.0f;
        
        while (timer <= 1.0f) 
        { 
            yield return null;

            timer += Time.unscaledDeltaTime * 3.0f;

            _loadingCanvas.alpha = isFadeIn ? Mathf.Lerp(0.0f, 1.0f, timer) : Mathf.Lerp(1.0f, 0.0f, timer);
        }

        if (!isFadeIn)
            gameObject.SetActive(false);
    }
}