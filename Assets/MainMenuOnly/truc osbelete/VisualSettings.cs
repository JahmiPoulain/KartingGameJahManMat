using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    [Header("Éléments de l'Interface (UI)")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown fpsDropdown;
    public Toggle vsyncToggle;

    private Resolution[] filteredResolutions;

    [Tooltip("Liste des limites FPS disponibles")]
    [SerializeField] private int[] fpsOptions = { 10, 15, 24, 30, 50, 60, 120, 144, 165, 240 };

    void Start()
    {
        SetupResolutions();
        SetupFPS();

        LoadSettings();
    }

    #region Initialisation (Setup)

    void SetupResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (!uniqueResolutions.Any(r => r.width == allResolutions[i].width && r.height == allResolutions[i].height))
            {
                uniqueResolutions.Add(allResolutions[i]);
            }
        }

        filteredResolutions = uniqueResolutions.ToArray();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Length; i++)
        {
            string option = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(option);
        }

        resolutionDropdown.AddOptions(options);
    }

    void SetupFPS()
    {
        fpsDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < fpsOptions.Length; i++)
        {
            options.Add(fpsOptions[i] + " FPS");
        }
        options.Add("Illimité");

        fpsDropdown.AddOptions(options);
    }

    #endregion

    #region Événements UI (Appelés par les boutons/toggles)

    public void SetResolution(int index)
    {
        Resolution res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResIndex", index);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetTargetFPS(int index)
    {
        if (index >= fpsOptions.Length)
        {
            Application.targetFrameRate = -1;
        }
        else
        {
            Application.targetFrameRate = fpsOptions[index];
        }

        PlayerPrefs.SetInt("FPSIndex", index);
        PlayerPrefs.Save();
    }

    #endregion

    #region Sauvegarde et Chargement

    void LoadSettings()
    {
        bool isFull = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFull;
        fullscreenToggle.isOn = isFull;

        int vsync = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount);
        QualitySettings.vSyncCount = vsync;
        vsyncToggle.isOn = (vsync > 0);

        int savedFPSIndex = PlayerPrefs.GetInt("FPSIndex", fpsDropdown.options.Count - 1);
        fpsDropdown.value = savedFPSIndex;
        SetTargetFPS(savedFPSIndex);
        fpsDropdown.RefreshShownValue();

        int defaultResIndex = 0;
        for (int i = 0; i < filteredResolutions.Length; i++)
        {
            if (filteredResolutions[i].width == Screen.currentResolution.width &&
                filteredResolutions[i].height == Screen.currentResolution.height)
            {
                defaultResIndex = i;
                break;
            }
        }

        int savedResIndex = PlayerPrefs.GetInt("ResIndex", defaultResIndex);
        if (savedResIndex >= filteredResolutions.Length) savedResIndex = filteredResolutions.Length - 1;

        resolutionDropdown.value = savedResIndex;
        SetResolution(savedResIndex);
        resolutionDropdown.RefreshShownValue();
    }

    #endregion
}