using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using YARG;
using YARG.Data;

using Cysharp.Threading.Tasks;

public class MusicItemButtonHelper : MonoBehaviour
{
    private MusicMeta meta;
    private MainMenuHelper helper;

    public Text TextName;
    public RawImage ImageJacket;

    private bool setupButton = false;

    public void InitializeData(MainMenuHelper helper, MusicMeta data)
    {
        this.helper = helper;
        meta = data;
    }

    private void setupAppearance()
    {
        if (string.IsNullOrWhiteSpace(meta.Name))
        {
            TextName.text = "[Untitled]";
        }
        else
        {
            TextName.text = meta.Name;
        }
        if (!string.IsNullOrWhiteSpace(meta.JacketPath))
        {
            var texture = Utils.LoadStreamingTexture(meta.JacketPath);
            texture.AsTask().ContinueWith(task =>
            {
                if (task.Result != null)
                {
                    meta.JacketTexture = task.Result;
                    ImageJacket.texture = meta.JacketTexture;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        setupButton = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (helper != null && meta != null)
        {
            setupAppearance();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!setupButton)
        {
            if (helper != null && meta != null)
            {
                setupAppearance();
            }
        }
    }

    public void OnButtonClick()
    {
        helper.SwitchToMusicDetailPage(meta);
    }
}
