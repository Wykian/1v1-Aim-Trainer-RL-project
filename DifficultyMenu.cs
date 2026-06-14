using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DifficultyMenu : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown difficultyDropdown;
    public Button startButton;
    public TMP_Text statusText;
    public TMP_Text hotkeyHintText;

    [Header("Scene Names")]
    public string easyScene = "Elimination_Easy";
    public string mediumScene = "Elimination_Medium";
    public string proScene = "Elimination_Pro";

    private int selectedIndex = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new List<string> {
            "-- Select Difficulty --",
            "Easy",
            "Medium",
            "Pro"
        });

        startButton.interactable = false;
        statusText.text = "Press 1-3 or use dropdown to select difficulty!";
        statusText.color = Color.white;

        if (hotkeyHintText != null)
        {
            hotkeyHintText.text = "[1] Easy    [2] Medium    [3] Pro";
        }

        difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        startButton.onClick.AddListener(OnStartClicked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectDifficulty(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectDifficulty(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectDifficulty(3);

        if (Input.GetKeyDown(KeyCode.Return) && selectedIndex != 0)
        {
            LoadSelectedScene();
        }
    }

    void SelectDifficulty(int index)
    {
        selectedIndex = index;
        difficultyDropdown.value = index;

        string[] names = { "", "Easy", "Medium", "Pro" };
        statusText.text = $"[{index}] {names[index]} selected! Press Enter or Start!";
        statusText.color = Color.green;
        startButton.interactable = true;
    }

    void OnDifficultyChanged(int index)
    {
        selectedIndex = index;

        if (index == 0)
        {
            startButton.interactable = false;
            statusText.text = "Press 1-3 or use dropdown to select difficulty!";
            statusText.color = Color.white;
            return;
        }

        string[] names = { "", "Easy", "Medium", "Pro" };
        statusText.text = $"[{index}] {names[index]} selected! Press Enter or Start!";
        statusText.color = Color.green;
        startButton.interactable = true;
    }

    void OnStartClicked()
    {
        if (selectedIndex == 0) return;
        LoadSelectedScene();
    }

    void LoadSelectedScene()
    {
        switch (selectedIndex)
        {
            case 1: SceneManager.LoadScene(easyScene); break;
            case 2: SceneManager.LoadScene(mediumScene); break;
            case 3: SceneManager.LoadScene(proScene); break;
        }
    }
}
