using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YARG;
using YARG.Data;
using Cysharp.Threading.Tasks;

public class MainMenuHelper : MonoBehaviour
{
    public GameObject AddMusicPrefab;
    public GameObject MusicItemPrefab;
    public GameObject MusicItemContainer;
    public GameObject MusicSelectionPage;
    public GameObject MusicDetailPanel;
    public GameObject MusicDetailEditPanel;

    public GameObject NoChartPrompt;

    public Button PlayButton;
    public Button EditButton;

    public GameObject LoadingScreen;

    private MusicCollections internalCollections;
    public MusicCollections externalCollections;

    private void Awake()
    {
        DontDestroyOnLoad(LoadingScreen);
    }

    // Start is called before the first frame update
    void Start()
    {
        ReloadMusicCollection();
    }

    public void RefreshMusicListOnly()
    {
        for (int i = 0; i < MusicItemContainer.transform.childCount; i++)
        {
            Destroy(MusicItemContainer.transform.GetChild(i).gameObject);
        }
        foreach (var musicmeta in internalCollections.Musics)
        {
            var gobj = Instantiate(MusicItemPrefab);
            var subhelper = gobj.GetComponent<MusicItemButtonHelper>();
            if (subhelper != null)
            {
                subhelper.InitializeData(this, musicmeta);
            }
            gobj.transform.SetParent(MusicItemContainer.transform, false);
        }
        foreach (var musicmeta in externalCollections.Musics)
        {
            var gobj = Instantiate(MusicItemPrefab);
            var subhelper = gobj.GetComponent<MusicItemButtonHelper>();
            if (subhelper != null)
            {
                subhelper.InitializeData(this, musicmeta);
            }
            gobj.transform.SetParent(MusicItemContainer.transform, false);
        }
        var ab = Instantiate(AddMusicPrefab);
        var abhelp = ab.GetComponent<AddMusicItemHelper>();
        if (abhelp != null)
        {
            abhelp.InitializeData(this);
        }
        ab.transform.SetParent(MusicItemContainer.transform, false);
    }

    public async void ReloadMusicCollection()
    {
        internalCollections = await Utils.ReadInternalMusicData();
        externalCollections = Utils.ReadExternalMusicData();
        RefreshMusicListOnly();
    }

    // Update is called once per frame
    void Update()
    {
        if (inLoadingStatus)
        {
            LoadingCheck();
        }
    }

    private MusicMeta selectedMusic = null;
    public Text TextTitle;
    public RawImage ImageJacket;
    public List<GameObject> LevelToggles;

    private int selectedLevel;
    private bool shouldGenerateChart;

    public void SwitchToMusicDetailPage(MusicMeta meta, bool requestAnimation = true)
    {
        selectedMusic = meta;
        if (requestAnimation)
        {
            Animator animator = MusicSelectionPage.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("NeedsDetail", true);
            }
            animator = MusicDetailPanel.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("NeedsDetail", true);
            }
        }
        TextTitle.text = selectedMusic.Name;
        if (meta.JacketTexture != null)
        {
            ImageJacket.texture = meta.JacketTexture;
        }

        LevelToggles.ForEach(obj => { obj.SetActive(false); obj.GetComponent<Toggle>().isOn = false; });

        foreach (var score in meta.AcceptedCharts)
        {
            LevelToggles[score.Difficulty].SetActive(true);
        }
        var firstActiveToggle = LevelToggles.FirstOrDefault(obj => obj.activeInHierarchy);
        if(firstActiveToggle != null)
        {
            firstActiveToggle.GetComponent<Toggle>().isOn = true;
            selectedLevel = LevelToggles.IndexOf(firstActiveToggle);
            NoChartPrompt.SetActive(false);
            PlayButton.interactable = true;
        }
        else
        {
            NoChartPrompt.SetActive(true);
            PlayButton.interactable = false;
        }
        EditButton.interactable = !meta.IsInternal;
    }

    public void OnDetailPanelBackButtonClick()
    {
        selectedMusic = null;
        Animator animator = MusicSelectionPage.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("NeedsDetail", false);
        }
        animator = MusicDetailPanel.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("NeedsDetail", false);
        }
    }

    public void OnEditButtonClick()
    {
        if(selectedMusic != null && !selectedMusic.IsInternal)
        {
            MusicDetailEditPanel.GetComponent<MusicDetailEditHelper>().StartEditingDetail(selectedMusic);
            MusicDetailEditPanel.SetActive(true);
            MusicDetailPanel.SetActive(false);
        }
    }

    public void EndEdit(MusicMeta newSelection = null, bool returnToList = false)
    {
        MusicDetailEditPanel.SetActive(false);
        MusicDetailPanel.SetActive(true);
        if (returnToList)
        {
            OnDetailPanelBackButtonClick();
        }
        else
        {
            SwitchToMusicDetailPage(newSelection ?? selectedMusic, true);
        }
    }

    public void OnButtonStartClick()
    {
        var firstActiveToggle = LevelToggles.FirstOrDefault(obj => obj.GetComponent<Toggle>().isOn);
        if (firstActiveToggle != null)
        {
            selectedLevel = LevelToggles.IndexOf(firstActiveToggle);
        }
        GameplayData.PlayingMusic = selectedMusic;
        GameplayData.Difficulty = selectedLevel;
        GameplayData.MusicVarient = null; // TODO allow other values
        GameplayData.Jacket = ImageJacket.texture;
        ScoreMeta scoreMeta = selectedMusic.Charts.First(meta => meta.Difficulty == selectedLevel);
        if (!string.IsNullOrWhiteSpace(scoreMeta.FilePath))
        {
            getNoteDataTask = null;
            if (Path.GetExtension(scoreMeta.FilePath).Equals(".sus"))
            {
                if (selectedMusic.IsInternal)
                {
                    LoadingScreen.SetActive(true);
                    LoadingScreen.GetComponent<Animator>().SetBool("show", true);
                    MusicDetailPanel.GetComponent<Animator>().SetBool("NeedsDetail", false);
                    getNoteDataTask = RawSusScore.ReadSekaiSusFromStreamingAssets(scoreMeta.FilePath).ContinueWith(task => {
                        if (task.IsFaulted) return new List<NoteData>();
                        RawSusScore rawSusScore = task.Result;
                        return NoteDataUtils.BakeSusData(rawSusScore);
                    });
                    getAudioClipTask = Utils.LoadStreamingAudio(selectedMusic.AudioPath).AsTask();
                    inLoadingStatus = true;
                }
            }
            else if (Path.GetExtension(scoreMeta.FilePath).Equals(".fbscore"))
            {
                if (selectedMusic.IsInternal)
                {
                    LoadingScreen.SetActive(true);
                    LoadingScreen.GetComponent<Animator>().SetBool("show", true);
                    MusicDetailPanel.GetComponent<Animator>().SetBool("NeedsDetail", false);
                    getNoteDataTask = FrameBasedScore.ReadScoreFromStreamingAssets(scoreMeta.FilePath).ContinueWith(task => {
                        if (task.IsFaulted) return new List<NoteData>();
                        var rawSusScore = task.Result;
                        return NoteDataUtils.BakeNoteData(rawSusScore.Data);
                    });
                    getAudioClipTask = Utils.LoadStreamingAudio(selectedMusic.AudioPath).AsTask();
                    inLoadingStatus = true;
                }
                else
                {
                    LoadingScreen.SetActive(true);
                    LoadingScreen.GetComponent<Animator>().SetBool("show", true);
                    MusicDetailPanel.GetComponent<Animator>().SetBool("NeedsDetail", false);
                    getNoteDataTask = FrameBasedScore.ReadScoreFromFilePath(scoreMeta.FilePath).ContinueWith(task => {
                        if (task.IsFaulted) return new List<NoteData>();
                        var rawSusScore = task.Result;
                        return NoteDataUtils.BakeNoteData(rawSusScore.Data);
                    });
                    getAudioClipTask = Utils.LoadAudioByUrl(Utils.WarpWithFileProtocol(selectedMusic.AudioPath)).AsTask();
                    inLoadingStatus = true;
                }
            }
        }
    }

    private Task<List<NoteData>> getNoteDataTask = null;
    private Task<AudioClip> getAudioClipTask = null;
    private AsyncOperation loadSceneStatus = null;
    private bool noteDataAssigned = false;
    private bool audioClipAssigned = false;
    private bool inLoadingStatus = false;

    private void LoadingCheck()
    {
        if (getNoteDataTask.IsCompleted)
        {
            if (!noteDataAssigned)
            {
                GameplayData.NoteData = getNoteDataTask.Result;
                noteDataAssigned = true;
                if (audioClipAssigned)
                {
                    GameplayData.ReadyToReceive = true;
                }
            }
        }
        if (getAudioClipTask.IsCompleted)
        {
            if (!audioClipAssigned)
            {
                GameplayData.MusicClip = getAudioClipTask.Result;
                audioClipAssigned = true;
                if (noteDataAssigned)
                {
                    GameplayData.ReadyToReceive = true;
                }
            }
        }
        if(noteDataAssigned && audioClipAssigned)
        {
            if (loadSceneStatus == null)
            {
                loadSceneStatus = SceneManager.LoadSceneAsync("GameplayScene", LoadSceneMode.Single);
                loadSceneStatus.completed += op => {
                    LoadingScreen.GetComponent<Animator>().SetBool("show", false);
                    LoadingScreen.SetActive(false);
                    inLoadingStatus = false;
                };
            }
            
            //if (loadSceneStatus.isDone)
            //{
            //    LoadingScreen.GetComponent<Animator>().SetBool("show", false);
            //    LoadingScreen.SetActive(false);
            //    inLoadingStatus = false;
            //}
        }
    }
}
