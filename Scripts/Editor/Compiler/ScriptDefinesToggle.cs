using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Anvil.Unity.DOTS.Editor.Compiler
{
    /// <summary>
    /// Provides a quick and easy way to enable/disable commonly used Scripting Defines for
    /// development and debugging. 
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptDefinesToggle
    {
        private const string MENU_PATH_BASE = "Anvil/Script Defines";
        private const string DEFINE_ANVIL_DEBUG_SAFETY_EXPENSIVE = "ANVIL_DEBUG_SAFETY_EXPENSIVE";
        private const string MENU_PATH_ANVIL_DEBUG_SAFETY_EXPENSIVE = MENU_PATH_BASE + "/" + DEFINE_ANVIL_DEBUG_SAFETY_EXPENSIVE;
        private const string DEFINE_ANVIL_DEBUG_LOGGING_EXPENSIVE = "ANVIL_DEBUG_LOGGING_EXPENSIVE";
        private const string MENU_PATH_ANVIL_DEBUG_LOGGING_EXPENSIVE= MENU_PATH_BASE + "/" + DEFINE_ANVIL_DEBUG_LOGGING_EXPENSIVE;

        private static readonly HashSet<string> ACTIVE_DEFINES = new HashSet<string>();
        private static readonly NamedBuildTarget NAMED_BUILD_TARGET;

        static ScriptDefinesToggle()
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            NAMED_BUILD_TARGET = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            
            ACTIVE_DEFINES.Clear();
            PlayerSettings.GetScriptingDefineSymbols(NAMED_BUILD_TARGET, out string[] defines);
            foreach (string define in defines)
            {
                ACTIVE_DEFINES.Add(define);
            }
        }
        
        [MenuItem(MENU_PATH_ANVIL_DEBUG_SAFETY_EXPENSIVE, true)]
        private static bool ToggleAnvilDebugSafetyExpensive_Validator()
        {
            return Toggle_Validator(DEFINE_ANVIL_DEBUG_SAFETY_EXPENSIVE, 
                                    MENU_PATH_ANVIL_DEBUG_SAFETY_EXPENSIVE);
        }
        
        [MenuItem(MENU_PATH_ANVIL_DEBUG_SAFETY_EXPENSIVE)]
        private static void ToggleAnvilDebugSafetyExpensive()
        {
            Toggle(DEFINE_ANVIL_DEBUG_SAFETY_EXPENSIVE);
        }
        
        [MenuItem(MENU_PATH_ANVIL_DEBUG_LOGGING_EXPENSIVE, true)]
        private static bool ToggleAnvilDebugLoggingExpensive_Validator()
        {
            return Toggle_Validator(DEFINE_ANVIL_DEBUG_LOGGING_EXPENSIVE, 
                                    MENU_PATH_ANVIL_DEBUG_LOGGING_EXPENSIVE);
        }
        
        [MenuItem(MENU_PATH_ANVIL_DEBUG_LOGGING_EXPENSIVE)]
        private static void ToggleAnvilDebugLoggingExpensive()
        {
            Toggle(DEFINE_ANVIL_DEBUG_LOGGING_EXPENSIVE);
        }

        
        private static bool Toggle_Validator(string define, string menuPath)
        {
            bool isEnabled = ACTIVE_DEFINES.Contains(define);
            Menu.SetChecked(menuPath, isEnabled);
            return true;
        }

        private static void Toggle(string define)
        {
            if (ACTIVE_DEFINES.Contains(define))
            {
                ACTIVE_DEFINES.Remove(define);
            }
            else
            {
                ACTIVE_DEFINES.Add(define);
            }
            
            PlayerSettings.SetScriptingDefineSymbols(NAMED_BUILD_TARGET, ACTIVE_DEFINES.ToArray());
        }
    }
}
