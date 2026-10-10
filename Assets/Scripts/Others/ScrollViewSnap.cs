using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollViewSnap : MonoBehaviour, IEndDragHandler
{
    #region VARIABLES

    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    [Header("Snap Settings")]
    private float snapSpeed = 10f;

    [SerializeField]
    private float stopVelocity = 100f;

    [SerializeField]
    private float snapDuration = 0.3f;

    private RectTransform content;
    private RectTransform viewport;
    private Coroutine snapCoroutine;

    private bool isDragging;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.LogError(
                "[ScrollViewSnap] ScrollRect is missing.",
                this
            );
            enabled = false;
            return;
        }

        content = scrollRect.content;

        viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        isDragging = false;
    }

    #endregion


    #region DRAG METHODS

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        StartSnap();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
    }

    #endregion


    #region SNAP METHODS

    private void StartSnap()
    {
        if (!isActiveAndEnabled || content == null || viewport == null)
            return;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        snapCoroutine = StartCoroutine(SnapWhenStopped());
    }

    private IEnumerator SnapWhenStopped()
    {
        // Wait for ScrollRect inertia to slow down.
        while (scrollRect != null &&
               scrollRect.velocity.magnitude > stopVelocity)
        {
            if (isDragging)
            {
                snapCoroutine = null;
                yield break;
            }

            yield return null;
        }

        if (isDragging)
        {
            snapCoroutine = null;
            yield break;
        }

        SnapToClosestItem();
    }

    private void SnapToClosestItem()
    {
        if (content == null || viewport == null)
        {
            snapCoroutine = null;
            return;
        }

        RectTransform closestItem = null;
        float closestDistance = float.MaxValue;

        Vector3 viewportCenter = viewport.TransformPoint(
            viewport.rect.center
        );

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform item =
                content.GetChild(i) as RectTransform;

            if (item == null || !item.gameObject.activeInHierarchy)
                continue;

            Vector3 itemCenter = item.TransformPoint(
                item.rect.center
            );

            Vector3 localOffset = viewport.InverseTransformVector(
                itemCenter - viewportCenter
            );

            float distance = scrollRect.horizontal
                ? Mathf.Abs(localOffset.x)
                : Mathf.Abs(localOffset.y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        if (closestItem != null)
        {
            snapCoroutine = StartCoroutine(
                SmoothSnap(closestItem)
            );
        }
        else
        {
            snapCoroutine = null;
        }
    }

    private IEnumerator SmoothSnap(RectTransform target)
    {
        if (target == null)
        {
            snapCoroutine = null;
            yield break;
        }

        scrollRect.StopMovement();

        Vector2 startPosition = content.anchoredPosition;

        Vector3 viewportCenter = viewport.TransformPoint(
            viewport.rect.center
        );

        Vector3 itemCenter = target.TransformPoint(
            target.rect.center
        );

        Vector3 localOffset = viewport.InverseTransformVector(
            viewportCenter - itemCenter
        );

        Vector2 targetPosition = startPosition;

        if (scrollRect.horizontal)
            targetPosition.x += localOffset.x;

        if (scrollRect.vertical)
            targetPosition.y += localOffset.y;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, snapDuration);

        while (elapsed < duration)
        {
            if (isDragging)
            {
                snapCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth interpolation.
            t = t * t * (3f - 2f * t);

            float speedFactor = Mathf.Max(0.01f, snapSpeed);

            float smoothT = 1f - Mathf.Pow(
                1f - t,
                speedFactor / 10f
            );

            content.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                targetPosition,
                smoothT
            );

            yield return null;
        }

        if (!isDragging)
        {
            content.anchoredPosition = targetPosition;
            scrollRect.StopMovement();
        }

        snapCoroutine = null;
    }

    #endregion
}
