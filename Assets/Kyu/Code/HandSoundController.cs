using UnityEngine;
using System.Collections;
using System; 

public class HandSoundController : MonoBehaviour
{
    // ... ประกาศตัวแปรอื่นๆ ...
    
    // Coroutine ที่คุณต้องการให้รันบน Main Thread
    private IEnumerator LeftHandSoundRoutine()
    {
        // ... โค้ด Coroutine ของคุณ ...
        Debug.Log("Playing sound on Main Thread.");
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// เมธอดที่ถูกเรียกจาก Background Thread (จาก MediaPipe)
    /// **นี่คือส่วนที่แก้ไขบรรทัดที่ 50 ตาม Log**
    /// </summary>
    public void PlayLeftHandSound() 
    {
        // 1. สร้าง Action ที่บรรจุโค้ดที่ต้องรันบน Main Thread
        Action mainThreadAction = () =>
        {
            // นี่คือโค้ด StartCoroutine ที่ต้องรันบน Main Thread เท่านั้น
            StartCoroutine(LeftHandSoundRoutine()); 
        };
        
        // 2. ส่ง Action นี้เข้าคิวของ MainThreadDispatcher เพื่อรอรันบน Main Thread
        if (MainThreadDispatcher.Instance != null)
        {
            MainThreadDispatcher.Instance.Enqueue(mainThreadAction);
        }
        else
        {
            Debug.LogError("Error: MainThreadDispatcher is not initialized. Please ensure it is set up.");
        }
    }
    
    // ... โค้ดส่วนอื่นๆ ของคลาส HandSoundController ...
}