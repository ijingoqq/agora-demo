using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.ProcessAudioRawData;
using RingBuffer;
using System;
using System.Drawing.Printing;
using UnityEngine;

namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio {
    public class SpatialJoinChannelAudioAgora : JoinChannelAudio {
        public static SpatialJoinChannelAudioAgora instance;


        private const int CHANNEL = 2; // 对于立体声
        public int PULL_FREQ_PER_SEC = 100;
        public int SAMPLE_RATE = 48000;
        public ILocalSpatialAudioEngine SpatialAudioEngine;

        protected override void Start() {
            base.Start();
            instance = this;
        }

        protected override void InitRtcEngine() {
            print ("InitRtcEngine");

            //You can hear two layers of sound, one is played by Rtc SDK,
            //and the other is played by Unity.audioClip
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext(_appID, 0,
                CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT, AREA_CODE.AREA_CODE_GLOB, new LogConfig("./audoFrameLog.txt"));
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);

            RtcEngine.SetPlaybackAudioFrameParameters(SAMPLE_RATE, CHANNEL,
                RAW_AUDIO_FRAME_OP_MODE_TYPE.RAW_AUDIO_FRAME_OP_MODE_READ_ONLY, 1024);
            RtcEngine.SetRecordingAudioFrameParameters(SAMPLE_RATE, CHANNEL,
                RAW_AUDIO_FRAME_OP_MODE_TYPE.RAW_AUDIO_FRAME_OP_MODE_READ_ONLY, 1024);
            RtcEngine.SetMixedAudioFrameParameters(SAMPLE_RATE, CHANNEL, 1024);
            RtcEngine.SetEarMonitoringAudioFrameParameters(SAMPLE_RATE, CHANNEL,
                RAW_AUDIO_FRAME_OP_MODE_TYPE.RAW_AUDIO_FRAME_OP_MODE_READ_ONLY, 1024);
            // Demo中没这句，这是根据官方文档加的
            RtcEngine.SetPlaybackAudioFrameBeforeMixingParameters(SAMPLE_RATE, CHANNEL);

            InitSpatialAudioEngine();

            //RtcEngine.AdjustPlaybackSignalVolume(0);
        }
        private void InitSpatialAudioEngine() {
            SpatialAudioEngine = RtcEngine.GetLocalSpatialAudioEngine();
            var ret = SpatialAudioEngine.Initialize();
            Debug.Log("_spatialAudioEngine: Initialize " + ret);
            Debug.Log(SpatialAudioEngine.SetAudioRecvRange(30));
            Debug.Log(SpatialAudioEngine.SetDistanceUnit(1));
        }

    }


}

