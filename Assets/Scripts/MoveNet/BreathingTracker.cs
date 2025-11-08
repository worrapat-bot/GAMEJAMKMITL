using UnityEngine;
using MoveNet;
using System.Collections.Generic; // แก้ Error: List<> และ IReadOnlyList<>

public class BreathingTracker : MonoBehaviour
{
    // *** ค่าการจูน ***
    [Header("Smoothing & Sensitivity")]
    public float smoothingFactor = 0.2f;      
    public float breathDepthThreshold = 0.015f; 
    public float minBreathDuration = 2.5f;      
    
    // --- ตัวแปรภายใน ---
    private float smoothedShoulderY = 0f;
    private bool isFirstUpdate = true;
    private float lastBreathTime = 0f;
    private List<float> yHistory = new List<float>();
    
    // Event ให้ TreeController มา Subscribe
    public static event System.Action OnDeepBreathDetected; 
    
    // Index ของจุดไหล่จาก MoveNet (คงที่)
    private const int LEFT_SHOULDER = 5;
    private const int RIGHT_SHOULDER = 6;
    
    // Method ที่ MarkerVisualizer.cs จะเรียกใช้ทุกเฟรม
    // *** แก้ไข: เปลี่ยน Keypoint เป็น MoveNet.Detection ***
    public void AnalyzeDetection(IReadOnlyList<Detection> detections)
    {
        // 1. ตรวจสอบข้อมูล
        if (detections.Count <= RIGHT_SHOULDER) return; 
        
        // เราใช้ .score และ .labelIndex (labelIndex คือ Index ในที่นี้)
        float leftScore = detections[LEFT_SHOULDER].score;
        float rightScore = detections[RIGHT_SHOULDER].score;

        if (leftScore < 0.5f || rightScore < 0.5f) return; // AI ไม่มั่นใจ
        
        // 2. ดึงค่า Y และหาจุดกึ่งกลาง (Y ถูกเก็บใน struct ด้วยชื่อ 'y')
        // *** แก้ไข: ใช้ .y แทน .position.y ***
        float currentCenterY = (detections[LEFT_SHOULDER].y + 
                                detections[RIGHT_SHOULDER].y) / 2f;
        
        // 3. ทำ Smoothing (ทำให้ไหล่นุ่มนวล)
        if (isFirstUpdate)
        {
            smoothedShoulderY = currentCenterY;
            isFirstUpdate = false;
        }
        else
        {
            smoothedShoulderY = Mathf.Lerp(smoothedShoulderY, currentCenterY, smoothingFactor);
        }
        
        // 4. Run Algorithm วิเคราะห์การหายใจ
        RunBreathAlgorithm(smoothedShoulderY);
    }

    private void RunBreathAlgorithm(float currentY)
    {
        // ... (ส่วน Algorithm การหายใจเหมือนเดิมทั้งหมด) ...
        yHistory.Add(currentY);
        if (yHistory.Count > 90) yHistory.RemoveAt(0); 

        if (yHistory.Count < 90) return; 

        float minY = Mathf.Min(yHistory.ToArray());
        float maxY = Mathf.Max(yHistory.ToArray());
        float depth = maxY - minY;

        if (depth > breathDepthThreshold)
        {
            if (Time.time - lastBreathTime > minBreathDuration)
            {
                Debug.Log("DEEP BREATH DETECTED! GROW TREE!");
                OnDeepBreathDetected?.Invoke(); 

                lastBreathTime = Time.time;
                
                int trimSize = yHistory.Count - 45; 
                if (trimSize > 0)
                {
                    yHistory.RemoveRange(0, trimSize);
                }
            }
        }
    }
}