using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace YagihataItems.AniPIN
{
    public class AniPINSettings : MonoBehaviour
    {
        [SerializeField] public bool SavePIN { get { return aniPINVariablesValues.SavePIN; } set { aniPINVariablesValues.SavePIN = value; } }
        [SerializeField] public string PINCode { get { return aniPINVariablesValues.PINCode; } set { aniPINVariablesValues.PINCode = value; } }
        [SerializeField] public VRCAvatarDescriptor AvatarRoot { get { return aniPINVariablesValues.AvatarRoot; } set { aniPINVariablesValues.AvatarRoot = value; } }
        [SerializeField] public string FolderID { get { return aniPINVariablesValues.FolderID; } set { aniPINVariablesValues.FolderID = value; } }
        [SerializeField] [HideInInspector] public AniPINVariables aniPINVariablesValues = new AniPINVariables();
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        public void SetVariables(AniPINVariables variables)
        {
            this.SavePIN = variables.SavePIN;
            this.PINCode = variables.PINCode;
            this.AvatarRoot = variables.AvatarRoot;
            this.FolderID = variables.FolderID;
        }
        public AniPINVariables GetVariables()
        {
            return new AniPINVariables()
            {
                AvatarRoot = this.AvatarRoot,
                PINCode = this.PINCode,
                SavePIN = this.SavePIN,
                FolderID = this.FolderID
            };
        }
    }
    [System.Serializable]
    public class AniPINVariables
    {
        [SerializeField] public VRCAvatarDescriptor AvatarRoot;
        [SerializeField]
        public string PINCode
        {
            get { return Regex.Replace(rawPIN, @"[^0-9]", ""); }
            set { rawPIN = Regex.Replace(value, @"[^0-9]", ""); }
        }
        [SerializeField] public bool SavePIN = true;
        [SerializeField] [HideInInspector] private string rawPIN = "";
        [SerializeField] public string FolderID = "";
    }
}