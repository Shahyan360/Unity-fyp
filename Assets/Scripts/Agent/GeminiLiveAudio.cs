using UnityEngine;

/// <summary>Consume native PCM at the audio-device clock, rather than prefetching a one-second streaming clip.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class GeminiLiveAudio:MonoBehaviour
{
    private readonly object gate=new object();
    private readonly float[] samples=new float[24000*60];
    private int read,write,count;
    private double phase;
    private float previous,next;
    private int outputRate;
    public float Level {get;private set;}
    public float BufferedSeconds {get{lock(gate)return count/24000f;}}
    private void Awake(){outputRate=AudioSettings.outputSampleRate;}
    public void Enqueue(byte[] pcm)
    {
        lock(gate)for(int i=0;i+1<pcm.Length&&count<samples.Length;i+=2)
        {samples[write]=(short)(pcm[i]|(pcm[i+1]<<8))/32768f;write=(write+1)%samples.Length;count++;}
    }
    public void Clear(){lock(gate){count=read=write=0;phase=0;previous=next=0;Level=0;}}
    private float Take(){if(count==0)return 0;float v=samples[read];read=(read+1)%samples.Length;count--;return v;}
    private void OnAudioFilterRead(float[] data,int channels)
    {
        float square=0;double step=24000.0/outputRate;
        lock(gate)for(int i=0;i<data.Length;i+=channels)
        {
            while(phase>=1){previous=next;next=Take();phase-=1;}
            float value=previous+(next-previous)*(float)phase;phase+=step;square+=value*value;
            for(int c=0;c<channels;c++)data[i+c]=value;
        }
        Level=Mathf.Sqrt(square/Mathf.Max(1,data.Length/channels));
    }
}
