using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using YARG;

public class ScoreSceneHelper : MonoBehaviour
{
    public RawImage ImageJacket;
    public Text TextTitle;
    public Image ImageLevel;
    public Text TextCombo;
    public Text TextGrading;

    public List<Sprite> DifficultySprites;

    // Start is called before the first frame update
    void Start()
    {
        ImageJacket.texture = GameplayData.Jacket;
        ImageLevel.sprite = DifficultySprites[GameplayData.Difficulty];
        TextTitle.text = GameplayData.PlayingMusic.Name;
        TextCombo.text = GameplayScoreData.MaxCombo.ToString();
        TextGrading.text = $"{GameplayScoreData.PerfectCount:0000}\n{GameplayScoreData.GoodCount:0000}\n{GameplayScoreData.OkayCount:0000}\n{GameplayScoreData.MissCount:0000}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnExitButtonClick()
    {
        GameplayData.ResetData();
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }
}
