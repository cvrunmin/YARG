using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG;
using YARG.Data;

public class FlickNoteActionHelper : MonoBehaviour, NoteActionInterface
{
    GameplayHelper gameplayHelper;
    NoteData noteData;

    SpriteRenderer noteRenderer;
    public SpriteRenderer ArrowRenderer;

    public Sprite UpArrow;
    public Sprite LeftArrow;
    public Sprite RightArrow;

    public void InitializeData(GameplayHelper helper, NoteData data)
    {
        gameplayHelper = helper;
        noteData = data;
    }

    private bool setupNote = false;

    private void setupNoteAppearance()
    {
        RawNoteData rawNoteData = noteData.RawNotes[0];
        noteRenderer.size = new Vector2(rawNoteData.Width, 1);
        if (rawNoteData.Modifiers.Contains(RawNoteData.NoteModifier.FlickLeft))
        {
            ArrowRenderer.sprite = LeftArrow;
        }
        else if (rawNoteData.Modifiers.Contains(RawNoteData.NoteModifier.FlickRight))
        {
            ArrowRenderer.sprite = RightArrow;
        }
        else
        {
            ArrowRenderer.sprite = UpArrow;
        }
        //this.transform.localScale = new Vector3(rawNoteData.Width, 1, 1);
        this.transform.SetPositionAndRotation(new Vector3(-6 + (rawNoteData.LeftPos) + rawNoteData.Width / 2.0f, 60), Quaternion.identity);
        CurrentState = State.InScreen;
        setupNote = true;
    }

    private float viewportBaselineReference;
    private float topReference;
    private Plane railPlane;
    private bool hitted;
    private State CurrentState = FlickNoteActionHelper.State.Pre;

    private TouchData PressTouchData;

    // Start is called before the first frame update
    void Start()
    {
        noteRenderer = GetComponent<SpriteRenderer>();

        railPlane = new Plane(Vector3.forward, new Vector3(0, 0, 0));
        viewportBaselineReference = Camera.main.WorldToViewportPoint(new Vector3(0, 0 + 0.5f)).y;
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 1));
        railPlane.Raycast(ray, out float distance);
        topReference = ray.GetPoint(distance).y + 0.5f;
        if (gameplayHelper != null && noteData != null)
        {
            setupNoteAppearance();
        }
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
        else if(CurrentState != State.Flicked && CurrentState != State.Post && noteData.StartTime - gameplayHelper.GetComplementedCurrentTime() < -140)
        {
            gameplayHelper.ScoreHelper.TriggerMiss();
            Destroy(gameObject);
        }
        else
        {
            float progress = 1 - (float)((noteData.StartTime - gameplayHelper.GetComplementedCurrentTime()) / gameplayHelper.AppearTimeBeforeZero);
            var y = EasingUtils.LinearEase(topReference, 0, progress);
            this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
        }
    }

    public void HandleTouchIn(TouchData touchData)
    {
        if (CurrentState == State.InScreen)
        {
            PressTouchData = touchData.Clone() as TouchData;
            CurrentState = State.Pressed;
            //Debug.Log("Flick track touch");
        }
    }

    public void HandleTouchKeep(TouchData touchData)
    {
        if(CurrentState == State.Pressed)
        {
            if (PressTouchData != null)
            {
                if ((PressTouchData.LastPosition - touchData.LastPosition).sqrMagnitude >= 4)
                {
                    //Debug.Log("Counted as Flicked");
                    CurrentState = State.Flicked;
                }
                else
                {
                    //Debug.Log($"Too short: {(PressTouchData.LastPosition - touchData.LastPosition).sqrMagnitude}");
                }
            }
        }
        else if(CurrentState == State.Flicked)
        {
            if(touchData.Phase == TouchPhase.Stationary || touchData.Phase == TouchPhase.Moved && touchData.DeltaPosition.magnitude < 2)
            {
                Debug.Log("Flick trigger");
                triggerHit();
            }
        }
    }

    public void HandleTouchOut(TouchData touchData)
    {
        if(CurrentState == State.Flicked)
        {
            triggerHit();
        }
    }

    private void triggerHit()
    {
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
        CurrentState = State.Post;
        Destroy(gameObject);
    }

    public Vector2 GetTouchBoundingBox()
    {
        return new Vector2(noteData.RawNotes[0].LeftPos - 1.25f, noteData.RawNotes[0].RightPos + 1.25f);
    }

    internal enum State
    {
        Pre,
        InScreen,
        Pressed,
        Flicked,
        Post
    }
}
