using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Looxid.Link;
using TMPro;

///Resposible for recording eeg raw data and transfer it to log system
public class Brain : MonoBehaviour
{
    private static List<(long,int)> freq=new List<(long,int)>();
    private List<(long, int)> freqtable=new List<(long, int)>();
    private List<DfSeqFrame<ExtendedEEGRawSignal>> rec = new List<DfSeqFrame<ExtendedEEGRawSignal>>();
    private Dictionary<string, object> LastSensorStatus;
    private LogSystem logsys;
    private bool DataAvailable=false;
    private float lastGameTime=0;
    public List<DfSeqFrame<ExtendedEEGRawSignal>> GetSig() {
        return rec.ConvertAll(d=>(DfSeqFrame<ExtendedEEGRawSignal>)d.Clone());
    }
    public List<(long, int)> GetFreq() {
        return freqtable.ConvertAll(d=>d);
    }
    public void RecordStop() {
        rec.Clear();
        freqtable.Clear();
        freq.Clear();
        LastSensorStatus=new Dictionary<string, object>();
    }

    void Start()
    {
        bool isIntialized = LooxidLinkManager.Instance.Initialize();
        logsys=GetComponent<LogSystem>();
        LastSensorStatus=new Dictionary<string, object>();
    }
    void OnEnable()
    {
        LooxidLinkData.OnReceiveEEGSensorStatus += OnReceiveEEGSensorStatus;
        LooxidLinkData.OnReceiveEEGRawSignals += OnReceiveEEGRawSignals;
        LooxidLinkManager.OnLinkCoreConnected += OnLinkCoreConnected;
        LooxidLinkManager.OnLinkCoreDisconnected += OnLinkCoreDisconnected;
        LooxidLinkManager.OnLinkHubConnected += OnLinkHubConnected;
        LooxidLinkManager.OnLinkHubDisconnected += OnLinkHubDisconnected;
    }
    void OnDisable()
    {
        LooxidLinkData.OnReceiveEEGSensorStatus -= OnReceiveEEGSensorStatus;
        LooxidLinkData.OnReceiveEEGRawSignals -= OnReceiveEEGRawSignals;
        LooxidLinkManager.OnLinkCoreConnected -= OnLinkCoreConnected;
        LooxidLinkManager.OnLinkCoreDisconnected -= OnLinkCoreDisconnected;
        LooxidLinkManager.OnLinkHubConnected -= OnLinkHubConnected;
        LooxidLinkManager.OnLinkHubDisconnected -= OnLinkHubDisconnected;
    }
    void CheckValidity() {
        if( !LooxidLinkManager.Instance.isLinkCoreConnected || !LooxidLinkManager.Instance.isLinkHubConnected) {
            DataAvailable=false;
        } else {
            DataAvailable=true;
        }
        
    }
    void OnLinkCoreConnected()
    {
        Debug.Log("Link Core is connected.");
        CheckValidity();
    }
    void OnLinkCoreDisconnected()
    {
        Debug.Log("Link Core is disconnected.");
        CheckValidity();
    }
    void OnLinkHubConnected()
    {
        Debug.Log("Link Hub is connected.");
        CheckValidity();
    }
    void OnLinkHubDisconnected()
    {
        Debug.Log("Link Hub is disconnected.");
        CheckValidity();
    }
    void OnReceiveEEGSensorStatus(EEGSensor sensorStatus)
    {
        var res = new Dictionary<string, object>();
        int[] temp = {0,1,2,3,4,5};
        foreach(int eeg in temp) {
            var now=(EEGSensorID)eeg;
            res.Add(now.ToString(),sensorStatus.IsSensorOn(now));
        }
        LastSensorStatus=res;
    }
    void OnReceiveEEGRawSignals(EEGRawSignal rawSignalData)
    {
        int c=rawSignalData.rawSignal.Count;
        if(c>0 && logsys.RecordStatus()) {
            freq.Add(((long)Tools.GetNowTimeInMsUTC(),c));
            rec.Add(new DfSeqFrame<ExtendedEEGRawSignal>(lastGameTime, DataAvailable, new ExtendedEEGRawSignal(rawSignalData,LastSensorStatus)));
            CalculateFreq();
        }
    }
    void CalculateFreq() {
        bool first=true;
        bool found=false;
        (long, int) firstitem=(-1, -1);
        int count=0;
        foreach((long time, int f) in freq){
            if(first){
                firstitem=(time, f);
                first=false;
            }
            if(time-firstitem.Item1>=1000) {
                Debug.Log(count);
                found=true;
                break;
            } else {
                count+=f;
            }
        }
        if(found){
            freqtable.Add(((long)(Tools.GetNowTimeInMsUTC()),count));
            freq.Clear();
        }
    }
    public Dictionary<string, object> GetLastSensorDataForDebug() {
        return LastSensorStatus;
    }
    public Dictionary<string, object> GetLastEEGDataForDebug() {
        var res=new Dictionary<string, object>();
        if(logsys.RecordStatus() && rec.Count>=1) {
            res.Add("raw",rec[rec.Count-1].data.rawSignal[0].ch_data);
        } else {
            res.Add("raw","None");
        }
        return res;
    }
    void Update()
    {
        lastGameTime=Time.time;
        CheckValidity();
    }
}
