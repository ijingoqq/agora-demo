using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Agora.Rtc;
using Random = UnityEngine.Random;

namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio
{
    public class JoinChannelAudio : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        public AppIdInput _appIdInput;

        [Header("_____________Basic Configuration_____________")]
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        public string _appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        public string _token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        public string _channelName = "";

        public Text LogText;
        internal Logger Log;
        public IRtcEngine RtcEngine = null;

        public IAudioDeviceManager _audioDeviceManager;
        public DeviceInfo[] _audioPlaybackDeviceInfos;
        public Dropdown _audioDeviceSelect;

        // Start is called before the first frame update
        protected virtual void Start()
        {
            LoadAssetData();
            if (CheckAppId())
            {
                InitRtcEngine();
                SetBasicConfiguration();
            }

#if UNITY_IOS || UNITY_ANDROID
            var text = GameObject.Find("Canvas/Scroll View/Viewport/Content/AudioDeviceManager").GetComponent<Text>();
            text.text = "Audio device manager not support in this platform";

            GameObject.Find("Canvas/Scroll View/Viewport/Content/AudioDeviceButton").SetActive(false);
            GameObject.Find("Canvas/Scroll View/Viewport/Content/deviceIdSelect").SetActive(false);
            GameObject.Find("Canvas/Scroll View/Viewport/Content/AudioSelectButton").SetActive(false);
#endif

        }

        public void Update()
        {
            PermissionHelper.RequestMicrophontPermission();
        }

        public bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset!!!!!");
        }

        //Show data in AgoraBasicProfile
        [ContextMenu("ShowAgoraBasicProfileData")]
        public void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        protected virtual void InitRtcEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext(_appID, 0,
                                        CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                                        AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_GAME_STREAMING);
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);
        }

        public void SetBasicConfiguration()
        {
            print ("SetBasicConfiguration");
            var res = RtcEngine.SetAINSMode(true, AUDIO_AINS_MODE.AINS_MODE_AGGRESSIVE);
            RtcEngine.EnableAudio();

            RtcEngine.SetParameters("{\"che.audio.livehc.enable\":true}");
            RtcEngine.SetParameters("{\"che.audio.bitrate_level\":1}");

            RtcEngine.SetChannelProfile(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        }

#region -- Button Events ---

        public void StartEchoTest()
        {
            RtcEngine.StartEchoTest(10);
            Log.UpdateLog("StartEchoTest, speak now. You cannot conduct another echo test or join a channel before StopEchoTest");
        }

        public void StopEchoTest()
        {
            RtcEngine.StopEchoTest();
        }

        public void JoinChannel()
        {
            if (PlayerController.localPlayer == null) {
                Random.InitState((int)System.DateTime.Now.Ticks);
                uint uid = (uint)Random.Range(1, 10000);
                RtcEngine.JoinChannel(_token, _channelName, uid, new ChannelMediaOptions());
            } else {
                RtcEngine.JoinChannel(_token, _channelName, PlayerController.localPlayer.uid, new ChannelMediaOptions());
            }
        }

        public void JoinChannel(uint uid) {
            RtcEngine.JoinChannel(_token, _channelName, uid, new ChannelMediaOptions());
        }

        public void JoinChannelAgora() {
            RtcEngine.EnableAudio();
            print("JoinChannelAgora uid: " + PlayerControllerAgora.localPlayer.UID);
            RtcEngine.EnableSpatialAudio(true);
            LocalSpatialAudioConfig localSpatialAudioConfig = new LocalSpatialAudioConfig();
            localSpatialAudioConfig.rtcEngine = RtcEngine;
            var localSpatial = RtcEngine.GetLocalSpatialAudioEngine();
            localSpatial.Initialize();
            AdvancedAudioOptions options = new AdvancedAudioOptions();
            options.audioProcessingChannels.SetValue(2);
            RtcEngine.SetAdvancedAudioOptions(options);
            RtcEngine.JoinChannel(_token, _channelName, PlayerControllerAgora.localPlayer.UID, new ChannelMediaOptions());
        }

        public void JoinChannelAgora(uint uid) {
            RtcEngine.EnableAudio();
            print("JoinChannelAgora uid: " + uid);
            RtcEngine.EnableSpatialAudio(true);
            LocalSpatialAudioConfig localSpatialAudioConfig = new LocalSpatialAudioConfig();
            localSpatialAudioConfig.rtcEngine = RtcEngine;
            var localSpatial = RtcEngine.GetLocalSpatialAudioEngine();
            localSpatial.Initialize();
            AdvancedAudioOptions options = new AdvancedAudioOptions();
            options.audioProcessingChannels.SetValue(2);
            RtcEngine.SetAdvancedAudioOptions(options);
            RtcEngine.JoinChannel(_token, _channelName, uid, new ChannelMediaOptions());
        }

        public void LeaveChannel()
        {
            RtcEngine.LeaveChannel();
        }

        public void StopPublishAudio()
        {
            var options = new ChannelMediaOptions();
            options.publishMicrophoneTrack.SetValue(false);
            var nRet = RtcEngine.UpdateChannelMediaOptions(options);
            this.Log.UpdateLog("UpdateChannelMediaOptions: " + nRet);
        }

        public void StartPublishAudio()
        {
            var options = new ChannelMediaOptions();
            options.publishMicrophoneTrack.SetValue(true);
            var nRet = RtcEngine.UpdateChannelMediaOptions(options);
            this.Log.UpdateLog("UpdateChannelMediaOptions: " + nRet);
        }

        public void GetAudioPlaybackDevice()
        {
            _audioDeviceSelect.ClearOptions();
            _audioDeviceManager = RtcEngine.GetAudioDeviceManager();
            _audioPlaybackDeviceInfos = _audioDeviceManager.EnumeratePlaybackDevices();
            Log.UpdateLog(string.Format("AudioPlaybackDevice count: {0}", _audioPlaybackDeviceInfos.Length));
            for (var i = 0; i < _audioPlaybackDeviceInfos.Length; i++)
            {
                Log.UpdateLog(string.Format("AudioPlaybackDevice device index: {0}, name: {1}, id: {2}", i,
                    _audioPlaybackDeviceInfos[i].deviceName, _audioPlaybackDeviceInfos[i].deviceId));
            }

            _audioDeviceSelect.AddOptions(_audioPlaybackDeviceInfos.Select(w =>
                    new Dropdown.OptionData(
                        string.Format("{0} :{1}", w.deviceName, w.deviceId)))
                .ToList());
        }

        public void SelectAudioPlaybackDevice()
        {
            if (_audioDeviceSelect == null) return;
            var option = _audioDeviceSelect.options[_audioDeviceSelect.value].text;
            if (string.IsNullOrEmpty(option)) return;

            var deviceId = option.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
            var ret = _audioDeviceManager.SetPlaybackDevice(deviceId);
            Log.UpdateLog("SelectAudioPlaybackDevice ret:" + ret + " , DeviceId: " + deviceId);
        }

#endregion

        public void OnDestroy()
        {
            Debug.Log("OnDestroy");
            if (RtcEngine == null) return;
            RtcEngine.InitEventHandler(null);
            RtcEngine.LeaveChannel();
            RtcEngine.Dispose();
        }
    }
}