using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState I { get; private set; }
    public bool IsRunning { get; private set; }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        IsRunning = false;
    }

    public void StartGame() => IsRunning = true;
    public void StopGame() => IsRunning = false;
}
