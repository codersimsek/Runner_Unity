using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] public GameObject gameStartMenu;
    [SerializeField] public GameObject gameRestartMenu;
    [SerializeField] public TextMeshProUGUI endScore;
    [SerializeField] public TextMeshProUGUI mainScore;
    
    [Header("High Score System")]
    [SerializeField] public TextMeshProUGUI highScoreText;
    [SerializeField] public GameObject newRecordText; // "NEW RECORD!" yazısı
    
    [Header("Health Display")]
    [SerializeField] public TextMeshProUGUI healthText; // Can göstergesi (❤❤❤)
    
    private int highScore = 0;
    private bool newRecordAchieved = false;

    private void Start()
    {
        // High score yükle
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        
        gameStartMenu.SetActive(true);
        gameRestartMenu.SetActive(false);
        
        if (newRecordText != null)
            newRecordText.SetActive(false);
    }

    private void Update()
    {
        // Skor göster
        mainScore.text = "Score : " + playerController.score;
        
        // Can göster (❤❤❤)
        UpdateHealthDisplay();
        
        if (playerController.isDead && !newRecordAchieved)
        {
            gameRestartMenu.SetActive(true);
            endScore.text = "Score : " + playerController.score;
            
            // High score kontrolü
            if (playerController.score > highScore)
            {
                highScore = playerController.score;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
                
                newRecordAchieved = true;
                if (newRecordText != null)
                    newRecordText.SetActive(true);
            }
            
            // High score göster
            if (highScoreText != null)
                highScoreText.text = "Best : " + highScore;
        }
    }
    
    void UpdateHealthDisplay()
    {
        if (healthText == null) return;
        
        // Oyuncu canını basit text olarak göster (font sorunu yok!)
        int health = playerController.Health;
        healthText.text = "HP: " + health;
    }

    public void StartGame()
    {
        playerController.isStart = true;
        playerController.myAnim.SetBool("Run", true);
        gameStartMenu.SetActive(false);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
