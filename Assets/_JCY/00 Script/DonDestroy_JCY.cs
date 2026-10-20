using System;
using UnityEngine;

public class DonDestroy_JCY : MonoBehaviour
{
    public static DonDestroy_JCY Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
