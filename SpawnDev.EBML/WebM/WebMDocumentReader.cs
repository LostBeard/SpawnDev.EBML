using SpawnDev.EBML.Matroska;

namespace SpawnDev.EBML.WebM
{
    public class WebMDocumentReader : EBMLDocumentReader
    {
        public WebMDocumentReader(Stream? stream = null) : base(stream, new List<EBMLSchema> { new WebMSchema(), new MatroskaSchema() })
        {
            
        }

        /// <summary>
        /// Get and Set for TimecodeScale from the first segment block
        /// </summary>
        public virtual uint? TimecodeScale
        {
            get
            {
                var timecodeScale = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.TimecodeScale);
                return timecodeScale != null ? (uint)timecodeScale.Data : null;
            }
            set
            {
                var timecodeScale = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.TimecodeScale);
                if (timecodeScale == null)
                {
                    if (value != null)
                    {
                        var info = GetContainer(MatroskaId.Segment, MatroskaId.Info);
                        info!.Add(MatroskaId.TimecodeScale, value.Value);
                    }
                }
                else
                {
                    if (value == null)
                    {
                        var info = GetContainer(MatroskaId.Segment, MatroskaId.Info);
                        info!.Remove(timecodeScale);
                    }
                    else
                    {
                        timecodeScale.Data = value.Value;
                    }
                }
            }
        }

        string? Title
        {
            get
            {
                var title = GetElement<StringElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.Title);
                return (string?)title;
            }
            set
            {
                var title = GetElement<StringElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.Title);
                if (title == null)
                {
                    if (value != null)
                    {
                        var info = GetContainer(MatroskaId.Segment, MatroskaId.Info);
                        info!.Add(MatroskaId.Title, value);
                    }
                }
                else
                {
                    if (value == null)
                    {
                        title.Remove();
                    }
                    else
                    {
                        title.Data = value;
                    }
                }
            }
        }

        string? MuxingApp
        {
            get
            {
                var docType = GetElement<StringElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.MuxingApp);
                return docType != null ? docType.Data : null;
            }
        }

        string? WritingApp
        {
            get
            {
                var docType = GetElement<StringElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.WritingApp);
                return docType != null ? docType.Data : null;
            }
        }

        //string? EBMLDocType
        //{
        //    get
        //    {
        //        var docType = GetElement<StringElement>(MatroskaId.EBML, MatroskaId.DocType);
        //        return docType != null ? docType.Data : null;
        //    }
        //}

        /// <summary>
        /// Returns true if audio tracks exist
        /// </summary>
        public virtual bool HasAudio => GetElements<TrackEntryElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry).Where(o => o.TrackType == TrackType.Audio).Any();

        public virtual uint? AudioChannels
        {
            get
            {
                var channels = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry, MatroskaId.Audio, MatroskaId.Channels);
                return channels != null ? (uint)channels : null;
            }
        }
        public virtual double? AudioSamplingFrequency
        {
            get
            {
                var samplingFrequency = GetElement<FloatElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry, MatroskaId.Audio, MatroskaId.SamplingFrequency);
                return samplingFrequency != null ? (double)samplingFrequency : null;
            }
        }
        public virtual uint? AudioBitDepth
        {
            get
            {
                var bitDepth = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry, MatroskaId.Audio, MatroskaId.BitDepth);
                return bitDepth != null ? (uint)bitDepth : null;
            }
        }


        /// <summary>
        /// Returns true if video tracks exist
        /// </summary>
        public virtual bool HasVideo => GetElements<TrackEntryElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry).Where(o => o.TrackType == TrackType.Video).Any();

        public virtual string VideoCodecID => GetElements<TrackEntryElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry).Where(o => o.TrackType == TrackType.Video).FirstOrDefault()?.CodecID ?? "";

        public virtual string AudioCodecID => GetElements<TrackEntryElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry).Where(o => o.TrackType == TrackType.Audio).FirstOrDefault()?.CodecID ?? "";

        public virtual uint? VideoPixelWidth
        {
            get
            {
                var pixelWidth = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry, MatroskaId.Video, MatroskaId.PixelWidth);
                return pixelWidth != null ? (uint)pixelWidth : null;
            }
        }

        public virtual uint? VideoPixelHeight
        {
            get
            {
                var pixelHeight = GetElement<UintElement>(MatroskaId.Segment, MatroskaId.Tracks, MatroskaId.TrackEntry, MatroskaId.Video, MatroskaId.PixelHeight);
                return pixelHeight != null ? (uint)pixelHeight : null;
            }
        }

        /// <summary>
        /// Get and Set for the first segment block duration
        /// </summary>
        public virtual double? Duration
        {
            get
            {
                var duration = GetElement<FloatElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.Duration);
                return duration != null ? duration.Data : null;
            }
            set
            {
                var duration = GetElement<FloatElement>(MatroskaId.Segment, MatroskaId.Info, MatroskaId.Duration);
                if (duration == null)
                {
                    if (value != null)
                    {
                        var info = GetContainer(MatroskaId.Segment, MatroskaId.Info);
                        info!.Add(MatroskaId.Duration, value.Value);
                    }
                }
                else
                {
                    if (value == null)
                    {
                        var info = GetContainer(MatroskaId.Segment, MatroskaId.Info);
                        info!.Remove(duration);
                    }
                    else
                    {
                        duration.Data = value.Value;
                    }
                }
            }
        }

        /// <summary>
        /// If the Duration is not set in the first segment block, the duration will be calculated using Cluster and SimpleBlock data and written to Duration
        /// </summary>
        /// <returns></returns>
        public virtual bool FixDuration()
        {
            if (EBML == null) return false;
            if (Duration == null)
            {
                var durationEstimate = GetDurationEstimate();
                Duration = durationEstimate;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Duration calculated using Cluster and SimpleBlock data and written to Duration
        /// </summary>
        /// <returns></returns>
        public virtual double GetDurationEstimate()
        {
            if (EBML == null) return 0;
            double duration = 0;
            var segments = GetContainers(MatroskaId.Segment);
            foreach (var segment in segments)
            {
                var clusters = segment.GetContainers(MatroskaId.Cluster);
                foreach (var cluster in clusters)
                {
                    var timecode = cluster.GetElement<UintElement>(MatroskaId.Timecode);
                    if (timecode != null)
                    {
                        duration = timecode.Data;
                    };
                    var simpleBlocks = cluster.GetElements<SimpleBlockElement>(MatroskaId.SimpleBlock);
                    var simpleBlockLast = simpleBlocks.LastOrDefault();
                    if (simpleBlockLast != null)
                    {
                        duration += simpleBlockLast.Timecode;
                    }
                }
            }
            return duration;
        }
    }
}
