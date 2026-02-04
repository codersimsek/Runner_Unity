using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// ==========================================
// GECE-GÜNDÜZ DÖNGÜSÜ YÖNETİCİSİ
// ==========================================
// Bu script oyunda dinamik bir gece-gündüz atmosferi oluşturur
// Post-processing profilleri ve directional light'ı kontrol eder
// ==========================================

public class DayNightCycleManager : MonoBehaviour
{
    [Header("Döngü Ayarları")]
    [Tooltip("Tam bir gece-gündüz döngüsünün süresi (saniye)")]
    [Range(30f, 300f)]
    public float cycleDuration = 100f;

    [Tooltip("Başlangıç zamanı (0=Gece Yarısı, 0.25=Sabah, 0.5=Öğlen, 0.75=Akşam, 1.3=Öğleden sonra)")]
    [Range(0f, 1f)]
    public float startingTimeOfDay = 0.3f; // Öğleden sonra başlangıcı (kullanıcı isteği: 1.3 -> 0.3)

    [Header("Post-Processing Profilleri")]
    [Tooltip("Gündüz post-processing profili")]
    public PostProcessProfile dayProfile;

    [Tooltip("Gün batımı post-processing profili")]
    public PostProcessProfile sunsetProfile;

    [Tooltip("Gece post-processing profili")]
    public PostProcessProfile nightProfile;

    [Header("Işıklandırma")]
    [Tooltip("Ana directional light (güneş)")]
    public Light directionalLight;

    [Tooltip("Gündüz ışık rengi")]
    public Color dayLightColor = new Color(1f, 0.96f, 0.84f); // Hafif sarımsı beyaz

    [Tooltip("Gün batımı ışık rengi")]
    public Color sunsetLightColor = new Color(1f, 0.6f, 0.3f); // Turuncu

    [Tooltip("Gece ışık rengi")]
    public Color nightLightColor = new Color(0.4f, 0.5f, 0.7f); // Mavi-mor

    [Header("Işık Yoğunluğu")]
    [Range(0f, 2f)]
    public float dayIntensity = 1.2f;

    [Range(0f, 2f)]
    public float sunsetIntensity = 0.8f;

    [Range(0f, 2f)]
    public float nightIntensity = 0.4f;

    [Header("Gece Feneri (Player Işığı)")]
    [Tooltip("Player objesinin transform'u (otomatik bulunur)")]
    public Transform playerTransform;

    [Tooltip("Fener etkinleştirme (gece olduğunda otomatik açılır)")]
    public bool enableFlashlight = true;

    [Tooltip("Fener başlama zamanı (0.55=akşam başlangıcı, sadece karanlıkta yanacak)")]
    [Range(0f, 1f)]
    public float flashlightStartTime = 0.55f; // Akşam başlangıcı

    [Tooltip("Fener bitme zamanı (0.25=sabah, gündüz kapalı)")]
    [Range(0f, 1f)]
    public float flashlightEndTime = 0.25f; // Sabah aydınlığı

    [Tooltip("Fener yoğunluğu")]
    [Range(0f, 10f)]
    public float flashlightIntensity = 3f;

    [Tooltip("Fener menzili")]
    [Range(5f, 50f)]
    public float flashlightRange = 25f;

    [Tooltip("Fener açısı (cone angle)")]
    [Range(30f, 120f)]
    public float flashlightAngle = 60f;

    [Tooltip("Fener rengi")]
    public Color flashlightColor = new Color(1f, 0.95f, 0.8f); // Hafif sarımsı beyaz

    [Header("Debug")]
    [Tooltip("Döngüyü hızlandırmak için (test amaçlı)")]
    public bool fastCycle = false;

    [Range(1f, 10f)]
    public float speedMultiplier = 1f;

    // Private değişkenler
    private float currentTime; // 0-1 arası normalize edilmiş zaman
    private PostProcessVolume postProcessVolume;
    private Light playerFlashlight; // Player feneri (dinamik oluşturulacak)

    // ==========================================
    // BAŞLANGIÇ
    // ==========================================
    void Start()
    {
        // Başlangıç zamanını ayarla
        currentTime = startingTimeOfDay;

        // Post-process volume'u bul veya ekle
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        
        if (postProcessVolume == null)
        {
            Debug.LogWarning("DayNightCycle: Post-process volume bulunamadı! Lütfen scene'e ekleyin.");
        }

        // Directional light yoksa otomatik bul
        if (directionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    Debug.Log($"DayNightCycle: Directional light bulundu: {light.name}");
                    break;
                }
            }
        }

        if (directionalLight == null)
        {
            Debug.LogWarning("DayNightCycle: Directional light bulunamadı!");
        }

        // Player objesini bul (yoksa)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("DayNightCycle: Player bulundu: " + player.name);
            }
            else
            {
                Debug.LogWarning("DayNightCycle: Player bulunamadı! 'Player' tag'i ekleyin.");
            }
        }

        // Fener ışığını oluştur
        if (enableFlashlight && playerTransform != null)
        {
            CreatePlayerFlashlight();
        }

        // İlk durumu güncelle
        UpdateCycle();
    }

    // ==========================================
    // HER FRAME
    // ==========================================
    void Update()
    {
        // Zamanı ilerlet
        float speedMult = fastCycle ? speedMultiplier : 1f;
        currentTime += (Time.deltaTime / cycleDuration) * speedMult;

        // 0-1 arasında tut (döngüsel)
        if (currentTime >= 1f)
        {
            currentTime -= 1f;
        }

        // Döngüyü güncelle
        UpdateCycle();
    }

    // ==========================================
    // DÖNGÜYÜ GÜNCELLE
    // ==========================================
    void UpdateCycle()
    {
        UpdateLighting();
        UpdatePostProcessing();
        UpdatePlayerFlashlight();
    }

    // ==========================================
    // IŞIKLANDIRMAYI GÜNCELLE
    // ==========================================
    void UpdateLighting()
    {
        if (directionalLight == null) return;

        // Zamanı 4 bölüme ayır: Gece (0-0.25), Gündüz (0.25-0.5), Akşam (0.5-0.75), Gece (0.75-1)
        Color targetColor;
        float targetIntensity;

        if (currentTime < 0.25f) // Gece -> Sabah geçişi
        {
            float t = currentTime / 0.25f; // 0-1 arası normalize et
            targetColor = Color.Lerp(nightLightColor, dayLightColor, t);
            targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
        }
        else if (currentTime < 0.5f) // Gündüz
        {
            targetColor = dayLightColor;
            targetIntensity = dayIntensity;
        }
        else if (currentTime < 0.75f) // Gündüz -> Akşam geçişi
        {
            float t = (currentTime - 0.5f) / 0.25f;
            targetColor = Color.Lerp(dayLightColor, sunsetLightColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity, sunsetIntensity, t);
        }
        else // Akşam -> Gece geçişi
        {
            float t = (currentTime - 0.75f) / 0.25f;
            targetColor = Color.Lerp(sunsetLightColor, nightLightColor, t);
            targetIntensity = Mathf.Lerp(sunsetIntensity, nightIntensity, t);
        }

        // Yumuşak geçiş için lerp kullan
        directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * 2f);
        directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, Time.deltaTime * 2f);

        // Güneşin gökyüzünde rotasyonu (opsiyonel)
        // float sunAngle = (currentTime - 0.25f) * 360f; // Sabahtan başlat
        // directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
    }

    // ==========================================
    // POST-PROCESSING'İ GÜNCELLE
    // ==========================================
    void UpdatePostProcessing()
    {
        if (postProcessVolume == null) return;
        if (dayProfile == null || sunsetProfile == null || nightProfile == null)
        {
            if (!hasWarned)
            {
                Debug.LogWarning("DayNightCycle: Post-processing profilleri atanmamış!");
                hasWarned = true;
            }
            return;
        }

        // Hangi profillerin blend edilmesi gerektiğini belirle
        PostProcessProfile profile1, profile2;
        float blendFactor;

        if (currentTime < 0.25f) // Gece -> Gündüz
        {
            profile1 = nightProfile;
            profile2 = dayProfile;
            blendFactor = currentTime / 0.25f;
        }
        else if (currentTime < 0.5f) // Gündüz
        {
            profile1 = dayProfile;
            profile2 = dayProfile;
            blendFactor = 0f;
        }
        else if (currentTime < 0.75f) // Gündüz -> Akşam
        {
            profile1 = dayProfile;
            profile2 = sunsetProfile;
            blendFactor = (currentTime - 0.5f) / 0.25f;
        }
        else // Akşam -> Gece
        {
            profile1 = sunsetProfile;
            profile2 = nightProfile;
            blendFactor = (currentTime - 0.75f) / 0.25f;
        }

        // Profil değiştirme
        // Not: Unity'de profiller arası blend için volume weight kullanılır
        // Basit yaklaşım: aktif profili değiştir
        if (blendFactor < 0.5f)
        {
            if (postProcessVolume.profile != profile1)
                postProcessVolume.profile = profile1;
        }
        else
        {
            if (postProcessVolume.profile != profile2)
                postProcessVolume.profile = profile2;
        }

        // Volume weight'i blend faktörüne göre ayarla
        postProcessVolume.weight = 1f; // Her zaman aktif
    }

    private bool hasWarned = false;

    // ==========================================
    // DEBUG: ZAMAN AYARLA (Inspector'dan kullanabilmek için)
    // ==========================================
    [ContextMenu("Sabah Yap")]
    void SetMorning()
    {
        currentTime = 0.25f;
        UpdateCycle();
    }

    [ContextMenu("Öğlen Yap")]
    void SetNoon()
    {
        currentTime = 0.5f;
        UpdateCycle();
    }

    [ContextMenu("Akşam Yap")]
    void SetEvening()
    {
        currentTime = 0.75f;
        UpdateCycle();
    }

    [ContextMenu("Gece Yap")]
    void SetNight()
    {
        currentTime = 0f;
        UpdateCycle();
    }

    // ==========================================
    // PLAYER FENERİNİ OLUŞTUR
    // ==========================================
    void CreatePlayerFlashlight()
    {
        // Daha önce oluşturulmuş mu kontrol et
        if (playerFlashlight != null) return;

        // Yeni GameObject oluştur
        GameObject flashlightObj = new GameObject("PlayerFlashlight");
        flashlightObj.transform.SetParent(playerTransform);
        flashlightObj.transform.localPosition = new Vector3(0f, 1f, 0.5f); // Player'ın biraz üstünde ve önünde
        flashlightObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f); // Hafif aşağı baksın

        // Light komponenti ekle
        playerFlashlight = flashlightObj.AddComponent<Light>();
        playerFlashlight.type = LightType.Spot; // Spotlight (fener tipi)
        playerFlashlight.color = flashlightColor;
        playerFlashlight.intensity = 0f; // Başlangıçta kapalı
        playerFlashlight.range = flashlightRange;
        playerFlashlight.spotAngle = flashlightAngle;
        playerFlashlight.innerSpotAngle = flashlightAngle * 0.5f; // İç açı daha dar
        playerFlashlight.shadows = LightShadows.None; // Performans için shadow yok

        Debug.Log("DayNightCycle: Player feneri oluşturuldu!");
    }

    // ==========================================
    // PLAYER FENERİNİ GÜNCELLE
    // ==========================================
    void UpdatePlayerFlashlight()
    {
        if (!enableFlashlight || playerFlashlight == null) return;

        // BASIT MANTIK: Fener sadece karanlıkta yanar
        // Gündüz: 0.2 - 0.65 arası = KAPALI
        // Akşam/Gece: 0.65 - 1.0 ve 0.0 - 0.2 = AÇIK
        
        bool isNightTime = false;
        const float NIGHT_START = 0.65f; // Akşam başlangıcı
        const float NIGHT_END = 0.2f;    // Sabah sonu
        
        // Gece mi kontrolü (döngüsel aralık)
        if (currentTime >= NIGHT_START || currentTime <= NIGHT_END)
        {
            isNightTime = true;
        }

        
        float targetIntensity = 0f;
        
        if (isNightTime)
        {
            // Gece - fener açık
            float fadeDistance = 0.05f;
            
            // Akşam fade-in (0.65'ten sonra)
            if (currentTime >= NIGHT_START && currentTime <= NIGHT_START + fadeDistance)
            {
                float t = (currentTime - NIGHT_START) / fadeDistance;
                targetIntensity = Mathf.Lerp(0f, flashlightIntensity, t);
            }
            // Sabah fade-out (0.2'den önce)
            else if (currentTime <= NIGHT_END && currentTime >= NIGHT_END - fadeDistance)
            {
                float t = (NIGHT_END - currentTime) / fadeDistance;
                targetIntensity = Mathf.Lerp(0f, flashlightIntensity, t);
            }
            else
            {
                // Tam gece - full yoğunluk
                targetIntensity = flashlightIntensity;
            }
        }
        else
        {
            // Gündüz - fener kapalı
            targetIntensity = 0f;
        }

        // Yumuşak geçiş
        playerFlashlight.intensity = Mathf.Lerp(playerFlashlight.intensity, targetIntensity, Time.deltaTime * 5f);
    }
}
