using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{

    //ノードを管理するクラス
    //ノードからのメッセージを外部に伝えます
    public static class NodeManager
    {
        public static Node selected;
        public static string message = "(non)";
        public static Node entry;
        public static Node running;
        public static void Init()
        {
            message = "(non)";
            selected = null;
            entry = null;
            running = null;
        }
    }
}