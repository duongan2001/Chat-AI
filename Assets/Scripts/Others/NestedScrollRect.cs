using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NestedScrollRect : ScrollRect
{
    private ScrollRect parentScrollRect;
    private bool routeToParent = false;

    protected override void Awake()
    {
        base.Awake();
        // Tự động tìm ScrollRect cha gần nhất
        if (transform.parent != null)
        {
            parentScrollRect = transform.parent.GetComponentInParent<ScrollRect>();
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        // Tính toán góc vuốt dựa trên Delta X và Y
        float deltaX = Mathf.Abs(eventData.delta.x);
        float deltaY = Mathf.Abs(eventData.delta.y);

        // Nếu vuốt Dọc nhiều hơn vuốt Ngang -> Chuyển sang Scroll Rect cha
        if (deltaY > deltaX)
        {
            routeToParent = true;
            if (parentScrollRect != null)
            {
                parentScrollRect.OnBeginDrag(eventData);
            }
        }
        else
        {
            routeToParent = false;
            base.OnBeginDrag(eventData);
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (routeToParent && parentScrollRect != null)
        {
            parentScrollRect.OnDrag(eventData);
        }
        else
        {
            base.OnDrag(eventData);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (routeToParent && parentScrollRect != null)
        {
            parentScrollRect.OnEndDrag(eventData);
        }
        else
        {
            base.OnEndDrag(eventData);
        }
        routeToParent = false;
    }
}