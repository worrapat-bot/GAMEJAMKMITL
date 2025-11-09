using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

public class TreeGrowthController : MonoBehaviour
{
    [Header("Growth Settings")]
    [Tooltip("ขนาดที่ต้นไม้จะโตขึ้นทุกครั้งที่ตรวจจับการยกไหล่ได้")]
    [SerializeField] private float growthAmount = 0.1f; 
    [Tooltip("ความเร็วในการเปลี่ยนขนาด (Lerp speed)")]
    [SerializeField] private float growthSpeed = 1.0f; 

    private Vector3 initialScale;
    private Vector3 targetScale;

    void Start()
    {
        initialScale = transform.localScale;
        targetScale = initialScale;
    }

    void OnEnable()
    {
        // สมัครรับ Event จาก PoseLandmarkerRunner
        PoseLandmarkerRunner.OnShoulderLiftDetected += GrowTree;
        PoseLandmarkerRunner.OnShoulderDropDetected += ShrinkTree;
    }

    void OnDisable()
    {
        // ยกเลิกรับ Event เพื่อป้องกัน Memory Leaks
        PoseLandmarkerRunner.OnShoulderLiftDetected -= GrowTree;
        PoseLandmarkerRunner.OnShoulderDropDetected -= ShrinkTree;
    }

    void Update()
    {
        // ค่อยๆ ปรับขนาดต้นไม้ให้เข้าใกล้ขนาดเป้าหมายอย่างราบรื่น (Vector3.Lerp)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * growthSpeed);
    }

    // Method ที่ถูกเรียกเมื่อมีการยกไหล่
    public void GrowTree()
    {
        // ป้องกันไม่ให้ต้นไม้โตเกิน 3 เท่าของขนาดเดิม
        if (targetScale.x < initialScale.x * 3)
        {
            Debug.Log("Tree is growing: + " + growthAmount);
            targetScale += new Vector3(growthAmount, growthAmount, growthAmount);
        }
    }

    // Method ที่ถูกเรียกเมื่อมีการลดไหล่
    public void ShrinkTree()
    {
        // คืนขนาดเป้าหมายกลับไปที่ขนาดเริ่มต้น
        targetScale = initialScale;
    }
}