using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using YARG;
using YARG.Data;

public class GameplayDisplayHelper : MonoBehaviour
{
    public GameObject EndPanel;

    public GameObject StartPanel;
    public RawImage ImageJacket;
    public Image ImageJacketBG;
    public Text TextTitle;
    public Image ImageDifficulty;
    public Text TextMusicInfo;

    public GameObject ComboGroup;
    private Animator comboGroupAnimator;
    public Text TextCombo;

    public List<Sprite> DifficultySprites;
    public List<Color> DifficultyColors;

    List<HighlightFlashHelper> highlights;

    // Start is called before the first frame update
    void Start()
    {
        ImageJacket.texture = GameplayData.Jacket;
        ImageDifficulty.sprite = DifficultySprites[GameplayData.Difficulty];
        ImageJacketBG.color = DifficultyColors[GameplayData.Difficulty];
        TextTitle.text = GameplayData.PlayingMusic.Name;
        var infotext = "";
        if (!string.IsNullOrWhiteSpace(GameplayData.PlayingMusic.Composer))
        {
            infotext += $"Composer: {GameplayData.PlayingMusic.Composer}\n";
        }
        if (!string.IsNullOrWhiteSpace(GameplayData.PlayingMusic.Arranger))
        {
            infotext += $"Arranger: {GameplayData.PlayingMusic.Arranger}\n";
        }
        if (!string.IsNullOrWhiteSpace(GameplayData.PlayingMusic.LyricsWriter))
        {
            infotext += $"Lyricist: {GameplayData.PlayingMusic.LyricsWriter}\n";
        }
        if (!string.IsNullOrWhiteSpace(GameplayData.PlayingMusic.Singer))
        {
            infotext += $"Vocal: {GameplayData.PlayingMusic.Singer}\n";
        }
        TextMusicInfo.text = infotext;
        comboGroupAnimator = ComboGroup.GetComponent<Animator>();
        highlights = new List<HighlightFlashHelper>();
        for (int i = 1; i <= 12; i++)
        {
            var gameObj = GameObject.Find("ChannelBGHighlight" + i);
            if (gameObj != null)
            {
                highlights.Add(gameObj.GetComponent<HighlightFlashHelper>());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(StartPanel.activeSelf && StartPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Empty"))
        {
            StartPanel.SetActive(false);
        }
        if(EndPanel.activeSelf && EndPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Base Layer.SplashDone"))
        {
            GetComponent<GameplayHelper>().GotoScoreScene();
        }
    }

    public void ReshowStartPanel()
    {
        StartPanel.SetActive(true);
        StartPanel.GetComponent<Animator>().Play("Base Layer.GameplayStart");
    }

    public void ShowEndPanel()
    {
        EndPanel.SetActive(true);
        EndPanel.GetComponent<Animator>().Play("Base Layer.EndSplash");
    }

    public void UpdateCombo(int combo)
    {
        TextCombo.text = combo.ToString();
        if (!ComboGroup.activeSelf)
        {
            ComboGroup.SetActive(true);
        }
        comboGroupAnimator.Play("Base.Popup");
    }

    public void ResetCombo()
    {
        if (ComboGroup.activeSelf)
        {
            ComboGroup.SetActive(false);
        }
    }

    public void TriggerFlash(int channel)
    {
        if(channel < 0 || channel >= highlights.Count)
        {
            Debug.LogWarning($"try triggering channel {channel}'s flash");
        }
        else
        {
            highlights[channel].TriggerFlash();
        }
    }

    public void TriggerNoteHitFlash(YARG.Data.NoteData noteData)
    {
        RawNoteData rawNoteData = noteData.SortedRawNotes.First();
        for (int i = rawNoteData.LeftPos; i <= rawNoteData.RightPos; i++)
        {
            highlights[i].TriggerNoteHitFlash();
        } 
    }
}
