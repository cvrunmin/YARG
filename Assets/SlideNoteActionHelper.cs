using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YARG;
using YARG.Data;

public class SlideNoteActionHelper : MonoBehaviour, NoteActionInterface
{
    GameplayHelper gameplayHelper;
    NoteData noteData;

    public GameObject StartNoteObject;
    public GameObject EndNoteObject;
    public GameObject SlideRouteObject;

    private Transform startNoteTransform;
    private Transform endNoteTransform;
    private LineRenderer slideRouteRenderer;

    private Queue<double> checkpoint = new Queue<double>();

    private Vector2 heldRegion = new Vector2(-6, 6);

    public void InitializeData(GameplayHelper helper, NoteData data)
    {
        gameplayHelper = helper;
        noteData = data;
    }

    private bool setupNote = false;

    private void setupNoteAppearance()
    {
        RawNoteData startNoteData = noteData.SortedRawNotes.First();
        RawNoteData endNoteData = noteData.SortedRawNotes.Last();
        StartNoteObject.GetComponent<SpriteRenderer>().size = new Vector2(startNoteData.Width, 1);
        EndNoteObject.GetComponent<SpriteRenderer>().size = new Vector2(endNoteData.Width, 1);
        this.transform.SetPositionAndRotation(new Vector3(0, 60), Quaternion.identity);
        this.startNoteTransform.localPosition = new Vector3(-6 + (startNoteData.LeftPos) + startNoteData.Width / 2.0f, 0);
        this.heldRegion = new Vector2((startNoteData.LeftPos) - 1.25f, (startNoteData.RightPos) + 1.25f);
        float totalLengthRate = 1 - (float)((noteData.EndTime - noteData.StartTime) / gameplayHelper.AppearTimeBeforeZero);
        this.endNoteTransform.localPosition = new Vector3(-6 + (endNoteData.LeftPos) + endNoteData.Width / 2.0f, EasingUtils.LinearEase(topReference, 0, totalLengthRate));
        //TODO sample route position
        List<Vector2> centerPosition = new List<Vector2>();
        List<float> lineWidths = new List<float>();
        RawNoteData lastData = null;
        var enumerator = noteData.SortedRawNotes
            .Where(datum => datum.Type == RawNoteData.NoteType.SlideCheckpoint || datum.Type == RawNoteData.NoteType.SlideProgramFilledCheckpoint)
            .Select(datum => datum.ActionMs)
            .GetEnumerator();
        while (enumerator.MoveNext())
        {
            checkpoint.Enqueue(enumerator.Current);
        }
        enumerator.Dispose();
        foreach (var item in noteData.SortedRawNotes.Where(datum => datum.Type != RawNoteData.NoteType.SlideProgramFilledCheckpoint))
        {
            if (lastData != null)
            {
                float rate1 = 1 - (float)((lastData.ActionMs - noteData.StartTime) / gameplayHelper.AppearTimeBeforeZero);
                var start = new Vector2(-6 + (lastData.LeftPos) + lastData.Width * 0.5f, EasingUtils.LinearEase(topReference, 0, rate1));
                float rate2 = 1 - (float)((item.ActionMs - noteData.StartTime) / gameplayHelper.AppearTimeBeforeZero);
                var end = new Vector2(-6 + (item.LeftPos) + item.Width * 0.5f, EasingUtils.LinearEase(topReference, 0, rate2));
                var startW = new Vector2(lastData.Width, 0);
                var endW = new Vector2(item.Width, 1);
                if (lastData.Modifiers.Contains(RawNoteData.NoteModifier.SlideEaseIn))
                {
                    for (double i = lastData.ActionMs; i < item.ActionMs; i += 10)
                    {
                        float t = Mathf.InverseLerp((float)lastData.ActionMs, (float)item.ActionMs, (float)i);
                        centerPosition.Add(EasingUtils.CubicBezierEase(start, end, new Vector2(start.x, start.y + 0.5f * (end.y - start.y)), end, t));
                        lineWidths.Add(EasingUtils.CubicBezierEase(startW, endW, new Vector2(startW.x, 0.5f), endW, t).x);
                    }
                }
                else if (lastData.Modifiers.Contains(RawNoteData.NoteModifier.SlideEaseOut))
                {
                    for (double i = lastData.ActionMs; i < item.ActionMs; i += 10)
                    {
                        float t = Mathf.InverseLerp((float)lastData.ActionMs, (float)item.ActionMs, (float)i);
                        centerPosition.Add(EasingUtils.CubicBezierEase(start, end, start, new Vector2(end.x, end.y - 0.5f * (end.y - start.y)), t));
                        lineWidths.Add(EasingUtils.CubicBezierEase(startW, endW, startW, new Vector2(endW.x, 0.5f), t).x);
                    }
                }
                else
                {
                    for (double i = lastData.ActionMs; i < item.ActionMs; i += 10)
                    {
                        float t = Mathf.InverseLerp((float)lastData.ActionMs, (float)item.ActionMs, (float)i);
                        centerPosition.Add(new Vector2(EasingUtils.LinearEase(start.x, end.x, t), EasingUtils.LinearEase(start.y, end.y, t)));
                        lineWidths.Add(EasingUtils.LinearEase(startW.x, endW.x, t));
                    }
                }
            }
            lastData = item;
        }
        slideRouteRenderer.positionCount = centerPosition.Count;
        slideRouteRenderer.SetPositions(centerPosition.Select(vec => new Vector3(vec.x, vec.y)).ToArray());
        slideRouteRenderer.widthCurve = new AnimationCurve(lineWidths.Select((val, idx) => new Keyframe(idx / (float)lineWidths.Count, val)).ToArray());
        setupNote = true;
        CurrentState = State.InScreen;
    }

    private float viewportBaselineReference;
    private float topReference;
    private Plane railPlane;

    private State CurrentState = SlideNoteActionHelper.State.Pre;

    // Start is called before the first frame update
    void Start()
    {
        startNoteTransform = StartNoteObject.GetComponent<Transform>();
        endNoteTransform = EndNoteObject.GetComponent<Transform>();
        slideRouteRenderer = SlideRouteObject.GetComponent<LineRenderer>();

        railPlane = new Plane(Vector3.forward, new Vector3(0, 0, 0));
        viewportBaselineReference = Camera.main.WorldToViewportPoint(new Vector3(0, 0 + 0.5f)).y;
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 1));
        railPlane.Raycast(ray, out float distance);
        topReference = ray.GetPoint(distance).y + 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!setupNote)
        {
            if (gameplayHelper != null && noteData != null)
            {
                setupNoteAppearance();
            }
        }
        else
        {
            if (CurrentState == State.InScreen && gameplayHelper.GetComplementedCurrentTime() - noteData.StartTime >= 125)
            {
                gameplayHelper.ScoreHelper.TriggerMiss();
                CurrentState = State.Loosen;
            }
            if (checkpoint.Count > 0 && (checkpoint.Peek() - gameplayHelper.GetComplementedCurrentTime()) < 16.7)
            {
                checkpoint.Dequeue();
                if (CurrentState == State.Held)
                {
                    gameplayHelper.ScoreHelper.TriggerPerfect();
                }
                else if (CurrentState == State.Loosen)
                {
                    gameplayHelper.ScoreHelper.TriggerMiss();
                }
            }
            if (endNoteTransform.position.y < -3 && CurrentState != State.Post)
            {
                gameplayHelper.ScoreHelper.TriggerMiss();
                Destroy(gameObject);
            }
            else
            {
                float startNoteProgress = 1 - (float)((noteData.StartTime - gameplayHelper.GetComplementedCurrentTime()) / gameplayHelper.AppearTimeBeforeZero);
                float endNoteProgress = 1 - (float)((noteData.EndTime - gameplayHelper.GetComplementedCurrentTime()) / gameplayHelper.AppearTimeBeforeZero);
                //if (startNoteProgress < 1)
                //{
                var y = EasingUtils.LinearEase(topReference, 0, startNoteProgress);
                this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
                //}
                //else
                if (startNoteProgress >= 1)
                {
                    if (endNoteProgress < 1)
                    {
                        //var y1 = EasingUtils.LinearEase(topReference, 0, endNoteProgress);
                        //var vec3 = this.endNoteTransform.localPosition;
                        //vec3.y = y1;
                        //this.endNoteTransform.localPosition = vec3;

                        var en = noteData.SortedRawNotes.Where(datum => datum.Type != RawNoteData.NoteType.SlideProgramFilledCheckpoint);
                        var backNearestNote = en.LastOrDefault(datum => datum.ActionMs <= gameplayHelper.GetComplementedCurrentTime());
                        backNearestNote = backNearestNote ?? en.First();
                        var forwardNearestNote = en.FirstOrDefault(datum => datum.ActionMs > gameplayHelper.GetComplementedCurrentTime());
                        forwardNearestNote = forwardNearestNote ?? en.Last();
                        var t = Mathf.InverseLerp((float)backNearestNote.ActionMs, (float)forwardNearestNote.ActionMs, (float)gameplayHelper.GetComplementedCurrentTime());
                        var start = new Vector2((backNearestNote.LeftPos), 0);
                        var end = new Vector2((forwardNearestNote.LeftPos), 1);
                        var startW = new Vector2(backNearestNote.Width, 0);
                        var endW = new Vector2(forwardNearestNote.Width, 1);
                        float x1, w1;
                        if (backNearestNote.Modifiers.Contains(RawNoteData.NoteModifier.SlideEaseIn))
                        {
                            x1 = EasingUtils.CubicBezierEase(start, end, new Vector2(start.x, start.y + 0.5f), end, t).x;
                            w1 = EasingUtils.CubicBezierEase(startW, endW, new Vector2(startW.x, 0.5f), endW, t).x;
                        }
                        else if (backNearestNote.Modifiers.Contains(RawNoteData.NoteModifier.SlideEaseOut))
                        {
                            x1 = EasingUtils.CubicBezierEase(start, end, start, new Vector2(end.x, 0.5f), t).x;
                            w1 = EasingUtils.CubicBezierEase(startW, endW, startW, new Vector2(endW.x, 0.5f), t).x;
                        }
                        else
                        {
                            x1 = EasingUtils.LinearEase(start.x, end.x, t);
                            w1 = EasingUtils.LinearEase(startW.x, endW.x, t);
                        }
                        x1 = x1 + w1 * 0.5f;
                        StartNoteObject.GetComponent<SpriteRenderer>().size = new Vector2(w1, 1);
                        startNoteTransform.SetPositionAndRotation(new Vector3(-6 + x1, 0), Quaternion.identity);
                        //vec3 = startNoteTransform.localPosition;
                        //vec3.x = x1;
                        //startNoteTransform.localPosition = vec3;
                        heldRegion = new Vector2(x1 - 0.5f * w1 - 1.25f, x1 + 0.5f * w1 + 1.25f);
                    }
                    else
                    {
                        if (StartNoteObject.activeSelf)
                            StartNoteObject.SetActive(false);
                        if (SlideRouteObject.activeSelf)
                            SlideRouteObject.SetActive(false);
                        this.endNoteTransform.localPosition = new Vector3(this.endNoteTransform.localPosition.x, 0);
                        y = EasingUtils.LinearEase(topReference, 0, endNoteProgress);
                        this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
                        RawNoteData rawNoteData = this.noteData.SortedRawNotes.Last();
                        heldRegion = new Vector2(rawNoteData.LeftPos - 1.25f, rawNoteData.RightPos + 1.25f);
                        //var y = EasingUtils.LinearEase(topReference, 0, endNoteProgress);
                        //this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
                    }
                }
                //var y = /*Ease(topReference, 0, progress)*/0;
                //this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
            }
        }
    }

    public void HandleTouchIn(TouchData touchData)
    {
        if (CurrentState == State.InScreen)
        {
            var difference = Mathf.Abs((float)(gameplayHelper.GetComplementedCurrentTime() - noteData.StartTime));
            if (difference <= 45)
            {
                gameplayHelper.ScoreHelper.TriggerPerfect();
            }
            else if (difference <= 90)
            {
                gameplayHelper.ScoreHelper.TriggerGood();
            }
            else if (difference <= 125)
            {
                gameplayHelper.ScoreHelper.TriggerOkay();
            }
        }
        CurrentState = State.Held;
    }

    public void HandleTouchKeep(TouchData touchData)
    {
        if (CurrentState == State.Held && (touchData.ChannelPosition < heldRegion.x || touchData.ChannelPosition > heldRegion.y))
        {
            CurrentState = State.Loosen;
        }
        else if (CurrentState == State.Loosen && (heldRegion.x <= touchData.ChannelPosition && touchData.ChannelPosition <= heldRegion.y))
        {
            CurrentState = State.Held;
        }
    }

    public void HandleTouchOut(TouchData touchData)
    {
        if (noteData.EndTime - gameplayHelper.GetComplementedCurrentTime() >= 125)
        {
            CurrentState = State.Loosen;
        }
        else
        {
            CurrentState = State.Post;
            var difference = ((float)(gameplayHelper.GetComplementedCurrentTime() - noteData.EndTime));
            if (-60 <= difference && difference <= 65)
            {
                gameplayHelper.ScoreHelper.TriggerPerfect();
            }
            else if (-100 <= difference && difference <= 120)
            {
                gameplayHelper.ScoreHelper.TriggerGood();
            }
            else if (-125 <= difference && difference <= 140)
            {
                gameplayHelper.ScoreHelper.TriggerOkay();
            }
            Destroy(gameObject);
        }
    }

    public Vector2 GetTouchBoundingBox()
    {
        return heldRegion;
    }

    internal enum State
    {
        Pre,
        InScreen,
        Held,
        Loosen,
        Post
    }
}
