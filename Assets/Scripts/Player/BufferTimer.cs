using UnityEngine;

[System.Serializable]
public struct BufferTimer
{
    [SerializeField] private float timer;
    public void Start(float duration) => timer = duration;
    public void Tick(float deltaTime)
    {
        if (timer > 0f) 
            timer -= deltaTime;
    }

    public bool IsActive => timer > 0f;

    public void Consume() => timer = -1f;




}