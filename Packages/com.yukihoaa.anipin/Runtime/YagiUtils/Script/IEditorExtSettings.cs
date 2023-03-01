using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace YagihataItems.YagiUtils
{
    public interface IEditorExtSettings
    {
        VRCAvatarDescriptor AvatarRoot { get; set; }
        IEditorExtVariables GetVariables();
        void SetVariables(IEditorExtVariables variables);
    }
}