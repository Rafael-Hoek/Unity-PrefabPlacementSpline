using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SplinePrefabPlacer
{
	[CustomEditor(typeof(PrefabPlacementSpline))]
	public class PrefabPlacementSplineInspector : Editor
	{

		private const int stepsPerCurve = 10;
		private const float directionScale = 0.5f;
		private const float handleSize = 0.08f;
		private const float pickSize = 0.06f;

		private static Vector3 copiedPosition = Vector3.zero;

		private static Color[] modeColors = {
		Color.white,
		Color.yellow,
		Color.cyan
		};

		private PrefabPlacementSpline spline;
		private Transform handleTransform;
		private Quaternion handleRotation;
		private int selectedIndex = -1;

		#region inspector GUI
		public override void OnInspectorGUI()
		{
			spline = target as PrefabPlacementSpline;

            #region spline data
            GUILayout.Label("Spline data", EditorStyles.boldLabel);
			GUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUI.BeginChangeCheck();
			bool loop = EditorGUILayout.Toggle("Loop", spline.Loop);            

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Toggle Loop");
				EditorUtility.SetDirty(spline);
				spline.Loop = loop;
			}

			if (selectedIndex >= 0 && selectedIndex < spline.ControlPointCount)
			{
				DrawSelectedPointInspector();
			}

			GUILayout.Space(20f);

			if (GUILayout.Button("Add Node to end of Spline"))
			{
				Undo.RecordObject(spline, "Add Node");
				spline.AddCurve();
				selectedIndex = spline.ControlPointCount - 1;
				EditorUtility.SetDirty(spline);
			}
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Y axis");
			spline.yAxisPosition = EditorGUILayout.FloatField(spline.yAxisPosition);
			if (GUILayout.Button("Flatten curve along Y-axis"))
			{
				spline.FlattenCurve();
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUILayout.Space(20f);
			serializedObject.Update();
			#endregion

			#region prefab placement settings
			GUILayout.Label("Prefab Placement Settings", EditorStyles.boldLabel);
			GUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabs.prefabs"));
			spline.prefabs.prefabPlacementMode = (SplinePrefabPlacer.SplinePrefabs.PlacementMode)EditorGUILayout.EnumPopup("Prefab Placement Mode", spline.prefabs.prefabPlacementMode);
			if(spline.prefabs.prefabPlacementMode == SplinePrefabs.PlacementMode.Pattern)
				spline.prefabs.Pattern = EditorGUILayout.TextField("Prefab Placement Pattern", spline.prefabs.Pattern);
			GUILayout.Space(10f);

			spline.offset = EditorGUILayout.Vector3Field("Offset", spline.offset);
			spline.rotation.eulerAngles = EditorGUILayout.Vector3Field("Rotation", spline.rotation.eulerAngles);
			spline.scale = EditorGUILayout.Vector3Field("Scale", spline.scale);
			GUILayout.Space(10f);

            #region randomness to prefab placements
            spline.OffsetRandomness = EditorGUILayout.ToggleLeft("Introduce randomness to offset", spline.OffsetRandomness);
            if (spline.OffsetRandomness)
            {
				spline.randomOffsetLowerLimit = EditorGUILayout.Vector3Field("Lower limit", spline.randomOffsetLowerLimit);
				spline.randomOffsetUpperLimit = EditorGUILayout.Vector3Field("Upper limit", spline.randomOffsetUpperLimit);
				GUILayout.Space(10f);
			} else
            {
				spline.randomOffsetLowerLimit = Vector3.zero;
				spline.randomOffsetUpperLimit = Vector3.zero;
            }


			spline.RotationRandomness = EditorGUILayout.ToggleLeft("Introduce randomness to rotation", spline.RotationRandomness);
			if(spline.RotationRandomness)
            {
				spline.randomRotationLowerLimit.eulerAngles = EditorGUILayout.Vector3Field("Lower limit", spline.randomRotationLowerLimit.eulerAngles);
				spline.randomRotationUpperLimit.eulerAngles = EditorGUILayout.Vector3Field("Upper limit", spline.randomRotationUpperLimit.eulerAngles);
				GUILayout.Space(10f);
			} else
            {
				spline.randomRotationLowerLimit = Quaternion.Euler(Vector3.zero);
				spline.randomRotationUpperLimit = Quaternion.Euler(Vector3.zero);
            }


			spline.ScaleRandomness = EditorGUILayout.ToggleLeft("Introduce randomness to scale", spline.ScaleRandomness);
			if (spline.ScaleRandomness)
			{
				spline.randomScaleLowerLimit = EditorGUILayout.Vector3Field("Lower limit", spline.randomScaleLowerLimit);
				spline.randomScaleUpperLimit = EditorGUILayout.Vector3Field("Upper limit", spline.randomScaleUpperLimit);
			}
			else
			{
				spline.randomScaleLowerLimit = Vector3.zero;
				spline.randomScaleUpperLimit = Vector3.zero;
			}
			#endregion
			GUILayout.Space(10f);
			spline.CastToGround = EditorGUILayout.ToggleLeft("Project spline onto ground", spline.CastToGround);
            if (spline.CastToGround)
            {
				spline.UseGroundNormal = EditorGUILayout.ToggleLeft("Use ground normal", spline.UseGroundNormal);
            }
			
			GUILayout.Space(20f);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfPrefabsOnSpline"));
            if (EditorGUI.EndChangeCheck())
            {
				if(serializedObject.FindProperty("numberOfPrefabsOnSpline").intValue >= 1)
					serializedObject.FindProperty("distanceBetweenPrefabs").floatValue = spline.TotalSplineLength / serializedObject.FindProperty("numberOfPrefabsOnSpline").intValue;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceBetweenPrefabs"));
			if (EditorGUI.EndChangeCheck())
			{
				if(serializedObject.FindProperty("distanceBetweenPrefabs").floatValue > 0)
					serializedObject.FindProperty("numberOfPrefabsOnSpline").intValue = Mathf.FloorToInt(spline.TotalSplineLength / serializedObject.FindProperty("distanceBetweenPrefabs").floatValue) + 1;
			}

			GUILayout.Space(10f);
			if(GUILayout.Button("Place Prefab on Spline"))
            {
				spline.ResetFromEditor();
				spline.DestroyChildren();
				if (spline.CastToGround)
					spline.ProjectPointsOntoGround();
				spline.AddPrefabsToSpline();
            }
			GUILayout.EndVertical();
            #endregion

            #region resampling and spline visuals
            GUILayout.Space(20f);
			GUILayout.Label("Resampling Settings", EditorStyles.boldLabel);
			GUILayout.BeginVertical(EditorStyles.helpBox);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("resampleTimeResolution"));

			spline.DrawDebugInfo();
			GUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();

			GUILayout.Space(20f);
			GUILayout.Label("Spline Visuals", EditorStyles.boldLabel);
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label("Scale points on spline");
			spline.ScaleSplinePoints = EditorGUILayout.Slider(spline.ScaleSplinePoints, 0.5f, 3f);
			GUILayout.Space(10f);
			GUILayout.EndVertical();
			#endregion
		}

		private void DrawSelectedPointInspector()
		{
			GUILayout.Label("Selected Point");
			EditorGUI.BeginChangeCheck();
			Vector3 point = EditorGUILayout.Vector3Field("Position", spline.GetControlPoint(selectedIndex));
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move Point");
				EditorUtility.SetDirty(spline);
				spline.SetControlPoint(selectedIndex, point);
			}
			EditorGUI.BeginChangeCheck();
			BezierControlPointMode mode = (BezierControlPointMode)EditorGUILayout.EnumPopup("Mode", spline.GetControlPointMode(selectedIndex));
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Change Point Mode");
				spline.SetControlPointMode(selectedIndex, mode);
				EditorUtility.SetDirty(spline);
			}

			if (GUILayout.Button("Copy position"))
			{
				copiedPosition = point;
			}

			if (GUILayout.Button("Paste position"))
			{
				Undo.RecordObject(spline, "Paste position");
				spline.SetControlPoint(selectedIndex, copiedPosition);
				EditorUtility.SetDirty(spline);
			}
			GUILayout.Space(5f);
			if(GUILayout.Button("Add node here"))
            {
				Undo.RecordObject(spline, "Add node here");
				spline.AddCurve(selectedIndex);
				selectedIndex = selectedIndex - (selectedIndex + 1) % 3 + 4;
				EditorUtility.SetDirty(spline);
			}
		}
		#endregion

		#region In-scene GUI
		private void OnSceneGUI()
		{
			spline = target as PrefabPlacementSpline;
			handleTransform = spline.transform;
			handleRotation = Tools.pivotRotation == PivotRotation.Local ?
				handleTransform.rotation : Quaternion.identity;

			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = CompareFunction.LessEqual;

			Event e = Event.current;
			if (e.type == EventType.KeyDown && e.shift && e.keyCode == KeyCode.X)
			{
				spline.RemoveControlPoint(selectedIndex);
			}

			Vector3 p0 = ShowPoint(0);
			for (int i = 1; i < spline.ControlPointCount; i += 3)
			{
				Handles.zTest = CompareFunction.Always;
				Vector3 p1 = ShowPoint(i);
				Vector3 p2 = ShowPoint(i + 1);
				Vector3 p3 = ShowPoint(i + 2);

				Handles.color = Color.gray;
				Handles.DrawLine(p0, p1);
				Handles.DrawLine(p2, p3);
				Handles.zTest = CompareFunction.LessEqual;

				Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 3f);
				p0 = p3;
			}
			ShowDirections();

			Handles.zTest = previousZTest;
		}

		private void ShowDirections()
		{
			Handles.color = Color.green;

			Vector3 point = spline.GetPoint(0f);
			Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
			int steps = stepsPerCurve * spline.CurveCount;
			for (int i = 1; i <= steps; i++)
			{
				point = spline.GetPoint(i / (float)steps);
				Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
			}
		}

		private Vector3 ShowPoint(int index)
		{
			Vector3 point = handleTransform.TransformPoint(spline.GetControlPoint(index));

			bool isMainPoint = (index % 3 == 0);

			Handles.color = modeColors[(int)spline.GetControlPointMode(index)];

			float size = HandleUtility.GetHandleSize(point);
			size *= spline.ScaleSplinePoints;
			if (index == 0)
			{
				size *= 2f;
				Handles.color = Color.magenta;
			}

			Handles.CapFunction capFunction = Handles.DotHandleCap;

			if (!isMainPoint)
			{
				size *= 1.5f; // sphere handle cap is pretty tiny
				capFunction = Handles.SphereHandleCap;
			}

			if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, capFunction))
			{
				selectedIndex = index;
				Repaint();
			}

			if (selectedIndex == index)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, handleRotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(spline, "Move Point");
					EditorUtility.SetDirty(spline);
					spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
				}
			}
			return point;
		}
	}
	#endregion
}
