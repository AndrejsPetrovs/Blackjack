using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameManager gm;
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;

    [SerializeField] private UnityEngine.UI.Slider sliderBotCount;
    [SerializeField] private TextMeshProUGUI textBotCount;
    [SerializeField] private UnityEngine.UI.Slider sliderDeckCount;
    [SerializeField] private TextMeshProUGUI textDeckCount;
    [SerializeField] private UnityEngine.UI.Slider sliderReshufflePart;
    [SerializeField] private TextMeshProUGUI textReshufflePart;
    public int activeMenu = 3;

    // Called on every frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            // Move to pause menu or back on Escape press
            if (activeMenu == 0 || activeMenu == 2)
            {
                ToPauseMenu();
            }
            else if (activeMenu == 1)
            {
                ToGame();
            }
        }
    }

    public void ToSettings()
    {
        settingsMenu.SetActive(true);
        startMenu.SetActive(false);
        pauseMenu.SetActive(false);
        activeMenu = 2;
    }

    public void ToPauseMenu()
    {
        pauseMenu.SetActive(true);
        startMenu.SetActive(false);
        settingsMenu.SetActive(false);
        activeMenu = 1;
    }

    public void ToGame()
    {
        pauseMenu.SetActive(false);
        startMenu.SetActive(false);
        settingsMenu.SetActive(false);
        activeMenu = 0;
    }

    public void GameExit()
    {
        Application.Quit();
    }

    public void SetPlayerCount()
    {
        int botCount = (int)sliderBotCount.value;
        textBotCount.text = "Bot Count: " + botCount.ToString();
        gm.playerCount = botCount + 1;
    }

    public void SetDeckCount()
    {
        int deckCount = (int)sliderDeckCount.value;
        textDeckCount.text = "Deck Count: " + deckCount.ToString();
        gm.deck.deckCount = deckCount;
    }
    
    public void SetReshufflePart()
    {
        float reshufflePart = sliderReshufflePart.value;
        textReshufflePart.text = "Reshuffle Deck Part: " + reshufflePart.ToString("F2");
        gm.deck.reshufflePart = reshufflePart;
    }
}
