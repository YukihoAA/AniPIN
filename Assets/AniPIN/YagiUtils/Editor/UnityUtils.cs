using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YagihataItems.YagiUtils
{
    public static class UnityUtils
    {
        public static void GetGameObjectsOfType<T>(ref List<T> list, GameObject gameObject, bool getInactive)
        {
            if(getInactive || (!getInactive && gameObject.activeInHierarchy))
            {
                var component = gameObject.GetComponent(typeof(T));
                if (component != null)
                    list.Add((T)((object)component));
            }
            if(gameObject.transform.childCount > 0)
                foreach(var v in Enumerable.Range(0, gameObject.transform.childCount))
                {
                    GetGameObjectsOfType<T>(ref list, gameObject.transform.GetChild(v).gameObject, getInactive);
                }

        }
        public static string GetHierarchyPath(this GameObject target)
        {
            string path = "";
            Transform current = target.transform;
            while (current != null)
            {
                int index = current.GetSiblingIndex();
                path = "/" + current.name + index + path;
                current = current.parent;
            }

            Scene belongScene = target.GetBelongsScene();

            return "/" + belongScene.name + path;
        }
        public static Scene GetBelongsScene(this GameObject target)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid())
                {
                    continue;
                }

                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root == target.transform.root.gameObject)
                    {
                        return scene;
                    }
                }
            }

            return default(Scene);
        }
    }
}
