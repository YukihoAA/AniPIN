using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace YagihataItems.YagiUtils
{
    public class EditorGUILayoutExtra
    {

        /// <summary>
        /// インデントレベル設定を考慮した仕切り線.
        /// </summary>
        /// <param name="useIndentLevel">インデントレベルを考慮するか.</param>
        public static void Separator(bool useIndentLevel = false)
        {
            EditorGUILayout.BeginHorizontal();
            if (useIndentLevel)
            {
                GUILayout.Space(EditorGUI.indentLevel * 15);
            }
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// インデントレベルを設定する仕切り線.
        /// </summary>
        /// <param name="indentLevel">インデントレベル</param>
        public static void Separator(int indentLevel)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 15);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            EditorGUILayout.EndHorizontal();
        }
        public static void SeparatorWithSpace()
        {
            EditorGUILayoutExtra.Space();
            EditorGUILayoutExtra.Separator();
            EditorGUILayoutExtra.Space();
        }
        public static int IndexedStringList(string label, IndexedList indexedList)
        {
            EditorGUI.BeginChangeCheck();
            var property = indexedList.list;
            Func<string, int, string> selector = (string name, int number) => $"{number}: \r{name}";
            var list = indexedList.list.Select(selector).ToList();
            var divider = string.Empty;
            var unselected = "（未選択）";
            list.Add(divider);
            list.Add(unselected);
            var selectedIndex = indexedList.list.Length == 0 ? -1 : indexedList.index < 0 ? indexedList.list.Length + 1 : indexedList.index;
            var displayOptions = list.ToArray();
            var index = indexedList.list.Length > 0 ? EditorGUILayout.Popup(new GUIContent(label), selectedIndex, displayOptions) : selectedIndex;
            if (EditorGUI.EndChangeCheck())
            {
                indexedList.index = index;
            }
            indexedList.index = index > indexedList.list.Length ? -1 : index;
            return indexedList.index;
        }
        public static void Space()
        {
            EditorGUILayout.LabelField("");
        }
        //Alternate Method
        public static bool LinkLabel(string labelText)
        {
            return LinkLabel(labelText, Color.black, new Vector2(), 0);
        }

        //Alternate Method
        public static bool LinkLabel(string labelText, Color labelColor)
        {
            return LinkLabel(labelText, labelColor, new Vector2(), 0);
        }

        //Alternate Method
        public static bool LinkLabel(string labelText, Color labelColor, Vector2 contentOffset)
        {
            return LinkLabel(labelText, labelColor, contentOffset, 0);
        }

        //The Main Method
        public static bool LinkLabel(string labelText, Color labelColor, Vector2 contentOffset, int fontSize)
        {
            //Let's use Unity's label style for this
            GUIStyle stl = EditorStyles.label;
            //Next let's record the settings for Unity's label style because we will have to make sure these settings get returned back to
            //normal after we are done changing them and drawing our LinkLabel.
            Color col = stl.normal.textColor;
            Vector2 os = stl.contentOffset;
            int size = stl.fontSize;
            //Now we can modify the label's settings via the editor style : EditorStyles.label (stl).
            stl.normal.textColor = labelColor;
            stl.contentOffset = contentOffset;
            stl.fontSize = fontSize;
            //We are now ready to draw our Linklabel. I will actually use a GUILayout.Button to do this and our "stl" style will
            //make the button appear as a label.

            //Note : You may include a web address parameter in this method and open a URL at this point if the button is clicked,
            //however, I am going to just return bool based on weather or not the link was clicked. This gives me more control over
            //what actually happens when a link label is used. I also will instead include a "URL version" of this method below.

            //Since the button already returns bool, I will just return that result straight across like this.

            try
            {
                return GUILayout.Button(labelText, stl);
            }
            finally
            {
                //Remember to set the editor style (stl) back to normal here. A try / finally clause will work perfectly for this!!!

                stl.normal.textColor = col;
                stl.contentOffset = os;
                stl.fontSize = size;
            }
        }

        //This is a modified version of link label that opens a URL automatically. Note : this can also return bool if you want.
        public static void LinkLabel(string labelText, Color labelColor, Vector2 contentOffset, int fontSize, string webAddress)
        {
            if (LinkLabel(labelText, labelColor, contentOffset, fontSize))
            {
                try
                {
                    Application.OpenURL(@webAddress);
                    //if returning bool, return true here.
                }
                catch
                {
                    //In most cases, the catch clause would not happen but in the interest of being thorough I will log an
                    //error and have Unity "beep" if an exception gets thrown for any reason.
                    Debug.LogError("Could not open URL. Please check your network connection and ensure the web address is correct.");
                    EditorApplication.Beep();
                }
            }
            //if returning bool, return false here.
        }
    }
}
