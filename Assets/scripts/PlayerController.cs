using UnityEngine;
using DG.Tweening; // Animasyonlar için DOTween kütüphanesi

public class PlayerController : MonoBehaviour
{
    [Header("Fizik ve Hareket Ayarları")]
    [SerializeField] Rigidbody rb;
    [SerializeField] public Animator myAnim;
    
    [Tooltip("Karakterin ileri koşma hızı")]
    public float speed = 10f;
    
    [Tooltip("Şeritler arası mesafe (Sol: -2, Orta: 0, Sağ: 2)")]
    public float laneDistance = 2f; 
    
    [Tooltip("Yanlara geçiş hızı (Manuel hareket için)")]
    public float sideSpeed = 15f;
    
    [Header("DOTween Animasyon Ayarları")]
    [Tooltip("Şerit değiştirme süresi (Saniye)")]
    [SerializeField] float laneSwitchDuration = 0.2f;
    
    [Tooltip("Hareket yumuşatma tipi")]
    [SerializeField] Ease laneEaseType = Ease.InOutQuad;
    
    [Header("Oyun Durumu")]
    [SerializeField] public int score = 0;
    [SerializeField] public int Health = 3; // Başlangıç canı
    
    // Özel değişkenler
    public float floatScore; // Zamanla artan skor sayacı
    public float passedTime; // Hızlanma kontrolü için zaman sayacı
    
    private int currentLane = 0; // Karakterin bulunduğu şerit (-1, 0, 1)
    
    // Durum Kontrolleri
    public bool isDead = false;
    public bool IsDead { get { return isDead; } }
    
    [HideInInspector] public bool isStart; // Oyun başladı mı?
    
    // Power-up Durumları
    private bool is2XActive = false;
    private bool isShieldActive = false;
    public bool isMagnetActive = false; // Collectables tarafından erişilir
    
    private float beforeSpeed; // Yavaşlatma öncesi hızı saklamak için

    [Header("Ses Efektleri (SFX)")]
    [SerializeField] AudioClip bonusSound;
    [SerializeField] AudioClip CoinSound;
    [SerializeField] AudioClip DeathSound;
    [SerializeField] AudioClip MagnetCoinSound;
    [SerializeField] AudioClip ShieldSound;
    [SerializeField] AudioSource PlayerSound;

    [Header("Görsel Efektler (VFX)")]
    [SerializeField] GameObject coinCollectedVFX;
    [SerializeField] GameObject deathVFX;
    [SerializeField] GameObject healthDeclineVFX;
    [SerializeField] GameObject magnetVFX;
    [SerializeField] GameObject wallBreakVFX;
    [SerializeField] GameObject ShieldVFX;

    // Optimizasyon için Cache
    private CameraFollow cachedCameraFollow; 

    void Start()
    {
        // Bileşenleri otomatik bul (Eğer atanmadıysa)
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (myAnim == null) myAnim = GetComponentInChildren<Animator>();
        if (PlayerSound == null) PlayerSound = GetComponent<AudioSource>();

        // Cache: Kamerayı bul ve sakla (Her karede arama yapmamak için)
        if (Camera.main != null)
        {
            cachedCameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        // Rigidbody ayarlarını güvene al
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Animator kök hareketini kapat (Kod ile hareket ettiriyoruz)
        if (myAnim != null)
        {
            myAnim.applyRootMotion = false;
        }
            
        // Hız güvenliği
        if (speed <= 0) speed = 10f;

        // SwipeManager kontrolü (Sahneye otomatik ekle)
        if (FindFirstObjectByType<SwipeManager>() == null)
        {
            GameObject manager = new GameObject("SwipeManager");
            manager.AddComponent<SwipeManager>();
            Debug.Log("SwipeManager sahneye otomatik eklendi.");
        }
    }

    void Update()
    {
        if (!isStart) return; // Oyun başlamadıysa dur
        if (isDead) return;   // Öldüysek dur
        
        MoveCharacter();
        HandleInput();
    }
    
    // Karakterin sürekli ileri hareketini yönetir
    void MoveCharacter()
    {
        // İleri hareket
        transform.position += Vector3.forward * speed * Time.deltaTime;
        
        // Zamanla skor artışı
        floatScore += Time.deltaTime;
        if (is2XActive)
        {
            floatScore += Time.deltaTime; // 2 kat hızlı puan
        }
        
        if (floatScore > 1)
        {
            score += 1;
            floatScore = 0;
        }
        
        // Zamanla oyunun hızlanması (Zorluk artışı)
        passedTime += Time.deltaTime;
        if (passedTime > 5) // Her 5 saniyede bir kontrol
        {
            if (speed < 25f) // Maksimum hız sınırı
            {
                speed += 0.5f;
                passedTime = 0;
            }
        }
    }
    
    // Klavye ve Dokunmatik girişleri dinler
    void HandleInput()
    {
        // SOLA GEÇİŞ (A, Sol Ok veya Sola Kaydırma)
        if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) || SwipeManager.swipeLeft) && currentLane > -1)
        {
            currentLane--;
            MoveToLane();
        }
        
        // SAĞA GEÇİŞ (D, Sağ Ok veya Sağa Kaydırma)
        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) || SwipeManager.swipeRight) && currentLane < 1)
        {
            currentLane++;
            MoveToLane();
        }
    }

    // Hedef şeride yumuşak geçiş yapar
    void MoveToLane()
    {
        // Önceki hareketi iptal et (Seri basışlarda takılmasın)
        transform.DOKill();
        
        float targetX = currentLane * laneDistance;
        transform.DOMoveX(targetX, laneSwitchDuration).SetEase(laneEaseType);
    }
    
    // Ölüm işlemleri
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Oyun Bitti! Karakter Öldü.");
        
        // Animasyon
        if (myAnim != null)
        {
            myAnim.SetTrigger("Death");
            myAnim.SetBool("Run", false);
        }
        
        // Fiziği durdur
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        speed = 0;
        
        // Ölüm Efekti
        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, Quaternion.Euler(-90, 0, 0));
            Destroy(vfx, 2f);
        }
        
        // Ölüm Sesi
        if (PlayerSound != null && DeathSound != null)
        {
            PlayerSound.PlayOneShot(DeathSound);
        }
        
        // UI'a haber verebiliriz (UIManager Update'de kontrol ediyor)
    }
    
    // Çarpışma Kontrolü (Engellerle temas)
    void OnCollisionEnter(Collision other)
    {
        if (isDead) return;

        if (other.gameObject.CompareTag("Obstacle"))
        {
            // Engelden hasar bilgisini al
            Obstacle obstacleScript = other.gameObject.GetComponent<Obstacle>();
            int damage = 1;
            
            if (obstacleScript != null)
            {
                damage = obstacleScript.damage;
            }

            if (isShieldActive)
            {
                // Kalkan varsa engeli yok et, can gitmesin
                
                // Önce Collider'ı kapat ki fizik motoru hesaplamayı kessin
                Collider col = other.collider;
                if(col != null) col.enabled = false;
                
                // Görseli kapat (titreşim olmasın)
                Renderer[] rends = other.gameObject.GetComponentsInChildren<Renderer>();
                foreach(Renderer r in rends) r.enabled = false;

                // Nesneyi biraz gecikmeli yok et (Güvenli yöntem)
                Destroy(other.gameObject, 0.1f);
                
                DeactivateShield(); // Kalkanı harca
                
                // Kalkan kırılma efekti
                if (wallBreakVFX != null)
                {
                    GameObject vfx = Instantiate(wallBreakVFX, other.transform.position, Quaternion.identity);
                    Destroy(vfx, 1f);
                }
            }
            else
            {
                // Hasar al ve sarsıl
                if (cachedCameraFollow != null)
                {
                    cachedCameraFollow.Shake(0.2f, 0.15f);
                }
                
                CheckHealth(damage, other.gameObject);
            }
        }
    }
    
    // Can kontrolü ve güncelleme
    private void CheckHealth(int damage, GameObject obstacle)
    {
        Health -= damage;
        
        // Can düşme efekti
        if (healthDeclineVFX != null)
        {
            GameObject healthvfx = Instantiate(healthDeclineVFX, transform.position, Quaternion.identity, this.transform);
            Destroy(healthvfx, 1f);
        }
        
        // Ölüm veya hayatta kalma durumu
        if (Health <= 0)
        {
            Die();
        }
        else
        {
            // Ölmedik ama çarptık -> Engel yok olsun ki içinden geçmeyelim
            Destroy(obstacle);
            
            if (wallBreakVFX != null)
            {
                GameObject vfx = Instantiate(wallBreakVFX, obstacle.transform.position, Quaternion.identity);
                Destroy(vfx, 1f);
            }
        }
    }
    
    // Toplanabilir objelerle temas (Coin, Powerup)
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("collectable")) 
        {
            Collectables collectables = other.GetComponent<Collectables>();
            if (collectables == null) return;
            
            // Hangi tip obje toplandı?
            switch (collectables.collectablesEnum)
            {
                case CollectablesEnum.Coin:
                    AddScore(collectables.toBeAddedScore);
                    break;

                case CollectablesEnum.Shield:
                    ActivateShield();
                    break;
                    
                case CollectablesEnum.Score2X:
                    ActivateBonus();
                    break;
                    
                case CollectablesEnum.SpeedUp:
                    // SpeedUp objesi oyunu YAVAŞLATARAK avantaj sağlar
                    SlowDown(collectables.toBeAddedSpeed); 
                    break;
                    
                case CollectablesEnum.Health:
                    AddHealth(collectables.TobeAddedHealth);
                    break;
                    
                case CollectablesEnum.Magnet:
                    ActivateMagnet();
                    break;
            }
            
            // Objeyi sahneden kaldır
            Destroy(other.gameObject);
        }
    }
    
    // Skor ekleme ve efektleri
    public void AddScore(int TobeAddedScore)
    {
        // Ses Çal
        if (PlayerSound != null)
        {
            if (isMagnetActive && MagnetCoinSound != null)
            {
                PlayerSound.PlayOneShot(MagnetCoinSound, 0.15f);
            }
            else if (CoinSound != null)
            {
                PlayerSound.PlayOneShot(CoinSound, 0.15f);
            }
        }

        // Görsel Efekt
        if (coinCollectedVFX != null)
        {
            GameObject vfx = Instantiate(coinCollectedVFX, transform.position + new Vector3(0, 1, 0), Quaternion.identity, this.transform);
            Destroy(vfx, 1f);
        }

        // 2X Bonus kontrolü
        if (is2XActive)
        {
            TobeAddedScore *= 2;
        }
        score += TobeAddedScore;
    }
    
    // --- POWER UP FONKSİYONLARI ---

    void ActivateShield()
    {
        if (isShieldActive) return; // Zaten aktifse süreyi uzatabiliriz ama şimdilik çıkalım

        isShieldActive = true;
        
        if (PlayerSound != null && ShieldSound != null)
            PlayerSound.PlayOneShot(ShieldSound);
        
        if (ShieldVFX != null)
        {
             GameObject vfx = Instantiate(ShieldVFX, transform.position, Quaternion.identity, this.transform);
             Destroy(vfx, 5f); // 5 saniye sonra efekti yok et
        }
        
        // 5 saniye sonra kalkanı kapat
        CancelInvoke("DeactivateShield");
        Invoke("DeactivateShield", 5f);
    }

    void DeactivateShield()
    {
        isShieldActive = false;
    }
    
    void AddHealth(int ToBeAddedHealth)
    {
        Health += ToBeAddedHealth;
    }
    
    void ActivateBonus()
    {
        is2XActive = true;
        if (bonusSound != null) AudioSource.PlayClipAtPoint(bonusSound, transform.position);
        
        CancelInvoke("DeActivateBonus");
        Invoke("DeActivateBonus", 5f);
    }

    void DeActivateBonus()
    {
        is2XActive = false;
    }
    
    private void SlowDown(int slowAmount)
    {
        // Eski hızlanma görevini iptal et
        CancelInvoke("BackToOriginalSpeed");
        
        // Mevcut hızı kaydet (Eğer zaten yavaşlamadıysak)
        if (beforeSpeed == 0 || speed > beforeSpeed)
        {
            beforeSpeed = speed;
        }
        
        // Hedef hızı hesapla (Min 5)
        float targetSpeed = Mathf.Max(5f, speed - Mathf.Abs(slowAmount));
        
        // Yavaşlatma animasyonu
        DOTween.To(() => speed, x => speed = x, targetSpeed, 0.5f).SetEase(Ease.OutQuad);
        
        // Kamera efekti
        if (cachedCameraFollow != null) cachedCameraFollow.SetSlowMotionFOV();
        
        // 5 saniye sonra normale dön
        Invoke("BackToOriginalSpeed", 5f);
    }

    void BackToOriginalSpeed()
    {
        if (beforeSpeed == 0) return;
        
        // Eski hıza yumuşak dönüş
        DOTween.To(() => speed, x => speed = x, beforeSpeed, 0.5f)
            .SetEase(Ease.InQuad)
            .OnComplete(() => { beforeSpeed = 0; });
            
        if (cachedCameraFollow != null) cachedCameraFollow.SetNormalFOV();
    }
    
    void ActivateMagnet()
    {
        isMagnetActive = true;
        
        if (magnetVFX != null)
        {
            GameObject vfx = Instantiate(magnetVFX, transform.position, Quaternion.identity, this.transform);
            Destroy(vfx, 5f);
        }
        
        CancelInvoke("DeActivateMagnet");
        Invoke("DeActivateMagnet", 5f);
    }

    void DeActivateMagnet()
    {
        isMagnetActive = false;
    }
}
