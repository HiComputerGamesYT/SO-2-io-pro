using UnityEngine;
using System;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;
    public static event Action<GamePhase> OnPhaseChanged;

    [Header("Налаштування часу")]
    public float fullDayDuration = 120f;
    [Range(0f, 1f)]
    public float currentTimeOfDay;

    [Header("Налаштування подій")]
    public float fullMoonChance = 0.25f;
    public float solarEclipseChance = 0.1f;

    [Header("Налаштування Освітлення")]
    public Light2D globalLight;
    public float lightTransitionDuration = 5f;

    [Header("Кольори та Інтенсивність")]
    public Color dayColor = Color.white;
    public float dayIntensity = 1f;
    public Color nightColor = new Color(0.2f, 0.2f, 0.4f, 1f);
    public float nightIntensity = 0.3f;
    public Color fullMoonColor = new Color(0.4f, 0.4f, 0.6f, 1f);
    public float fullMoonIntensity = 0.5f;
    public Color solarEclipseColor = new Color(0.5f, 0.2f, 0.2f, 1f);
    public float solarEclipseIntensity = 0.2f;

    private GamePhase currentPhase;
    private bool isEventActive = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        OnPhaseChanged += HandlePhaseChange;
        HandlePhaseChange(CalculateCurrentPhase());
    }

    void OnDestroy()
    {
        OnPhaseChanged -= HandlePhaseChange;
    }

    void Update()
    {
        currentTimeOfDay += Time.deltaTime / fullDayDuration;
        if (currentTimeOfDay >= 1f)
        {
            currentTimeOfDay = 0;
            isEventActive = false;
        }

        GamePhase newPhase = CalculateCurrentPhase();

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    private void HandlePhaseChange(GamePhase newPhase)
    {
        Debug.Log($"Настала нова фаза: {newPhase}");
        switch (newPhase)
        {
            case GamePhase.Day:
                StartCoroutine(TransitionLight(dayColor, dayIntensity));
                break;
            case GamePhase.Night:
                StartCoroutine(TransitionLight(nightColor, nightIntensity));
                break;
            case GamePhase.FullMoon:
                StartCoroutine(TransitionLight(fullMoonColor, fullMoonIntensity));
                break;
            case GamePhase.SolarEclipse:
                StartCoroutine(TransitionLight(solarEclipseColor, solarEclipseIntensity));
                break;
        }
    }

    private IEnumerator TransitionLight(Color targetColor, float targetIntensity)
    {
        if (globalLight == null) yield break;
        Color startColor = globalLight.color;
        float startIntensity = globalLight.intensity;
        float elapsedTime = 0f;
        while (elapsedTime < lightTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / lightTransitionDuration;
            globalLight.color = Color.Lerp(startColor, targetColor, progress);
            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, progress);
            yield return null;
        }
        globalLight.color = targetColor;
        globalLight.intensity = targetIntensity;
    }

    private GamePhase CalculateCurrentPhase()
    {
        if (currentTimeOfDay > 0.25f && currentTimeOfDay < 0.75f)
        {
            if (!isEventActive && currentTimeOfDay > 0.49f && currentTimeOfDay < 0.51f)
            {
                // ВИПРАВЛЕНО: Чітко вказуємо, який Random використовувати
                if (UnityEngine.Random.value < solarEclipseChance)
                {
                    isEventActive = true;
                    return GamePhase.SolarEclipse;
                }
            }
            return isEventActive ? GamePhase.SolarEclipse : GamePhase.Day;
        }
        else
        {
            if (!isEventActive && (currentTimeOfDay > 0.99f || currentTimeOfDay < 0.01f))
            {
                // ВИПРАВЛЕНО: Чітко вказуємо, який Random використовувати
                if (UnityEngine.Random.value < fullMoonChance)
                {
                    isEventActive = true;
                    return GamePhase.FullMoon;
                }
            }
            return isEventActive ? GamePhase.FullMoon : GamePhase.Night;
        }
    }

    public GamePhase GetCurrentPhase() => currentPhase;
}