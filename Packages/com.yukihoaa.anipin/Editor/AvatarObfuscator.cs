﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase.Editor.BuildPipeline;
using YagihataItems.YagiUtils;

namespace YagihataItems.AniPIN
{
    public class AvatarObfuscator : IVRCSDKPreprocessAvatarCallback, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 0;
        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            try
            {
                var avatar = avatarGameObject.GetComponent<VRCAvatarDescriptor>();
                GameObject idObject = null;
                foreach (var v in Enumerable.Range(0, avatarGameObject.transform.childCount))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Finding obfuscate ID tag", v, avatarGameObject.transform.childCount);
                    var child = avatarGameObject.transform.GetChild(v);
                    if (child.name.StartsWith("AniPIN_IDC_"))
                    {
                        idObject = child.gameObject;
                        break;
                    }
                }
                if (avatar == null || AvatarDeobfuscator.variables == null || idObject == null)
                {
                    YagiAPI.ClearProgressBar();
                    return true;
                }
                var idStr = idObject.name.Replace("AniPIN_IDC_", "");
                UnityEngine.Object.DestroyImmediate(idObject);
                var aniPINVariables = AvatarDeobfuscator.variables.FirstOrDefault(n => n.FolderID == idStr);
                if (aniPINVariables == null || !aniPINVariables.ObfuscateAnimator)
                    return true;
                var list = avatar.baseAnimationLayers;
                YagiAPI.CreateFolderRecursively(AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/");
                var count = list.Count();
                foreach (var v in Enumerable.Range(0, count))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Copying base animation layers.", v, count);
                    if (list[v].animatorController != null)
                    {
                        var path = AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/baseAnimationLayer_" + v.ToString() + ".controller";
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(list[v].animatorController), path);

                        var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorController)) as AnimatorController;
                        if (newAsset != null)
                            avatar.baseAnimationLayers[v].animatorController = newAsset;
                        EditorUtility.SetDirty(newAsset);
                        EditorUtility.SetDirty(avatar);
                    }
                }
                list = avatar.specialAnimationLayers;
                count = list.Count();
                foreach (var v in Enumerable.Range(0, count))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Copying special animation layers.", v, count);
                    if (list[v].animatorController != null)
                    {
                        var path = AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/specialAnimationLayer_" + v.ToString() + ".controller";
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(list[v].animatorController), path);

                        var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(AnimatorController)) as AnimatorController;
                        if (newAsset != null)
                            avatar.specialAnimationLayers[v].animatorController = newAsset;
                        EditorUtility.SetDirty(newAsset);
                        EditorUtility.SetDirty(avatar);
                    }
                }
                var expParams = avatar.expressionParameters;
                if (expParams != null)
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Copying special expression parameters.", 0, 1);
                    var newPath = AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/expParams" + ".asset";
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(expParams), newPath);
                    var newExpParam = AssetDatabase.LoadAssetAtPath(newPath, typeof(VRCExpressionParameters)) as VRCExpressionParameters;
                    if (newExpParam != null)
                        avatar.expressionParameters = newExpParam;
                    EditorUtility.SetDirty(newExpParam);
                    EditorUtility.SetDirty(avatar);
                }
                var allStates = new List<AnimatorState>();
                var paramNames = new List<string>();
                var motions = new List<string>();
                YagiAPI.UpdateProgressBar("[AniPIN]Get all states from animation layers.", 0, 2);
                GetAllStatesFromCustomLayer(avatar.baseAnimationLayers, ref paramNames, ref allStates, ref motions, true);
                YagiAPI.UpdateProgressBar("[AniPIN]Get all states from animation layers.", 1, 2);
                GetAllStatesFromCustomLayer(avatar.specialAnimationLayers, ref paramNames, ref allStates, ref motions, true);
                var motionDicts = new Dictionary<string, Motion>();
                count = motions.Count;
                foreach (var motionIndex in Enumerable.Range(0, count))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Cloning motions.", motionIndex, count);
                    var path = AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/copyAnim_" + Guid.NewGuid() + ".anim";
                    AssetDatabase.CopyAsset(motions[motionIndex], path);
                    var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
                    if (newAsset != null)
                        motionDicts.Add(motions[motionIndex], newAsset);
                }
                var paramDicts = new Dictionary<string, string>();
                count = paramNames.Count;
                foreach (var paramIndex in Enumerable.Range(0, count))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Cloning parameters.", paramIndex, count);
                    var param = paramNames[paramIndex];
                    if (!AniPINEditor.systemParams.Contains(param))
                        paramDicts.Add(param, Guid.NewGuid().ToString());
                }
                expParams = avatar.expressionParameters;
                var expParamList = expParams.parameters;
                count = expParamList.Length;
                foreach (var index in Enumerable.Range(0, count))
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Obfuscating parameters.", index, count);
                    var parameter = expParamList[index];
                    var param = parameter.name;
                    if (paramDicts.ContainsKey(param))
                        parameter.name = paramDicts[param];
                }
                expParams.parameters = expParamList.OrderBy(n => Guid.NewGuid()).ToArray();
                avatar.expressionParameters = expParams;
                if (avatar.expressionsMenu != null)
                {
                    var menus = new List<VRCExpressionsMenu>();
                    avatar.expressionsMenu = ExpMenuDeepCopy(avatar.expressionsMenu, aniPINVariables, ref menus);
                    foreach (var menu in menus)
                    {
                        var controls = menu.controls;
                        var subcount = controls.Count;
                        foreach (var subIndex in Enumerable.Range(0, subcount))
                        {
                            YagiAPI.UpdateProgressBar("[AniPIN]Obfuscating menus.", subIndex, subcount);
                            var control = controls[subIndex];
                            var param = control.parameter;
                            if (paramDicts.ContainsKey(param.name))
                                param.name = paramDicts[param.name];
                            control.parameter = param;
                            var subParams = control.subParameters;
                            foreach (var subParam in subParams)
                            {
                                if (paramDicts.ContainsKey(subParam.name))
                                    subParam.name = paramDicts[subParam.name];
                            }
                            control.subParameters = subParams;
                        }
                        menu.controls = controls;
                        EditorUtility.SetDirty(menu);
                    }
                }
                var baseAnimationLayers = avatar.baseAnimationLayers;
                count = baseAnimationLayers.Length;
                var currentIndex = 0;
                foreach (var layer in baseAnimationLayers)
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Obfuscating base animation layers.", currentIndex++, count);
                    if (layer.animatorController != null)
                    {
                        var controller = layer.animatorController as AnimatorController;
                        var contLayers = controller.layers.ToList();
                        var targetIndex = UnityEngine.Random.Range(0, contLayers.Count - 2);
                        var targerLayer = contLayers.FirstOrDefault(n => n.name == AniPINEditor.mainLayerName);
                        if (targerLayer != null)
                        {
                            contLayers.Remove(targerLayer);
                            contLayers.Insert(targetIndex, targerLayer);
                        }
                        targetIndex = UnityEngine.Random.Range(0, contLayers.Count - 2);
                        targerLayer = contLayers.FirstOrDefault(n => n.name == AniPINEditor.overlayLayerName);
                        if (targerLayer != null)
                        {
                            contLayers.Remove(targerLayer);
                            contLayers.Insert(targetIndex, targerLayer);
                        }
                        foreach (var contLayer in contLayers)
                        {
                            contLayer.name = Guid.NewGuid().ToString();
                            var anyTransitions = contLayer.stateMachine.anyStateTransitions;
                            foreach (var transition in anyTransitions)
                            {
                                transition.name = Guid.NewGuid().ToString();
                                var conditions = transition.conditions;
                                foreach (var condition in Enumerable.Range(0, conditions.Length))
                                {
                                    var param = conditions[condition].parameter;
                                    if (paramDicts.ContainsKey(param))
                                        conditions[condition].parameter = paramDicts[param];
                                }
                                transition.conditions = conditions;
                                EditorUtility.SetDirty(transition);
                            }
                            contLayer.stateMachine.anyStateTransitions = anyTransitions;
                            var entryTransitions = contLayer.stateMachine.entryTransitions;
                            foreach (var transition in entryTransitions)
                            {
                                transition.name = Guid.NewGuid().ToString();
                                var conditions = transition.conditions;
                                foreach (var condition in Enumerable.Range(0, conditions.Length))
                                {
                                    var param = conditions[condition].parameter;
                                    if (paramDicts.ContainsKey(param))
                                        conditions[condition].parameter = paramDicts[param];
                                }
                                transition.conditions = conditions;
                                EditorUtility.SetDirty(transition);
                            }
                            contLayer.stateMachine.entryTransitions = entryTransitions;
                        }
                        controller.layers = contLayers.ToArray();
                        var paramList = controller.parameters;
                        foreach (var v in paramList)
                        {
                            var paramName = v.name;
                            if (paramDicts.ContainsKey(paramName))
                                v.name = paramDicts[paramName];

                        }
                        controller.parameters = paramList;
                        EditorUtility.SetDirty(controller);
                    }
                }
                avatar.baseAnimationLayers = baseAnimationLayers;
                var specialAnimationLayers = avatar.specialAnimationLayers;
                count = specialAnimationLayers.Length;
                currentIndex = 0;
                foreach (var layer in specialAnimationLayers)
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Obfuscating special animation layers.", currentIndex++, count);
                    if (layer.animatorController != null)
                    {
                        var controller = layer.animatorController as AnimatorController;
                        var contLayers = controller.layers.ToList();
                        foreach (var contLayer in contLayers)
                        {
                            contLayer.name = Guid.NewGuid().ToString();
                            var anyTransitions = contLayer.stateMachine.anyStateTransitions;
                            foreach (var transition in anyTransitions)
                            {
                                transition.name = Guid.NewGuid().ToString();
                                var conditions = transition.conditions;
                                foreach (var condition in Enumerable.Range(0, conditions.Length))
                                {
                                    var param = conditions[condition].parameter;
                                    if (paramDicts.ContainsKey(param))
                                        conditions[condition].parameter = paramDicts[param];
                                }
                                transition.conditions = conditions;
                                EditorUtility.SetDirty(transition);
                            }
                            contLayer.stateMachine.anyStateTransitions = anyTransitions;
                            var entryTransitions = contLayer.stateMachine.entryTransitions;
                            foreach (var transition in entryTransitions)
                            {
                                transition.name = Guid.NewGuid().ToString();
                                var conditions = transition.conditions;
                                foreach (var condition in Enumerable.Range(0, conditions.Length))
                                {
                                    var param = conditions[condition].parameter;
                                    if (paramDicts.ContainsKey(param))
                                        conditions[condition].parameter = paramDicts[param];
                                }
                                transition.conditions = conditions;
                                EditorUtility.SetDirty(transition);
                            }
                            contLayer.stateMachine.entryTransitions = entryTransitions;
                        }
                        controller.layers = contLayers.ToArray();
                        var paramList = controller.parameters;
                        foreach (var v in paramList)
                        {
                            var paramName = v.name;
                            if (paramDicts.ContainsKey(paramName))
                                v.name = paramDicts[paramName];

                        }
                        controller.parameters = paramList;
                        EditorUtility.SetDirty(controller);
                    }
                }
                avatar.specialAnimationLayers = specialAnimationLayers;
                EditorUtility.SetDirty(avatar);
                count = allStates.Count;
                currentIndex = 0;
                foreach (var state in allStates)
                {
                    YagiAPI.UpdateProgressBar("[AniPIN]Obfuscating states.", currentIndex++, count);
                    state.name = Guid.NewGuid().ToString();
                    var param = state.speedParameter;
                    if (paramDicts.ContainsKey(param))
                        state.speedParameter = paramDicts[param];
                    param = state.timeParameter;
                    if (paramDicts.ContainsKey(param))
                        state.timeParameter = paramDicts[param];
                    param = state.mirrorParameter;
                    if (paramDicts.ContainsKey(param))
                        state.mirrorParameter = paramDicts[param];
                    param = state.cycleOffsetParameter;
                    if (paramDicts.ContainsKey(param))
                        state.cycleOffsetParameter = paramDicts[param];
                    var transitions = state.transitions;
                    foreach (var transition in transitions)
                    {
                        transition.name = Guid.NewGuid().ToString();
                        var conditions = transition.conditions;
                        foreach (var condition in Enumerable.Range(0, conditions.Length))
                        {
                            param = conditions[condition].parameter;
                            if (paramDicts.ContainsKey(param))
                                conditions[condition].parameter = paramDicts[param];
                        }
                        EditorUtility.SetDirty(transition);
                        transition.conditions = conditions;
                    }
                    state.transitions = transitions;
                    var behaviours = state.behaviours;
                    foreach (var behaviour in behaviours)
                    {
                        if (behaviour is VRCAvatarParameterDriver)
                        {
                            var driver = behaviour as VRCAvatarParameterDriver;
                            var parameters = driver.parameters;
                            foreach (var parameter in parameters)
                            {
                                param = parameter.name;
                                if (paramDicts.ContainsKey(param))
                                    parameter.name = paramDicts[param];
                            }
                            driver.parameters = parameters;
                            EditorUtility.SetDirty(driver);
                        }
                    }
                    state.behaviours = behaviours;
                    if (state.motion != null)
                    {
                        var path = AssetDatabase.GetAssetPath(state.motion);
                        if(motionDicts.ContainsKey(path))
                        {
                            state.motion = motionDicts[path];
                        }
                        if (state.motion is BlendTree)
                        {
                            var blendTree = state.motion as BlendTree;
                            ObfuscateBlendTree(ref blendTree, ref paramDicts);
                            state.motion = blendTree;
                        }
                    }
                    EditorUtility.SetDirty(state);
                }
                AssetDatabase.SaveAssets();
                YagiAPI.ClearProgressBar();
                return true;
            }
            catch
            {

            }
            YagiAPI.ClearProgressBar();
            return false;

        }
        public void ObfuscateBlendTree(ref BlendTree blendTree, ref Dictionary<string, string> paramDicts)
        {
            var paramName = blendTree.blendParameter;
            if (paramDicts.ContainsKey(paramName))
                blendTree.blendParameter = paramDicts[paramName];
            paramName = blendTree.blendParameterY;
            if (paramDicts.ContainsKey(paramName))
                blendTree.blendParameterY = paramDicts[paramName];
            var childs = blendTree.children;
            foreach (var v in Enumerable.Range(0, childs.Length))
                if (childs[v].motion is BlendTree)
                {
                    var childTree = childs[v].motion as BlendTree;
                    ObfuscateBlendTree(ref childTree, ref paramDicts);
                    childs[v].motion = childTree;
                }
            blendTree.children = childs;
            EditorUtility.SetDirty(blendTree);

        }
        public VRCExpressionsMenu ExpMenuDeepCopy(VRCExpressionsMenu menu, AniPINVariables aniPINVariables, ref List<VRCExpressionsMenu> menus)
        {
            var path = AniPINEditor.autoGeneratedFolderPath + aniPINVariables.FolderID + "/DeleteOnStop/copyMenu_" +Guid.NewGuid() + ".asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(menu), path);

            var newAsset = AssetDatabase.LoadAssetAtPath(path, typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
            var list = newAsset.controls;
            foreach(var v in list)
            {
                if(v.subMenu != null)
                {
                    v.subMenu = ExpMenuDeepCopy(v.subMenu, aniPINVariables, ref menus);
                }
            }
            newAsset.controls = list;
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssets();
            menus.Add(newAsset);
            return newAsset;
        }
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
            {
                AvatarDeobfuscator.variables = new List<AniPINVariables>();
                var arr = UnityEngine.Object.FindObjectsOfType(typeof(AniPINSettings));
                if (arr == null) return true;
                var castedArr = arr.Cast<AniPINSettings>();
                foreach(var avatarVariables in castedArr)
                {
                    if (avatarVariables.aniPINVariablesValues != null)
                    {
                        var variablesValues = avatarVariables.aniPINVariablesValues;
                        AvatarDeobfuscator.variables.Add(variablesValues);
                        var name = "AniPIN_IDC_" + variablesValues.FolderID;
                        var idContainer = GameObject.Find(name);
                        if (idContainer == null)
                        {
                            idContainer = new GameObject(name);
                            idContainer.transform.SetParent(variablesValues.AvatarRoot.transform);
                            EditorUtility.SetDirty(variablesValues.AvatarRoot);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }
            return true;
        }
        private void GetAllStatesFromCustomLayer(VRCAvatarDescriptor.CustomAnimLayer[] layers, ref List<string> paramNames, ref List<AnimatorState> animatorStates, ref List<string> motions, bool obfuscate)
        {
            foreach (var layer in layers)
            {
                if (layer.animatorController != null)
                {
                    var controller = layer.animatorController as AnimatorController;
                    foreach (var paramName in controller.parameters.Select(n => n.name))
                    {
                        if (!paramNames.Contains(paramName))
                            paramNames.Add(paramName);
                    }
                    if(obfuscate)
                    {
                        var parameters = controller.parameters;
                        controller.parameters = parameters.OrderBy(n => Guid.NewGuid()).ToArray();
                    }
                    foreach (var conLayer in controller.layers)
                    {
                        if (conLayer.stateMachine != null)
                            GetAllStates(conLayer.stateMachine, ref animatorStates, ref motions, obfuscate);
                    }
                }
            }
        }
        private void GetAllStates(AnimatorStateMachine stateMachine, ref List<AnimatorState> animatorStates, ref List<string> motions, bool changePos = true)
        {
            var states = stateMachine.states;
            foreach (var v in Enumerable.Range(0, states.Length))
            {
                if (states[v].state.motion != null)
                {
                    var path = AssetDatabase.GetAssetPath(states[v].state.motion);
                    if (!motions.Contains(path))
                        motions.Add(path);
                }
                if (!animatorStates.Contains(states[v].state))
                {
                    animatorStates.Add(states[v].state);
                }
                if(changePos)
                    states[v].position = new Vector3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
            }
            if (changePos)
            {
                stateMachine.entryPosition = new Vector3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
                stateMachine.anyStatePosition = new Vector3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
                stateMachine.exitPosition = new Vector3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100));
            }
            stateMachine.states = states;
            var machines = stateMachine.stateMachines;
            foreach (var v in machines)
            {
                GetAllStates(v.stateMachine, ref animatorStates, ref motions, changePos);
            }
            stateMachine.stateMachines = machines;
        }
    }
    public static class AvatarDeobfuscator
    {
        public static List<AniPINVariables> variables = null;
        [InitializeOnLoadMethod]
        static void Init()
        {

            //playModeStateChangedイベントにメソッド登録
            EditorApplication.playModeStateChanged += OnChangedPlayMode;

        }

        private static void OnChangedPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                AvatarDeobfuscator.variables = new List<AniPINVariables>();
                var arr = UnityEngine.Object.FindObjectsOfType(typeof(AniPINSettings));
                if (arr == null) return;
                var castedArr = arr.Cast<AniPINSettings>();
                foreach (var avatarVariables in castedArr)
                {
                    if (avatarVariables.aniPINVariablesValues != null)
                    {
                        var variablesValues = avatarVariables.aniPINVariablesValues;
                        Debug.Log(AniPINEditor.autoGeneratedFolderPath + avatarVariables.FolderID + "/DeleteOnStop/");
                        FileUtil.DeleteFileOrDirectory(AniPINEditor.autoGeneratedFolderPath + avatarVariables.FolderID + "/DeleteOnStop/");
                        AvatarDeobfuscator.variables.Add(variablesValues);
                        var name = "AniPIN_IDC_" + variablesValues.FolderID;
                        var idContainer = GameObject.Find(name);
                        while ((idContainer = GameObject.Find(name)) != null)
                        {
                            UnityEngine.Object.DestroyImmediate(idContainer);
                            EditorUtility.SetDirty(variablesValues.AvatarRoot);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
            }
        }
    }
}