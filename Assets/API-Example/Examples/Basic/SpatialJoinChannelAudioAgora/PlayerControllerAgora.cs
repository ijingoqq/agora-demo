using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Advanced.ProcessAudioRawData;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio;
using Mirror;
using RingBuffer;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using UnityEngine;

public class PlayerControllerAgora : NetworkBehaviour {
    public float speed = 5.0f;
    public float rotationSpeed = 200.0f;
    private uint uid = 0;

    // 使用普通属性包装 uid，并在 set 中调用 SetUid
    public uint UID {
        get { return uid; }
        set { CmdSetUid(value); }
    }

    public static List<PlayerControllerAgora> players = new List<PlayerControllerAgora>();

    internal RingBuffer<float> _audioBuffer;
    private const int CHANNEL = 2; // 对于立体声
    public int PULL_FREQ_PER_SEC = 100;
    public int SAMPLE_RATE = 48000;

    internal AudioClip _audioClip;
    internal int _count;

    internal int _writeCount;
    internal int _readCount;

    float _updatePositionTime = 0f;
    float updatePositionInterval = 1f;

    public static PlayerControllerAgora localPlayer {
        get {
            return players.Find(p => p.isLocalPlayer);
        }
    }

    [Command]
    void CmdSetUid(uint newUid) {
        uid = newUid;
        RpcUpdateUid(newUid);
    }

    // 客户端 RPC，用于更新所有客户端上的 uid 值
    [ClientRpc]
    void RpcUpdateUid(uint newUid) {
        uid = newUid;
    }

    void Start() {
        players.Add(this);
        if (isLocalPlayer) {
            Random.InitState((int)System.DateTime.Now.Ticks);
            UID = (uint)Random.Range(1, 10000);
        }
    }

    void Update() {

        if (_updatePositionTime <= updatePositionInterval) {
            _updatePositionTime += Time.deltaTime;
        } else {
            _updatePositionTime = 0f;
            if (isLocalPlayer) {
                var res = SpatialJoinChannelAudioAgora.instance.SpatialAudioEngine.UpdateSelfPosition(new float[] { transform.position.x, transform.position.y, transform.position.z }, new float[] { transform.forward.x, transform.forward.y, transform.forward.z },
                new float[] { transform.right.x, transform.right.y, transform.right.z }, new float[] { transform.up.x, transform.up.y, transform.up.z });
                print (res);
                print ("update self position: " + transform.position.x + " " + transform.position.y + " " + transform.position.z);
            } else {
                var res = SpatialJoinChannelAudioAgora.instance.SpatialAudioEngine.UpdateRemotePosition(uid, new RemoteVoicePositionInfo(new float[] { transform.position.x, transform.position.y, transform.position.z }, new float[] { transform.forward.x, transform.forward.y, transform.forward.z }));
                print (res);
                print ("update remote position: " + transform.position.x + " " + transform.position.y + " " + transform.position.z);
            }
            
        }

        if (!isLocalPlayer) {
            return;
        }
        float horizontal = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        transform.Translate(0, 0, vertical);
        transform.Rotate(0, horizontal, 0);

    }

    internal class UserEventHandlerAgora : IRtcEngineEventHandler {
        private readonly JoinChannelAudio _joinChannelAudio;

        internal UserEventHandlerAgora(JoinChannelAudio joinChannelAudio) {
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

}
