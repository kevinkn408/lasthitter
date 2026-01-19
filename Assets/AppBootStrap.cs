using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBootStrap : MonoBehaviour
{
    [Header("Performance")]
    public int targetFrameRate = 60;
    public bool showFPS = false;

    float deltaTime;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyPerformanceSettings();
        SceneManager.sceneLoaded += (_, __) => ApplyPerformanceSettings();
    }

    void ApplyPerformanceSettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    void Update()
    {
        if (!showFPS) return;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!showFPS) return;

        float fps = 1f / deltaTime;
        GUI.Label(
            new Rect(10, 10, 200, 30),
            $"FPS: {fps:0.}"
        );
    }
}
