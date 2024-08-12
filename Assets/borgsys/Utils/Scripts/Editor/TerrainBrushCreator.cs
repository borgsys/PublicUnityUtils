using System.IO;
using UnityEditor;
using UnityEngine;

namespace borgsys.Utils
{
   public class TerrainBrushCreator : EditorWindow
   {
      private string folderPath = "Assets/BrushImageFolder"; // Default path to your images folder
      private AnimationCurve defaultFalloff = AnimationCurve.Linear(0, 0, 1, 1);
      private float radiusScale = 1f;
      private Vector2 brushRemap = new Vector2(0, 1);
      private bool invertBrush = false;

      private bool searchSubfolders = true; // Option to search subfolders
      private bool overwriteExisting = false; // Option to overwrite existing brush files


      [MenuItem("Tools/borgsys/Utils/Create Terrain Brushes")]
      public static void ShowWindow()
      {
         GetWindow<TerrainBrushCreator>("Create Terrain Brushes");
      }

      void OnGUI()
      {
         GUILayout.Label("Create Terrain Brushes", EditorStyles.boldLabel);

         // Drag and Drop Field
         EditorGUILayout.LabelField("Drag and Drop Folder Here:");
         Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
         GUI.Box(dropArea, "Drop Folder Here");
         HandleDragAndDrop(dropArea);

         // Folder Selection Button
         if (GUILayout.Button("Select Folder"))
         {
            string selectedFolder = EditorUtility.OpenFolderPanel("Select Brush Image Folder", "", "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
               folderPath = "Assets" + selectedFolder.Replace(Application.dataPath, "");
            }
         }

         folderPath = EditorGUILayout.TextField("Images Folder Path", folderPath);
         GUILayout.Space(10);
         // Search subfolders option
         searchSubfolders = EditorGUILayout.Toggle("Search Subfolders", searchSubfolders);
         // Overwrite existing files option
         overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Brushes", overwriteExisting);
         GUILayout.Space(10);

         if (GUILayout.Button("Create Brushes"))
            {
               CreateBrushes();
            }

      }

      void HandleDragAndDrop(Rect dropArea)
      {
         Event evt = Event.current;
         if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
         {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
               DragAndDrop.AcceptDrag();
               foreach (string draggedObject in DragAndDrop.paths)
               {
                  if (Directory.Exists(draggedObject))
                  {
                     folderPath = draggedObject.Replace(Application.dataPath, "");
                     Repaint();
                  }
               }
            }
         }
      }

      void CreateBrushes()
      {
         if (!Directory.Exists(folderPath))
         {
            Debug.LogError("Folder path does not exist.");
            return;
         }

         SearchAndCreateBrushes(folderPath);

         AssetDatabase.Refresh();
      }

      void SearchAndCreateBrushes(string path)
      {
         // Get all image files in the current directory
         string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
         foreach (string file in files)
         {
            if (file.EndsWith(".tif") || file.EndsWith(".jpg") || file.EndsWith(".png"))
            {
               CreateBrush(file, path);
            }
         }

         // Recursively search subfolders if the option is enabled
         if (searchSubfolders)
         {
            string[] subfolders = Directory.GetDirectories(path);
            foreach (string subfolder in subfolders)
            {
               SearchAndCreateBrushes(subfolder);
            }
         }
      }

      void CreateBrush(string filePath, string folder)
      {
         Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
         if (texture == null)
         {
            Debug.LogError("Failed to load texture: " + filePath);
            return;
         }

         // Prepare texture importer settings
         TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
         if (textureImporter != null)
         {
            textureImporter.alphaSource = TextureImporterAlphaSource.FromGrayScale;
            textureImporter.isReadable = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
         }

         string brushName = Path.GetFileNameWithoutExtension(filePath);
         string brushFilePath = $"{folder}/{brushName}.brush";

         // Check if the brush file already exists
         if (File.Exists(brushFilePath) && !overwriteExisting)
         {
            Debug.Log($"Brush '{brushName}' already exists and will not be overwritten.");
            return;
         }

         // Get the GUID of the texture asset
         string textureGUID = AssetDatabase.AssetPathToGUID(filePath);

         // Create the .brush file content
         string brushContent = GenerateBrushFileContent(brushName, textureGUID);

         // Write the .brush file
         File.WriteAllText(brushFilePath, brushContent);
      }

      string GenerateBrushFileContent(string brushName, string textureGUID)
      {
         // Serialize the falloff curve
         string falloffCurve = SerializeAnimationCurve(defaultFalloff);

         // Create the YAML content
         return $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 12146, guid: 0000000000000000e000000000000000, type: 0}}
  m_Name: {brushName}
  m_EditorClassIdentifier: 
  m_Mask: {{fileID: 2800000, guid: {textureGUID}, type: 3}}
  m_Falloff:
    serializedVersion: 2
    m_Curve: {falloffCurve}
    m_PreInfinity: 2
    m_PostInfinity: 2
    m_RotationOrder: 4
  m_RadiusScale: {radiusScale}
  m_BlackWhiteRemapMin: {brushRemap.x}
  m_BlackWhiteRemapMax: {brushRemap.y}
  m_InvertRemapRange: {(invertBrush ? 1 : 0)}
";
      }

      string SerializeAnimationCurve(AnimationCurve curve)
      {
         string result = "";
         for (int i = 0; i < curve.keys.Length; i++)
         {
            var key = curve.keys[i];
            result += $@"
    - serializedVersion: 3
      time: {key.time}
      value: {key.value}
      inSlope: {key.inTangent}
      outSlope: {key.outTangent}
      tangentMode: {(int)key.tangentMode}
      weightedMode: {(int)key.weightedMode}
      inWeight: {key.inWeight}
      outWeight: {key.outWeight}";
         }
         return result;
      }
   }

}
