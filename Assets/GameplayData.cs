using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YARG.Data;
using UnityEngine;

namespace YARG
{
    public static class GameplayData
    {
        public static bool ReadyToReceive { get; set; } = false;

        public static MusicMeta PlayingMusic { get; set; }

        /**
         * If null, we can assume that we use the default audio
         **/
        public static MusicVarientMeta MusicVarient { get; set; }

        public static int Difficulty { get; set; }

        public static List<NoteData> NoteData { get; set; }

        public static AudioClip MusicClip { get; set; }

        public static Texture Jacket { get; set; }

        public static void ResetData()
        {
            PlayingMusic = null;
            MusicVarient = null;
            NoteData = null;
            MusicClip = null;
            Jacket = null;
            Difficulty = 0;
            ReadyToReceive = false;
        }
    }

    public static class GameplayScoreData
    {
        public static int PerfectCount { get; set; }
        public static int GoodCount { get; set; }
        public static int OkayCount { get; set; }
        public static int MissCount { get; set; }
        public static int MaxCombo { get; set; }
    }
}
