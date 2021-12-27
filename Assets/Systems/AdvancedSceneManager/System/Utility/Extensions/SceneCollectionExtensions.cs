using System.Linq;
using System.Collections.Generic;
using AdvancedSceneManager.Core;

using scene = UnityEngine.SceneManagement.Scene;
using Scene = AdvancedSceneManager.Models.Scene;

public static class SceneCollectionExtensions
{

    public static Scene Find(this IEnumerable<Scene> list, string path) =>
        list.FirstOrDefault(s => s ? s.path == path : false);

    public static scene Find(this IEnumerable<scene> list, string path) =>
        list.FirstOrDefault(s => s.path == path);

    public static OpenSceneInfo Find(this IEnumerable<OpenSceneInfo> list, Scene scene) =>
        list.FirstOrDefault(s => s?.scene == scene);

    public static OpenSceneInfo Find(this IEnumerable<OpenSceneInfo> list, scene scene) =>
        list.FirstOrDefault(s => s.unityScene.HasValue && s.unityScene.Value.path == scene.path);

}

