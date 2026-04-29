using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinAnimator : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float randomSpread = 0.5f;
    [SerializeField] private Vector3 randomRotation = new Vector3(0, 0, 360);
    [SerializeField] private bool debugMode = false;

    private Canvas canvas;

    private void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
    }

    public void AnimateCoinsFlying(
        Vector3 startWorldPos,
        Vector3 endWorldPos,
        int coinCount = 5,
        System.Action onComplete = null)
    {
        if (canvas == null)
        {
            Debug.LogError("[CoinAnimator] Canvas não encontrado");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(PerformCoinAnimation(startWorldPos, endWorldPos, coinCount, onComplete));
    }

    private IEnumerator PerformCoinAnimation(
        Vector3 startWorldPos,
        Vector3 endWorldPos,
        int coinCount,
        System.Action onComplete)
    {
        for (int i = 0; i < coinCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-randomSpread, randomSpread),
                Random.Range(-randomSpread, randomSpread),
                0
            );

            StartCoroutine(AnimateSingleCoin(
                startWorldPos + offset,
                endWorldPos,
                animationDuration - (i * 0.05f)
            ));

            yield return new WaitForSeconds(0.05f);
        }

        yield return new WaitForSeconds(animationDuration);
        onComplete?.Invoke();

        if (debugMode)
            Debug.Log($"[CoinAnimator] Animação de {coinCount} moedas concluída");
    }

    private IEnumerator AnimateSingleCoin(Vector3 startPos, Vector3 endPos, float duration)
    {
        if (coinPrefab == null)
            yield break;

        GameObject coinInstance = Instantiate(coinPrefab);
        RectTransform coinRect = coinInstance.GetComponent<RectTransform>();

        if (coinRect == null)
            coinRect = coinInstance.AddComponent<RectTransform>();

        if (coinInstance.GetComponent<CanvasGroup>() == null)
            coinInstance.AddComponent<CanvasGroup>();

        CanvasGroup canvasGroup = coinInstance.GetComponent<CanvasGroup>();
        coinRect.SetParent(canvas.transform, false);

        Vector3 randomRot = new Vector3(
            Random.Range(-randomRotation.x, randomRotation.x),
            Random.Range(-randomRotation.y, randomRotation.y),
            Random.Range(-randomRotation.z, randomRotation.z)
        );

        coinInstance.transform.Rotate(randomRot);

        Vector3 screenStartPos = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            startPos
        );
        Vector3 screenEndPos = RectTransformUtility.WorldToScreenPoint(
            Camera.main,
            endPos
        );

        coinRect.position = screenStartPos;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = movementCurve.Evaluate(normalizedTime);

            Vector3 currentPos = Vector3.Lerp(screenStartPos, screenEndPos, curveValue);
            coinRect.position = currentPos;

            float alpha = Mathf.Lerp(1f, 0f, normalizedTime);
            canvasGroup.alpha = alpha;

            coinInstance.transform.Rotate(randomRot * Time.deltaTime * 5f);

            yield return null;
        }

        Destroy(coinInstance);
    }
}
