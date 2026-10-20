using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    private const string MasterKey = "Master";
    private const string BgmKey = "BGM";
    private const string SfxKey = "SFX";
    private const float DefaultVolume = 0.3f;

    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private int titleSceneIndex;

    private void Start()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        Init(masterSlider, MasterKey, SetMasterVolume);
        Init(bgmSlider, BgmKey, SetBgmVolume);
        Init(sfxSlider, SfxKey, SetSfxVolume);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePanel();
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Init(Slider slider, string key, UnityEngine.Events.UnityAction<float> apply)
    {
        float volume = PlayerPrefs.GetFloat(key, DefaultVolume);
        if (slider != null)
        {
            slider.SetValueWithoutNotify(volume);
            slider.onValueChanged.AddListener(apply);
        }
        apply(volume);
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(MasterKey, volume);
    }

    public void SetBgmVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmVolume(volume);
        }
        PlayerPrefs.SetFloat(BgmKey, volume);
    }

    public void SetSfxVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(volume);
        }
        PlayerPrefs.SetFloat(SfxKey, volume);
    }

    public void TogglePanel()
    {
        if (settingPanel == null)
        {
            return;
        }

        bool open = !settingPanel.activeSelf;
        settingPanel.SetActive(open);
        Time.timeScale = open ? 0f : 1f;
    }

    public void GoTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
