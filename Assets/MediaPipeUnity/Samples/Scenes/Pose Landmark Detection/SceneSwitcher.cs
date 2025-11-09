using UnityEngine;
using UnityEngine.SceneManagement; // 🚨 ต้องมี using นี้เสมอสำหรับการจัดการ Scene

public class SceneSwitcher : MonoBehaviour
{
    // กำหนดชื่อ Scene ที่ต้องการเปลี่ยนไปใน Inspector
    [Tooltip("ใส่ชื่อ Scene ที่ต้องการโหลดเมื่อกด F12 (ต้องอยู่ใน Build Settings)")]
    [SerializeField] 
    private string targetSceneName = "Scene2"; // 🚨 แก้ชื่อตรงนี้เป็น Scene ที่คุณต้องการ

    void Update()
    {
        // 1. ตรวจสอบ Input ทุกเฟรม (ใน Update)
        // Input.GetKeyDown(KeyCode.F12) จะเป็นจริงแค่เฟรมเดียวที่กดปุ่มลงไป
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Debug.Log($"Switching to Scene: {targetSceneName}");
            LoadNewScene();
        }
    }

    private void LoadNewScene()
    {
        // 2. ใช้ SceneManager.LoadScene เพื่อโหลด Scene ใหม่
        // LoadSceneMode.Single หมายถึงการแทนที่ Scene ปัจจุบันด้วย Scene ใหม่
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
    
    // 💡 ข้อควรจำ: Scene ที่จะโหลดต้องถูกเพิ่มใน File > Build Settings ก่อนเสมอ
}