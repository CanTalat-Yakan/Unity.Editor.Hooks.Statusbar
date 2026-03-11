#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    public static class StatusbarCallback
    {
        private const int MaxSetupAttempts = 400;
        private const string OverlayDockName = "UnityEssentialsStatusbarOverlay";
        private const string LeftDockName = "UnityEssentialsStatusbarLeftDock";
        private const string RightDockName = "UnityEssentialsStatusbarRightDock";
        private const float SideGutter = 6f;

        private static int setupAttempts;
        private static VisualElement appStatusBarRoot;
        private static VisualElement overlayRoot;
        private static VisualElement nativeStatusBarContent;
        private static VisualElement leftDockRoot;
        private static VisualElement rightDockRoot;

        public static Action OnStatusbarGUI;
        public static Action OnStatusbarGUILeft;
        public static Action OnStatusbarGUIRight;
        public static float LeftPadding;
        public static float RightPadding;

        static StatusbarCallback()
        {
            EditorApplication.update -= Initialize;
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            setupAttempts++;

            if (!TryFindAppStatusBarRoot(out VisualElement root))
            {
                TryAbort("Could not find AppStatusBar root.");
                return;
            }

            if (root == null)
            {
                TryAbort("AppStatusBar root is null.");
                return;
            }

            if (root.Q(OverlayDockName) != null)
            {
                appStatusBarRoot = root;
                overlayRoot = root.Q(OverlayDockName);
                nativeStatusBarContent = FindNativeStatusBarContent(root);
                leftDockRoot = overlayRoot?.Q(LeftDockName);
                rightDockRoot = overlayRoot?.Q(RightDockName);
                EditorApplication.update -= UpdateStatusBarOffsets;
                EditorApplication.update += UpdateStatusBarOffsets;
                EditorApplication.update -= Initialize;
                return;
            }

            VisualElement overlay = new VisualElement { name = OverlayDockName };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.flexDirection = FlexDirection.Row;
            overlay.style.alignItems = Align.Center;
            overlay.pickingMode = PickingMode.Ignore;

            VisualElement leftDock = CreateDock(LeftDockName, Justify.FlexStart, false);
            VisualElement centerDock = CreateDock(Justify.Center, true);
            VisualElement rightDock = CreateDock(RightDockName, Justify.FlexEnd, false);

            leftDock.Add(new IMGUIContainer(() => OnStatusbarGUILeft?.Invoke()));
            centerDock.Add(new IMGUIContainer(() => OnStatusbarGUI?.Invoke()));
            rightDock.Add(new IMGUIContainer(() => OnStatusbarGUIRight?.Invoke()));

            overlay.Add(leftDock);
            overlay.Add(centerDock);
            overlay.Add(rightDock);

            root.Add(overlay);
            overlay.BringToFront();

            appStatusBarRoot = root;
            overlayRoot = overlay;
            nativeStatusBarContent = FindNativeStatusBarContent(root);
            leftDockRoot = leftDock;
            rightDockRoot = rightDock;
            EditorApplication.update -= UpdateStatusBarOffsets;
            EditorApplication.update += UpdateStatusBarOffsets;

            EditorApplication.update -= Initialize;
        }

        private static VisualElement CreateDock(Justify justify, bool flexible)
        {
            return CreateDock(string.Empty, justify, flexible);
        }

        private static VisualElement CreateDock(string name, Justify justify, bool flexible)
        {
            VisualElement dock = new VisualElement { name = name };
            dock.style.flexGrow = flexible ? 1 : 0;
            dock.style.flexShrink = flexible ? 1 : 0;
            dock.style.flexBasis = flexible ? 0 : StyleKeyword.Auto;
            dock.style.flexDirection = FlexDirection.Row;
            dock.style.alignItems = Align.Center;
            dock.style.justifyContent = justify;
            dock.pickingMode = PickingMode.Ignore;
            return dock;
        }

        private static void UpdateStatusBarOffsets()
        {
            if (appStatusBarRoot == null)
                return;

            if (overlayRoot == null)
                overlayRoot = appStatusBarRoot.Q(OverlayDockName);

            if (overlayRoot == null)
                return;

            if (nativeStatusBarContent == null)
                nativeStatusBarContent = FindNativeStatusBarContent(appStatusBarRoot);

            if (leftDockRoot == null)
                leftDockRoot = overlayRoot.Q(LeftDockName);

            if (rightDockRoot == null)
                rightDockRoot = overlayRoot.Q(RightDockName);

            if (nativeStatusBarContent == null)
                return;

            float measuredLeft = leftDockRoot != null ? Mathf.Max(0f, leftDockRoot.resolvedStyle.width) : 0f;
            float measuredRight = rightDockRoot != null ? Mathf.Max(0f, rightDockRoot.resolvedStyle.width) : 0f;

            float leftReserve = measuredLeft > 0f ? measuredLeft + SideGutter : 0f;
            float rightReserve = measuredRight > 0f ? measuredRight + SideGutter : 0f;

            // Optional manual additions for special cases.
            leftReserve += Mathf.Max(0f, LeftPadding);
            rightReserve += Mathf.Max(0f, RightPadding);

            // Keep UnityEssentials overlay full-width for center overlay behavior.
            overlayRoot.style.left = 0f;
            overlayRoot.style.right = 0f;

            // Shift native Unity AppStatusBar content so left/right injected GUI has room.
            nativeStatusBarContent.style.left = leftReserve;
            nativeStatusBarContent.style.right = rightReserve;
        }

        private static VisualElement FindNativeStatusBarContent(VisualElement root)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                VisualElement child = root[i];
                if (child == null)
                    continue;

                if (!string.IsNullOrEmpty(child.name) && child.name.Equals(OverlayDockName, StringComparison.Ordinal))
                    continue;

                if (child is IMGUIContainer)
                    return child;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                VisualElement child = root[i];
                if (child == null)
                    continue;

                if (!string.IsNullOrEmpty(child.name) && child.name.Equals(OverlayDockName, StringComparison.Ordinal))
                    continue;

                return child;
            }

            return null;
        }

        private static bool TryFindAppStatusBarRoot(out VisualElement root)
        {
            root = null;

            Assembly editorAssembly = typeof(Editor).Assembly;
            Type guiViewType = editorAssembly.GetType("UnityEditor.GUIView");
            if (guiViewType == null)
                return false;

            PropertyInfo visualTreeProperty = guiViewType.GetProperty(
                "visualTree",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (visualTreeProperty == null)
                return false;

            UnityEngine.Object[] guiViews;
            try
            {
                guiViews = Resources.FindObjectsOfTypeAll(guiViewType);
            }
            catch
            {
                return false;
            }

            for (int i = 0; i < guiViews.Length; i++)
            {
                UnityEngine.Object guiView = guiViews[i];
                if (guiView == null)
                    continue;

                string typeName = guiView.GetType().FullName;
                if (string.IsNullOrEmpty(typeName) ||
                    typeName.IndexOf("AppStatusBar", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                try
                {
                    root = visualTreeProperty.GetValue(guiView) as VisualElement;
                }
                catch
                {
                    root = null;
                }

                if (root != null)
                    return true;
            }

            return false;
        }

        private static void TryAbort(string reason)
        {
            if (setupAttempts <= MaxSetupAttempts)
                return;

            Debug.LogWarning($"[StatusbarCallback] {reason} Aborting statusbar callback setup.");
            EditorApplication.update -= Initialize;
        }
    }
}
#endif