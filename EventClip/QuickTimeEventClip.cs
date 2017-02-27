/*
*   最終更新日:
*       7.8.2016
*
*   説明:
*       QuickTimeEventを実装する際に使用します.
*   
*   必須:
*       Component:
*           EventController:
*           HUD_MessageManager:
*           LanguagePackManager:
*
*   更新履歴:
*       7.3.2016:
*           プログラムの完成
*
*       7.8.2016:
*           効果音を設定できるようにした
*
*/
using UnityEngine;
using System.Collections;
using System;

public class QuickTimeEventClip : BaseEventClip
{
    [System.Serializable]
    public class InputKey
    {
        //入力するキー
        public KeyCode key;

        //Event達成に必要な入力数
        public int requiredCount = 20;

        //現在の入力数
        public int keyCount = 0;
    }
    public EventController eventCtrl;

    [Space(10)]
    public InputKey[] inputKeyList;

    //制限時間
    public float timeLimit = 5.0f;

    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip failSound;

    [Space(10)]
    //メッセージのリスト
    public string[] messageList;

    //メッセージ切り替え時間
    public float messageIntervalTime = 0.3f;

    [Space(10)]
    //Event達成時に引くトリガーの名前
    public string triggerNameOnSuccess = "Success";

    //Event失敗時に引くトリガーの名前
    public string triggerNameOnFail = "Fail";

    //===内部パラメータ==============================================================
    float startTime = 0.0f;

    HUD_MessageManager messageManager;
    HUD_MessageManager.Param messageParam = new HUD_MessageManager.Param();
    int messageID = -1;
    float messageSwitchTime = 0.0f;
    int messageIndex = 0;

    void Awake()
    {
        //AudioSourceの取得
        if (successSound || failSound)
        {
            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }


    }
    void Start()
    {
        messageManager = HUD_MessageManager.instance;
        messageParam.entranceAnimation = HUD_MessageManager.MESSAGE_ENTRANCE.APPEAR;
        messageParam.exitAnimation = HUD_MessageManager.MESSAGE_EXIT.DISAPPEAR;
    }

    public override void StartEvent()
    {
        startTime = Time.time;

        //Key情報を初期化
        foreach (var inputKey in inputKeyList)
        {
            inputKey.keyCount = 0;
        }
    }

    public override void OnEvent()
    {
        //QTEに失敗したとき
        if (Time.time - startTime > timeLimit)
        {
            eventCtrl.SetTrigger(triggerNameOnFail);
            if (audioSource && failSound)
            {
                audioSource.clip = failSound;
                audioSource.Play();
            }
        }


        bool allRequired = true;

        //keyの情報を更新
        foreach (var inputKey in inputKeyList)
        {
            if (Input.GetKeyDown(inputKey.key))
            {

                inputKey.keyCount++;
            }

            if (inputKey.keyCount < inputKey.requiredCount)
            {
                allRequired = false;

            }

        }

        //QTEに成功したとき
        if (allRequired)
        {
            eventCtrl.SetTrigger(triggerNameOnSuccess);
            if (audioSource && successSound)
            {
                audioSource.clip = successSound;
                audioSource.Play();
            }
        }

        if (messageList.Length > 0)
        {
            if (Time.time - messageSwitchTime > messageIntervalTime)
            {
                messageManager.Exit(messageID);
                messageID = messageManager.Set(LanguagePackManager.instance.GetString(messageList[messageIndex]),
                    HUD_MessageManager.MESSAGE_TYPE.CENTER, HUD_MessageManager.MESSAGE_MODE.NORMAL, messageParam);
                messageManager.Show(messageID);

                //メッセージ切り替え
                if (++messageIndex >= messageList.Length)
                {
                    messageIndex = 0;
                }

                messageSwitchTime = Time.time;
            }
        }

    }

    public override void EndEvent()
    {
        messageManager.Exit(messageID);
    }
}
