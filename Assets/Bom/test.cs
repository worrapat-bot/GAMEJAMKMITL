using UnityEngine;

public class test : MonoBehaviour
{

    void Start()
    {
        debug.Log("Test script is running.");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            debug.Log("Space key was pressed.");
        }
    }
}
