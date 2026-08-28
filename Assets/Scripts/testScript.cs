using UnityEngine;

public class testScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        transform.localPosition = new Vector2(3f, 0f); // FESTER Wert, kein Lerp, kein State, nichts
    }



}
