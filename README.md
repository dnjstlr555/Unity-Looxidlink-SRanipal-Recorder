# Unity-Looxidlink-SRanipal-Recorder
It is tool for recording Eye Tracking data from HTC vive, Raw EEG data from [LooxidLink](https://looxidlink.looxidlabs.com/) and converting stored data into Dictionary to be able to pass data to python into Json type.

# Dependencies
[Follow this instruction](https://looxidlabs.github.io/link-sdk/#/getting-started) to install SteamVR and LooxidLink SDK and [Follow this instruction](https://forum.htc.com/topic/5642-sranipal-getting-started-steps/) for install SRanipal Setup. (HTC Vive Eye tracker).<br>**For SRaniapl(Eye tracking), If sdk's version does not match with SRanipal Runtime version project will be crashed.**<br>
**The version of SRanipal used in this project is 1.3.5.5.** It is latest version from 2022/12/04.<br>
This asset requires these assets and execute files.<br>
|Name|Location|Project Used Version|Latest URL|
|---|---|---|---|
|SteamVR Asset|At unity project|-|[Download at here](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)|
|LooxidLink Unity SDK|At unity project|[Download at here(LooxidLink_Unity_SDK_1.0.1.unitypackage)](https://github.com/dnjstlr555/Unity-Looxidlink-SRanipal-Recorder/releases/tag/v1.0.1)|[Download at here](https://github.com/LooxidLabs/link-sdk/releases/)|
|LooxidLink Core|At computer|-|[Download at here](https://looxidlink.looxidlabs.com/product/looxid-link-core/)|
|SRanipal SDK|At unity project|[Download at here(ViveSR.unitypackage)](https://github.com/dnjstlr555/Unity-Looxidlink-SRanipal-Recorder/releases/tag/v1.0.1)|[Download at here](https://developer.vive.com/resources/vive-sense/eye-and-facial-tracking-sdk/download/latest/)|
|SRanipal Runtime|At computer|1.3.5.5|[Download at here](https://developer.vive.com/kr/support/sdk/category_howto/how-to-update-vive-eye-tracking-runtime.html)
# Install
To install log system, you need to import package from [From release page of this repository](https://github.com/dnjstlr555/Unity-Looxidlink-SRanipal-Recorder/releases) . after import, you can import SteamVR, LooxidLink, SRanipal SDK to work with this system.<br><br>
![image](https://user-images.githubusercontent.com/21963949/188419872-bb180f7a-6bcc-4986-84cc-d82773011113.png)<br>
To create log system to record eye tracking data and eeg signal, create empty GameObject and add **SRanipal_Eye_Framework** and **LogSystem**, **EyeSys**, **Brain** components(scripts) at single GameObject. and set **Enable Eye Data Callback** to True and select **Version 2** at **Enable Eye Version** of **SRanipal_Eye_Framework**. <br>
# Usage
![logsystem](https://user-images.githubusercontent.com/21963949/188422657-22c0640b-b8da-4563-9b48-018257e89a13.png)
Log system follows **singleton** pattern, once you made GameObject following above instruction, you can access logsystem and call **RecordStart()** method and **RecordStop()** method by accessing static variable **instance** of **LogSystem** class.<br><br>you can record multiple times by calling recordstart many times, the result will be stored at **RecordStore** list as type of **DfRaw**. <br><br>DfRaw have **ToDict()** methods, which converts all stored data into dictionary which consisted compatible data type to python and javascript and so on.<br><br>
![image](https://user-images.githubusercontent.com/21963949/188426398-4f990c4f-1981-437f-a13c-cbba719396a5.png)<br>
There is example scene and script part in the package, you can start or stop record by pressing left **plant** button and save it to file by pressing right **plant** button at htc vive controller. **plant** button is custom button created by following [this tutorial](https://valvesoftware.github.io/steamvr_unity_plugin/tutorials/SteamVR-Input.html), to create new input button from HTC Vive controller check this article and tweak LogSystem's update method.
```
if(LogSystem.instance.RecordStore.Count>0) {
    File.WriteAllText("./out.json",JsonConvert.SerializeObject(LogSystem.instance.RecordStore[0].ToDict()));
}
```
## Data Structure
Data structure detailed in **translator.ipynb**(Korean)
## Python Integration
Tested for importing json data exported from LogSystem. To see sample codes importing eeg data and draw PSD and filtered EEG signal, see **interpolation.ipynb**
