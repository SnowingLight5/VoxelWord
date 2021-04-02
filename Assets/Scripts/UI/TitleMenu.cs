using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;


public class TitleMenu : MonoBehaviour
{

    public GameObject mainMenuObject;
    public GameObject settingsObject;

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;


    [Header("Settings Menu UI Elements")]
    public Slider viewDistSlider;
    public TextMeshProUGUI viewDistText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseText;
    public Toggle threadingToggle;
    public Toggle chunkAnimToggle;

    Settings settings;

    private void Awake() {
        if(!File.Exists(Application.dataPath + "/settings.json")){
            Debug.Log("No settings file found, creating new one.");

            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.json", jsonExport);
        } else{
            Debug.Log("Settings file found, loading.");

            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.json");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }    
    }

    public void StartGame(){

        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.worldSizeInChunks;
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void EnterSettings(){

        viewDistSlider.value = settings.viewDistance;
        viewDistText.text = "View Distance: " + viewDistSlider.value;
        mouseSlider.value = settings.mouseSensitivy;
        mouseText.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
        threadingToggle.isOn = settings.enableThreading;
        chunkAnimToggle.isOn = settings.enableAnimatedChunks;

        mainMenuObject.SetActive(false);
        settingsObject.SetActive(true);
    }

    public void LeaveSettings(){

        settings.viewDistance = (int) viewDistSlider.value;
        settings.mouseSensitivy = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;
        settings.enableAnimatedChunks = chunkAnimToggle.isOn;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.json", jsonExport);

        mainMenuObject.SetActive(true);
        settingsObject.SetActive(false);
    }

    public void QuitGame(){
        Application.Quit();
    }

    public void UpdateViewDistSlider(){
        viewDistText.text = "View Distance: " + viewDistSlider.value;
    }

    public void UpdateMouseSlider(){
        mouseText.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
    }
}
