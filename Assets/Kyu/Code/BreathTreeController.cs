using UnityEngine;
using UnityEngine.UI;

public class BreathTreeController : MonoBehaviour
{
    public Image breathingCircle;
    public Transform treeParent;
    public GameObject[] treeStages;

    public float inhaleTime = 4f;
    public float exhaleTime = 4f;

    private float breathTimer = 0f;
    private bool isInhale = true;

    private int currentStage = 0;
    private float growthMeter = 0f;
    private float growthThreshold = 5f;  // หายใจครบ 5 รอบจะโต 1 ขั้น

    // ค่า input จาก MediaPipe
    public float shoulderHeight = 0f;
    private float lastShoulderHeight = 0f;

    void Start()
    {
        UpdateTreeVisual();
    }

    void Update()
    {
        DetectBreathFromShoulder();
        AnimateBreathingCircle();
        UpdateGrowth();
    }

    // ✅ ตรวจจับหายใจจากไหล่
    void DetectBreathFromShoulder()
    {
        float diff = shoulderHeight - lastShoulderHeight;

        if (diff > 0.002f)
            OnInhale();
        else if (diff < -0.002f)
            OnExhale();

        lastShoulderHeight = shoulderHeight;
    }

    void OnInhale()
    {
        isInhale = true;
        breathTimer += Time.deltaTime;
    }

    void OnExhale()
    {
        isInhale = false;

        // เมื่อหายใจออกครบ 1 รอบ → เพิ่มค่าการเติบโต
        if (breathTimer >= inhaleTime)
        {
            growthMeter++;
            breathTimer = 0f;
        }
    }

    // ✅ ขยาย/หดวงกลมตามลมหายใจ
    void AnimateBreathingCircle()
    {
        float targetScale = isInhale ? 1.3f : 0.8f;

        breathingCircle.transform.localScale =
            Vector3.Lerp(breathingCircle.transform.localScale,
                         new Vector3(targetScale, targetScale, 1),
                         Time.deltaTime * 2f);
    }

    // ✅ อัปเดตต้นไม้ให้โตตาม GrowthMeter
    void UpdateGrowth()
    {
        if (growthMeter >= growthThreshold)
        {
            growthMeter = 0;
            currentStage++;

            if (currentStage >= treeStages.Length)
                currentStage = treeStages.Length - 1;

            UpdateTreeVisual();
        }
    }

    // ✅ เปลี่ยนโมเดลต้นไม้ตาม Stage
    void UpdateTreeVisual()
    {
        for (int i = 0; i < treeStages.Length; i++)
            treeStages[i].SetActive(i == currentStage);
    }
}
