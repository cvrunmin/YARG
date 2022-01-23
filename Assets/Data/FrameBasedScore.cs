using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;

namespace YARG.Data
{
    public class FrameBasedScore
    {
        private static readonly Regex NotePattern = new Regex("T(?<type>\\d+)X(?<leftpos>\\d+)W(?<width>\\d+)");

        public List<RawNoteData> Data { get; }

        public static async Task<FrameBasedScore> ReadScoreFromStreamingAssets(string resourcePath)
        {
            var url = Utils.GetStreamingAssetUrl(resourcePath);
            using (var request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();
                using (var reader = new StreamReader(new MemoryStream(request.downloadHandler.data), Encoding.UTF8))
                {
                    return ReadScore(reader);
                }
            }
        }

        public static Task<FrameBasedScore> ReadScoreFromFilePath(string path)
        {
            try
            {
                using (var reader = new StreamReader(File.OpenRead(path), Encoding.UTF8))
                {
                    return Task.FromResult(ReadScore(reader));
                }
            }
            catch (IOException ex)
            {
                Debug.LogWarning(ex);
                return Task.FromException<FrameBasedScore>(ex);
            }
        }

        public static FrameBasedScore ReadScore(StreamReader reader)
        {
            var data = new List<RawNoteData>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#")) continue;
                var s = line.Trim().Split(':', ',');
                int frame;
                try
                {
                    frame = int.Parse(s[0]);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"cannot convert frame no. {s[0]} for line: {line.Trim()}");
                    continue;
                }
                var c0Match = NotePattern.Match(s[1]);
                var c1Match = NotePattern.Match(s[2]);
                if (!c0Match.Success)
                {
                    Debug.LogWarning($"fail to match {s[1]}, corrupted line?");
                    continue;
                }
                if (!c1Match.Success)
                {
                    Debug.LogWarning($"fail to match {s[2]}, corrupted line?");
                    continue;
                }
                int t1, x1, w1, t2, x2, w2;
                try
                {
                    t1 = int.Parse(c0Match.Groups["type"].Value);
                    x1 = int.Parse(c0Match.Groups["leftpos"].Value);
                    w1 = int.Parse(c0Match.Groups["width"].Value);
                    t2 = int.Parse(c1Match.Groups["type"].Value);
                    x2 = int.Parse(c1Match.Groups["leftpos"].Value);
                    w2 = int.Parse(c1Match.Groups["width"].Value);
                }
                catch (FormatException ex)
                {
                    Debug.LogWarning($"cannot convert note info for line: {line.Trim()}");
                    Debug.LogWarning(ex.ToString());
                    continue;
                }
                if (t1 != 0)
                {
                    data.Add(new RawNoteData(RemapNoteType(t1), frame * 10.0, 0, x1, w1, RawNoteData.NoteModifier.None));
                }
                if (t2 != 0)
                {
                    data.Add(new RawNoteData(RemapNoteType(t2), frame * 10.0, 1, x2, w2, RawNoteData.NoteModifier.None));
                }
            }
            return new FrameBasedScore(data);
        }

        public static RawNoteData.NoteType RemapNoteType(int type)
        {
            switch (type)
            {
                default:
                case 0:
                    return RawNoteData.NoteType.None;
                case 1:
                    return RawNoteData.NoteType.Click;
                case 2:
                    return RawNoteData.NoteType.Flick;
                case 3:
                    return RawNoteData.NoteType.SlideStart;
                case 4:
                    return RawNoteData.NoteType.SlideCheckpoint;
                case 5:
                    return RawNoteData.NoteType.SlideEnd;
            }
        }

        private FrameBasedScore(List<RawNoteData> data)
        {
            Data = data;
        }
    }
}
