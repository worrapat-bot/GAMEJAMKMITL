using UnityEngine;
using System.Collections; 

public class TreeController : MonoBehaviour
{
    // ... (ตัวแปรอื่นๆ เหมือนเดิม) ...

    void OnEnable()
    {
        // ลบบรรทัดนี้ชั่วคราว: BreathingTracker.OnDeepBreathDetected += GrowTree;
    }

    void OnDisable()
    {
        // ลบบรรทัดนี้ชั่วคราว: BreathingTracker.OnDeepBreathDetected -= GrowTree;
    }

    void GrowTree()
    {
        // ... (โค้ดสั่งต้นไม้โต) ...
    }

    // ... (ฟังก์ชัน ScaleOverTime) ...
}