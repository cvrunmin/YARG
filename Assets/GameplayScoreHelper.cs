using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YARG;

public class GameplayScoreHelper : MonoBehaviour
{
    public int PerfectCount { get; private set; }
    public int GoodCount { get; private set; }
    public int OkayCount { get; private set; }
    public int MissCount { get; private set; }

    public int ComboCount { get; private set; }

    public int MaxComboCount { get; private set; }

    public GameplayHelper mainHelper;

    private float lastTriggerTime;

    public void TriggerPerfect()
    {
        PerfectCount++;
        ComboCount++;
        mainHelper.TextGrading.text = "PERFECT";
        lastTriggerTime = Time.unscaledTime;
        mainHelper.TextGrading.GetComponent<Animator>().Play("Base Layer.TextPopup");
        mainHelper.DisplayHelper.UpdateCombo(ComboCount);
    }

    public void TriggerGood()
    {
        GoodCount++;
        ComboCount++;
        mainHelper.TextGrading.text = "GOOD";
        lastTriggerTime = Time.unscaledTime;
        mainHelper.TextGrading.GetComponent<Animator>().Play("Base Layer.TextPopup");
        mainHelper.DisplayHelper.UpdateCombo(ComboCount);
    }

    public void TriggerOkay()
    {
        OkayCount++;
        MaxComboCount = Mathf.Max(MaxComboCount, ComboCount);
        ComboCount = 0;
        mainHelper.TextGrading.text = "OKAY";
        lastTriggerTime = Time.unscaledTime;
        mainHelper.TextGrading.GetComponent<Animator>().Play("Base Layer.TextPopup");
        mainHelper.DisplayHelper.ResetCombo();
    }

    public void TriggerMiss()
    {
        MissCount++;
        MaxComboCount = Mathf.Max(MaxComboCount, ComboCount);
        ComboCount = 0;
        mainHelper.TextGrading.text = "MISS";
        lastTriggerTime = Time.unscaledTime;
        mainHelper.TextGrading.GetComponent<Animator>().Play("Base Layer.TextShake");
        mainHelper.DisplayHelper.ResetCombo();
    }

    public void FinalizeScore()
    {
        GameplayScoreData.PerfectCount = PerfectCount;
        GameplayScoreData.GoodCount = GoodCount;
        GameplayScoreData.OkayCount = OkayCount;
        GameplayScoreData.MissCount = MissCount;
        GameplayScoreData.MaxCombo = Mathf.Max(MaxComboCount, ComboCount);
    }

    public void ResetScore()
    {
        PerfectCount = 0;
        GoodCount = 0;
        OkayCount = 0;
        MissCount = 0;
        ComboCount = 0;
        MaxComboCount = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        mainHelper = GetComponent<GameplayHelper>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.unscaledTime - lastTriggerTime > 2)
        {
            mainHelper.TextGrading.text = "";
        }
    }
}
