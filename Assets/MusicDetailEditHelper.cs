using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using YARG;
using YARG.Data;
using SimpleFileBrowser;

public class MusicDetailEditHelper : MonoBehaviour
{
    public MainMenuHelper MainHelper;

    public InputField MusicPathTextInput;
    public InputField MusicNameTextInput;
    public GameObject[] ChartButtons;

    private MusicMeta editingMeta;
    private bool isEditing = false;
    private bool hasModified = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartEditingDetail(MusicMeta meta)
    {
        this.editingMeta = meta;
        this.hasModified = false;
        MusicPathTextInput.text = editingMeta.AudioPath;
        MusicNameTextInput.text = editingMeta.Name;
        MusicNameTextInput.onEndEdit.AddListener(txt => { editingMeta.Name = txt; hasModified = true; });
        UpdateChartButtonState();
        isEditing = true;
    }

    private void UpdateChartButtonState()
    {
        var foundLevel = new List<int>();
        foreach(var chart in editingMeta.AcceptedCharts){
            if (!foundLevel.Contains(chart.Difficulty) && !string.IsNullOrWhiteSpace(chart.FilePath))
            {
                foundLevel.Add(chart.Difficulty);
                ChartButtons[chart.Difficulty].GetComponentInChildren<Text>().text = chart.FilePath;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            if (!foundLevel.Contains(i))
            {
                ChartButtons[i].GetComponentInChildren<Text>().text = "[Click To Add]";
            }
        }
    }

    public void EditChart(int difficulty)
    {
        if (!isEditing) return;
        if (difficulty < 0 || difficulty >= 5) return;
        hasModified = true;
        editingMeta.Charts.RemoveAll(score => score.Difficulty == difficulty);
        FileBrowser.SetFilters(false,
            //new FileBrowser.Filter("Chart File", ".fbscore", ".sus"),
            new FileBrowser.Filter("Frame-based Chart", ".fbscore")/*,
            new FileBrowser.Filter("Sliding Universal Score", ".sus")*/);
        SimpleFileBrowser.FileBrowser.ShowLoadDialog(paths => {
            var pickedFile = paths[0];
            var scoreMeta = new ScoreMeta
            {
                Difficulty = difficulty,
                FilePath = pickedFile
            };
            editingMeta.Charts.Add(scoreMeta);
            UpdateChartButtonState();
        }, () => UpdateChartButtonState(), FileBrowser.PickMode.Files);
    }

    public void OnBackButton()
    {
        isEditing = false;
        if (hasModified)
        {
            Utils.WriteExternalMusicData(MainHelper.externalCollections);
            MainHelper.ReloadMusicCollection();
        }
        var newinst = MainHelper.externalCollections.Musics.Find(meta => editingMeta.Equals(meta));
        MainHelper.EndEdit(newinst, newinst == null);
        editingMeta = null;
    }

    public void OnDeleteButton()
    {
        isEditing = false;
        MainHelper.externalCollections.Musics.Remove(editingMeta);
        Utils.WriteExternalMusicData(MainHelper.externalCollections);
        MainHelper.ReloadMusicCollection();
        MainHelper.EndEdit(returnToList: true);
        editingMeta = null;
    }
}
