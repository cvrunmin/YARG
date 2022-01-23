using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YARG;
using YARG.Data;
using System.Linq;
using System;

public class GameplayHelper : MonoBehaviour
{
    public float AppearTimeSecond = 1.0f; //TODO ingame setting

    public float AppearTimeBeforeZero => AppearTimeSecond * 1000; // in milliseconds
    public float chartTuningOffset = 0; //TODO ingame setting

    public AudioSource AudioSource;

    private MusicMeta musicPlaying;
    private float BPM = 120; // TODO: in MusicMeta
    private static readonly int QuarterNoteWidthTick = 480; //TODO: in util
    private static float ToMillisecond(int tick, float bpm)
    {
        return tick * 60000.0f / QuarterNoteWidthTick / bpm;
    }
    public bool Paused { get; private set; }
    public bool Ended { get; private set; }

    private Queue<NoteData> pendingNotes;

    public LinkedList<OnScreenNotePacked> onScreenNotes;

    private double currentFrameSec;
    private double referenceAudioTime;

    public GameObject ClickNotePrefab;
    public GameObject FlickNotePrefab;
    public GameObject SlideNotePrefab;
    public GameObject InGameUI;
    public GameObject PauseMenu;
    public UnityEngine.UI.Text TextGrading;
    public UnityEngine.UI.Button PauseButton;

    public GameplayDisplayHelper DisplayHelper { get; private set; }
    public GameplayScoreHelper ScoreHelper { get; private set; }

    public double GetCurrentPlayTime()
    {
        return (currentFrameSec - referenceAudioTime) * 1000.0;
    }

    public double GetComplementedCurrentTime()
    {
        return (currentFrameSec - referenceAudioTime - musicPlaying.ChartOffset) * 1000.0 - chartTuningOffset;
    }

    private void loadPendingNotes()
    {
        pendingNotes.Clear();
        foreach (var item in onScreenNotes)
        {
            Destroy(item.NoteGameObject);
        }
        onScreenNotes.Clear();
        foreach (var item in GameplayData.NoteData.OrderBy(data => data.StartTime).AsEnumerable())
        {
            pendingNotes.Enqueue(item);
        }
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(2 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(3 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(4 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(5 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(6 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(7 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(8 * 480, BPM), 0, 5, 4) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(9 * 480, BPM), 0, 5, 4) }));

        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(2 * 480, BPM), 0, 1, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(4 * 480, BPM), 0, 4, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(6 * 480, BPM), 0, 7, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(8 * 480, BPM), 0, 10, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(10 * 480, BPM), 0, 7, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(12 * 480, BPM), 0, 4, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(14 * 480, BPM), 0, 1, 3) }));
        //pendingNotes.Enqueue(new NoteData(new List<RawNoteData> { new RawNoteData(RawNoteData.NoteType.Click, ToMillisecond(16 * 480, BPM), 0, 10, 3) }));
    }

    public GameplayHelper(){
        pendingNotes = new Queue<NoteData>();
        onScreenNotes = new LinkedList<OnScreenNotePacked>();
        //loadPendingNotes();
    }

    private void setToStartingPosition()
    {
        //currentFrameSec = -musicPlaying.Padding;
        //currentMs = -ToMillisecond(8 * 480, BPM); //TODO: leave some beat before playing audio
    }

    private void PlayAudio()
    {
        AudioSource.timeSamples = 0;
        referenceAudioTime = AudioSettings.dspTime + musicPlaying.Padding;
        currentFrameSec = AudioSettings.dspTime;
        AudioSource.PlayScheduled(referenceAudioTime);
    }

    void OnDestroy()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        musicPlaying = GameplayData.PlayingMusic;
        DisplayHelper = GetComponent<GameplayDisplayHelper>();
        ScoreHelper = GetComponent<GameplayScoreHelper>();
        foreach (var item in GameplayData.NoteData.OrderBy(data => data.StartTime).AsEnumerable())
        {
            pendingNotes.Enqueue(item);
        }
        AudioSource = GetComponent<AudioSource>();
        AudioSource.clip = GameplayData.MusicClip;
        PlayAudio();
    }

    public void SnapshotPlayingTime()
    {
        if (referenceAudioTime != 0)
        {
            if (AudioSettings.dspTime >= referenceAudioTime)
            {
                referenceAudioTime = 0;
                currentFrameSec = (double)AudioSource.timeSamples / AudioSource.clip.frequency;
            }
            else
            {
                currentFrameSec = AudioSettings.dspTime;
            }
        }
        else
        {
            currentFrameSec = (double)AudioSource.timeSamples / AudioSource.clip.frequency;
        }
    }

    private void RearrangePlayTime()
    {
        AudioSource.timeSamples = Mathf.Max(0, (int)(currentFrameSec * AudioSource.clip.frequency));
    }

    // Update is called once per frame
    void Update()
    {
        if (!Paused)
        {
            SnapshotPlayingTime();
            if(!Ended && onScreenNotes.Count == 0 && pendingNotes.Count == 0 && !AudioSource.isPlaying)
            {
                Ended = true;
                PauseButton.interactable = false;
                DisplayHelper.ShowEndPanel();
            }
            while (true)
            {
                if (pendingNotes.Count <= 0) break;
                var pending = pendingNotes.Peek();
                if (GetComplementedCurrentTime() + AppearTimeBeforeZero >= pending.StartTime)
                {
                    GameObject noteObj;
                    NoteActionInterface actionHelper;
                    if(pending.Type == NoteData.NoteGroupType.Slide)
                    {
                        noteObj = Instantiate(SlideNotePrefab, new Vector3(0, 60, 0), Quaternion.identity);
                        SlideNoteActionHelper slideNoteActionHelper = noteObj.GetComponent<SlideNoteActionHelper>();
                        slideNoteActionHelper.InitializeData(this, pending);
                        actionHelper = slideNoteActionHelper;
                    }
                    else if(pending.Type == NoteData.NoteGroupType.Flick)
                    {
                        noteObj = Instantiate(FlickNotePrefab, new Vector3(0, 60, 0), Quaternion.identity);
                        FlickNoteActionHelper noteActionHelper = noteObj.GetComponent<FlickNoteActionHelper>();
                        noteActionHelper.InitializeData(this, pending);
                        actionHelper = noteActionHelper;
                    }
                    else
                    {
                        noteObj = Instantiate(ClickNotePrefab, new Vector3(0, 60, 0), Quaternion.identity);
                        NoteActionHelper noteActionHelper = noteObj.GetComponent<NoteActionHelper>();
                        noteActionHelper.InitializeData(this, pending);
                        actionHelper = noteActionHelper;
                    }
                    onScreenNotes.AddLast(new OnScreenNotePacked() { NoteData = pending, NoteGameObject = noteObj, ActionHelper = actionHelper });
                    pendingNotes.Dequeue();
                }
                else
                {
                    break; // assume pending notes is ordered by starting time, then if first note is still waiting for timing,
                           // then others must still be waiting
                }
            }
        }
        var node = onScreenNotes.First;
        while(node != null)
        {
            if(node.Value.NoteGameObject == null)
            {
                var n1 = node.Next;
                onScreenNotes.Remove(node);
                node = n1;
            }
            else
            {
                node = node.Next;
            }
        }
    }

    public void HandleTouchEvent(IEnumerable<TouchData> touchDatas)
    {
        var l = touchDatas.ToList();
        List<Tuple<OnScreenNotePacked, TouchData>> AcceptedNotes = new List<Tuple<OnScreenNotePacked, TouchData>>();
        foreach(var item in onScreenNotes.Where(pack => pack.NoteGameObject != null && pack.TrackedTouchData != null))
        {
            var trackee = l.FirstOrDefault(datum => datum.FingerId == item.TrackedTouchData.FingerId);
            if(trackee != null)
            {
                AcceptedNotes.Add(Tuple.Create(item, trackee));
                l.Remove(trackee);
            }
        }
        foreach (var item in l)
        {
            if(item.Phase != TouchPhase.Canceled)
            {
                foreach(var i1 in onScreenNotes.Where(pack => pack.NoteGameObject != null && !AcceptedNotes.Any(tuple => tuple.Item1 == pack) && pack.NoteData.StartTime - GetComplementedCurrentTime() <= 125).OrderBy(pack => pack.NoteData.StartTime).ThenBy(pack => {
                    var vec = pack.ActionHelper.GetTouchBoundingBox();
                    return Mathf.Abs((vec.x + vec.y) / 2 - item.TriggeredChannel);
                }))
                {
                    var vec = i1.ActionHelper.GetTouchBoundingBox();
                    if (vec.x <= item.TriggeredChannel && item.TriggeredChannel <= vec.y)
                    {
                        AcceptedNotes.Add(Tuple.Create(i1, item));
                        break;
                    }
                }
            }
        }
        foreach (var item in AcceptedNotes)
        {
            if(item.Item2.Phase == TouchPhase.Began)
            {
                item.Item1.TrackedTouchData = item.Item2;
                item.Item1.ActionHelper.HandleTouchIn(item.Item2);
            }
            else if(item.Item2.Phase == TouchPhase.Ended)
            {
                item.Item1.TrackedTouchData = null;
                item.Item1.ActionHelper.HandleTouchOut(item.Item2);
            }
            else if(item.Item2.Phase != TouchPhase.Canceled)
            {
                item.Item1.ActionHelper.HandleTouchKeep(item.Item2);
            }
        }
        foreach (var item in l.Except(AcceptedNotes.Select(tuple=>tuple.Item2)))
        {
            if(item.OldTriggeredChannel != item.TriggeredChannel)
            {
                DisplayHelper.TriggerFlash(item.TriggeredChannel);
            }
        } 
    }

    public void OnPauseButtonClick()
    {
        SnapshotPlayingTime();
        Paused = true;
        Debug.Log("pause pressed");
        AudioSource.Pause();
        RearrangePlayTime();
        PauseMenu.SetActive(true);
        InGameUI.SetActive(false);
    }

    public void OnPauseMenuContinueButtonClick()
    {
        PauseMenu.SetActive(false);
        InGameUI.SetActive(true);
        Paused = false;
        //TODO: countdown before the game continue
        AudioSource.Play();
    }

    public void OnRestartButtonClick()
    {
        PauseMenu.SetActive(false);
        InGameUI.SetActive(true);
        Paused = false;
        loadPendingNotes();
        AudioSource.Stop();
        ScoreHelper.ResetScore();
        DisplayHelper.ReshowStartPanel();
        PlayAudio();
    }

    public void OnExitButtonClick()
    {
        AudioSource.Stop();
        pendingNotes.Clear();
        foreach (var item in onScreenNotes)
        {
            Destroy(item.NoteGameObject);
        }
        Destroy(GameObject.Find("LoadingCanvas"));
        SceneManager.LoadSceneAsync("MainMenuScene");
    }

    public void GotoScoreScene()
    {
        ScoreHelper.FinalizeScore();
        SceneManager.LoadScene("ScoreScene");
    }

    public class OnScreenNotePacked
    {
        public NoteData NoteData { get; set; }

        public GameObject NoteGameObject { get; set; }

        public NoteData.NoteGroupType NoteType => NoteData.Type;

        public NoteActionInterface ActionHelper { get; set; }

        public TouchData TrackedTouchData { get; set; }
    }
}
