#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
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
        private static bool s_isRepainting;

        private static MethodInfo s_focusMethod;
        private static MethodInfo s_grabKeyboardFocusMethod;
        private static MethodInfo s_repaintMethod;

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
            CacheReflection(appStatusBarView);
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
            StartContinuousRepaint();

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
            StartContinuousRepaint();

            if (TryGetLeftDockContainer(out var container))
            {
                container.focusable = false;
                container.focusable = true;
                container.Focus();
            }
        }

        private static void StartContinuousRepaint()
        {
            if (s_isRepainting)
                return;

            s_isRepainting = true;
            EditorApplication.update += ContinuousRepaint;
        }

        private static void ContinuousRepaint()
        {
            RepaintStatusBarView();
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

        private static void CacheReflection(UnityEngine.Object view)
        {
            if (view == null)
                return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = view.GetType();
            s_focusMethod = type.GetMethod("Focus", flags);
            s_grabKeyboardFocusMethod = type.GetMethod("GrabKeyboardFocus", flags);
            s_repaintMethod = type.GetMethod("Repaint", flags);
        }

        private static void FocusStatusBarView()
        {
            if (s_appStatusBarView == null)
                return;

            s_focusMethod?.Invoke(s_appStatusBarView, null);
            s_grabKeyboardFocusMethod?.Invoke(s_appStatusBarView, null);
        }

        private static void RepaintStatusBarView()
        {
            if (s_appStatusBarView == null)
                return;

            s_repaintMethod?.Invoke(s_appStatusBarView, null);
        }
    }
}
#endif