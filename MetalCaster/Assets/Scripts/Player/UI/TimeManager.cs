using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    private Coroutine timeSlow;

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetTime(float desiredTime, float speed)
    {
        if (timeSlow != null) StopCoroutine(timeSlow);
        timeSlow = StartCoroutine(TimeChange(desiredTime, speed));
    }

    private IEnumerator TimeChange(float desiredTime, float speed)
    {
        float velocity = 0;

        while (Mathf.Abs(Time.timeScale - desiredTime) > Mathf.Epsilon)
        {
            Time.timeScale = Mathf.SmoothDamp(Time.timeScale, desiredTime, ref velocity, speed, Mathf.Infinity, Time.unscaledDeltaTime);
            yield return null;
        }

        Time.timeScale = desiredTime;
    }
}
