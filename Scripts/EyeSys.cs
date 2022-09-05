using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using ViveSR.anipal.Eye;
///Resposible for recording eye raw data and transfer it to log system
public class EyeSys : MonoBehaviour
{
    private bool RegisteredEyeCallBack=false;
    private static List<int> freq=new List<int>();
    private static List<(long, int)> freqtable=new List<(long, int)>();
    private static bool DataAvailable=false;
    ///Used static record flag because eye tracking system uses static callback so cannot access to LogSystem
    public static bool RecordFlag=false;
    private static List<DfSeqFrame<ExtendedEyeData_v2>> rec = new List<DfSeqFrame<ExtendedEyeData_v2>>();
    private static float lastGameTime=0;
    private LogSystem logsys;
    private static EyeData_v2 lastEyeData=new EyeData_v2();
    private static ExtendedEyeInfo lastExtendedEyeInfo = new ExtendedEyeInfo();
    public List<DfSeqFrame<ExtendedEyeData_v2>> GetSig() {
        return rec.ConvertAll(d=>(DfSeqFrame<ExtendedEyeData_v2>)d.Clone());
    }
    public List<(long, int)> GetFreq() {
        return freqtable.ConvertAll(d=>d);
    }
    public void RecordStop() {
        rec.Clear();
        RecordFlag=false;
        freqtable.Clear();
        freq.Clear();
    }
    
    void Start()
    {
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            Debug.LogError("Eye system malfunctioned, couldn't enable");
            enabled = false;
            DataAvailable=false;
            return;
        }
        logsys=GetComponent<LogSystem>();
    }
    void Update()
    {
        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && RegisteredEyeCallBack==false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            RegisteredEyeCallBack=true;
        }
        lastGameTime=Time.time;
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) {
                DataAvailable=false;
            } else {
                DataAvailable=true;
            }
        lastExtendedEyeInfo=getExtendedEyeData(ref lastEyeData);
    }
    
    private static void CalculateFreq(){
        bool first=true;
        bool found=false;
        int firstitem=-1;
        int count=0;
        foreach(int f in freq){
            if(first){
                firstitem=f;
                first=false;
            }
            if(f-firstitem>=1000) {
                Debug.Log(count);
                found=true;
                break;
            } else {
                count+=1;
            }
        }
        if(found){
            freqtable.Add(((long)(Tools.GetNowTimeInMsUTC()),count));
            freq.Clear();
        }
    }
    private static ExtendedEyeInfo getExtendedEyeData(ref EyeData_v2 eye_data) {
        var temp=false;
        List<ColidedGameobjectInfo> focusRayInfo=new List<ColidedGameobjectInfo>();
        List<bool> validFocus=new List<bool>();
        Ray focusRay;
        FocusInfo focusinfo;
        for(int i=0;i<3;i++) {
            temp=SRanipal_Eye_v2.Focus((GazeIndex)i, out focusRay, out focusinfo, eye_data);
            validFocus.Add(temp);
            if(temp) {
                focusRayInfo.Add(new ColidedGameobjectInfo(focusinfo, focusRay));
            } else {
                focusRayInfo.Add(new ColidedGameobjectInfo());
            }
            
        }
        ExtendedEyeInfo addition;
        addition.focusRayInfo=focusRayInfo;
        addition.validFocus=validFocus;
        return addition;
    }
    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        lastEyeData=eye_data;
        if(RecordFlag) {
            freq.Add(eye_data.timestamp);
            rec.Add(new DfSeqFrame<ExtendedEyeData_v2>(lastGameTime,DataAvailable,new ExtendedEyeData_v2(eye_data, lastExtendedEyeInfo)));
            CalculateFreq();
        }
    }
}
