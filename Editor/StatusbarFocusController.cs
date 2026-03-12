#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    public static class StatusbarFocusController
    {
        private static UnityEngine.Object s_appStatusBarView;
        private static VisualElement s_leftDockRoot;
        private static VisualElement s_rightDockRoot;
        private static IMGUIContainer s_leftDockContainer;
        private static IMGUIContainer s_rightDockContainer;
        private static bool s_isMonitoringFocusLoss;
        private static bool s_wasStatusBarFocused;
        private static bool s_hasObservedStatusBarFocus;

        public static void Configure(
            UnityEngine.Object appStatusBarView,
            VisualElement leftDockRoot,
            VisualElement rightDockRoot,
            IMGUIContainer leftDockContainer,
            IMGUIContainer rightDockContainer)
        {
            s_appStatusBarView = appStatusBarView;
            s_leftDockRoot = leftDockRoot;
            s_rightDockRoot = rightDockRoot;
            s_leftDockContainer = leftDockContainer;
            s_rightDockContainer = rightDockContainer;
        }

        public static void UpdateDockRoots(VisualElement leftDockRoot, VisualElement rightDockRoot)
        {
            if (leftDockRoot != null)
                s_leftDockRoot = leftDockRoot;

            if (rightDockRoot != null)
                s_rightDockRoot = rightDockRoot;
        }

        public static void FocusRightDockContainer()
        {
            FocusStatusBarView();
            StartFocusLossMonitoring();

            if (TryGetRightDockContainer(out var container))
            {
                container.focusable = false;
                container.focusable = true;
                container.Focus();
            }
        }

        public static void FocusLeftDockContainer()
        {
            FocusStatusBarView();
            StartFocusLossMonitoring();

            if (TryGetLeftDockContainer(out var container))
            {
                container.focusable = false;
                container.focusable = true;
                container.Focus();
            }
        }


        public static void DefocusDockContainers()
        {
            if (TryGetRightDockContainer(out var rightContainer))
                ScheduleContainerVisualDefocus(rightContainer);
                
            if (TryGetLeftDockContainer(out var leftContainer))
                ScheduleContainerVisualDefocus(leftContainer);

            StopFocusLossMonitoring();
        }

        private static void StartFocusLossMonitoring()
        {
            if (s_isMonitoringFocusLoss)
                return;

            s_wasStatusBarFocused = IsStatusBarViewFocused();
            s_hasObservedStatusBarFocus = s_wasStatusBarFocused;
            s_isMonitoringFocusLoss = true;
            EditorApplication.update += MonitorStatusBarFocusLoss;
        }

        private static void StopFocusLossMonitoring()
        {
            if (!s_isMonitoringFocusLoss)
                return;

            s_isMonitoringFocusLoss = false;
            s_hasObservedStatusBarFocus = false;
            EditorApplication.update -= MonitorStatusBarFocusLoss;
        }

        private static void MonitorStatusBarFocusLoss()
        {
            if (!s_isMonitoringFocusLoss)
                return;

            var isFocused = IsStatusBarViewFocused();

            // Ignore early transient states until status bar focus has been observed at least once
            // during this monitoring window. This avoids first-use false positives after domain reload.
            if (!s_hasObservedStatusBarFocus)
            {
                s_wasStatusBarFocused = isFocused;
                if (isFocused)
                    s_hasObservedStatusBarFocus = true;
                return;
            }

            if (s_wasStatusBarFocused && !isFocused)
            {
                DefocusDockContainers();
                return;
            }

            s_wasStatusBarFocused = isFocused;
        }

        private static bool IsStatusBarViewFocused()
        {
            if (s_appStatusBarView == null)
                return false;

            var type = s_appStatusBarView.GetType();

            var hasFocusMethod = type.GetMethod("HasFocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (hasFocusMethod != null)
            {
                try
                {
                    var value = hasFocusMethod.Invoke(s_appStatusBarView, null);
                    if (value is bool focused)
                        return focused;
                }
                catch
                {
                }
            }

            return GUIUtility.keyboardControl > 0;
        }

        private static bool TryGetRightDockContainer(out IMGUIContainer container)
        {
            if (s_rightDockContainer == null && s_rightDockRoot != null)
                s_rightDockContainer = s_rightDockRoot.Q<IMGUIContainer>();

            container = s_rightDockContainer;
            return container != null;
        }

        private static bool TryGetLeftDockContainer(out IMGUIContainer container)
        {
            if (s_leftDockContainer == null && s_leftDockRoot != null)
                s_leftDockContainer = s_leftDockRoot.Q<IMGUIContainer>();

            container = s_leftDockContainer;
            return container != null;
        }

        private static void FocusStatusBarView()
        {
            if (s_appStatusBarView == null)
                return;

            var type = s_appStatusBarView.GetType();
            var focusMethod = type.GetMethod("Focus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            focusMethod?.Invoke(s_appStatusBarView, null);

            var grabKeyboardFocusMethod = type.GetMethod("GrabKeyboardFocus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            grabKeyboardFocusMethod?.Invoke(s_appStatusBarView, null);
        }

        private static void RepaintStatusBarView()
        {
            if (s_appStatusBarView == null)
                return;

            var type = s_appStatusBarView.GetType();
            var repaintMethod = type.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            repaintMethod?.Invoke(s_appStatusBarView, null);
        }
        
        private static void ScheduleContainerVisualDefocus(IMGUIContainer container)
        {
            EditorApplication.delayCall += () =>
            {
                if (container == null)
                    return;

                container.Blur();
                container.focusable = false;
                container.MarkDirtyRepaint();
                RepaintStatusBarView();
            };
        }

    }
}
#endif