﻿namespace Unosquare.FFME.Decoding
{
    using FFmpeg.AutoGen;
    using Shared;
    using System;

    /// <summary>
    /// Represents a wrapper from an unmanaged FFmpeg audio frame
    /// </summary>
    /// <seealso cref="MediaFrame" />
    /// <seealso cref="IDisposable" />
    internal sealed unsafe class AudioFrame : MediaFrame, IDisposable
    {
        #region Private Members

        private readonly object DisposeLock = new object();
        private AVFrame* m_Pointer = null;
        private bool IsDisposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFrame" /> class.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <param name="component">The component.</param>
        internal AudioFrame(AVFrame* frame, MediaComponent component)
            : base(frame, component)
        {
            m_Pointer = (AVFrame*)InternalPointer;

            // Compute the timespans.
            // We don't use for Audio frames: frame->pts = ffmpeg.av_frame_get_best_effort_timestamp(frame);
            HasValidStartTime = frame->pts != ffmpeg.AV_NOPTS_VALUE;
            StartTime = frame->pts == ffmpeg.AV_NOPTS_VALUE ?
                TimeSpan.FromTicks(0) :
                TimeSpan.FromTicks(frame->pts.ToTimeSpan(StreamTimeBase).Ticks - component.Container.MediaStartTimeOffset.Ticks);

            // Compute the audio frame duration
            if (frame->pkt_duration != 0)
                Duration = frame->pkt_duration.ToTimeSpan(StreamTimeBase);
            else
                Duration = TimeSpan.FromTicks(Convert.ToInt64(TimeSpan.TicksPerMillisecond * 1000d * frame->nb_samples / frame->sample_rate));

            EndTime = TimeSpan.FromTicks(StartTime.Ticks + Duration.Ticks);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        public override MediaType MediaType => MediaType.Audio;

        /// <summary>
        /// Gets the pointer to the unmanaged frame.
        /// </summary>
        internal AVFrame* Pointer => m_Pointer;

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public override void Dispose() =>
            Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="alsoManaged"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool alsoManaged)
        {
            lock (DisposeLock)
            {
                if (IsDisposed) return;

                if (m_Pointer != null)
                    ReleaseAVFrame(m_Pointer);

                m_Pointer = null;
                InternalPointer = IntPtr.Zero;
                IsDisposed = true;
            }
        }

        #endregion
    }
}
