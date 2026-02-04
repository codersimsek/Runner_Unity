using System.Collections.Generic;
using UnityEngine;

public class GameSpawnManager : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] Transform player; // Oyuncu takibi için
    [SerializeField] GameObject obstaclePrefab; // Varsayılan engel (Yedek)
    [SerializeField] GameObject coinPrefab;     // Coin objesi
    [SerializeField] GameObject[] collectables; // Güçlendiriciler (Magnet, Shield vb.)

    [Header("Görsel Çeşitlilik")]
    [SerializeField] GameObject[] obstaclePrefabs;  // Farklı engel tipleri
    [SerializeField] GameObject[] environmentPrefabs; // Çevre objeleri (Bina, ağaç)
    [SerializeField] GameObject groundPrefab;       // Zemin
    [SerializeField] GameObject fencePrefab;        // Çitler
    
    [Header("Spawn Ayarları")]
    [Tooltip("Oyuncunun ne kadar ilerisini dolduralım?")]
    private float spawnDistanceAhead = 500f; // Inspector override'ı engellemek için private 
    
    [Tooltip("Her bir oyun parçasının (segmentin) uzunluğu")]
    [SerializeField] float segmentLength = 6f; 
    
    [Tooltip("Şerit genişliği (PlayerController ile aynı olmalı)")]
    [SerializeField] float laneDistance = 2f; 
    
    [Tooltip("Coinler arası mesafe")]
    [SerializeField] float coinSpacing = 1.5f;
    
    [Header("Pozisyon Ayarları")]
    [SerializeField] float obstacleYOffset = 0f;    
    [SerializeField] float environmentYOffset = 0f; 
    private float groundYOffset = -2.5f; // Gizli değişken, Inspector bozamaz
    
    // Spawn takibi
    private float nextSpawnZ = -50f; // Başlangıç noktası (Arkayı doldurarak başla)
    
    void Start()
    {
        // Müzik ayarı: Arkaplan müziğinin sürekli dönmesini garanti et
        GameObject musicObj = GameObject.Find("GameMusic");
        if (musicObj != null)
        {
            AudioSource musicSource = musicObj.GetComponent<AudioSource>();
            if (musicSource != null)
            {
                musicSource.loop = true;
                if (!musicSource.isPlaying) musicSource.Play();
            }
        }
        
        // ZORLA 500 YAP (Inspector override'ını ezdir)
        spawnDistanceAhead = 500f;
        Debug.Log($"<color=cyan>[SPAWN SETTINGS]</color> spawnDistanceAhead = {spawnDistanceAhead}, segmentLength = {segmentLength}");
        
        // Pre-spawn: Başlangıçta yeterli segment yaratmuyorsun
        for (int i = 0; i < 100; i++) // 40'tan 100'e çıkardım
        {
            SpawnSegment();
        }
    }
    
    
    void Update()
    {
        // FAILSAFE: Eğer player kaybolduysa tekrar bul
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.LogWarning("Player kaybedilmişti, tekrar bulundu!");
            }
            else
            {
                Debug.LogError("PLAYER BULUNAMIYOR! Tag kontrol edin!");
                return;
            }
        }
        
        // DEBUG: Her frame'de durumu logla
        if (Time.frameCount % 60 == 0) // Her saniye
        {
            Debug.Log($"<color=yellow>[UPDATE]</color> Player Z={player.position.z:F1}, NextSpawn Z={nextSpawnZ:F1}, Should Spawn={(nextSpawnZ < player.position.z + spawnDistanceAhead)}");
        }
        
        // Spawn döngüsü
        int safety = 0;
        while (nextSpawnZ < player.position.z + spawnDistanceAhead && safety < 200)
        {
            SpawnSegment();
            safety++;
        }
        
        if (safety >= 200)
        {
            Debug.LogError($"INFINITE LOOP! Player Z: {player.position.z}, NextSpawn: {nextSpawnZ}");
        }
    }
    
    // PlayerController tarafından çağrılır - ARTIK KULLANILMIYOR, kaldırılabilir
    public void OnPlayerSpeedChange()
    {
        // Boş bırakıldı - hız değişimi artık sorun çıkarmıyor
    }
    
    // DEBUG: Spawn sayacı
    private int spawnCount = 0;
    
    // Tek bir yol parçasını (segment) ve üzerindeki her şeyi oluşturur
    void SpawnSegment()
    {
        spawnCount++;
        float spawnZ = nextSpawnZ + (segmentLength / 2f);
        
        // DEBUG: Her 20 segmentte bir log
        if (spawnCount % 20 == 0)
        {
            Debug.Log($"<color=green>[SPAWN #{spawnCount}]</color> Z={spawnZ:F1}, Player Z={player.position.z:F1}, Gap={(spawnZ - player.position.z):F1}");
        }
        
        // Hangi şeritlerin dolu olduğunu takip et (0=Sol, 1=Orta, 2=Sağ)
        bool[] occupiedLanes = new bool[3]; 
        
        // GÜVENLİ BÖLGE: İlk 5 metrede engel çıkmasın
        if (spawnZ > 5f)
        {
            // 1. Engelleri oluştur
            SpawnObstaclePattern(spawnZ, occupiedLanes);
            
            // 2. Şanslıysak güçlendirici (Collectable) oluştur
            SpawnCollectableInSegment(spawnZ, occupiedLanes);

            // 3. Kalan boş yerlere Coin diz
            SpawnCoinsInFreeLanes(spawnZ, occupiedLanes);
        }
        
        // --- ÇEVRESEL DETAYLAR ---
        
        // Çevre objeleri (Binalar, Ağaçlar)
        SpawnEnvironment(spawnZ);
        
        // Yol kenarı çitleri
        SpawnFences(spawnZ);

        // Zemin (Yolun altı)
        SpawnGround(spawnZ);
        
        // Bir sonraki parça için ilerle
        nextSpawnZ += segmentLength;
    }
    
    // Zemini oluşturur
    void SpawnGround(float zPos)
    {
        if (groundPrefab == null) 
        {
            Debug.LogError("HATA: Ground Prefab atanmamış! Inspector'dan atayın.");
            return;
        }

        Vector3 groundPos = new Vector3(0, groundYOffset, zPos);
        GameObject ground = Instantiate(groundPrefab, groundPos, Quaternion.identity, transform);
        
        // ZAMANA GÖRE YOK ETME KALDIRILDI - Objeler artık kalıcı
    }
    
    // Engelleri rastgele desenlere göre oluşturur
    void SpawnObstaclePattern(float zPos, bool[] occupiedLanes)
    {
        if (obstaclePrefab == null && (obstaclePrefabs == null || obstaclePrefabs.Length == 0)) return;
        
        // KURAL: Her segmentte EN AZ 1 ŞERİT BOŞ OLMALI (Oynanabilirlik için)
        int patternType = Random.Range(1, 7); // 1-6 arası güvenli desenler
        
        switch (patternType)
        {
            case 1: // Sol ve Sağ Dolu (Orta Boş)
                CreateObstacle(-1, zPos); occupiedLanes[0] = true;
                CreateObstacle(1, zPos);  occupiedLanes[2] = true;
                break;
                
            case 2: // Sadece Orta Dolu (Sol ve Sağ Boş)
                CreateObstacle(0, zPos); occupiedLanes[1] = true;
                break;
                
            case 3: // Sadece Sol (Orta ve Sağ Boş)
                CreateObstacle(-1, zPos); occupiedLanes[0] = true;
                break;
                
            case 4: // Sadece Sağ (Sol ve Orta Boş)
                CreateObstacle(1, zPos); occupiedLanes[2] = true;
                break;
                
            case 5: // Sol ve Orta (Sağ Boş)
                CreateObstacle(-1, zPos); occupiedLanes[0] = true;
                CreateObstacle(0, zPos);  occupiedLanes[1] = true;
                break;
                
            case 6: // Sağ ve Orta (Sol Boş)
                CreateObstacle(1, zPos);  occupiedLanes[2] = true;
                CreateObstacle(0, zPos);  occupiedLanes[1] = true;
                break;
        }
    }
    
    // Boş kalan şeritlere Coin doldurur
    void SpawnCoinsInFreeLanes(float zPos, bool[] occupiedLanes)
    {
        if (coinPrefab == null) 
        {
            Debug.LogError("HATA: Coin Prefab atanmamış!");
            return;
        }
        
        // Boş şeritleri tespit et
        List<int> freeLanes = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            // i=0 -> Sol(-1), i=1 -> Orta(0), i=2 -> Sağ(1)
            if (!occupiedLanes[i]) freeLanes.Add(i - 1); 
        }
        
        if (freeLanes.Count == 0) return; // Yer yoksa çık
        
        // Rastgele bir boş şerit seç
        int selectedLane = freeLanes[Random.Range(0, freeLanes.Count)];
        
        // Coin sayısı (4 ile 8 arasında)
        int coinCount = Random.Range(4, 9);
        float startZ = zPos - ((coinCount - 1) * coinSpacing / 2f); // Ortala
        
        for (int i = 0; i < coinCount; i++)
        {
            float coinZ = startZ + (i * coinSpacing);
            float xPos = selectedLane * laneDistance;
            
            Vector3 spawnPos = new Vector3(xPos, 1.0f, coinZ); // Y=1.0 (Daha yukarıda)
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity, transform);
            
            if (coin != null)
            {
                // Dönme efekti yoksa ekle
                CoinRotator rotator = coin.GetComponent<CoinRotator>();
                if (rotator == null) coin.AddComponent<CoinRotator>();
                
                // ZAMANA GÖRE YOK ETME KALDIRILDI
            }
        }
    }
    
    // Belirtilen şeride tek bir engel koyar
    void CreateObstacle(int lane, float zPosition)
    {
        // Havuzdan rastgele bir engel seç
        GameObject prefabToSpawn = obstaclePrefab;
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            prefabToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        }

        if (prefabToSpawn == null) 
        {
            Debug.LogError("HATA: Obstacle Prefab bulunamadı! Inspector'ı kontrol edin.");
            return;
        }
        
        float xPos = lane * laneDistance; 
        Vector3 position = new Vector3(xPos, obstacleYOffset, zPosition);
        
        Instantiate(prefabToSpawn, position, Quaternion.identity, transform);
        // ZAMANA GÖRE YOK ETME KALDIRILDI
    }

    // Yol kenarına rastgele bina/ağaç koyar (Çakışma Önleyici ile)
    void SpawnEnvironment(float zPos)
    {
        if (environmentPrefabs == null || environmentPrefabs.Length == 0) return;

        // SOL TARAF (-35 ile -18 arası)
        TrySpawnDecoration(zPos, -35f, -18f);

        // SAĞ TARAF (+18 ile +35 arası)
        TrySpawnDecoration(zPos, 18f, 35f);
    }

    // Güvenli alan bulup dekorasyon oluşturmayı dener
    void TrySpawnDecoration(float zPos, float minX, float maxX)
    {
        // Her taraf için 2 adet obje denemesi
        for (int i = 0; i < 2; i++)
        {
            GameObject prefab = environmentPrefabs[Random.Range(0, environmentPrefabs.Length)];
            
            // 5 kere güvenli yer bulmayı dene
            for (int attempt = 0; attempt < 5; attempt++)
            {
                float dist = Random.Range(minX, maxX);
                float zOffset = Random.Range(-3f, 3f);
                Vector3 pos = new Vector3(dist, environmentYOffset, zPos + zOffset);
                
                // 4 metre yarıçapında başka bir collider var mı?
                if (!Physics.CheckSphere(pos, 4f)) 
                {
                    Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    Instantiate(prefab, pos, rot, transform);
                    // ZAMANA GÖRE YOK ETME KALDIRILDI
                    break; // Başarılı, döngüden çık
                }
            }
        }
    }
    
    [SerializeField] float fenceLength = 1.5f; 

    // Yol kenarına çit dizer
    void SpawnFences(float zPos)
    {
        if (fencePrefab == null) return;
        
        Quaternion fenceRot = Quaternion.Euler(0, 90, 0); // Yola paralel
        
        float startZ = zPos - (segmentLength / 2f);
        float endZ = zPos + (segmentLength / 2f);
        float currentZ = startZ + (fenceLength / 2f);
        
        while (currentZ < endZ)
        {
            // Sol Çit
            Instantiate(fencePrefab, new Vector3(-4f, 0f, currentZ), fenceRot, transform);
            // ZAMANA GÖRE YOK ETME KALDIRILDI 
            
            // Sağ Çit
            Instantiate(fencePrefab, new Vector3(4f, 0f, currentZ), fenceRot, transform);
            // ZAMANA GÖRE YOK ETME KALDIRILDI 
            
            currentZ += fenceLength;
        }
    }
    
    // Güçlendirici (Power-up) oluşturma
    void SpawnCollectableInSegment(float zPos, bool[] occupiedLanes)
    {
        if (collectables == null || collectables.Length == 0) return;
        
        // %30 şansla oluştur (Her yerde çıkmasın, değerli olsun)
        if (Random.value > 0.3f) return;

        // Boş şerit bul
        List<int> freeLanes = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            if (!occupiedLanes[i]) freeLanes.Add(i - 1);
        }
        
        if (freeLanes.Count == 0) return;
        
        // Seçilen şeride yerleştir
        int selectedLaneIndex = Random.Range(0, freeLanes.Count);
        int selectedLane = freeLanes[selectedLaneIndex];
        
        // Şeridi dolu işaretle ki üstüne coin gelmesin
        occupiedLanes[selectedLane + 1] = true;
        
        GameObject randomCollectable = collectables[Random.Range(0, collectables.Length)];
        
        float xPos = selectedLane * laneDistance;
        Vector3 spawnPos = new Vector3(xPos, 1.0f, zPos); // Havada dursun
        
        Instantiate(randomCollectable, spawnPos, Quaternion.identity, transform);
        // ZAMANA GÖRE YOK ETME KALDIRILDI
    }
}
