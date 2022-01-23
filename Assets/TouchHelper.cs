using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using YARG.Data;

public class TouchHelper : MonoBehaviour
{
    BoxCollider2D boxCollider;
    List<HighlightFlashHelper> highlights;
    public GameplayHelper GameplayHelper;

    // Start is called before the first frame update
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        highlights = new List<HighlightFlashHelper>();
        for (int i = 1; i <= 12; i++)
        {
            var gameObj = GameObject.Find("ChannelBGHighlight" + i);
            if(gameObj != null)
            {
                highlights.Add(gameObj.GetComponent<HighlightFlashHelper>());
            }
        }
    }

    Dictionary<int, TouchData> touches = new Dictionary<int, TouchData>();
    List<int> touchRemoval = new List<int>();

    // Update is called once per frame
    void Update()
    {
        if (GameplayHelper.Paused) return;
        touchRemoval.ForEach(id => touches.Remove(id));
        touchRemoval.Clear();
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.touches[i];
            if (touch.phase == TouchPhase.Began)
            {
                var ray = Camera.current.ScreenPointToRay(new Vector3(touch.position.x, touch.position.y, 0));
                var hitResult = Physics2D.GetRayIntersection(ray);
                if (hitResult.collider == boxCollider)
                {
                    var xPos = hitResult.point.x - boxCollider.bounds.min.x - 1.0f;
                    var touchData = new TouchData() { FingerId = touch.fingerId, LastPosition = touch.position, Phase = touch.phase };
                    touchData.ChannelPosition = xPos;
                    if (xPos < 0 || xPos >= 11)
                    {
                        touchData.TriggeredChannel = xPos < 0 ? 0 : 11;
                    }
                    else
                    {
                        touchData.TriggeredChannel = Mathf.FloorToInt(xPos);
                    }
                    this.touches.Add(touch.fingerId, touchData);
                }
            }
            else if(touch.phase == TouchPhase.Stationary && touches.ContainsKey(touch.fingerId))
            {
                TouchData touchData = this.touches[touch.fingerId];
                touchData.LastPosition = touch.position;
                touchData.Phase = touch.phase;
                touchData.OldTriggeredChannel = touchData.TriggeredChannel;
                touchData.DeltaPosition = touch.deltaPosition;
            }
            else if(touch.phase == TouchPhase.Moved && touches.ContainsKey(touch.fingerId))
            {
                TouchData touchData = this.touches[touch.fingerId];
                var lastPos = touchData.LastPosition;
                touchData.OldTriggeredChannel = touchData.TriggeredChannel;
                // If no note is hit, try trigger flash
                var ray = Camera.current.ScreenPointToRay(new Vector3(touch.position.x, touch.position.y, 0));
                var hitResult = Physics2D.GetRayIntersection(ray);
                if (hitResult.collider == boxCollider)
                {
                    var xPos = hitResult.point.x - boxCollider.bounds.min.x - 1.0f;
                    touchData.ChannelPosition = xPos;
                    var triggerChannel = xPos < 0 ? 0 : xPos > 12 ? 11 : Mathf.FloorToInt(xPos);
                    if(touchData.TriggeredChannel != triggerChannel)
                    {
                        touchData.TriggeredChannel = triggerChannel;
                    }
                }
                touchData.LastPosition = touch.position;
                touchData.Phase = touch.phase;
                touchData.DeltaPosition = touch.deltaPosition;
            }
            else if(touch.phase == TouchPhase.Ended)
            {
                if (touches.ContainsKey(touch.fingerId))
                {
                    this.touchRemoval.Add(touch.fingerId);
                    TouchData touchData = this.touches[touch.fingerId];
                    touchData.Phase = TouchPhase.Ended;
                }
            }
        }
        GameplayHelper.HandleTouchEvent(this.touches.Values);
        //if (Input.GetMouseButtonDown(((int)MouseButton.LeftMouse)))
        //{
        //    var pos = Input.mousePosition;
        //    var ray = Camera.main.ScreenPointToRay(new Vector3(pos.x, pos.y, 0));
        //    var hitResult = Physics2D.GetRayIntersection(ray);
        //    if (hitResult.collider == boxCollider)
        //    {
        //        var xPos = hitResult.point.x - boxCollider.bounds.min.x - 0.5;
        //        Debug.Log(xPos);
        //    }
        //}
    }
}
