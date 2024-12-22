using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace YagihataItems.YagiUtils
{
    public static class AvatarUtils
    {
        public static AnimatorController GetFXLayer(this VRCAvatarDescriptor avatar, string createFolderDest, bool createNew = true)
        {
            AnimatorController controller = null;
            if (avatar.baseAnimationLayers != null && avatar.baseAnimationLayers.Length >= 5 && avatar.baseAnimationLayers[4].animatorController != null)
                controller = (AnimatorController)avatar.baseAnimationLayers[4].animatorController;
            else
            {
                if(createNew)
                {
                    var path = createFolderDest + "GeneratedFXLayer.controller";
                    YagiAPI.CreateFolderRecursively(createFolderDest);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (asset != null)
                        AssetDatabase.DeleteAsset(path);

                    controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                    if (avatar.baseAnimationLayers == null || avatar.baseAnimationLayers.Length < 5)
                    {
                        avatar.baseAnimationLayers = new CustomAnimLayer[]
                        {
                        new CustomAnimLayer(),
                        new CustomAnimLayer(),
                        new CustomAnimLayer(),
                        new CustomAnimLayer(){ isEnabled = true, animatorController = controller, type = AnimLayerType.FX }
                        };
                    }
                    else
                    {
                        avatar.baseAnimationLayers[4] = new CustomAnimLayer() { isEnabled = true, animatorController = controller, type = AnimLayerType.FX };
                    }
                }
            }
            return controller;
        }
        public static VRCExpressionParameters GetExpressionParameters(this VRCAvatarDescriptor avatar, string createFolderDest)
        {
            if (avatar.expressionParameters != null)
                return avatar.expressionParameters;
            else
            {
                var param = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                param.parameters = new VRCExpressionParameters.Parameter[16];
                param.parameters[0] = new VRCExpressionParameters.Parameter() { name = "VRCEmote", valueType = VRCExpressionParameters.ValueType.Int };
                param.parameters[1] = new VRCExpressionParameters.Parameter() { name = "VRCFaceBlendH", valueType = VRCExpressionParameters.ValueType.Float };
                param.parameters[2] = new VRCExpressionParameters.Parameter() { name = "VRCFaceBlendV", valueType = VRCExpressionParameters.ValueType.Float };
                var path = createFolderDest + "GeneratedExpressionParameters.asset";
                YagiAPI.CreateFolderRecursively(createFolderDest);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                    AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(param, path);
                avatar.expressionParameters = param;
                return param;
            }
        }
        public static void TryRemoveParameter(this VRCExpressionParameters expressionParameters, string name)
        {
            expressionParameters.parameters = expressionParameters.parameters.Where(n => n.name != name).ToArray();
        }
        public static VRCExpressionParameters.Parameter FindParameter(this VRCExpressionParameters expressionParameters, string name, VRCExpressionParameters.ValueType valueType)
        {
            return expressionParameters.parameters.FirstOrDefault(n => n.name == name && n.valueType == valueType);
        }
        public static VRCAvatarParameterDriver AddParameterDriver(this AnimatorState state, string name, float value = 0)
        {
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters = new List<VRC_AvatarParameterDriver.Parameter>
            {
                new VRC_AvatarParameterDriver.Parameter { name = name, value = value }
            };
            EditorUtility.SetDirty(driver);
            return driver;
        }
    }
}
