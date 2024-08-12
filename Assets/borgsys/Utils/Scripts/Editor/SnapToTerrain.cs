using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace borgsys.Utils
{
   public class SnapToTerrain : EditorWindow
   {

      private float yOffset = 0f;

      [MenuItem("Tools/borgsys/Utils/Snap to Terrain")]
      public static void ShowWindow()
      {
         GetWindow<SnapToTerrain>("Snap to Terrain");
      }


      void OnGUI()
      {
         yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
         if (GUILayout.Button("Snap Selected Objects to Terrain"))
         {
            SnapSelectedObjectsToAllTerrains();
         }
      }

      private void SnapSelectedObjectsToAllTerrains()
      {
         Terrain[] terrains = Terrain.activeTerrains;

         if (terrains.Length == 0)
         {
            Debug.LogError("No active terrains found in the scene.");
            return;
         }

         foreach (GameObject obj in Selection.gameObjects)
         {
            Vector3 position = obj.transform.position;
            Terrain closestTerrain = null;
            float closestDistance = float.MaxValue;

            // Find the closest terrain to the object
            foreach (Terrain terrain in terrains)
            {
               Vector3 terrainPosition = terrain.transform.position;
               Vector3 terrainSize = terrain.terrainData.size;
               Rect terrainBounds = new Rect(terrainPosition.x, terrainPosition.z, terrainSize.x, terrainSize.z);

               if (terrainBounds.Contains(new Vector2(position.x, position.z)))
               {
                  float distance = Vector3.Distance(position, terrainPosition);
                  if (distance < closestDistance)
                  {
                     closestDistance = distance;
                     closestTerrain = terrain;
                  }
               }
            }

            if (closestTerrain != null)
            {
               float terrainHeight = closestTerrain.SampleHeight(position) + closestTerrain.transform.position.y;
               Vector3 newPosition = new Vector3(position.x, terrainHeight + yOffset, position.z);
               obj.transform.position = newPosition;

               //Debug.Log($"Snapped {obj.name} to terrain at {newPosition}");
            }
            else
            {
               Debug.LogWarning($"No terrain found directly under {obj.name}. Object not snapped.");
            }
         }
      }
   }
}
