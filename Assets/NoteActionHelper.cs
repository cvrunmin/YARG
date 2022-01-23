using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YARG;
using YARG.Data;

public class NoteActionHelper : MonoBehaviour, NoteActionInterface
{
    GameplayHelper gameplayHelper;
    NoteData noteData;

    SpriteRenderer noteRenderer;

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
        //this.transform.localScale = new Vector3(rawNoteData.Width, 1, 1);
        this.transform.SetPositionAndRotation(new Vector3(-6 + (rawNoteData.LeftPos) + rawNoteData.Width / 2.0f, 60), Quaternion.identity);
        setupNote = true;
    }

    private float viewportBaselineReference;
    private float topReference;
    private Plane railPlane;
    private bool hitted;

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
        else if(!hitted && this.transform.position.y < -3)
        {
            gameplayHelper.ScoreHelper.TriggerMiss();
            Destroy(gameObject);
        }
        else
        {
            float progress = 1 - (float)((noteData.StartTime - gameplayHelper.GetComplementedCurrentTime()) / gameplayHelper.AppearTimeBeforeZero);
            //float viewportY = Ease(1, viewportBaselineReference, progress);
            //Vector3 viewportPos = new Vector3(0.5f, viewportY);
            //var ray = Camera.main.ViewportPointToRay(viewportPos);
            //railPlane.Raycast(ray, out float distance);
            //var y = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, Ease(1, baselineReference, progress))).y;
            //var y = ray.GetPoint(distance).y;
            var y = Ease(topReference, 0, progress);
            //var offset = Camera.main.WorldToViewportPoint(new Vector3(this.transform.position.x, y)) - Camera.main.WorldToViewportPoint(new Vector3(this.transform.position.x, y - 1f));
            //offset.Scale(new Vector3(0, 1, 1));
            //y = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, viewportY, distance)- offset).y;
            this.transform.SetPositionAndRotation(new Vector3(this.transform.position.x, y), Quaternion.identity);
            //Debug.Log(Camera.main.WorldToViewportPoint(this.transform.position));
        }
    }

    float Ease(float start, float end, float progress)
    {
        var mid = end - start;
        return start + mid * progress/*Mathf.Sin((float)(progress * 0.5 * Mathf.PI))*/;
    }

    public void HandleTouchIn(TouchData touchData)
    {
        var difference = Mathf.Abs((float)(gameplayHelper.GetComplementedCurrentTime() - noteData.StartTime));
        if(difference <= 45)
        {
            gameplayHelper.ScoreHelper.TriggerPerfect();
        }
        else if(difference <= 90)
        {
            gameplayHelper.ScoreHelper.TriggerGood();
        }
        else if(difference <= 125)
        {
            gameplayHelper.ScoreHelper.TriggerOkay();
        }
        hitted = true;
        gameplayHelper.DisplayHelper.TriggerNoteHitFlash(noteData);
        if(gameObject != null)
        Destroy(gameObject);
    }

    public void HandleTouchKeep(TouchData touchData)
    {

    }

    public void HandleTouchOut(TouchData touchData)
    {

    }

    public Vector2 GetTouchBoundingBox()
    {
        return new Vector2(noteData.RawNotes[0].LeftPos - 1.25f, noteData.RawNotes[0].RightPos + 1.25f);
    }
}
