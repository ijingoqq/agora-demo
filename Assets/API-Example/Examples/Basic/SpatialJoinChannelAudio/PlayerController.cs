using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelAudio;
using Mirror;
using RingBuffer;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using UnityEngine;

public class PlayerController : NetworkBehaviour {
    public float speed = 5.0f;
    public float rotationSpeed = 200.0f;
    [SyncVar]
    public uint uid = 0;

    public static List<PlayerController> players = new List<PlayerController>();

    internal RingBuffer<float> _audioBuffer;
    private const int CHANNEL = 2; // 对于立体声
    public int PULL_FREQ_PER_SEC = 100;
    public int SAMPLE_RATE = 114514;

    internal AudioClip _audioClip;
    internal int _count;

    internal int _writeCount;
    internal int _readCount;

    public static PlayerController localPlayer {
        get { 
            return players.Find(p => p.isLocalPlayer);
        }
    }

    // 期待：audioClip调用声网api后，通过unity混音支持立体声和空间音效，由agora提供AEC
    // 问题：unity混音和空间音效能够正常工作，但是声网的AEC无法正常工作
    // 初步分析：声网的api调用返回的原始音频数据是没有经过AEC处理的。根据声网官网文档，欲获得能用于混音的音频数据必须调用原始音频数据的api，
    // 因此我们需要一种获取AEC处理后的音频数据的api的方案。
    public void SetupAudio(AudioSource aud, string clipName) {
        _audioClip = AudioClip.Create(clipName,
            SAMPLE_RATE / PULL_FREQ_PER_SEC * CHANNEL,
            CHANNEL, SAMPLE_RATE, true,
            OnAudioRead);
        aud.clip = _audioClip;
        aud.loop = true;
        aud.Play();
    }

    private void OnAudioRead(float[] data) {
        lock (_audioBuffer) {
            for (var i = 0; i < data.Length; i++) {
                if (_audioBuffer.Count > 0) {
                    data[i] = _audioBuffer.Get();
                    _readCount += 1;
                }
            }
        }

        Debug.LogFormat("buffer length remains: {0}", _writeCount - _readCount);
    }

    void Start() {
        // get the system sample rate
        SAMPLE_RATE = AudioSettings.outputSampleRate;
        print("SAMPLE_RATE: " + SAMPLE_RATE);

        players.Add(this);
        if (isLocalPlayer) {
            Random.InitState((int)System.DateTime.Now.Ticks);
            uid = (uint)Random.Range(1, 10000);
            SpatialJoinChannelAudio.instance.localPlayer = this;
        } else {
            //you must init _audioBuffer before RegisterAudioFrameObserver
            //becasue when you RegisterAudioFrameObserver the OnPlaybackAudioFrame will be trigger immediately
            var bufferLength = SAMPLE_RATE * CHANNEL; // 1-sec-length buffer
            _audioBuffer = new RingBuffer<float>(bufferLength, true);

            SpatialJoinChannelAudio.instance.RtcEngine.RegisterAudioFrameObserver(new AudioFrameObserver(this),
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_PLAYBACK |
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_RECORD |
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_MIXED |
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_BEFORE_MIXING |
                 AUDIO_FRAME_POSITION.AUDIO_FRAME_POSITION_EAR_MONITORING,
                OBSERVER_MODE.RAW_DATA);
            SetupAudio(GetComponent<AudioSource>(), "RemoteAudio");
        }
    }

    void Update() {
        if (!isLocalPlayer) {
            return;
        }
        float horizontal = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        transform.Translate(0, 0, vertical);
        transform.Rotate(0, horizontal, 0);
    }
}
