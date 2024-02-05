using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.ProcessAudioRawData;
using UnityEngine;

namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio {
    public class SpatialJoinChannelAudio : JoinChannelAudio {
        private AudioSource _audioSource {
            get {
                return localPlayer.GetComponent<AudioSource>();
            }
        }

        public static SpatialJoinChannelAudio instance;

        private PlayerController _localPlayer;
        public PlayerController localPlayer {
            get {
                return _localPlayer;
            }
            set {
                _localPlayer = value;
            }
        }


        private const int CHANNEL = 2; // 对于立体声
        public int PULL_FREQ_PER_SEC = 100;
        public int SAMPLE_RATE = 48000;

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
                AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
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

            // 关闭agora自带的操纵扬声器播放的功能，然后希望能把api传来的pcm数据传给unity的audioClip
            RtcEngine.AdjustPlaybackSignalVolume(0);
        }

    }

    internal class UserEventHandler : IRtcEngineEventHandler {
        private readonly JoinChannelAudio _joinChannelAudio;

        internal UserEventHandler(JoinChannelAudio joinChannelAudio) {
            _joinChannelAudio = joinChannelAudio;
        }
        public override void OnError(int err, string msg) {
            _joinChannelAudio.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed) {
            int build = 0;
            _joinChannelAudio.Log.UpdateLog(string.Format("sdk version: ${0}",
                _joinChannelAudio.RtcEngine.GetVersion(ref build)));
            _joinChannelAudio.Log.UpdateLog(
                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                    connection.channelId, connection.localUid, elapsed));
        }

        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed) {
            _joinChannelAudio.Log.UpdateLog("OnRejoinChannelSuccess");
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats) {
            _joinChannelAudio.Log.UpdateLog("OnLeaveChannel");
        }

        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole,
            CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions) {
            _joinChannelAudio.Log.UpdateLog("OnClientRoleChanged");
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed) {
            _joinChannelAudio.Log.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid,
                elapsed));
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason) {
            _joinChannelAudio.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
                (int)reason));
        }
    }

    internal class AudioFrameObserver : IAudioFrameObserver {
        private PlayerController _playerController;
        private AudioParams _audioParams;


        internal AudioFrameObserver(PlayerController playerController) {
            _playerController = playerController;

        }

        // 需要在这里获得混音前的音频数据
        // 如果获得的是混音后的数据的话，那么它会受到AdjustPlaybackSignalVolume(0)的影响。混音在这里似乎不仅指的是AEC后的声音，还包括AdjustPlaybackSignalVolume(0)的调整，也就是说：
        // 要么我能获得AEC后的数据，但是无法避免声网的默认声音从扬声器中直接播放，
        // 要么我能避免声网的默认声音从扬声器中直接播放，但是无法获得AEC后的数据。
        // 这是我遇到的矛盾点，因此我希望声网工程师能够提供一个解决方案。
        public override bool OnPlaybackAudioFrameBeforeMixing(string channel_id,
                                                        uint uid,
                                                        AudioFrame audio_frame) {
            Debug.Log("OnPlaybackAudioFrameBeforeMixing-----------");

            var floatArray = ProcessAudioRawData.ConvertByteToFloat16(audio_frame.RawBuffer);

            lock (_playerController._audioBuffer) {
                _playerController._audioBuffer.Put(floatArray);
                _playerController._writeCount += floatArray.Length;
                _playerController._count++;
            }
            Debug.Log(uid);
            return false;
        }
    }


}

