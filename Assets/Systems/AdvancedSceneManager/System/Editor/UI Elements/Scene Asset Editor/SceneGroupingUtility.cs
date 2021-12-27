#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE0029 // Use coalesce expression
#pragma warning disable IDE0051 // Remove unused private members

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor
{

    internal static class SceneGroupingUtility
    {

        public class Item
        {
            public (SceneCollection collection, bool asLoadingScreen)[] collections;
            public Scene scene;
            public bool? include;
            public override string ToString() => scene.name;
        }

        public static Item[] GetItems(Profile profile, params SceneAsset[] objects) =>
            GetItems(profile, objects.
                Select(t => AssetDatabase.GetAssetPath(t)).
                Where(path => profile ? profile.scenes.Any(s => s.path == path) : true).
                Select(path => SceneManager.assetManagement.scenes.Find(path)).
                OfType<Scene>().
                ToArray());

        public static Item[] GetItems(Profile profile, params Scene[] scenes) =>
            scenes?.Select(scene =>
            {

                if (!scene)
                    return null;

                var collections = scene.FindCollections(profile);

                var isSplashScreen = profile ? profile.splashScreen == scene : false;

                var isLoadingScreen =
                    collections.Any(c => c.asLoadingScreen) ||
                    (profile ? profile.loadingScreen == scene : false);

                var forceInclude = collections.Any() || isSplashScreen || isLoadingScreen;

                return new Item()
                {
                    scene = scene,
                    collections = collections?.ToArray() ?? Array.Empty<(SceneCollection collection, bool asLoadingScreen)>(),
                    include = forceInclude ? null : new bool?(profile ? profile.standaloneScenes?.Contains(scene) ?? false : false)
                };

            })?.OfType<Item>()?.ToArray() ?? Array.Empty<Item>();

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(SceneAsset[] scenes, Profile profile = null) =>
            Group(profile, GetItems(profile, scenes));

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(Scene[] scenes, Profile profile = null) =>
            Group(profile, GetItems(profile, scenes));

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(Profile profile, Item[] items)
        {

            //If we don't have a profile, just show all scenes in a flat list
            if (!profile)
                return new Dictionary<string, List<(Item item, bool isLoadingScreen)>>()
                {
                    { "", items.Select(o => (o, false)).ToList() }
                };

            Item splashScreen = null;
            Item defaultLoadingScreen = null;
            Dictionary<SceneCollection, List<(Item scene, bool isLoadingScreen)>> collectionScenes = new Dictionary<SceneCollection, List<(Item scene, bool isLoadingScreen)>>();
            List<Item> standalone = new List<Item>();

            //Disassemble input list
            foreach (var item in items)
            {

                if (profile && profile.splashScreen == item.scene)
                    splashScreen = item;
                else if (profile && profile.loadingScreen == item.scene)
                    defaultLoadingScreen = item;
                else
                {

                    var collections = item.collections;
                    if (!collections.Any())
                        standalone.Add(item);
                    else
                    {

                        foreach (var collection in collections)
                            if (!collectionScenes.ContainsKey(collection.collection))
                                collectionScenes.Add(collection.collection, new List<(Item item, bool isLoadingScreen)>() { (item, collection.asLoadingScreen) });
                            else
                                collectionScenes[collection.collection].Add((item, collection.asLoadingScreen));

                    }
                }

            }


            //Reassemble variables as output dictionary
            var dict = new Dictionary<string, List<(Item item, bool isLoadingScreen)>>();

            if (splashScreen != null)
                dict.Add("Splash screen", new List<(Item item, bool isLoadingScreen)>() { (splashScreen, false) });

            if (defaultLoadingScreen != null)
                dict.Add("Default loading screen", new List<(Item item, bool isLoadingScreen)>() { (defaultLoadingScreen, false) });

            foreach (var item in collectionScenes.OrderBy(c => profile ? profile.Order(c.Key) : 0))
            {

                var sortedScenes = item.Value.OrderBy(s => s.isLoadingScreen).ThenBy(s => s.scene.scene.name);

                if (!dict.ContainsKey(item.Key.name))
                    dict.Add(item.Key.name, new List<(Item item, bool isLoadingScreen)>());

                dict[item.Key.name].AddRange(sortedScenes);

            }

            if (standalone.Any())
                dict.Set("Standalone", standalone.Select(s => (s, false)).ToList());

            return dict;

        }

    }

}
