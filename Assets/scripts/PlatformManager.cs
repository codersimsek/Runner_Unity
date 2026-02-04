using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [Header("Platform Ayarları")]
    [Tooltip("Sırasıyla üretilecek yol parçaları")]
    [SerializeField] GameObject[] platformPrefabs;
    
    [Tooltip("Her parçanın uzunluğu (Boşluk olmaması için hassas ayarlanmalı)")]
    [SerializeField] float platformLength = 6f; 
    
    [Tooltip("Oyuncu Referansı")]
    [SerializeField] Transform player;
    
    [Tooltip("Düzen için parent obje")]
    [SerializeField] Transform platformParent;
    
    [Header("Spawn Kontrolü")]
    [Tooltip("Başlangıçta kaç parça üretilsin?")]
    [SerializeField] int startPlatformCount = 50; 
    
    // Spawn takibi
    private float nextSpawnZ = -12f; // Oyuncunun arkasından başla
    private int currentPlatformIndex = 0;
    
    void Start()
    {
        // Oyuna başlarken zemini hazırla
        for (int i = 0; i < startPlatformCount; i++)
        {
            SpawnNextPlatform();
        }
    }
    
    void Update()
    {
        // Oyuncu ilerledikçe yeni yol ekle
        // While döngüsü: Takılma olsa bile boşluk bırakmadan hepsini doldur
        while (player.position.z > nextSpawnZ - (platformLength * 50))
        {
            SpawnNextPlatform();
        }
    }
    
    void SpawnNextPlatform()
    {
        if (platformPrefabs == null || platformPrefabs.Length == 0) return;
        
        // Sıradaki prefab'i seç (Mevcut indeks mod uzunluk)
        GameObject platformToSpawn = platformPrefabs[currentPlatformIndex];
        
        // Oluştur
        Vector3 spawnPos = new Vector3(0, 0, nextSpawnZ);
        GameObject newPlatform = Instantiate(platformToSpawn, spawnPos, Quaternion.identity, platformParent);
        
        // Kendini otomatik yok etme özelliği ekle
        PlatformDestroyer destroyer = newPlatform.AddComponent<PlatformDestroyer>();
        destroyer.player = player;
        
        // Sonraki pozisyonu güncelle
        nextSpawnZ += platformLength;
        
        // İndeksi bir sonrakine kaydır (Döngüsel)
        currentPlatformIndex = (currentPlatformIndex + 1) % platformPrefabs.Length;
    }
}

// Yardımcı Sınıf: Platformları arkada kalınca siler
public class PlatformDestroyer : MonoBehaviour
{
    public Transform player;
    
    void Update()
    {
        // Oyuncu 60 metre uzaklaştıysa sil (Performans için)
        if (player != null && player.position.z - transform.position.z > 60f)
        {
            Destroy(gameObject);
        }
    }
}
