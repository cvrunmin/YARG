using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace YARG.Data
{
    public class RawNoteData
    {
        public NoteType Type { get; private set; }

        public double ActionMs { get; private set; }

        public int GroupId { get; private set; }

        public int LeftPos { get; private set; }

        public int Width { get; private set; }

        public int RightPos => LeftPos + Width - 1;

        public NoteModifier[] Modifiers { get; private set; }

        public enum NoteType
        {
            /**
             * Special note type which act as 'nothing'
             **/
            None,
            Click,
            Flick,
            SlideStart,
            SlideEnd,
            SlideIntermediate,
            SlideCheckpoint,
            /**
             * Special Note Type which only indicate where the slide note should check
             * if the note is still being held.
             * 
             * This note type doesn't required to fill in left pos 
             **/
            SlideProgramFilledCheckpoint
        }

        public enum NoteModifier
        {
            None,
            SlideEaseIn,
            SlideEaseOut,
            CheckpointSnapCenter,
            FlickLeft,
            FlickRight
        }

        public RawNoteData(NoteType type, double posMs, int groupId, int xPos, int width, params NoteModifier[] modifier)
        {
            Type = type;
            ActionMs = posMs;
            GroupId = groupId;
            LeftPos = xPos;
            Width = width;
            Modifiers = modifier;
        }

        public static bool IsAcceptedTypeMatch(NoteType first, NoteType second)
        {
            if (first == NoteType.Click) return second == NoteType.Click;
            if (first == NoteType.Flick) return second == NoteType.Flick;
            return second != NoteType.Click && second != NoteType.Flick;
        }

        public bool IsSlideType()
        {
            return IsSlideType(Type);
        }

        public static bool IsSlideType(NoteType type)
        {
            return type != NoteType.Click && type != NoteType.Flick;
        }
    }

    public class NoteData
    {
        public NoteGroupType Type { get; private set; }

        public enum NoteGroupType
        {
            Click,
            Flick,
            Slide
        }

        public List<RawNoteData> RawNotes { get; } = new List<RawNoteData>();

        public IEnumerable<RawNoteData> SortedRawNotes => RawNotes.OrderBy(d => d.ActionMs);

        public double StartTime => SortedRawNotes.First()?.ActionMs ?? 0;

        public double EndTime => SortedRawNotes.Last()?.ActionMs ?? 0;

        public NoteData(List<RawNoteData> data)
        {
            if (data.Count == 0) throw new ArgumentException();
            RawNoteData first = data.First();
            if(!data.Skip(1).All(d => d.GroupId == first.GroupId && RawNoteData.IsAcceptedTypeMatch(d.Type, first.Type)))
            {
                throw new ArgumentException("mismatch groupId or type in note data");
            }
            if(first.Type == RawNoteData.NoteType.Click)
            {
                this.Type = NoteGroupType.Click;
            }
            else if(first.Type == RawNoteData.NoteType.Flick)
            {
                this.Type = NoteGroupType.Flick;
            }
            else
            {
                this.Type = NoteGroupType.Slide;
            }
            RawNotes.AddRange(data);
            if(this.Type == NoteGroupType.Slide)
            {
                if(SortedRawNotes.First().Type != RawNoteData.NoteType.SlideStart)
                {
                    throw new ArgumentException("first slide note is not starting note");
                }
                if (SortedRawNotes.Last().Type != RawNoteData.NoteType.SlideEnd)
                {
                    throw new ArgumentException("last slide note is not ending note");
                }
                if (SortedRawNotes.Skip(1).Take(Math.Max(0, RawNotes.Count - 2)).Any(n => n.Type == RawNoteData.NoteType.SlideStart || n.Type == RawNoteData.NoteType.SlideEnd))
                {
                    throw new ArgumentException("inner slide note is not intermediate note or checkpoint note");
                }
            }
            else if(RawNotes.Count > 1)
            {
                throw new ArgumentException("too many notes for click/flick note");
            }
        }
    }

    public static class NoteDataUtils
    {
        public static List<NoteData> BakeSusData(RawSusScore score)
        {
            List<RawNoteData> tickbasedData = score.GetTickBasedRawNoteData();
            var baked = new List<NoteData>();
            var slideDict = new Dictionary<int, List<RawNoteData>>();
            foreach (var datum in tickbasedData.OrderBy(datum => datum.ActionMs).ThenByDescending(datum => datum.Type))
            {
                if (!datum.IsSlideType())
                {
                    baked.Add(new NoteData(new List<RawNoteData> { new RawNoteData(datum.Type, score.TickToMillisecond(((int)datum.ActionMs)), datum.GroupId, datum.LeftPos, datum.Width, datum.Modifiers) }));
                }
                else
                {
                    if (datum.Type == RawNoteData.NoteType.SlideStart)
                    {
                        if (slideDict.ContainsKey(datum.GroupId))
                        {
                            Debug.LogWarning($"duplicate slide start note for group {datum.GroupId} at {datum.ActionMs} ms");
                            continue;
                        }
                        slideDict.Add(datum.GroupId, new List<RawNoteData> { datum });
                    }
                    else
                    {
                        if (!slideDict.ContainsKey(datum.GroupId))
                        {
                            Debug.LogWarning($"stray non-start slide note for group {datum.GroupId} at {datum.ActionMs} ms");
                            continue;
                        }
                        slideDict[datum.GroupId].Add(datum);
                        if (datum.Type == RawNoteData.NoteType.SlideEnd)
                        {
                            List<RawNoteData> rawNoteDatas = slideDict[datum.GroupId];
                            List<int> keyTicks = rawNoteDatas.Select(d => ((int)d.ActionMs)).OrderBy(x => x).ToList();
                            for (int i = keyTicks.First() + RawSusScore.TickPerBeat / 2; i < keyTicks.Last(); i+=RawSusScore.TickPerBeat / 2)
                            {
                                if (keyTicks.Contains(i)) continue;
                                rawNoteDatas.Add(new RawNoteData(RawNoteData.NoteType.SlideProgramFilledCheckpoint, i, datum.GroupId, 0, 0, RawNoteData.NoteModifier.None));
                            }
                            baked.Add(new NoteData(rawNoteDatas.Select(d => new RawNoteData(d.Type, score.TickToMillisecond(((int)d.ActionMs)), d.GroupId, d.LeftPos, d.Width, d.Modifiers)).ToList()));
                            slideDict.Remove(datum.GroupId);
                        }
                    }
                }
            }
            return baked;
        }

        public static List<NoteData> BakeNoteData(List<RawNoteData> rawNoteDatas)
        {
            var baked = new List<NoteData>();
            var slideDict = new Dictionary<int, List<RawNoteData>>();
            foreach (var datum in rawNoteDatas.OrderBy(datum => datum.ActionMs).ThenByDescending(datum => datum.Type))
            {
                if (!datum.IsSlideType())
                {
                    baked.Add(new NoteData(new List<RawNoteData> { datum }));
                }
                else
                {
                    if (datum.Type == RawNoteData.NoteType.SlideStart)
                    {
                        if (slideDict.ContainsKey(datum.GroupId))
                        {
                            Debug.LogWarning($"duplicate slide start note for group {datum.GroupId} at {datum.ActionMs} ms");
                            continue;
                        }
                        slideDict.Add(datum.GroupId, new List<RawNoteData> { datum });
                    }
                    else
                    {
                        if (!slideDict.ContainsKey(datum.GroupId))
                        {
                            Debug.LogWarning($"stray non-start slide note for group {datum.GroupId} at {datum.ActionMs} ms");
                            continue;
                        }
                        slideDict[datum.GroupId].Add(datum);
                        if (datum.Type == RawNoteData.NoteType.SlideEnd)
                        {
                            List<RawNoteData> r1 = slideDict[datum.GroupId];
                            List<double> keyMs = r1.Select(d => (d.ActionMs)).OrderBy(x => x).ToList();
                            for (double i = keyMs.First() + 250; i < keyMs.Last(); i += 250)
                            {
                                if (keyMs.Contains(i)) continue;
                                r1.Add(new RawNoteData(RawNoteData.NoteType.SlideProgramFilledCheckpoint, i, datum.GroupId, 0, 0, RawNoteData.NoteModifier.None));
                            }
                            baked.Add(new NoteData(r1));
                            slideDict.Remove(datum.GroupId);
                        }
                    }
                }
            }
            return baked;
        }
    }
}
