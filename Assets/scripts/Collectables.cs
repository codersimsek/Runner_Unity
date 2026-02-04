using UnityEngine;
using DG.Tweening; 

public class Collectables : MonoBehaviour
{
    [Header("Özellikler")]
    public CollectablesEnum collectablesEnum;
    public int toBeAddedScore;
    public int TobeAddedHealth;
    public int toBeAddedSpeed; // SpeedUp -> Yavaşlatma etkisi için
    
    // Cache
    private PlayerController cachedPlayer;
    private bool isBeingMagnetized = false; 

    private void Start()
    {
        // Player'ı bir kere bul ve sakla
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<PlayerController>();
        }
    }
    
    private void Update()
    {
        if (cachedPlayer == null) return;

        // 1. Mıknatıs Etkisi Kontrolü
        if (collectablesEnum == CollectablesEnum.Coin && !isBeingMagnetized)
        {
            // Player'da mıknatıs aktif mi?
            if (cachedPlayer.isMagnetActive)
            {
                // Mesafe kontrolü (12 birim)
                float distance = Vector3.Distance(cachedPlayer.transform.position, transform.position);
                if (distance < 12f)
                {
                    isBeingMagnetized = true; // Çekilmeye başla
                }
            }
        }
        
        // 2. Çekilme Hareketi
        if (isBeingMagnetized)
        {
            // Hedef: Oyuncunun göğüs hizası
            Vector3 targetPos = cachedPlayer.transform.position + Vector3.up * 1.2f;
            
            // Player'a doğru uç
            float magnetSpeed = 50f; 
            transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);
            
            // 3. Yaklaşınca Topla
            if (Vector3.Distance(transform.position, targetPos) < 0.5f)
            {
                CollectItem();
            }
        }
    }
    
    private void CollectItem()
    {
        if (cachedPlayer != null)
        {
            cachedPlayer.AddScore(toBeAddedScore);
        }
        
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // Animasyonlar karışmasın diye kill et
        transform.DOKill();
    }
}
