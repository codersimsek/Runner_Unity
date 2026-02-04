using UnityEngine;

public class CoinRotator : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 100f;
    
    void Update()
    {
        // Y ekseninde döndür
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
