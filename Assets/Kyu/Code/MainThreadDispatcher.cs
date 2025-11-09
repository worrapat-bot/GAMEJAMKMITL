using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour utility to safely execute code on Unity's Main Thread
/// when called from a background thread (like MediaPipe's processing thread).
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    // ใช้ ConcurrentQueue เพื่อความปลอดภัยในการเข้าถึงจากหลายเธรด
    private static readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();
    private static MainThreadDispatcher _instance;
    private static bool _isInitialized = false;

    // Static property สำหรับการเข้าถึงแบบ Singleton
    public static MainThreadDispatcher Instance
    {
        get
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Initializer (ถูกเรียกครั้งเดียว) เพื่อสร้าง GameObject และ Component ถ้ายังไม่มี
    /// </summary>
    private static void Initialize()
    {
        if (_isInitialized) return;

        _instance = FindObjectOfType<MainThreadDispatcher>();

        if (_instance == null)
        {
            // สร้าง GameObject ใหม่ใน Scene และเพิ่ม Component นี้เข้าไป
            GameObject dispatcherObject = new GameObject("[MainThreadDispatcher]");
            _instance = dispatcherObject.AddComponent<MainThreadDispatcher>();
        }

        DontDestroyOnLoad(_instance.gameObject);
        _isInitialized = true;
    }

    private void Awake()
    {
        // ยืนยันความเป็น Singleton และทำลายตัวซ้ำ
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _isInitialized = true;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // ในทุกเฟรมของ Main Thread จะตรวจสอบและรัน Action ทั้งหมดในคิว
        while (_executionQueue.TryDequeue(out Action action))
        {
            action?.Invoke(); // เรียกใช้ Action บน Main Thread
        }
    }

    /// <summary>
    /// เมธอดหลักสำหรับ Background Thread เพื่อส่งโค้ดมายัง Main Thread
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action != null)
        {
            _executionQueue.Enqueue(action);
        }
    }
}