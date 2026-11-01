using UnityEngine;

public class DestroyMeEvent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public void DestroyMe() => Destroy(gameObject);
}
