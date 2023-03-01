using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace YagihataItems.YagiUtils
{
    public static class EditorExtSettingsTool
    {
        public static IEditorExtSettings RestoreSettings<T>(VRCAvatarDescriptor avatarRoot, string settingsName) where T : IEditorExtSettings
        {
            var searchedFromAvatarRoot = UnityEngine.Object.FindObjectsOfType(typeof(T)).FirstOrDefault(n => (n as IEditorExtSettings).AvatarRoot == avatarRoot);
            if (searchedFromAvatarRoot != null)
            {
                return searchedFromAvatarRoot as IEditorExtSettings;
            }
            else
            {
                var settingsContainerRoot = GameObject.Find(settingsName);
                if (settingsContainerRoot != null)
                {
                    GameObject aniPINVariablesObject;
                    var v = settingsContainerRoot.transform.Find(avatarRoot.name);
                    if (v != null)
                    {
                        aniPINVariablesObject = v.gameObject;
                        return aniPINVariablesObject.GetComponent(typeof(T)) as IEditorExtSettings;
                    }
                }
            }
            return null;
        }
        public static void SaveSettings<T>(VRCAvatarDescriptor avatarRoot, string settingsName, IEditorExtVariables variables) where T : IEditorExtSettings
        {
            var settingsContainerRoot = GameObject.Find(settingsName);
            if (settingsContainerRoot == null)
            {
                settingsContainerRoot = new GameObject(settingsName);
                Undo.RegisterCreatedObjectUndo(settingsContainerRoot, $"Create {settingsName} Root");
                EditorUtility.SetDirty(settingsContainerRoot);
            }
            GameObject variablesContainingObject;
            var v = settingsContainerRoot.transform.Find(avatarRoot.name);
            if (v == null)
            {
                variablesContainingObject = new GameObject(avatarRoot.name);
                Undo.RegisterCreatedObjectUndo(variablesContainingObject, "Create Variables");
                variablesContainingObject.transform.SetParent(settingsContainerRoot.transform);
            }
            else
                variablesContainingObject = v.gameObject;
            var settings = variablesContainingObject.GetComponent(typeof(T));
            if (settings == null)
                settings = Undo.AddComponent(variablesContainingObject, typeof(T));
            Undo.RecordObject(settings, "Update Variables");
            (settings as IEditorExtSettings).SetVariables(variables);
            EditorUtility.SetDirty(settingsContainerRoot);
            EditorUtility.SetDirty(settings);
        }
    }
}
