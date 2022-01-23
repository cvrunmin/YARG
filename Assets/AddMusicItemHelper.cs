using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
//#if UNITY_EDITOR || UNITY_STANDALONE
using SimpleFileBrowser;
//#endif
using YARG.Data;

public class AddMusicItemHelper : MonoBehaviour
{
    private MainMenuHelper helper;

    public void InitializeData(MainMenuHelper helper)
    {
        this.helper = helper;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnButtonClick()
    {
        string pickedFile = null;
        bool isCancel = false;
//#if UNITY_EDITOR || UNITY_STANDALONE
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Supported Music File", ".ogg", ".mp3"), new FileBrowser.Filter("Ogg", ".ogg"), new FileBrowser.Filter("Mp3", ".mp3"));
        SimpleFileBrowser.FileBrowser.ShowLoadDialog(paths => {
            pickedFile = paths[0];
            MusicMeta meta = new MusicMeta();
            var musicName = Path.GetFileNameWithoutExtension(pickedFile);
            meta.Name = musicName;
            meta.AudioPath = pickedFile;
            helper.externalCollections.Musics.Add(meta);
            YARG.Utils.WriteExternalMusicData(helper.externalCollections);
            helper.RefreshMusicListOnly();
        }, () => isCancel = true, FileBrowser.PickMode.Files);
//#endif
//#if UNITY_ANDROID
//        NativeFilePicker.PickFile(path => {
//            Debug.Log(path);
//        }, new string[] { "*" });
//#endif
        if (!isCancel)
        {
        }
    }
}
