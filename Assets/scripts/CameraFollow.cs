using UnityEngine;
using DG.Tweening;

public class CameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;

    [Header("Mesafe Ayarları")]
    [Tooltip("Kameranın oyuncudan ne kadar geride ve yukarıda duracağı")]
    public Vector3 offset = new Vector3(0, 5, -8); // Tam arkasında ve yukarısında

    [Header("Takip Pürüzsüzlüğü")]
    [Tooltip("Kamera takibi ne kadar gecikmeli/yumuşak olsun (Düşük = Daha hızlı)")]
    public float smoothTime = 0.25f;
    private Vector3 currentVelocity;

    [Header("Bakış Ayarları")]
    [Tooltip("Kameranın oyuncunun neresine bakacağı (Y ekseninde ofset)")]
    public float lookAtOffset = 1.0f; // Oyuncunun tam ayaklarına değil, biraz beline/kafasına baksın

    [Header("FOV Efektleri")]
    [SerializeField] float baseFOV = 60f;
    [SerializeField] float slowFOV = 50f;
    [SerializeField] float fastFOV = 70f;

    private Camera cam;
    private PlayerController playerController;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Oyuncu bulunamazsa tag ile bulmayı dene
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (cam != null)
        {
            cam.fieldOfView = baseFOV;
        }

        // Başlangıçta direkt konuma git, kayarak gelmesin
        if (player != null)
        {
             transform.position = player.position + offset;
             transform.LookAt(player.position + Vector3.up * lookAtOffset);
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Oyuncu öldüyse takibi bırakabiliriz veya devam ettirebiliriz. 
        // Genelde runnerlarda öldükten sonra da hafifçe takip etmesi veya durması istenir.
        // Şimdilik oyuncu yoksa return ediyoruz.

        // Hedef pozisyon
        Vector3 targetPos = player.position + offset;
        
        // Z ekseni: KESİN VE ANLIK TAKİP (Titremeyi önlemek için)
        // Runner oyunlarında Z ekseninde yumuşatma yapmak zeminin titriyormuş gibi görünmesine neden olur.
        float currentZ = targetPos.z;

        // X ve Y ekseni: YUMUŞAK TAKİP (Şerit değiştirme ve zıplama için)
        float newX = Mathf.SmoothDamp(transform.position.x, targetPos.x, ref currentVelocity.x, smoothTime);
        float newY = Mathf.SmoothDamp(transform.position.y, targetPos.y, ref currentVelocity.y, smoothTime);
        
        // Yeni pozisyonu uygula
        transform.position = new Vector3(newX, newY, currentZ);

        // Oyuncuya bak
        Vector3 targetLookPos = player.position + Vector3.up * lookAtOffset;
        Vector3 direction = targetLookPos - transform.position;
        if(direction != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    // --- Efekt Metotları (Eski yapı korundu) ---

    public void Shake(float amount = 0.5f, float duration = 0.2f)
    {
        transform.DOShakePosition(duration, amount);
    }

    public void SetSlowMotionFOV()
    {
        if (cam != null) cam.DOFieldOfView(slowFOV, 0.5f).SetEase(Ease.OutQuad);
    }

    public void SetNormalFOV()
    {
        if (cam != null) cam.DOFieldOfView(baseFOV, 0.5f).SetEase(Ease.InQuad);
    }

    public void SetFastFOV()
    {
        if (cam != null) cam.DOFieldOfView(fastFOV, 0.3f).SetEase(Ease.OutQuad);
    }
}
