using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    // ----------------------------------------------------
    //  Public Variables (ตั้งค่าใน Inspector)
    // ----------------------------------------------------
    
    [Header("Target & Position")]
    [Tooltip("ลากวัตถุที่ต้องการให้กล้องล็อคเป้ามาใส่")]
    public Transform target;
    
    [Tooltip("ระยะห่างเริ่มต้นจากเป้าหมาย")]
    public float distance = 5.0f; 

    [Header("Rotation Settings")]
    [Tooltip("ความเร็วในการหมุนกล้องตามแกน X (แนวนอน)")]
    public float xSpeed = 120.0f;
    
    [Tooltip("ความเร็วในการหมุนกล้องตามแกน Y (แนวตั้ง)")]
    public float ySpeed = 120.0f;
    
    [Tooltip("จำกัดมุมสูงสุดและต่ำสุดในการก้มเงย")]
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;

    [Header("Zoom Settings")]
    [Tooltip("ความเร็วในการซูมเข้า/ออก ด้วยล้อเมาส์")]
    public float zoomSpeed = 4f; 
    
    [Tooltip("ระยะซูมสูงสุดและต่ำสุด")]
    public float minDistance = 1f;
    public float maxDistance = 15f;

    // ----------------------------------------------------
    //  Private Variables
    // ----------------------------------------------------
    private float x = 0.0f;
    private float y = 0.0f;

    // ----------------------------------------------------
    //  Core Methods
    // ----------------------------------------------------

    void Start()
    {
        // ตรวจสอบเป้าหมาย
        if (target == null)
        {
            Debug.LogError("กรุณากำหนด Target ให้กับ OrbitCamera!");
            enabled = false; // ปิด Script ถ้าไม่มีเป้าหมาย
            return;
        }

        // อ่านค่ามุมเริ่มต้นของกล้อง
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    // LateUpdate ถูกเรียกหลังจาก Update ทั้งหมด เหมาะสำหรับกล้อง
    void LateUpdate()
    {
        if (target)
        {
            // 1. >>> รับค่า Mouse Input สำหรับการหมุน (Orbit) <<<
            // รับค่าจากเมาส์แกน X (แนวนอน) และแกน Y (แนวตั้ง)
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) 
            {
                // ใช้ GetMouseButton(0) = คลิกซ้าย หรือ GetMouseButton(1) = คลิกขวา เพื่อหมุน
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
            }
            
            // จำกัดมุมก้มเงย (Y-axis)
            y = ClampAngle(y, yMinLimit, yMaxLimit);
            
            // แปลงมุม (x, y) เป็น Quaternion (การหมุน)
            Quaternion rotation = Quaternion.Euler(y, x, 0);

            // 2. >>> รับค่า Scroll Input สำหรับการซูม (Zoom) <<<
            // รับค่าจากล้อเมาส์ (Scroll Wheel)
            distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            
            // จำกัดระยะห่างให้อยู่ในช่วง minDistance ถึง maxDistance
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // 3. >>> คำนวณตำแหน่งสุดท้ายของกล้อง <<<
            // หาจุดออฟเซ็ต: (0, 0, -distance) หมายถึง ถอยหลังไปตามระยะ distance
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            
            // ตำแหน่งกล้อง: (เป้าหมาย + การหมุน) + ออฟเซ็ต
            Vector3 position = rotation * negDistance + target.position;

            // 4. >>> อัปเดตกล้อง <<<
            transform.rotation = rotation;
            transform.position = position;
        }
    }

    // ฟังก์ชันช่วยจำกัดมุม (สำคัญสำหรับการหมุนแบบ Orbit)
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}