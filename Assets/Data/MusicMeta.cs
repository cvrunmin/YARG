using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace YARG.Data
{
    [Serializable]
    public class MusicMeta
    {
        public string Name;
        public string Composer;
        public string Arranger;
        public string LyricsWriter;
        public string Singer;
        public string AudioPath;
        public string PreviewAudioPath;
        public string JacketPath;
        [NonSerialized]
        public Texture2D JacketTexture;

        // offset time from audio start position to chart start time. in second.
        public double ChartOffset = 0;
        // delay time for audio to start
        public double Padding = 8;

        public List<ScoreMeta> Charts = new List<ScoreMeta>();

        public IEnumerable<ScoreMeta> AcceptedCharts => Charts.Where(meta => meta.Difficulty >= 0 && meta.Difficulty < 5);

        public List<MusicVarientMeta> Varients = new List<MusicVarientMeta>();
        
        public bool IsInternal { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = obj as MusicMeta;
            if (!string.Equals(Name, other.Name)) return false;
            if (!string.Equals(Composer, other.Composer)) return false;
            if (!string.Equals(Arranger, other.Arranger)) return false;
            if (!string.Equals(LyricsWriter, other.LyricsWriter)) return false;
            if (!string.Equals(Singer, other.Singer)) return false;
            if (!string.Equals(AudioPath, other.AudioPath)) return false;
            if (!string.Equals(PreviewAudioPath, other.PreviewAudioPath)) return false;
            if (!string.Equals(JacketPath, other.JacketPath)) return false;
            if (ChartOffset != other.ChartOffset) return false;
            if (Padding != other.Padding) return false;
            if (IsInternal != other.IsInternal) return false;
            if (Charts.Count != other.Charts.Count || !Charts.All(other.Charts.Contains)) return false;
            if (Varients.Count != other.Varients.Count || !Varients.All(other.Varients.Contains)) return false;
            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            int hc = (Name.GetHashCode());
            hc = hc * 17 + Composer.GetHashCode();
            hc = hc * 17 + Arranger.GetHashCode();
            hc = hc * 17 + LyricsWriter.GetHashCode();
            hc = hc * 17 + Singer.GetHashCode();
            hc = hc * 17 + AudioPath.GetHashCode();
            hc = hc * 17 + PreviewAudioPath.GetHashCode();
            hc = hc * 17 + JacketPath.GetHashCode();
            hc = hc * 17 + ChartOffset.GetHashCode();
            hc = hc * 17 + Padding.GetHashCode();
            hc = hc * 17 + IsInternal.GetHashCode();
            hc = hc * 17 + Charts.GetHashCode();
            hc = hc * 17 + Varients.GetHashCode();
            return hc;
        }
    }

    [Serializable]
    public class ScoreMeta
    {
        public int Difficulty;
        public string FilePath;
        public bool AllowAutoGenerate = false;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = obj as ScoreMeta;
            return other.Difficulty == Difficulty && object.Equals(FilePath, other.FilePath) && AllowAutoGenerate == other.AllowAutoGenerate;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            int hc = AllowAutoGenerate.GetHashCode();
            hc = hc * 17 + Difficulty.GetHashCode();
            hc = hc * 17 + FilePath.GetHashCode();
            return hc;
        }
    }

    [Serializable]
    public class MusicVarientMeta
    {
        public string Singer;
        public string VarientAudioPath;
        public string VarientJacketPath;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = obj as MusicVarientMeta;
            return string.Equals(Singer, other.Singer) && string.Equals(VarientAudioPath, other.VarientAudioPath) && string.Equals(VarientJacketPath, other.VarientJacketPath);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            int hc = Singer.GetHashCode();
            hc = hc * 23 + VarientAudioPath.GetHashCode();
            hc = hc * 23 + VarientJacketPath.GetHashCode();
            return hc;
        }
    }
}
