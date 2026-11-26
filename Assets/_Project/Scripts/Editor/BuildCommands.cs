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

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"The web build ended {summary.result} with {summary.totalErrors} errors.");
            }
        }
    }
}
