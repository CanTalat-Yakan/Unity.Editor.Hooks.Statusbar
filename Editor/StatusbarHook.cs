#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    [InitializeOnLoad]
    public static class StatusbarHook
    {
        public static readonly List<Action> StatusbarGUI = new List<Action>();
        public static readonly List<Action> LeftStatusbarGUI = new List<Action>();
        public static readonly List<Action> RightStatusbarGUI = new List<Action>();

        public static float LeftPadding;
        public static float RightPadding;

        static StatusbarHook()
        {
            StatusbarCallback.OnStatusbarGUI = GUICenter;
            StatusbarCallback.OnStatusbarGUILeft = GUILeft;
            StatusbarCallback.OnStatusbarGUIRight = GUIRight;
        }

        public static void GUICenter()
        {
            GUILayout.BeginHorizontal();
            foreach (Action handler in StatusbarGUI)
                handler?.Invoke();

            GUILayout.EndHorizontal();
        }

        public static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (Action handler in LeftStatusbarGUI)
                handler?.Invoke();

            GUILayout.EndHorizontal();
        }

        public static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach (Action handler in RightStatusbarGUI)
                handler?.Invoke();

            GUILayout.EndHorizontal();
        }
    }
}
#endif
