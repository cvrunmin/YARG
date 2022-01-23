using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using YARG;
using Cysharp.Threading.Tasks;

namespace YARG.Data
{
    public struct NoteKey
    {
        public int Measure;
        public int OffsetTick;
        public int Group;
        public int LeftPos;
        public int Width;

        public NoteKey(int measure, int offsetTick, int channel, int xPos, int width) : this()
        {
            Measure = measure;
            OffsetTick = offsetTick;
            Group = channel;
            LeftPos = xPos;
            Width = width;
        }
    }

    class NoteKeyComparerWithoutChannel : IEqualityComparer<NoteKey>
    {
        public bool Equals(NoteKey x, NoteKey y)
        {
            return x.Width == y.Width && x.LeftPos == y.LeftPos && x.Measure == y.Measure && x.OffsetTick == y.OffsetTick;
        }

        public int GetHashCode(NoteKey obj)
        {
            int hash = obj.Measure.GetHashCode();
            hash = hash * 23 + obj.OffsetTick.GetHashCode();
            hash = hash * 23 + obj.LeftPos.GetHashCode();
            hash = hash * 23 + obj.Width.GetHashCode();
            return hash;
        }
    }

    public class RawSusScore
    {
        public static RawSusScore ReadSusFromStreamingAssets(string resourcePath, Encoding encoding, Predicate<RawNoteEventInfo> predicate, Func<RawNoteEventInfo, RawNoteEventInfo> mapper)
        {
            var url = Utils.GetStreamingAssetUrl(resourcePath);
            using (var request = UnityWebRequest.Get(url))
            {
                request.SendWebRequest();
                while (!request.isDone) ;
                using (var reader = new StreamReader(new MemoryStream(request.downloadHandler.data), encoding))
                {
                    return ReadSus(reader, predicate, mapper);
                }
            }
        }

        public static Task<RawSusScore> ReadSekaiSusFromStreamingAssets(string resourcePath)
        {
            return ReadSekaiSusFromStreamingAssets(resourcePath, Encoding.UTF8);
        }

        public static async Task<RawSusScore> ReadSekaiSusFromStreamingAssets(string resourcePath, Encoding encoding)
        {
            var url = Utils.GetStreamingAssetUrl(resourcePath);
            using (var request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest();
                using (var reader = new StreamReader(new MemoryStream(request.downloadHandler.data), encoding))
                {
                    return ReadSekaiSus(reader);
                }
            }
        }

        private static readonly Regex BpmDefPattern = new Regex("BPM(\\d{2}):\\s*(\\d+(\\.\\d+)?)");
        private static readonly Regex BpmApplyPattern = new Regex("(\\d{3})08:\\s*(\\d{2})");
        private static readonly Regex BeatUnitPattern = new Regex("(\\d{3})02:\\s*(\\d+)");
        private static readonly Regex NoteEventPattern = new Regex("(?<measure>\\d{3})(?<type>[1-5])(?<left>[0-9a-zA-Z])(?<channel>[0-9a-zA-Z])?:(\\s*[0-9a-zA-Z][0-9a-zA-Z])+\\s*");

        private static readonly List<string> MetaHeader = new List<string> {
            "TITLE",
            "SUBTITLE",
            "ARTIST",
            "GENRE",
            "DESIGNER",
            "DIFFICULTY",
            "PLAYLEVEL",
            "SONGID",
            "WAVE" ,
            "WAVEOFFSET" ,
            "JACKET",
            "BACKGROUND" ,
            "MOVIE" ,
            "MOVIEOFFSET" ,
            "BASEBPM",
            "REQUEST" ,
            "HISPEED",
            "MEASUREHS" ,
            "TIL"
        };
        public static readonly int TickPerBeat = 480;

        public RawSusScore(List<BeatCountInfo> beatDefList, Dictionary<int, float> bpmDef, List<Tuple<int, int>> bpmApply, List<RawNoteEventInfo> noteEventList)
        {
            BeatDefinations = beatDefList;
            Bpms = new List<BpmInfo>();
            foreach (var item in bpmApply)
            {
                if (bpmDef.ContainsKey(item.Item2))
                {
                    Bpms.Add(new BpmInfo() { Measure = item.Item1, Bpm = bpmDef[item.Item2] });
                }
            }

            RawNoteEvents = noteEventList.Where(x => 1 <= x.Type && x.Type <= 5).ToList();
        }

        public List<BeatCountInfo> BeatDefinations { get; }
        public List<BpmInfo> Bpms { get; }
        public List<RawNoteEventInfo> RawNoteEvents { get; }
        public IEnumerable<RawNoteEventInfo> ShortNoteEvents => RawNoteEvents.Where(e => e.Type == 1);
        public IEnumerable<RawNoteEventInfo> ModifierNoteEvents => RawNoteEvents.Where(e => e.Type == 5);
        public IEnumerable<RawNoteEventInfo> LongNoteEvents => RawNoteEvents.Where(e => 2 <= e.Type && e.Type <= 4);

        /**
         * read *.sus file specially provided for Project Sekai
         * This filter out skill trigger and fever trigger which we don't have
         **/
        public static RawSusScore ReadSekaiSus(StreamReader reader)
        {
            return ReadSus(reader, info => info.XPos >= 2 && info.XPos <= 13, info => { info.XPos -= 2; return info; });
        }

        public static RawSusScore ReadSus(StreamReader reader)
        {
            return ReadSus(reader, _ => true, _ => _);
        }

        public static RawSusScore ReadSus(StreamReader reader, Predicate<RawNoteEventInfo> predicate, Func<RawNoteEventInfo, RawNoteEventInfo> mapper)
        {
            var beatDefinations = new List<BeatCountInfo>();
            var bpmDefinations = new Dictionary<int, float>();
            var bpmApplications = new List<Tuple<int, int>>();
            var noteEvents = new List<RawNoteEventInfo>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("#")) continue;
                if (line.ContainsAny(MetaHeader)) continue;
                Match match;
                if ((match = BeatUnitPattern.Match(line)).Success)
                {
                    var measure = int.Parse(match.Groups[1].Value);
                    var count = int.Parse(match.Groups[2].Value);
                    beatDefinations.Add(new BeatCountInfo() { Measure = measure, Count = count });
                    continue;
                }
                if ((match = BpmDefPattern.Match(line)).Success)
                {
                    var bpmId = int.Parse(match.Groups[1].Value);
                    var bpmValue = float.Parse(match.Groups[2].Value);
                    bpmDefinations.Add(bpmId, bpmValue);
                    continue;
                }
                if ((match = BpmApplyPattern.Match(line)).Success)
                {
                    var measure = int.Parse(match.Groups[1].Value);
                    var bpmId = int.Parse(match.Groups[2].Value);
                    bpmApplications.Add(new Tuple<int, int>(measure, bpmId));
                    continue;
                }
                if ((match = NoteEventPattern.Match(line)).Success)
                {
                    var measure = int.Parse(match.Groups["measure"].Value);
                    var type = int.Parse(match.Groups["type"].Value);
                    var left_pos = Utils.AlphanumericToDecimal(match.Groups["left"].Value);
                    var c = match.Groups["channel"];
                    var channel = c.Success ? Utils.AlphanumericToDecimal(c.Value) : 0;
                    var data_str = line.Split(':')[1].Trim();
                    if (data_str.Length % 2 != 0)
                    {
                        Debug.LogWarning($"wrong length of data part ({data_str.Length}): \"{line}\"");
                        continue;
                    }
                    var subdivided_count = data_str.Length / 2;
                    var note_step = 4.0f / subdivided_count;
                    for (int i = 0; i < subdivided_count; i++)
                    {
                        var note_str = data_str.Substring(i * 2, 2);
                        if (note_str[0] == '0')
                            continue; // skip no event position
                        var note_subtype = Utils.AlphanumericToDecimal(note_str[0].ToString());
                        var note_width = Utils.AlphanumericToDecimal(note_str[1].ToString());
                        noteEvents.Add(new RawNoteEventInfo()
                        {
                            Measure = measure,
                            OffsetTick = (int)(TickPerBeat * note_step * i),
                            XPos = left_pos,
                            Width = note_width,
                            Type = type,
                            SubType = note_subtype,
                            Channel = channel
                        });
                    }
                    continue;
                }
            }
            return new RawSusScore(beatDefinations, bpmDefinations, bpmApplications, noteEvents.Where(x => predicate.Invoke(x)).Select(mapper).ToList());
        }

        public int MeasureToTick(int measure)
        {
            var requestedBeatCounts = BeatDefinations.OrderBy(def => def.Measure).TakeWhile(def => measure < def.Measure);
            if (requestedBeatCounts.Any())
            {
                int lastMeasure = 0;
                int lastCount = 0;
                int totalBeats = 0;
                foreach (var item in requestedBeatCounts)
                {
                    totalBeats += (item.Measure - lastMeasure) * lastCount;
                    lastMeasure = item.Measure;
                    lastCount = item.Count;
                }
                totalBeats += (measure - lastMeasure) * lastCount;
                return totalBeats * TickPerBeat;
            }
            else return measure * 4 * TickPerBeat; //fallback
        }

        public double TickToMillisecond(int tick)
        {
            var requestedBpms = Bpms.OrderBy(info => info.Measure).TakeWhile(info => MeasureToTick(info.Measure) < tick);
            if (requestedBpms.Any())
            {
                int lastTick = 0;
                double totalMs = 0;
                float lastBpm = 120;
                foreach (var item in requestedBpms)
                {
                    var currentTick = MeasureToTick(item.Measure);
                    totalMs += TickToMillisecond(currentTick - lastTick, lastBpm);
                    lastTick = currentTick;
                    lastBpm = item.Bpm;
                }
                totalMs += TickToMillisecond(tick - lastTick, lastBpm);
                return totalMs;
            }
            else return TickToMillisecond(tick, 120); //fallback
        }

        private double TickToMillisecond(int tick, float bpm)
        {
            return tick * 60000.0f / bpm / TickPerBeat;
        }

        public List<RawNoteData> GetUsableRawNoteData()
        {
            var list = new List<RawNoteData>();
            return GetNoteDataInternal();
            //var snapCenterModifierList = new List<RawNoteEventInfo>();
            //foreach (var item in ShortNoteEvents)
            //{
            //    if (item.SubType == 3)
            //    {
            //        snapCenterModifierList.Add(item);
            //        continue;
            //    }
            //    var tick = MeasureToTick(item.Measure) + item.OffsetTick;
            //    IEnumerable<RawNoteEventInfo> modifiers = ModifierNoteEvents.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos);
            //    if (modifiers.Any())
            //    {
            //        foreach (var i1 in modifiers)
            //        {
            //            if (i1.SubType == 1 || i1.SubType == 3 || i1.SubType == 4)
            //            {
            //                var mod = i1.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : i1.SubType == 4 ? RawNoteData.NoteModifier.FlickRight : RawNoteData.NoteModifier.None;
            //                list.Add(new RawNoteData(RawNoteData.NoteType.Flick, TickToMillisecond(tick), item.Channel, item.XPos, item.Width, mod));
            //                break;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        list.Add(new RawNoteData(RawNoteData.NoteType.Click, TickToMillisecond(tick), item.Channel, item.XPos, item.Width, RawNoteData.NoteModifier.None));
            //    }
            //}
            //foreach (var item in LongNoteEvents)
            //{
            //    RawNoteData.NoteType subtype;
            //    switch (item.SubType)
            //    {
            //        case 1:
            //            subtype = RawNoteData.NoteType.SlideStart;
            //            break;
            //        case 2:
            //            subtype = RawNoteData.NoteType.SlideEnd;
            //            break;
            //        case 3:
            //            subtype = RawNoteData.NoteType.SlideCheckpoint;
            //            break;
            //        case 5:
            //            subtype = RawNoteData.NoteType.SlideIntermediate;
            //            break;
            //        default:
            //            continue;
            //    }
            //    var tick = MeasureToTick(item.Measure) + item.OffsetTick;
            //    IEnumerable<RawNoteEventInfo> modifiers = ModifierNoteEvents.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos);
            //    var mods = new List<RawNoteData.NoteModifier>();
            //    var snap = snapCenterModifierList.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos).FirstOrDefault();
            //    if (snap != null)
            //    {
            //        mods.Add(RawNoteData.NoteModifier.CheckpointSnapCenter);
            //    }
            //    if (modifiers.Any())
            //    {
            //        foreach (var i1 in modifiers)
            //        {
            //            if (i1.SubType == 3 || i1.SubType == 4)
            //            {
            //                mods.Add(i1.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : RawNoteData.NoteModifier.FlickRight);
            //            }
            //            else if (i1.SubType == 2)
            //            {
            //                mods.Add(RawNoteData.NoteModifier.SlideEaseIn);
            //            }
            //            else if (i1.SubType == 5 || i1.SubType == 6)
            //            {
            //                mods.Add(RawNoteData.NoteModifier.SlideEaseOut);
            //            }
            //        }
            //    }
            //    if (mods.Count > 0)
            //    {
            //        list.Add(new RawNoteData(subtype, TickToMillisecond(tick), item.Channel, item.XPos, item.Width, mods.ToArray()));
            //    }
            //    else
            //    {
            //        list.Add(new RawNoteData(subtype, TickToMillisecond(tick), item.Channel, item.XPos, item.Width, RawNoteData.NoteModifier.None));
            //    }
            //}
            //return list;
        }

        private List<RawNoteData> GetNoteDataInternal(bool tickBased = false)
        {
            //var test = RawNoteEvents.GroupBy(info => new NoteKey(info.Measure, info.OffsetTick, info.Channel, info.XPos, info.Width)).Any(gp => gp.Count() > 1);
            return RawNoteEvents.GroupBy(info => new NoteKey(info.Measure, info.OffsetTick, info.Channel, info.XPos, info.Width), new NoteKeyComparerWithoutChannel()).Select(gp => {
                var k = gp.Key;
                var snapCenter = false;
                var flickMod = (RawNoteData.NoteModifier?)null;
                var slideEaseMod = (RawNoteData.NoteModifier?)null;
                var t = (RawNoteData.NoteType?)null;
                var finalChannel = k.Group;
                foreach (var item in gp)
                {
                    if (item.Type == 1)
                    {
                        if (item.SubType == 3)
                        {
                            snapCenter = true;
                        }
                        else if (t == null)
                        {
                            t = RawNoteData.NoteType.Click;
                        }
                    }
                    else if (item.Type == 5)
                    {
                        if ((t == null || !RawNoteData.IsSlideType(t.Value)) && (item.SubType == 1 || item.SubType == 3 || item.SubType == 4))
                        {
                            flickMod = item.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : item.SubType == 4 ? RawNoteData.NoteModifier.FlickRight : RawNoteData.NoteModifier.None;
                            t = RawNoteData.NoteType.Flick;
                        }
                        else if (item.SubType == 2)
                        {
                            slideEaseMod = (RawNoteData.NoteModifier.SlideEaseIn);
                        }
                        else if (item.SubType == 5 || item.SubType == 6)
                        {
                            slideEaseMod = (RawNoteData.NoteModifier.SlideEaseOut);
                        }
                    }
                    else if (2 <= item.Type && item.Type <= 4) // no type null checking - slide note has highest priority
                    {
                        switch (item.SubType)
                        {
                            case 1:
                                finalChannel = item.Channel;
                                t = RawNoteData.NoteType.SlideStart;
                                break;
                            case 2:
                                finalChannel = item.Channel;
                                t = RawNoteData.NoteType.SlideEnd;
                                break;
                            case 3:
                                finalChannel = item.Channel;
                                t = RawNoteData.NoteType.SlideCheckpoint;
                                break;
                            case 5:
                                finalChannel = item.Channel;
                                t = RawNoteData.NoteType.SlideIntermediate;
                                break;
                            default:
                                continue;
                        }
                    }
                }
                if (t != null)
                {
                    double time = MeasureToTick(k.Measure) + k.OffsetTick;
                    if (!tickBased)
                    {
                        time = TickToMillisecond((int)time);
                    }
                    if (RawNoteData.IsSlideType(t.Value))
                    {
                        var mods = new List<RawNoteData.NoteModifier>();
                        if (snapCenter)
                        {
                            mods.Add(RawNoteData.NoteModifier.CheckpointSnapCenter);
                        }
                        if (slideEaseMod != null)
                        {
                            mods.Add(slideEaseMod.Value);
                        }
                        if (mods.Count == 0) mods.Add(RawNoteData.NoteModifier.None);
                        return new RawNoteData(t.Value, time, finalChannel, k.LeftPos, k.Width, mods.ToArray());
                    }
                    else if (t == RawNoteData.NoteType.Click)
                    {
                        return new RawNoteData(t.Value, time, finalChannel, k.LeftPos, k.Width, RawNoteData.NoteModifier.None);
                    }
                    else
                    {
                        return new RawNoteData(t.Value, time, finalChannel, k.LeftPos, k.Width, flickMod.Value);
                    }
                }
                return null;
            }).Where(datum => datum != null).OrderBy(datum => datum.ActionMs).ToList();
            //return RawNoteEvents.GroupBy(info => new NoteKey(info.Measure, info.OffsetTick, info.Channel, info.XPos, info.Width), resultSelector: (k, l) =>
            //{
            //    var snapCenter = false;
            //    var flickMod = (RawNoteData.NoteModifier?)null;
            //    var slideEaseMod = (RawNoteData.NoteModifier?)null;
            //    var t = (RawNoteData.NoteType?)null;
            //    foreach (var item in l)
            //    {
            //        if (item.Type == 1)
            //        {
            //            if (item.SubType == 3)
            //            {
            //                snapCenter = true;
            //            }
            //            else if (t == null)
            //            {
            //                t = RawNoteData.NoteType.Click;
            //            }
            //        }
            //        else if (item.Type == 5)
            //        {
            //            if ((t == null || !RawNoteData.IsSlideType(t.Value)) && (item.SubType == 1 || item.SubType == 3 || item.SubType == 4))
            //            {
            //                flickMod = item.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : item.SubType == 4 ? RawNoteData.NoteModifier.FlickRight : RawNoteData.NoteModifier.None;
            //                t = RawNoteData.NoteType.Flick;
            //            }
            //            else if (item.SubType == 2)
            //            {
            //                slideEaseMod = (RawNoteData.NoteModifier.SlideEaseIn);
            //            }
            //            else if (item.SubType == 5 || item.SubType == 6)
            //            {
            //                slideEaseMod = (RawNoteData.NoteModifier.SlideEaseOut);
            //            }
            //        }
            //        else if (2 <= item.Type && item.Type <= 4) // no type null checking - slide note has highest priority
            //        {
            //            switch (item.SubType)
            //            {
            //                case 1:
            //                    t = RawNoteData.NoteType.SlideStart;
            //                    break;
            //                case 2:
            //                    t = RawNoteData.NoteType.SlideEnd;
            //                    break;
            //                case 3:
            //                    t = RawNoteData.NoteType.SlideCheckpoint;
            //                    break;
            //                case 5:
            //                    t = RawNoteData.NoteType.SlideIntermediate;
            //                    break;
            //                default:
            //                    continue;
            //            }
            //        }
            //    }
            //    if (t != null)
            //    {
            //        double time = MeasureToTick(k.Measure) + k.OffsetTick;
            //        if (!tickBased)
            //        {
            //            time = TickToMillisecond((int)time);
            //        }
            //        if (RawNoteData.IsSlideType(t.Value))
            //        {
            //            var mods = new List<RawNoteData.NoteModifier>();
            //            if (snapCenter)
            //            {
            //                mods.Add(RawNoteData.NoteModifier.CheckpointSnapCenter);
            //            }
            //            if (slideEaseMod != null)
            //            {
            //                mods.Add(slideEaseMod.Value);
            //            }
            //            if (mods.Count == 0) mods.Add(RawNoteData.NoteModifier.None);
            //            return new RawNoteData(t.Value, time, k.Group, k.LeftPos, k.Width, mods.ToArray());
            //        }
            //        else if (t == RawNoteData.NoteType.Click)
            //        {
            //            return new RawNoteData(t.Value, time, k.Group, k.LeftPos, k.Width, RawNoteData.NoteModifier.None);
            //        }
            //        else
            //        {
            //            return new RawNoteData(t.Value, time, k.Group, k.LeftPos, k.Width, flickMod.Value);
            //        }
            //    }
            //    return null;
            //}, new NoteKeyComparerWithoutWidth()).Where(datum => datum != null).OrderBy(datum => datum.ActionMs).ToList();
        }

        /**
         * Special method for internal use
         **/
        internal List<RawNoteData> GetTickBasedRawNoteData()
        {
            return GetNoteDataInternal(true);
            //var list = new List<RawNoteData>();
            //var snapCenterModifierList = new List<RawNoteEventInfo>();
            //foreach (var item in ShortNoteEvents)
            //{
            //    if (item.SubType == 3)
            //    {
            //        snapCenterModifierList.Add(item);
            //        continue;
            //    }
            //    var tick = MeasureToTick(item.Measure) + item.OffsetTick;
            //    IEnumerable<RawNoteEventInfo> modifiers = ModifierNoteEvents.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos);
            //    if (modifiers.Any())
            //    {
            //        foreach (var i1 in modifiers)
            //        {
            //            if (i1.SubType == 1 || i1.SubType == 3 || i1.SubType == 4)
            //            {
            //                var mod = i1.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : i1.SubType == 4 ? RawNoteData.NoteModifier.FlickRight : RawNoteData.NoteModifier.None;
            //                list.Add(new RawNoteData(RawNoteData.NoteType.Flick, tick, item.Channel, item.XPos, item.Width, mod));
            //                break;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        list.Add(new RawNoteData(RawNoteData.NoteType.Click, tick, item.Channel, item.XPos, item.Width, RawNoteData.NoteModifier.None));
            //    }
            //}
            //foreach (var item in LongNoteEvents)
            //{
            //    RawNoteData.NoteType subtype;
            //    switch (item.SubType)
            //    {
            //        case 1:
            //            subtype = RawNoteData.NoteType.SlideStart;
            //            break;
            //        case 2:
            //            subtype = RawNoteData.NoteType.SlideEnd;
            //            break;
            //        case 3:
            //            subtype = RawNoteData.NoteType.SlideCheckpoint;
            //            break;
            //        case 5:
            //            subtype = RawNoteData.NoteType.SlideIntermediate;
            //            break;
            //        default:
            //            continue;
            //    }
            //    var tick = MeasureToTick(item.Measure) + item.OffsetTick;
            //    IEnumerable<RawNoteEventInfo> modifiers = ModifierNoteEvents.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos);
            //    var mods = new List<RawNoteData.NoteModifier>();
            //    var snap = snapCenterModifierList.Where(i1 => i1.Measure == item.Measure && i1.OffsetTick == item.OffsetTick && i1.XPos == item.XPos).FirstOrDefault();
            //    if (snap != null)
            //    {
            //        mods.Add(RawNoteData.NoteModifier.CheckpointSnapCenter);
            //    }
            //    if (modifiers.Any())
            //    {
            //        foreach (var i1 in modifiers)
            //        {
            //            if (i1.SubType == 3 || i1.SubType == 4)
            //            {
            //                mods.Add(i1.SubType == 3 ? RawNoteData.NoteModifier.FlickLeft : RawNoteData.NoteModifier.FlickRight);
            //            }
            //            else if (i1.SubType == 2)
            //            {
            //                mods.Add(RawNoteData.NoteModifier.SlideEaseIn);
            //            }
            //            else if (i1.SubType == 5 || i1.SubType == 6)
            //            {
            //                mods.Add(RawNoteData.NoteModifier.SlideEaseOut);
            //            }
            //        }
            //    }
            //    if (mods.Count > 0)
            //    {
            //        list.Add(new RawNoteData(subtype, tick, item.Channel, item.XPos, item.Width, mods.ToArray()));
            //    }
            //    else
            //    {
            //        list.Add(new RawNoteData(subtype, tick, item.Channel, item.XPos, item.Width, RawNoteData.NoteModifier.None));
            //    }
            //}
            //return list;
        }
    }

    public class RawNoteEventInfo
    {
        public int Measure { get; set; }
        public int OffsetTick { get; set; }
        public int XPos { get; set; }
        public int Width { get; set; }
        public int Type { get; set; }
        public int SubType { get; set; }
        public int Channel { get; set; } = 0;
    }

    public class BeatCountInfo
    {
        public int Measure { get; set; }
        public int Count { get; set; }
    }

    public class BpmInfo
    {
        public int Measure { get; set; }
        public float Bpm { get; set; }
    }
}
