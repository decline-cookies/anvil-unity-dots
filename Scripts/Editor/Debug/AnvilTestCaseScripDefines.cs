using Anvil.Unity.Editor.Debug;
using UnityEditor;

namespace Anvil.Unity.DOTS.Editor.Debug
{
    /// <summary>
    /// Sets up script defines for common Anvil specific script defines
    /// </summary>
    [InitializeOnLoad]
    public static class AnvilTestCaseScriptDefines
    {
        /// <summary>
        /// Script Define for code safety checks.
        /// Should not be performance intensive.
        /// </summary>
        public const string ANVIL_TEST_CASE_SHARED_WRITE = "ANVIL_TEST_CASE_SHARED_WRITE";



        private const string ANVIL_TEST_CASE_SHARED_WRITE_MENU_PATH = ScriptDefinesToggle.MENU_PATH_BASE + "/" + ANVIL_TEST_CASE_SHARED_WRITE;


        //For ANVIL_TEST_CASE_SHARED_WRITE, we require ANVIL_DEBUG_SAFETY to be on if we're on, but we don't need to turn anything off if we turn off
        private static readonly ScriptDefineDefinition ANVIL_TEST_CASE_SHARED_WRITE_DEFINITION = new ScriptDefineDefinition(
                                                                                                                            ANVIL_TEST_CASE_SHARED_WRITE,
                                                                                                                            ANVIL_TEST_CASE_SHARED_WRITE_MENU_PATH,
                                                                                                                            new[] { AnvilDebugSafetyScriptDefines.ANVIL_DEBUG_SAFETY },
                                                                                                                            null);

        static AnvilTestCaseScriptDefines()
        {
            ScriptDefinesToggle.RegisterScriptDefineDefinition(ANVIL_TEST_CASE_SHARED_WRITE_DEFINITION);
        }


        [MenuItem(ANVIL_TEST_CASE_SHARED_WRITE_MENU_PATH)]
        private static void Toggle_TestCaseSharedWrite()
        {
            ScriptDefinesToggle.Toggle(ANVIL_TEST_CASE_SHARED_WRITE_DEFINITION);
        }

        [MenuItem(ANVIL_TEST_CASE_SHARED_WRITE_MENU_PATH, true)]
        private static bool Toggle_TestCaseSharedWrite_Validator()
        {
            return ScriptDefinesToggle.Toggle_Validator(ANVIL_TEST_CASE_SHARED_WRITE_DEFINITION);
        }
    }
}