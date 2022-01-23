using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;
using YARG.Data;

using Cysharp.Threading.Tasks;

namespace YARG
{
    public static class EasingUtils
    {
        public static float LinearEase(float start, float end, float progress)
        {
            var mid = end - start;
            return start + mid * progress/*Mathf.Sin((float)(progress * 0.5 * Mathf.PI))*/;
        }

        // from the expanded equation from Wikipedia
        public static Vector2 CubicBezierEase(Vector2 start, Vector2 end, Vector2 ctrl1, Vector2 ctrl2, float t)
        {
            return Mathf.Pow(1 - t, 3) * start + 3 * Mathf.Pow(1 - t, 2) * t * ctrl1 + 3 * (1 - t) * t * t * ctrl2 + t * t * t * end;
        }
    }

    public static class Utils
    {
        public static bool ContainsAny(this string str, List<string> candidates)
        {
            foreach (var item in candidates)
            {
                if (str.Contains(item)) return true;
            }
            return false;
        }

        public static int AlphanumericToDecimal(string str)
        {
            string lookup = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int result = 0;
            foreach(var c in str.ToUpper())
            {
                result *= 36;
                int placeValue = lookup.IndexOf(c);
                if (placeValue == -1) throw new FormatException($"Unknown character {c}");
                result += placeValue;
            }
            return result;
        }

        public static MusicCollections ReadExternalMusicData()
        {
            var basePath = Application.persistentDataPath;
            try
            {
                return JsonUtility.FromJson<MusicCollections>(File.ReadAllText(Path.Combine(basePath, "musics.json"), Encoding.UTF8));
            }
            catch (IOException)
            {
                return new MusicCollections();
            }
        }

        public static void WriteExternalMusicData(MusicCollections musicCollections)
        {
            var basePath = Application.persistentDataPath;
            try
            {
                File.WriteAllText(Path.Combine(basePath, "musics.json"),  JsonUtility.ToJson(musicCollections), Encoding.UTF8);
            }
            catch (IOException)
            {
            }
        }

        public static string GetStreamingAssetUrl(string path)
        {
            var url = Path.Combine(Application.streamingAssetsPath, path);
            if (!url.StartsWith("jar:file://")) // if not in Android environment, add file:// protocol
            {
                url = WarpWithFileProtocol(url);
            }
            return url;
        }
        public static string WarpWithFileProtocol(string path)
        {
            return "file://" + path;
        }

        public static UniTask<MusicCollections> ReadInternalMusicData()
        {
            return ReadStreamingText("musics.json").ContinueWith(result =>
            {
                MusicCollections musicCollections = JsonUtility.FromJson<MusicCollections>(result);
                musicCollections.Musics.ForEach(meta => meta.IsInternal = true);
                return musicCollections;
            });
        }

        public static async UniTask<string> ReadStreamingText(string path)
        {
            var url = GetStreamingAssetUrl(path);
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                await request.SendWebRequest();
                return request.downloadHandler.text;
            }
        }

        public static async UniTask<Texture2D> LoadStreamingTexture(string path)
        {
            var url = GetStreamingAssetUrl(path);
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;
                try
                {
                    request.downloadHandler = new DownloadHandlerTexture();
                    await request.SendWebRequest();
                    return DownloadHandlerTexture.GetContent(request);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                    return null;
                }
            }
        }
        public static UniTask<AudioClip> LoadStreamingAudio(string path)
        {
            var url = GetStreamingAssetUrl(path);
            return LoadAudioByUrl(url);
        }

        public static async UniTask<AudioClip> LoadAudioByUrl(string url)
        {
            if (Path.GetExtension(url).Equals(".ogg", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
                {
                    request.timeout = 10;
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) return null;
                    return DownloadHandlerAudioClip.GetContent(request);
                }
            }
            else if (Path.GetExtension(url).Equals(".mp3", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
                {
                    request.timeout = 10;
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) return null;
                    return DownloadHandlerAudioClip.GetContent(request);
                }
            }
            else
            {
                using (var www = new WWW(url))
                {
                    return www.GetAudioClip();
                }
            }

        }
    }
}
