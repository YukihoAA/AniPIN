using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace YagihataItems.AniPIN
{
    [CustomEditor(typeof(AniPINSettings))]
    public class AniPINSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var settings = target as AniPINSettings;

            var bef1 = settings.AvatarRoot; 
            settings.AvatarRoot = EditorGUILayout.ObjectField("Avatar Root", settings.AvatarRoot, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;
            if (settings.AvatarRoot != bef1)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef2 = settings.PINCode;
            settings.PINCode = EditorGUILayout.TextField("PIN Code", settings.PINCode);
            if (settings.PINCode != bef2)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef3 = settings.SavePIN;
            settings.SavePIN = EditorGUILayout.Toggle("Save PIN", settings.SavePIN);
            if (settings.SavePIN != bef3)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef4 = settings.WriteDefaults;
            settings.WriteDefaults = EditorGUILayout.Toggle("Write Defaults", settings.WriteDefaults);
            if (settings.WriteDefaults != bef4)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef5 = settings.FolderID;
            settings.FolderID = EditorGUILayout.TextField("Folder ID", settings.FolderID);
            if (settings.FolderID != bef5)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef6 = settings.ObfuscateAnimator;
            settings.ObfuscateAnimator = EditorGUILayout.Toggle("Obfuscate Animator", settings.ObfuscateAnimator);
            if (settings.ObfuscateAnimator != bef6)
            {
                EditorUtility.SetDirty(settings);
            }

            var bef7 = settings.GetInactiveObjects;
            settings.GetInactiveObjects = EditorGUILayout.Toggle("Get Inactive Objects", settings.GetInactiveObjects);
            if (settings.GetInactiveObjects != bef7)
            {
                EditorUtility.SetDirty(settings);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}