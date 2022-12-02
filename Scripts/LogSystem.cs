using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; 
using Looxid.Link;
using ViveSR.anipal.Eye;
using Valve.VR;
using System;
using Newtonsoft.Json;
using TMPro;
/// <summary>Static methods that used in brain and eyesys to help their jobs</summary>
public static class Tools {
    private static void WriteFiles(string filePath, string message)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
        if (!directoryInfo.Exists) {
            directoryInfo.Create();
        }
        using (StreamWriter writer = new StreamWriter(filePath, false)){ 
            writer.Write(message);
        }
    }
    /// <summary>Write frequncy table for tracking performances</summary>
    public static void WriteTable(string filePath, ref List<(long, int)> table) {
        string str="";
        foreach((long time, int f) in table) {
            Debug.Log($"{((int)time)},{f}");
            str+=$"{((int)time)},{f}\n";
        }
        WriteFiles(filePath,str);
        Debug.Log($"Log Writed - {filePath}\nStr Length:{str.Length}\nTable Items:{table.Count}");
    }
    /// <summary>Calculate average frequency for tracking performances</summary>
    public static double avgFreq(ref List<(long, int)> table) {
        int sum=0;
        if(table.Count<=0) {
            return 0;
        }
        foreach((long time, int f) in table) {
            sum+=f; 
        }
        return sum/(double)table.Count;
    }
    /// <summary>Return current time to utc</summary>
    public static System.TimeSpan GetNowTimeSpan() {
        return System.DateTime.Now.ToUniversalTime().Subtract(new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
    }
    private static long copylong(long d) {
        return d;
    }
    public static long GetNowTimeInMsUTC() {
        return copylong((long)GetNowTimeSpan().TotalMilliseconds);
    }
    public static float[] vecToFloat(Vector3 v) {
        return new float[] {v.x,v.y,v.z};
    }
    public static float[] vecToFloat(Vector2 v) {
        return new float[] {v.x, v.y};
    }
}
/// <summary>Single Sequence Data that contains raw data.
/// Check out https://looxidlabs.github.io/link-sdk/#/looxidlinkdata
/// and https://developer-express.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/
/// to use raw data and see their structures. </summary>
public class DfSeqFrame<T> :ICloneable {
    public long timestamp=-1;
    public float gametime=-1;
    public bool isNormalData=false;
    public T data;
    public DfSeqFrame(float gameTime, bool DataAvailable, T raw) {
        timestamp=(long)Tools.GetNowTimeInMsUTC();
        gametime=gameTime;
        isNormalData=DataAvailable;
        data=raw;
    }
    public DfSeqFrame(DfSeqFrame<T> copy) {
        timestamp=copy.timestamp;
        gametime=copy.gametime;
        isNormalData=copy.isNormalData;
        data=copy.data;
    }
    public object Clone() {
        return new DfSeqFrame<T>(this);
    }
    public Dictionary<string,object> ToDict() {
        var res=new Dictionary<string,object>();
        res.Add("timestamp", timestamp); //long that time of the sequence from milliseconds
        res.Add("gametime",gametime);  //float of running gametime
        res.Add("OrdinaryDataFlag",isNormalData); //bool that contains info that weather that sequence data is captured in normal situation or not
        DictableDataParent temp = data as DictableDataParent;
        res.Add("DataType",data.GetType().ToString());
        res.Add("data",temp.ToDict());
        return res;
    }
}

public abstract class DictableDataParent {
    public abstract Dictionary<string,object> ToDict();
}
public class ColidedGameobjectInfo : DictableDataParent{
    public Vector3 pos;
    public string tag;
    public string name;
    public string scenename;
    public int layer;
    public Vector3 RayOrigin, RayDirection;
    public ColidedGameobjectInfo(FocusInfo f, Ray r) {
        pos=f.transform.position;
        tag=f.transform.gameObject.tag;
        name=f.transform.gameObject.name;
        scenename=f.transform.gameObject.scene.name;
        layer=f.transform.gameObject.layer;
        RayOrigin=r.origin;
        RayDirection=r.direction;
    }
    public ColidedGameobjectInfo() {
        pos=Vector3.zero;
        tag="Undefined";
        name="Undefined";
        scenename="Undefined";
        layer=-9999;
        RayOrigin=Vector3.zero;
        RayDirection=Vector3.zero;
    }
    public override Dictionary<string,object> ToDict() {
        var res=new Dictionary<string,object>();
        res.Add("ColidedObjectPosition",Tools.vecToFloat(pos));
        res.Add("ColidedObjectTag",tag);
        res.Add("ColidedObjectName",name);
        res.Add("SceneOfColidedObject",scenename);
        res.Add("ColidedObjectLayer",layer);
        res.Add("RayOriginPos",Tools.vecToFloat(RayOrigin));
        res.Add("RayDirectionPos",Tools.vecToFloat(RayDirection));
        return res;
    }
}
public struct ExtendedEyeInfo {
    public List<ColidedGameobjectInfo> focusRayInfo; //index uses GazeIndex enum. for left,right,combined
    public List<bool> validFocus;
}
/// <summary>Constructed by EyeData_v2 and ExetendedEyeInfo(Raycast Information)
/// Child of DictableDataParent (dictable data type)
/// Contains overall eyedata and raycast information.</summary>
public class ExtendedEyeData_v2 : DictableDataParent{ //The reason of why its name is v2:Origianl eyedata name is EyeData_v2. check constructer type
    /** Indicate if there is a user in front of HMD. */
    public bool no_user;
    /** The frame sequence.*/
    public int frame_sequence;
    /** The time when the frame was capturing. in millisecond.*/
    public int timestamp;
    public VerboseData verbose_data;
    public EyeExpression expression_data;
    public ExtendedEyeInfo extended_info;
    public ExtendedEyeData_v2(EyeData_v2 eye) {
        no_user=eye.no_user;
        frame_sequence=eye.frame_sequence;
        timestamp=eye.timestamp;
        verbose_data=eye.verbose_data;
        expression_data=eye.expression_data;
    }
    public ExtendedEyeData_v2(EyeData_v2 eye, ExtendedEyeInfo info):this(eye) {
        extended_info=info;
    }
    Dictionary<string,object> ExtendedEyeInfoToDict(ExtendedEyeInfo d) {
        var res=new Dictionary<string,object>();
        var left=new Dictionary<string,object>();
        var right=new Dictionary<string,object>();
        var combined=new Dictionary<string,object>();
        left.Add("Data",d.focusRayInfo[0].ToDict());
        left.Add("Validaty",d.validFocus[0]);
        right.Add("Data",d.focusRayInfo[1].ToDict());
        right.Add("Validaty",d.validFocus[1]);
        combined.Add("Data",d.focusRayInfo[2].ToDict());
        combined.Add("Validaty",d.validFocus[2]);
        res.Add("left",left);
        res.Add("right",right);
        res.Add("combined",combined);
        return res;
    }
    Dictionary<string,object> SingleEyeDataToDict(SingleEyeData d) {
        var res=new Dictionary<string,object>();
        res.Add("eye_data_validata_bit_mask", d.eye_data_validata_bit_mask);
        res.Add("gaze_origin_mm", Tools.vecToFloat(d.gaze_origin_mm));
        res.Add("gaze_direction_normalized", Tools.vecToFloat(d.gaze_direction_normalized));
        res.Add("pupil_diameter_mm", d.pupil_diameter_mm);
        res.Add("eye_openness", d.eye_openness);
        res.Add("pupil_position_in_sensor_area",Tools.vecToFloat(d.pupil_position_in_sensor_area));
        return res;
    }
    Dictionary<string,object> ExpressionToDict() {
        var left=new Dictionary<string,object>();
        left.Add("eye_frown",expression_data.left.eye_frown);
        left.Add("eye_squeeze",expression_data.left.eye_squeeze);
        left.Add("eye_wide",expression_data.left.eye_wide);
        var right=new Dictionary<string,object>();
        right.Add("eye_frown",expression_data.right.eye_frown);
        right.Add("eye_squeeze",expression_data.right.eye_squeeze);
        right.Add("eye_wide",expression_data.right.eye_wide);
        var res=new Dictionary<string,object>();
        res.Add("left",left);
        res.Add("right",right);
        return res;
    }
    Dictionary<string,object> verboseToDict() {
        var res=new Dictionary<string,object>();
        res.Add("left", SingleEyeDataToDict(verbose_data.left));
        res.Add("right", SingleEyeDataToDict(verbose_data.right));
        var combined=SingleEyeDataToDict(verbose_data.combined.eye_data);
        combined.Add("convergence_distance_validity", verbose_data.combined.convergence_distance_validity);
        combined.Add("convergence_distance_mm", verbose_data.combined.convergence_distance_mm);
        res.Add("combined",combined);
        return res;
    }
    public override Dictionary<string,object> ToDict() {
        var res=new Dictionary<string,object>();
        res.Add("isThereUserInFrontOfHMD",no_user);
        res.Add("FrameSequence",frame_sequence);
        res.Add("TimestampInMs",timestamp);
        res.Add("verbose_data", verboseToDict());
        res.Add("expression_data",ExpressionToDict());
        res.Add("RayFocusData", ExtendedEyeInfoToDict(this.extended_info));
        return res;
    } 
}
/// <summary>Constructed by EEGRawSignalData and main_timestamp(Timestamp of EEGRawSignal), seq(iteration number of EEGRawSignal.rawSignal)
/// Child of DictableDataParent (dictable data type)
/// Contains single EEG channel data.</summary>
public class ExtendedEEGRawSignal
{
    public double timestamp;
    public List<EEGRawSignalData> rawSignal;
    public Dictionary<string, object> LastSensorData;
    public ExtendedEEGRawSignal(EEGRawSignal origin, Dictionary<string, object> LastSensorData) {
        timestamp=origin.timestamp;
        rawSignal=origin.rawSignal;
        this.LastSensorData=LastSensorData;
    }
}
public class ExtendedBrainInfo : DictableDataParent{
    public double r_main_timestamp,  r_sub_timestamp; //main:timestamp created by EEGRawSignal, SubTimestamp:timestamp created by EEGRawSignalData(sequenced)
    public int r_seq_num, inner_seq_num; //r_seq_num:Sequence num of EEGRawSignalData, inner_seq_num:sequence from iteration of single EEGRawSignal (which ExtendedBrainInfo created)
    public double[] ch_data;
    public Dictionary<string, object> LastSensorData;
    public ExtendedBrainInfo(EEGRawSignalData raw, double main_timestamp, int seq, Dictionary<string, object> LastSensorData) {
        r_sub_timestamp=raw.timestamp;
        r_seq_num=raw.seq_num;
        ch_data=raw.ch_data;
        r_main_timestamp=main_timestamp;
        inner_seq_num=seq; // Sequence of EEGRawSignal of one frame
        this.LastSensorData=LastSensorData;
    }
    public override Dictionary<string,object> ToDict() {
        var res=new Dictionary<string,object>();
        res.Add("MainTimestamp",r_main_timestamp);
        res.Add("SeqTimestamp", r_sub_timestamp);
        res.Add("SeqNumber",r_seq_num);
        res.Add("MainIterationSeqNum",inner_seq_num);
        res.Add("ChannelData",ch_data);
        res.Add("LastSensorData",LastSensorData);
        return res;
    } 
}
public class DfRaw {
    /// <summary>Variable that indicates time recorded in milliseconds</summary>
    public long timestamp {get;set;}
    public long timestampEnd {get;set;}
    /// <summary>Variable that indicates sequence number from starting of record</summary>
    public long seq {get;set;}
    private List<DfSeqFrame<ExtendedEyeData_v2>> eye {get;set;}
    private List<DfSeqFrame<ExtendedBrainInfo>> eeg_seq {get;set;}
    public List<(long, int)> eyeFreq; //time, freq
    public List<(long, int)> eegFreq; //time, freq
    /// <summary>Dataframe that contains raw datas of tracked data.
    /// Check out https://looxidlabs.github.io/link-sdk/#/looxidlinkdata
    /// and https://developer-express.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/
    /// to use raw data and see their structures. </summary>
    public DfRaw(long starttime, long sequence, List<DfSeqFrame<ExtendedEEGRawSignal>> brainRawSeqFrame, List<DfSeqFrame<ExtendedEyeData_v2>> eyeRawSeqFrame, List<(long, int)> eyeFrequency, List<(long, int)> eegFrequency) {
        timestamp=starttime;
        seq=sequence;
        eegRawToSeq(brainRawSeqFrame);
        eye=eyeRawSeqFrame;
        eyeFreq=eyeFrequency;
        eegFreq=eegFrequency;
        timestampEnd=(long)Tools.GetNowTimeInMsUTC();
    }
    public DfRaw() {}
    /// <summary>Convert raw eeg data into sequenced data.
    /// Because EEGRawSignal contains sequence of EEGRawSignalData(single EEG data)
    /// This method works for converting List(EEGRawSIgnal) into ExtendedBrainInfo for each channel.</summary>
    public void eegRawToSeq(List<DfSeqFrame<ExtendedEEGRawSignal>> eeg) {
        eeg_seq=new List<DfSeqFrame<ExtendedBrainInfo>>();
        foreach(DfSeqFrame<ExtendedEEGRawSignal> b in eeg) {
            for(int i=0;i<b.data.rawSignal.Count;i++) {
                DfSeqFrame<ExtendedBrainInfo> newseq=new DfSeqFrame<ExtendedBrainInfo>(b.gametime, b.isNormalData, new ExtendedBrainInfo(b.data.rawSignal[i], b.data.timestamp, i,b.data.LastSensorData));
                eeg_seq.Add(newseq);
            }
        }
    }
    public override string ToString() {
        return $"timestamp:{timestamp}, seq:{seq}, eegcount:{eeg_seq.Count}, eyecount:{eye.Count}, avgeyefreq:{Tools.avgFreq(ref eyeFreq)}, avgbrainfreq:{Tools.avgFreq(ref eegFreq)}";
    }
    Dictionary<string, object> FreqToDict(ref List<(long, int)> table) {
        var res=new Dictionary<string,object>();
        foreach((long time, int freq) in table) {
            res.Add(time.ToString(), freq);
        }
        return res;
    }
    public Dictionary<string,object> ToDict() {
        var res=new Dictionary<string,object>();
        res.Add("ExperimentStartTimestampInMsUTC",timestamp);
        res.Add("ExperimentSequence",seq);
        var eegdict=new List<Dictionary<string,object>>();
        foreach(DfSeqFrame<ExtendedBrainInfo> df in eeg_seq) {
            eegdict.Add(df.ToDict());
        }
        res.Add("EEGData",eegdict);
        var eyedict=new List<Dictionary<string,object>>();
        foreach(DfSeqFrame<ExtendedEyeData_v2> df in eye) {
            eyedict.Add(df.ToDict());
        }
        res.Add("EyeData",eyedict);
        res.Add("RealtimeEyeFreq",FreqToDict(ref eyeFreq));
        res.Add("RealtimeEEGFreq",FreqToDict(ref eegFreq));
        res.Add("ExperimentEndTimestampInMsUTC",timestampEnd);
        return res;
    }
}
public class LogSystem : MonoBehaviour
{
    private bool isRecording=false, isAssignedLogPanel=false;
    private long seq=-1;
    private long timestamp=0;
    private EyeSys esys;
    private Brain bsys;
    public List<DfRaw> RecordStore=new List<DfRaw>();
    public TextMeshProUGUI log;
    public static LogSystem instance;
    void Awake() {
        if (null == instance) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(this.gameObject);
        }
    }
    void Start() {
        esys=GetComponent<EyeSys>();
        bsys=GetComponent<Brain>();
        if(log) {
            isAssignedLogPanel=true;
        }
    }
    void OnApplicationQuit() {
    }

    public void RecordStart() {
        isRecording=true;
        EyeSys.RecordFlag=isRecording;
        timestamp=Tools.GetNowTimeInMsUTC();
        seq+=1;
        Debug.Log("Record Started");
    }
    public bool RecordStatus() {
        return isRecording;
    }
    public void RecordStop() {
        isRecording=false;
        EyeSys.RecordFlag=isRecording;
        List<DfSeqFrame<ExtendedEEGRawSignal>> bsysdata=bsys.GetSig();
        List<DfSeqFrame<ExtendedEyeData_v2>> esysdata=esys.GetSig();
        List<(long, int)> efreq=esys.GetFreq();
        List<(long, int)> bfreq=bsys.GetFreq();
        DfRaw data=new DfRaw(timestamp, seq, bsysdata, esysdata, efreq, bfreq);
        RecordStore.Add(data);
        bsys.RecordStop();
        esys.RecordStop();
        Debug.Log("Record Stopped");
    }
    public void RecordSave() {
        Debug.Log(RecordStore[RecordStore.Count-1]);
        Debug.Log("Record Saving...");
        foreach(var r in RecordStore) {
            File.WriteAllText($"./{r.timestamp}.json",JsonConvert.SerializeObject(r.ToDict()));
        }
    }
    void Update() {
        
        bool plant=SteamVR_Input.GetStateDown("plant",SteamVR_Input_Sources.LeftHand) || Input.GetKeyDown(KeyCode.Home); //homekey 여부
        bool seed=SteamVR_Input.GetStateDown("plant",SteamVR_Input_Sources.RightHand) || Input.GetKeyDown(KeyCode.End); //end키 여부
        if(plant) {
            if(isRecording) {
                RecordStop();
            } else {
                RecordStart();
            }
        }
        if (seed) {
            if(RecordStore.Count>0) { //Record를 반복해서 껏다켰다 껏다켰다 하면 RecordStore에 여러개의 Record 집합체들이 저장됨
                Debug.Log(RecordStore[RecordStore.Count-1]);
                Debug.Log(Directory.GetCurrentDirectory()+"\\out.json");
                File.WriteAllText("./out.json",JsonConvert.SerializeObject(RecordStore[0].ToDict())); //그중에 맨 첫번째꺼를 파일화 시키는 소스
                //파일화는 RecordStore에 있는 element중 파일화하고싶은 애를 ToDict()를 통해 dictionary 화 시켜서 이를 JSOn이나 여러 형식으로 저장시키면 파이썬에서 불러 올 수 있음.
            }
        }
        
        if(isAssignedLogPanel) {
            log.text=$"isRecording:{isRecording}\nLastSensorStatus:{JsonConvert.SerializeObject(bsys.GetLastSensorDataForDebug(), Formatting.Indented)}\nLastEEGData:{JsonConvert.SerializeObject(bsys.GetLastEEGDataForDebug(), Formatting.Indented)}";
        }
    }
}
