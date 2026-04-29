using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnapScrollManager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float snapDuration = 0.5f;
    [SerializeField] private AnimationCurve snapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float snapThreshold = 50f;

    private List<SnapScrollItem> scrollItems = new List<SnapScrollItem>();
    private SnapScrollItem currentSelectedItem;
    private int currentSelectedIndex = -1;
    private Coroutine snapCoroutine;
    private bool isSnapping = false;
    private bool isDragging = false;

    public delegate void OnGameSelected(int itemIndex, string gameId);
    public delegate void OnGameExited(int itemIndex, string gameId);

    public event OnGameSelected GameSelected;
    public event OnGameExited GameExited;

    private void OnEnable()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    }

    private void OnDisable()
    {
        if (scrollRect != null)
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    private void Start()
    {
        InitializeScrollItems();
        SelectItemAtIndex(0);
    }

    private void InitializeScrollItems()
    {
        scrollItems.Clear();
        SnapScrollItem[] items = scrollRect.content.GetComponentsInChildren<SnapScrollItem>();

        for (int i = 0; i < items.Length; i++)
        {
            items[i].Initialize(i);
            scrollItems.Add(items[i]);
        }

        if (scrollItems.Count > 0)
        {
            SelectItemAtIndex(0);
        }
    }

    private void OnScrollValueChanged(Vector2 scrollPosition)
    {
        if (isSnapping || scrollItems.Count == 0)
            return;

        if (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            isDragging = true;
        }
        else if (isDragging)
        {
            isDragging = false;
            SnapToClosestItem();
        }
    }

    private void SnapToClosestItem()
    {
        int closestIndex = GetClosestItemIndex();

        if (closestIndex == currentSelectedIndex)
        {
            SnapToItemAtIndex(closestIndex);
            return;
        }

        SnapToItemAtIndex(closestIndex);
    }

    private int GetClosestItemIndex()
    {
        if (scrollItems.Count == 0)
            return 0;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        RectTransform scrollContent = scrollRect.content;
        RectTransform scrollViewport = scrollRect.viewport ?? (RectTransform)scrollRect.transform;

        Vector3 viewportCenter = scrollViewport.rect.center;
        viewportCenter = scrollViewport.TransformPoint(viewportCenter);

        for (int i = 0; i < scrollItems.Count; i++)
        {
            RectTransform itemRect = scrollItems[i].GetComponent<RectTransform>();
            Vector3 itemWorldPos = itemRect.TransformPoint(itemRect.rect.center);

            float distance = Mathf.Abs(viewportCenter.y - itemWorldPos.y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void SnapToItemAtIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= scrollItems.Count)
            return;

        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        snapCoroutine = StartCoroutine(AnimateSnapToItem(targetIndex));
    }

    private IEnumerator AnimateSnapToItem(int targetIndex)
    {
        isSnapping = true;
        scrollRect.enabled = false;

        RectTransform targetItemRect = scrollItems[targetIndex].GetComponent<RectTransform>();
        RectTransform scrollContent = scrollRect.content;
        RectTransform scrollViewport = scrollRect.viewport ?? (RectTransform)scrollRect.transform;

        Vector3 targetWorldPos = targetItemRect.TransformPoint(targetItemRect.rect.center);
        Vector3 viewportWorldPos = scrollViewport.TransformPoint(scrollViewport.rect.center);

        float targetOffsetY = targetWorldPos.y - viewportWorldPos.y;
        Vector3 startContentPos = scrollContent.anchoredPosition;
        Vector3 targetContentPos = new Vector3(
            scrollContent.anchoredPosition.x,
            scrollContent.anchoredPosition.y - targetOffsetY,
            0
        );

        float elapsedTime = 0f;

        while (elapsedTime < snapDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / snapDuration);
            float easeValue = snapEase.Evaluate(normalizedTime);

            scrollContent.anchoredPosition = Vector3.Lerp(startContentPos, targetContentPos, easeValue);

            yield return null;
        }

        scrollContent.anchoredPosition = targetContentPos;

        SelectItemAtIndex(targetIndex);

        scrollRect.enabled = true;
        isSnapping = false;
    }

    private void SelectItemAtIndex(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= scrollItems.Count)
            return;

        if (currentSelectedIndex == targetIndex)
            return;

        int previousIndex = currentSelectedIndex;
        currentSelectedIndex = targetIndex;

        if (previousIndex >= 0 && previousIndex < scrollItems.Count)
        {
            SnapScrollItem previousItem = scrollItems[previousIndex];
            previousItem.Deselect();

            GameExited?.Invoke(previousIndex, previousItem.GetGameId());
        }

        SnapScrollItem selectedItem = scrollItems[targetIndex];
        selectedItem.Select();
        currentSelectedItem = selectedItem;

        GameSelected?.Invoke(targetIndex, selectedItem.GetGameId());
    }

    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    public SnapScrollItem GetCurrentSelectedItem()
    {
        return currentSelectedItem;
    }

    public SnapScrollItem GetItemAtIndex(int index)
    {
        if (index >= 0 && index < scrollItems.Count)
            return scrollItems[index];

        return null;
    }

    public int GetItemCount()
    {
        return scrollItems.Count;
    }

    public void AddItem(SnapScrollItem item)
    {
        item.Initialize(scrollItems.Count);
        scrollItems.Add(item);
    }

    public void RemoveItemAtIndex(int index)
    {
        if (index >= 0 && index < scrollItems.Count)
        {
            SnapScrollItem removedItem = scrollItems[index];
            scrollItems.RemoveAt(index);

            for (int i = index; i < scrollItems.Count; i++)
            {
                scrollItems[i].Initialize(i);
            }

            if (currentSelectedIndex == index)
            {
                if (scrollItems.Count > 0)
                {
                    SelectItemAtIndex(Mathf.Min(index, scrollItems.Count - 1));
                }
                else
                {
                    currentSelectedIndex = -1;
                    currentSelectedItem = null;
                }
            }
        }
    }

    public void ScrollToItemAtIndex(int index)
    {
        if (index >= 0 && index < scrollItems.Count)
        {
            SnapToItemAtIndex(index);
        }
    }
}
