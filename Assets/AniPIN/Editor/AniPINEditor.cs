﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using YagihataItems.YagiUtils;
using nadena.dev.modular_avatar.core;

namespace YagihataItems.AniPIN
{
    public class AniPINEditor : EditorWindow
    {
        public const string resFolderPath = "Assets/AniPIN/";
        public const string workFolderPath = "Assets/AniPIN/";
        public const string autoGeneratedFolderPath = workFolderPath + "AutoGenerated/";
        public const string paramName = "AniPINParam";
        public const string mainLayerName = "vCAP_Main";
        public const string overlayLayerName = "vCAP_Overlay";
        public static readonly string[] systemParams =
        {
            "IsLocal",
            "Viseme",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation"
        };
        private const string currentVersion = "1.6";
        private const string versionUrl = "https://raw.githubusercontent.com/YukihoAA/AniPIN/main/CurrentVersion.txt";
        private const string manualUrl = "https://github.com/YukihoAA/AniPIN/";
        private const string releaseUrl = "https://github.com/YukihoAA/AniPIN/releases";
        private const VRCExpressionParameters.ValueType IntParam = VRCExpressionParameters.ValueType.Int;
        private const VRCExpressionParameters.ValueType BoolParam = VRCExpressionParameters.ValueType.Bool;
        private AniPINSettings aniPINSettings;
        private VRCAvatarDescriptor avatarRoot = null;
        private VRCAvatarDescriptor avatarRootBefore = null;
        private IndexedList indexedList = new IndexedList();
        private GameObject aniPINSettingsRoot = null;
        private AniPINVariables aniPINVariables;
        private Vector2 ScrollPosition = new Vector2();
        [SerializeField] private Texture2D headerTexture = null;
        private bool showingVerticalScroll = false;
        private static string newerVersion = "";

        [MenuItem("AniPIN/AniPIN")]
        private static void Create()
        {
            GetWindow<AniPINEditor>("AniPIN");
            CheckNewerVersion();
        }

        private static void CheckNewerVersion()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    string text = wc.DownloadString(versionUrl);
                    newerVersion = text.Trim();
                }
                catch (WebException exc)
                {
                    newerVersion = "";
                }
            }
        }

        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(ScrollPosition))
            {
                using (var verticalScope = new EditorGUILayout.VerticalScope())
                {
                    ScrollPosition = scrollScope.scrollPosition;
                    if (headerTexture == null)
                        headerTexture = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(resFolderPath + "Textures/MenuHeader.png");
                    if (verticalScope.rect.height != 0)
                        showingVerticalScroll = verticalScope.rect.height > position.size.y;
                    var height = position.size.x / headerTexture.width * headerTexture.height;
                    if (height > headerTexture.height)
                        height = headerTexture.height;
                    GUILayout.Box(headerTexture, GUILayout.Width(position.size.x - (showingVerticalScroll ? 22 : 8)), GUILayout.Height(height));

                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"AniPIN-v{currentVersion}");
                    }
                    if (newerVersion.Length > 0 && Convert.ToDouble(currentVersion) < Convert.ToDouble(newerVersion))
                    {
                        EditorGUILayout.HelpBox($"新しいバージョン「{newerVersion}」がリリースされています！", MessageType.Info);
                        if (GUILayout.Button("Update"))
                        {
                            Application.OpenURL(releaseUrl);
                        }
                        EditorGUILayoutExtra.Separator();
                    }
                    EditorGUILayoutExtra.Space();

                    var avatarDescriptors = FindObjectsOfType(typeof(VRCAvatarDescriptor));
                    indexedList.list = avatarDescriptors.Select(n => n.name).ToArray();
                    indexedList.index = EditorGUILayoutExtra.IndexedStringList("対象アバター", indexedList);
                    if (avatarDescriptors.Length <= 0)
                    {
                        EditorGUILayout.HelpBox("VRCAvatarDescriptorが設定されているオブジェクトが存在しません。", MessageType.Error);
                    }
                    else
                    {
                        if (indexedList.index >= 0 && indexedList.index < avatarDescriptors.Length)
                            avatarRoot = avatarDescriptors[indexedList.index] as VRCAvatarDescriptor;
                        else
                            avatarRoot = null;
                        if (avatarRoot == null)
                        {
                            avatarRootBefore = null;
                        }
                        else
                        {
                            //AvatarRootが変更されたら設定を復元
                            if (avatarRoot != avatarRootBefore)
                            {
                                aniPINVariables = new AniPINVariables();
                                RestoreSettings();
                                avatarRootBefore = avatarRoot;
                            }

                            EditorGUILayoutExtra.SeparatorWithSpace();
                            aniPINVariables.AvatarRoot = avatarRoot;
                            aniPINVariables.PINCode = EditorGUILayout.TextField("PINコード", aniPINVariables.PINCode);
                            aniPINVariables.SavePIN = EditorGUILayout.Toggle("アンロック状態を保存する", aniPINVariables.SavePIN);
                            EditorGUILayoutExtra.SeparatorWithSpace();
                            if (GUILayout.Button("適用する"))
                            {
                                if (aniPINVariables.PINCode.Length <= 3)
                                {
                                    EditorUtility.DisplayDialog("AniPIN", "PINCode should be longer than 4 digits.", "OK");
                                }
                                else {
                                    SaveSettings();
                                    YagiAPI.CreateFolderRecursively(autoGeneratedFolderPath + aniPINVariables.FolderID);
                                    ApplyToAvatar();
                                }
                            }
                            if (GUILayout.Button("適用を解除する"))
                            {
                                SaveSettings();
                                RemoveAutoGenerated();
                            }
                        }
                    }
                }
            }
        }
        private void ApplyToAvatar()
        {
            var hudTransform = avatarRoot.transform.Find("AniPINOverlay");
            GameObject hudObject;
            if (hudTransform == null)
            {
                hudObject = PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath(resFolderPath + "AniPINOverlay.prefab", typeof(GameObject))) as GameObject;
                hudObject.transform.SetParent(avatarRoot.transform);
            }
            else
                hudObject = hudTransform.gameObject;
            hudObject.transform.localPosition = new Vector3(0, 1, 0);
            EditorUtility.SetDirty(hudObject);

            var hudON = new AnimationClip();
            var hudOFF = new AnimationClip();
            var objPath = YagiAPI.GetGameObjectPath(hudObject, avatarRoot.gameObject);

            var curveON = new AnimationCurve();
            curveON.AddKey(new Keyframe(0f, 1));
            curveON.AddKey(new Keyframe(1f / hudON.frameRate, 1));
            hudON.SetCurve(objPath, typeof(GameObject), "m_IsActive", curveON);

            var curveOFF = new AnimationCurve();
            curveOFF.AddKey(new Keyframe(0f, 0));
            curveOFF.AddKey(new Keyframe(1f / hudOFF.frameRate, 0));
            hudOFF.SetCurve(objPath, typeof(GameObject), "m_IsActive", curveOFF);

            AssetDatabase.CreateAsset(hudON, autoGeneratedFolderPath + aniPINVariables.FolderID + "/EnableHUD.anim");
            EditorUtility.SetDirty(hudON);
            AssetDatabase.CreateAsset(hudOFF, autoGeneratedFolderPath + aniPINVariables.FolderID + "/DisableHUD.anim");
            EditorUtility.SetDirty(hudOFF);

            var clipON = new AnimationClip();
            var clipOFF = new AnimationClip();
            var meshRenderers = new List<MeshRenderer>();
            UnityUtils.GetGameObjectsOfType<MeshRenderer>(ref meshRenderers, avatarRoot.gameObject, true);
            foreach (var v in meshRenderers)
            {
                objPath = YagiAPI.GetGameObjectPath((v as MeshRenderer).gameObject, avatarRoot.gameObject);
                if (!string.IsNullOrEmpty(objPath))
                {
                    var propValue = 1;
                    if ((v as MeshRenderer).gameObject.name == "AniPINOverlay" || (v as MeshRenderer).enabled == false)
                        propValue = 0;
                    curveON = new AnimationCurve();
                    curveON.AddKey(new Keyframe(0f, propValue));
                    curveON.AddKey(new Keyframe(1f / clipON.frameRate, propValue));
                    clipON.SetCurve(objPath, typeof(MeshRenderer), "m_Enabled", curveON);

                    if (propValue == 0)
                        propValue = 1;
                    else
                        propValue = 0;
                    curveOFF = new AnimationCurve();
                    curveOFF.AddKey(new Keyframe(0f, propValue));
                    curveOFF.AddKey(new Keyframe(1f / clipON.frameRate, propValue));
                    clipOFF.SetCurve(objPath, typeof(MeshRenderer), "m_Enabled", curveOFF);
                }
            }
            var skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            UnityUtils.GetGameObjectsOfType<SkinnedMeshRenderer>(ref skinnedMeshRenderers, avatarRoot.gameObject, true);
            foreach (var v in skinnedMeshRenderers)
            {
                objPath = YagiAPI.GetGameObjectPath((v as SkinnedMeshRenderer).gameObject, avatarRoot.gameObject);
                if (!string.IsNullOrEmpty(objPath))
                {
                    var propValue = 1;
                    if ((v as SkinnedMeshRenderer).gameObject.name == "AniPINOverlay" || (v as SkinnedMeshRenderer).enabled == false)
                        propValue = 0;
                    curveON = new AnimationCurve();
                    curveON.AddKey(new Keyframe(0f, propValue));
                    curveON.AddKey(new Keyframe(1f / clipON.frameRate, propValue));
                    clipON.SetCurve(objPath, typeof(SkinnedMeshRenderer), "m_Enabled", curveON);

                    if (propValue == 0)
                        propValue = 1;
                    else
                        propValue = 0;
                    curveOFF = new AnimationCurve();
                    curveOFF.AddKey(new Keyframe(0f, propValue));
                    curveOFF.AddKey(new Keyframe(1f / clipON.frameRate, propValue));
                    clipOFF.SetCurve(objPath, typeof(SkinnedMeshRenderer), "m_Enabled", curveOFF);
                }
            }
            AssetDatabase.CreateAsset(clipON, autoGeneratedFolderPath + aniPINVariables.FolderID + "/ONAnimation.anim");
            EditorUtility.SetDirty(clipON);
            AssetDatabase.CreateAsset(clipOFF, autoGeneratedFolderPath + aniPINVariables.FolderID + "/OFFAnimation.anim");
            EditorUtility.SetDirty(clipOFF);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(autoGeneratedFolderPath + aniPINVariables.FolderID + "/AniPIN.controller") != null)
            {
                AssetDatabase.DeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/AniPIN.controller");
            }

            var fxLayer = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(autoGeneratedFolderPath + aniPINVariables.FolderID + "/AniPIN.controller");

            fxLayer.AddParameter(paramName, AnimatorControllerParameterType.Int);
            fxLayer.AddParameter(aniPINVariables.FolderID, AnimatorControllerParameterType.Bool);
            fxLayer.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);


            var layer = fxLayer.AddAnimatorControllerLayer(overlayLayerName);

            var stateMachine = layer.stateMachine;
            stateMachine.Clear();
            var disableOverlayState = stateMachine.AddState("DisableOverlay", new Vector2(240, 240));
            disableOverlayState.writeDefaultValues = true;
            var enableOverlayState = stateMachine.AddState("EnableOverlay", new Vector2(240, 480));
            enableOverlayState.writeDefaultValues = true;
            stateMachine.defaultState = disableOverlayState;
            var transition = stateMachine.AddAnyStateTransition(enableOverlayState);
            transition.canTransitionToSelf = false;
            transition.duration = 0;
            transition.exitTime = 0;
            transition.name = "EnableOverlay";
            transition.CreateSingleCondition(AnimatorConditionMode.If, "IsLocal", 0, false, false);
            transition = stateMachine.AddAnyStateTransition(disableOverlayState);
            transition.canTransitionToSelf = false;
            transition.duration = 0;
            transition.exitTime = 0;
            transition.name = "DisableOverlay";
            transition.CreateSingleCondition(AnimatorConditionMode.IfNot, "IsLocal", 0, false, false);
            disableOverlayState.motion = hudOFF;
            enableOverlayState.motion = hudON;
            EditorUtility.SetDirty(transition);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(disableOverlayState);
            EditorUtility.SetDirty(enableOverlayState);

            layer = fxLayer.AddAnimatorControllerLayer(mainLayerName);

            stateMachine = layer.stateMachine;
            stateMachine.Clear();
            var pinCode = aniPINVariables.PINCode;
            var pinLength = pinCode.Length;
            var unlockedState = stateMachine.AddState(string.Format("8"), new Vector2(240 * pinLength, 360));
            unlockedState.writeDefaultValues = true;
            unlockedState.motion = clipON;
            var waitEnterState = stateMachine.AddState(string.Format("7"), new Vector2(240 * (pinLength - 1), 360));
            waitEnterState.writeDefaultValues = true;
            waitEnterState.motion = clipOFF;
            var resetState = stateMachine.AddState(string.Format("10"), new Vector2(0, 480));
            resetState.writeDefaultValues = true;
            resetState.motion = clipOFF;
            var resetNeutralState = stateMachine.AddState(string.Format("9"), new Vector2(-240, 480));
            resetNeutralState.writeDefaultValues = true;
            resetNeutralState.motion = clipOFF;
            var driver = resetNeutralState.AddParameterDriver(aniPINVariables.FolderID, 0);

            transition = waitEnterState.MakeTransition(unlockedState, "InputEnter");
            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 11, true, true);
            EditorUtility.SetDirty(transition);

            transition = waitEnterState.MakeTransition(resetNeutralState, "InputReset");
            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 12, true, true);
            EditorUtility.SetDirty(transition);

            transition = waitEnterState.MakeTransition(unlockedState, "JumpToUnlock");
            transition.CreateSingleCondition(AnimatorConditionMode.If, aniPINVariables.FolderID, 1, false, false);
            EditorUtility.SetDirty(transition);

            transition = unlockedState.MakeTransition(resetNeutralState, "InputReset");
            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 12, true, true);
            EditorUtility.SetDirty(transition);

            transition = unlockedState.MakeTransition(resetNeutralState, "JumpToReset");
            transition.CreateSingleCondition(AnimatorConditionMode.IfNot, aniPINVariables.FolderID, 0, false, false);
            EditorUtility.SetDirty(transition);
            driver = unlockedState.AddParameterDriver(aniPINVariables.FolderID, 1);

            AnimatorState beforePinInputWaitState = null;
            AnimatorState beforeWaitNeutralState = null;
            AnimatorState firstState = null;
            foreach (var n in Enumerable.Range(0, pinLength))
            {
                var pinNum = pinCode[n] - '0';
                var pinInputWaitState = stateMachine.AddState(string.Format("{0}", n), new Vector2(240 * n, 240));
                pinInputWaitState.writeDefaultValues = true;
                pinInputWaitState.motion = clipOFF;
                transition = pinInputWaitState.MakeTransition(resetNeutralState, "InputReset");
                transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 12, true, true);
                EditorUtility.SetDirty(transition);
                transition = pinInputWaitState.MakeTransition(unlockedState, "JumpToUnlock");
                transition.CreateSingleCondition(AnimatorConditionMode.If, aniPINVariables.FolderID, 1, false, false);
                EditorUtility.SetDirty(transition);
                if (n != pinLength - 1)
                {
                    var waitNeutralState = stateMachine.AddState(string.Format("{0}", n + 4), new Vector2(240 * n, 360));
                    waitNeutralState.writeDefaultValues = true;
                    waitNeutralState.motion = clipOFF;
                    transition = waitNeutralState.MakeTransition(unlockedState, "JumpToUnlock");
                    transition.CreateSingleCondition(AnimatorConditionMode.If, aniPINVariables.FolderID, 1, false, false);
                    EditorUtility.SetDirty(transition);
                    transition = waitNeutralState.MakeTransition(resetNeutralState, "InputReset");
                    transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 12, true, true);
                    EditorUtility.SetDirty(transition);

                    //PIN入力待機ステートからのトランジション
                    transition = pinInputWaitState.MakeTransition(waitNeutralState, "CorrectPIN");
                    transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, pinNum + 1, true, true);
                    EditorUtility.SetDirty(transition);
                    transition = pinInputWaitState.MakeTransition(resetState, "IncorrectPIN");
                    transition.name = "IncorrectPIN";
                    transition.conditions = new AnimatorCondition[]
                    {
                        new AnimatorCondition(){ mode = AnimatorConditionMode.Greater, parameter = paramName, threshold = 0 },
                        new AnimatorCondition(){ mode = AnimatorConditionMode.NotEqual, parameter = paramName, threshold = pinNum + 1 },
                        new AnimatorCondition(){ mode = AnimatorConditionMode.If, parameter = "IsLocal", threshold = 0 }
                    };
                    EditorUtility.SetDirty(transition);

                    if (n == 0)
                    {
                        // stateMachine.AddEntryTransition(pinInputWaitState);
                        firstState = pinInputWaitState;
                    }
                    else
                    {
                        //前回のPINニュートラル待機ステートからのトランジション
                        transition = beforeWaitNeutralState.MakeTransition(pinInputWaitState, "ResetToZero");
                        transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 0, true, true);
                        EditorUtility.SetDirty(transition);
                    }

                    beforeWaitNeutralState = waitNeutralState;
                }
                else
                {
                    //PIN入力待機ステートからのトランジション
                    transition = pinInputWaitState.MakeTransition(waitEnterState, "CorrectPIN");
                    transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, pinNum + 1, true, true);
                    EditorUtility.SetDirty(transition);
                    transition = pinInputWaitState.MakeTransition(resetState, "IncorrectPIN");
                    transition.name = "IncorrectPIN";
                    transition.conditions = new AnimatorCondition[]
                    {
                        new AnimatorCondition(){ mode = AnimatorConditionMode.Greater, parameter = paramName, threshold = 0 },
                        new AnimatorCondition(){ mode = AnimatorConditionMode.NotEqual, parameter = paramName, threshold = pinNum + 1 },
                        new AnimatorCondition(){ mode = AnimatorConditionMode.If, parameter = "IsLocal", threshold = 0 }
                    };
                    EditorUtility.SetDirty(transition);
                    //前回のPINニュートラル待機ステートからのトランジション
                    transition = beforeWaitNeutralState.MakeTransition(pinInputWaitState, "ResetToZero");
                    transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 0, true, true);
                    EditorUtility.SetDirty(transition);
                }
                beforePinInputWaitState = pinInputWaitState;
            }
            transition = resetState.MakeTransition(resetNeutralState, "InputReset");
            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 12, true, true);
            EditorUtility.SetDirty(transition);
            transition = resetNeutralState.MakeTransition(firstState, "ResetToZero");
            transition.CreateSingleCondition(AnimatorConditionMode.Equals, paramName, 0, true, true);
            EditorUtility.SetDirty(transition);
            stateMachine.defaultState = firstState;
            fxLayer.RemoveLayer(0);
            EditorUtility.SetDirty(fxLayer);
           
            ModularAvatarMergeAnimator MaAnimator = hudObject.AddComponent<ModularAvatarMergeAnimator>();
            ModularAvatarMenuInstaller MaMenu = hudObject.AddComponent<ModularAvatarMenuInstaller>();
            ModularAvatarParameters MaPara = hudObject.AddComponent<ModularAvatarParameters>();

            MaAnimator.animator = fxLayer;
            MaAnimator.deleteAttachedAnimator = true;
            MaAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            MaAnimator.matchAvatarWriteDefaults = true;

            MaMenu.menuToAppend = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(resFolderPath + "/Menu/AniPINMenu.asset");

            ParameterConfig[] configs = new ParameterConfig[2];

            configs[0] = new ParameterConfig
            {
                nameOrPrefix = paramName,
                syncType = ParameterSyncType.Int,
                defaultValue = 0.0f,
                saved = false
            };

            configs[1] = new ParameterConfig
            {
                nameOrPrefix = aniPINVariables.FolderID,
                syncType = ParameterSyncType.Bool,
                defaultValue = 0.0f,
                saved = aniPINVariables.SavePIN
            };

            MaPara.parameters.AddRange(configs);
        }
        private void RemoveAutoGenerated()
        {
            var fxLayer = avatarRoot.GetFXLayer(autoGeneratedFolderPath + aniPINVariables.FolderID + "/");
            var param = fxLayer.parameters.FirstOrDefault(n => n.name == aniPINVariables.FolderID);
            fxLayer.TryRemoveParameter(aniPINVariables.FolderID);
            fxLayer.TryRemoveParameter(paramName);
            fxLayer.TryRemoveLayer("vCAP_Overlay");
            fxLayer.TryRemoveLayer("vCAP_Main");
            if (avatarRoot.expressionParameters != null)
            {
                avatarRoot.expressionParameters.TryRemoveParameter(aniPINVariables.FolderID);
                avatarRoot.expressionParameters.TryRemoveParameter(paramName);
                EditorUtility.SetDirty(avatarRoot.expressionParameters);
            }
            var obj = avatarRoot.transform.Find("AniPINOverlay");
            if (obj != null)
                DestroyImmediate(obj.gameObject);
            EditorUtility.SetDirty(avatarRoot);
            EditorUtility.SetDirty(fxLayer);
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/AniPINMenu.asset");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/AniPINSubMenu.asset");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/GeneratedExpressionParameters.asset");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/GeneratedFXLayer.controller");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/DisableHUD.anim");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/EnableHUD.anim");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/OFFAnimation.anim");
            YagiAPI.TryDeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID + "/ONAnimation.anim");
            AssetDatabase.DeleteAsset(autoGeneratedFolderPath + aniPINVariables.FolderID);
            var searchedFromAvatarRoot = FindObjectsOfType(typeof(AniPINSettings)).FirstOrDefault(n => (n as AniPINSettings).AvatarRoot == avatarRoot);
            if (searchedFromAvatarRoot != null)
                DestroyImmediate((searchedFromAvatarRoot as AniPINSettings).gameObject);
        }
        private void RestoreSettings()
        {
            var searchedFromAvatarRoot = FindObjectsOfType(typeof(AniPINSettings)).FirstOrDefault(n => (n as AniPINSettings).AvatarRoot == avatarRoot);
            aniPINSettings = null;
            if (searchedFromAvatarRoot != null)
            {
                aniPINSettings = searchedFromAvatarRoot as AniPINSettings;
            }
            else
            {
                aniPINSettingsRoot = GameObject.Find("AniPINSettings");
                if (aniPINSettingsRoot != null)
                {
                    GameObject aniPINVariablesObject;
                    var v = aniPINSettingsRoot.transform.Find(avatarRoot.name);
                    if (v != null)
                    {
                        aniPINVariablesObject = v.gameObject;
                        aniPINSettings = aniPINVariablesObject.GetComponent<AniPINSettings>();
                    }
                }
            }
            if (aniPINSettings != null)
            {
                aniPINVariables = aniPINSettings.GetVariables();
            }
            else
            {
                aniPINVariables.FolderID = System.Guid.NewGuid().ToString();
            }
        }
        private void SaveSettings()
        {

            aniPINSettingsRoot = GameObject.Find("AniPINSettings");
            if (aniPINSettingsRoot == null)
            {
                aniPINSettingsRoot = new GameObject("AniPINSettings");
                Undo.RegisterCreatedObjectUndo(aniPINSettingsRoot, "Create AniPINSettings Root");
                EditorUtility.SetDirty(aniPINSettingsRoot);
            }
            GameObject aniPINVariablesObject;
            var v = aniPINSettingsRoot.transform.Find(avatarRoot.name);
            if (v == null)
            {
                aniPINVariablesObject = new GameObject(avatarRoot.name);
                Undo.RegisterCreatedObjectUndo(aniPINVariablesObject, "Create AniPINVariables");
                aniPINVariablesObject.transform.SetParent(aniPINSettingsRoot.transform);
            }
            else
                aniPINVariablesObject = v.gameObject;
            aniPINSettings = aniPINVariablesObject.GetComponent<AniPINSettings>();
            if (aniPINSettings == null)
                aniPINSettings = Undo.AddComponent(aniPINVariablesObject, typeof(AniPINSettings)) as AniPINSettings;
            Undo.RecordObject(aniPINSettings, "Update AniPINVariables");
            aniPINSettings.SetVariables(aniPINVariables);
            EditorUtility.SetDirty(aniPINVariablesObject);
            EditorUtility.SetDirty(aniPINSettings);
        }
    }
}
