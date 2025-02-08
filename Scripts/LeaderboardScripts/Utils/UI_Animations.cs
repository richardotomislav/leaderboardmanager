using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimations : MonoBehaviour
{
    public static UIAnimations Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }


    /// <summary>
    ///     Animates the given <paramref name="panel" /> from its current position
    ///     to the specified end position, either opening or closing based on the <paramref name="show" /> flag. <br />
    ///     The animation transitions from the <paramref name="initialPosition" /> to the <paramref name="targetPosition" />
    ///     <br />
    ///     over the specified <paramref name="duration" /> using an in-out cubic easing function. <br />
    /// </summary>
    /// <param name="panel">The panel to animate.</param>
    /// <param name="show">
    ///     If show is true, the panel animates from initialPosition to targetPosition (show). If
    ///     false, it animates from targetPosition to initialPosition (hide).
    /// </param>
    /// <param name="targetPosition">The target position to animate to when opening.</param>
    /// <param name="initialPosition">The initial position to animate to when closing.</param>
    /// <param name="duration">The duration of the animation, defaulting to 0.5 seconds.</param>
    /// <param name="callback">An optional callback (function) to invoke when the animation completes.</param>
    /// <returns>An IEnumerator for use with a coroutine to animate the panel.</returns>
    public virtual IEnumerator AnimateUIElement(RectTransform panel, bool show, Vector2 targetPosition,
        Vector2 initialPosition, float duration = 0.5f, Action callback = null)
    {
        return AnimateUIElement(panel, show, targetPosition, initialPosition, duration, callback, false);
    }

    /// <summary>
    ///     Animates the given <paramref name="panel" /> from its current position
    ///     to the specified end position, either opening or closing based on the <paramref name="show" /> flag. <br />
    ///     The animation transitions from the <paramref name="initialPosition" /> to the <paramref name="targetPosition" />
    ///     <br />
    ///     over the specified <paramref name="duration" /> using an in-out cubic easing function. <br />
    ///     This overload allows the animation to optionally ignore the <see cref="Time.timeScale" /> by using unscaled time. <br />
    /// </summary>
    /// <param name="panel">The panel to animate.</param>
    /// <param name="show">
    ///     If show is true, the panel animates from initialPosition to targetPosition (show). If
    ///     false, it animates from targetPosition to initialPosition (hide).
    /// </param>
    /// <param name="targetPosition">The target position to animate to when opening.</param>
    /// <param name="initialPosition">The initial position to animate to when closing.</param>
    /// <param name="duration">The duration of the animation, defaulting to 0.5 seconds.</param>
    /// <param name="callback">An optional callback (function) to invoke when the animation completes.</param>
    /// <param name="useUnscaledTime">
    ///     If true, the animation uses unscaled time (ignoring <see cref="Time.timeScale" />). If false, it uses scaled time.
    /// </param>
    /// <returns>An IEnumerator for use with a coroutine to animate the panel.</returns>
    public virtual IEnumerator AnimateUIElement(RectTransform panel, bool show, Vector2 targetPosition,
        Vector2 initialPosition, float duration, Action callback, bool useUnscaledTime)
    {
        float elapsedTime = 0f;
        Vector2 startPosition = show ? initialPosition : targetPosition;
        Vector2 endPosition = show ? targetPosition : initialPosition;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = EasingInOutCubic(t);
            panel.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);
            elapsedTime += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        panel.anchoredPosition = endPosition;
        callback?.Invoke();

        float EasingInOutCubic(float x)
        {
            return x < 0.5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }
    }


    public IEnumerator PanelFader(Image panel, float start, float end, float duration = 0.5f, Action callback = null) //Runto complition beforex
    {
        float counter = 0f;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            panel.color = new Color(panel.color.r, panel.color.g, panel.color.b,
                Mathf.Lerp(start, end, counter / duration));
            // Mathf.Lerp(start, end, counter / duration);

            yield return null; //Because we don't need a return value.
        }

        callback?.Invoke();
    }

    /// <summary>
    /// Fades the alpha of a CanvasGroup over a specified duration.
    /// </summary>
    /// <param name="panel">The CanvasGroup to fade.</param>
    /// <param name="reverse">If true, fades from 1 to 0. If false, fades from 0 to 1.</param>
    /// <param name="duration">The duration of the fade animation, defaulting to 0.5 seconds.</param>
    /// <param name="callback">An optional callback (function) to invoke when the fade animation completes.</param>
    public IEnumerator CanvasFader(CanvasGroup panel, bool reverse, float duration = 0.5f, Action callback = null)
    {
        float counter = 0f;
        float startValue = reverse ? 1f : 0f;
        float endValue = reverse ? 0f : 1f;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            panel.alpha = Mathf.Lerp(startValue, endValue, counter / duration);
            yield return null;
        }

        panel.alpha = endValue; // Ensure final value is exact
        callback?.Invoke();
    }
    /// <remarks>
    /// Actualmente no se usa este método, pero se mantiene por si se necesita en el futuro.
    /// </remarks>
    /// <summary>
    ///     Anima el botón dado escalándolo hacia arriba o hacia abajo durante la duración especificada.
    /// </summary>
    /// <param name="button">El botón a animar.</param>
    /// <param name="sizeUp">Si es true, el botón se escala hacia arriba. Si es false, el botón se escala hacia abajo.</param>
    /// <param name="duration">La duración de la animación, por defecto 0.5 segundos.</param>
    /// <returns>Un IEnumerator para usar con una corrutina para animar el botón.</returns>
    public IEnumerator ButtonAnimations(Button button, bool sizeUp, float duration = 0.5f)
    {
        Vector3 currentScale = button.transform.localScale; // Start from the current scale
        Vector3 targetScale = sizeUp ? Vector3.one * 1.3f : Vector3.one; // Target based on action

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            button.transform.localScale = Vector3.Lerp(currentScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the scale is exactly at the target after the animation
        button.transform.localScale = targetScale;
    }

    public IEnumerator SmoothScrollToEnd(ScrollRect scrollRect, float duration = 0.5f)
    {
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect is not assigned.");
            yield break;
        }

        float elapsedTime = 0f;
        float startValue = scrollRect.verticalNormalizedPosition;
        float endValue = 0f; // 0 represents the bottom in vertical scrolling

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            scrollRect.verticalNormalizedPosition = Util.EaseInOut(startValue, endValue, elapsedTime, duration);
            yield return null;
        }

        // Ensure the final position is exactly at the bottom
        scrollRect.verticalNormalizedPosition = endValue;
    }
}


public static class Util
{
    public static float EaseInOut(float initial, float final, float time, float duration)
    {
        float change = final - initial;
        time /= duration / 2;
        if (time < 1f) return change / 2 * time * time + initial;
        time--;
        return -change / 2 * (time * (time - 2) - 1) + initial;
    }
}
