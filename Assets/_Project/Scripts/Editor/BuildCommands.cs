using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Chromacolony.Editor
{
    public static class BuildCommands
    {
        private const string WebFolder = "Builds/Web";

        /// <summary>
        /// From a shell: <c>Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod Chromacolony.Editor.BuildCommands.BuildWeb</c>.
        /// </summary>
        [MenuItem("Chromacolony/Build/Web")]
        public static void BuildWeb()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("The build settings list no enabled scene.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            // IL2CPP's runtime names its own source files in its messages by their path in the editor's install folder, and the
            // map turns that folder into /unity for this build. Only the path is quoted: IL2CPP drops a value's outer quotes and
            // hands the flags to the compiler unquoted, so a space in the path would split the flag.
            string projectArgs = PlayerSettings.GetAdditionalIl2CppArgs();
            string prefixMap = $"--compiler-flags=-ffile-prefix-map=\"{EditorApplication.applicationContentsPath}\"=/unity";
            PlayerSettings.SetAdditionalIl2CppArgs($"{projectArgs} {prefixMap}".Trim());
            try
            {
                BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException($"The web build ended {summary.result} with {summary.totalErrors} errors.");
                }
            }
            finally
            {
                PlayerSettings.SetAdditionalIl2CppArgs(projectArgs);
            }
        }
    }
}
