using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerEventClip : BaseEventClip, IUndetachableEventClip, IMessageableEventClip
{
    [System.Serializable]
    public class SerializableSection
    {
        public string targetBoolName;
        public string timeFloatName;
    }
    public SerializableSection[] serializableSectionList;

    public class Section
    {

        public string targetBoolName;
        public string timeFloatName;

        public float startTime;
    }

    Section[] sectionList;

    void Awake()
    {
        sectionList = new Section[serializableSectionList.Length];
        for(int i = 0; i < sectionList.Length; i++)
        {
            sectionList[i] = new Section();
            sectionList[i].targetBoolName = serializableSectionList[i].targetBoolName;
            sectionList[i].timeFloatName = serializableSectionList[i].timeFloatName;
            sectionList[i].startTime = 0.0f;

        }
    }

    public override void EndEvent()
    {
    }

    public override void OnEvent()
    {

        foreach(var section in sectionList)
        {
            
            if(!eventController.GetBool(section.targetBoolName))
            {
                section.startTime = Time.time;
            }

            eventController.SetFloat(section.timeFloatName, Time.time - section.startTime);
        }
    }

    public override void StartEvent()
    {
    }

    public string Message
    {
        get
        {
            string text = "";
            foreach (var section in sectionList)
            {
                text += section.targetBoolName + ": " + (Time.time - section.startTime).ToString() + "\n";
            }
            return text;
        }
    }
}
