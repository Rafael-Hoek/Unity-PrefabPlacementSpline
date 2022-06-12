using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using Rand = UnityEngine.Random;
#endif

namespace SplinePrefabPlacer
{
	public class PrefabPlacementSpline : MonoBehaviour
	{

        #region variables
        public bool Loop
		{
			get
			{
				return loop;
			}
			set
			{
				loop = value;
				if (value)
				{
					modes[modes.Length - 1] = modes[0];
					SetControlPoint(0, points[0]);
				}
			}
		}

		public float TotalSplineLength
		{
			get
			{
				return totalSplineLength;
			}
		}

		public struct Sample
		{
			public float t;
			public float totalDistance;
			public Vector3 point;
			public Vector3 forward;
			public Vector3 up;
		}

		[SerializeField]
		private Vector3[] points;
		[SerializeField]
		private BezierControlPointMode[] modes;
		[SerializeField]
		private bool loop;
		[SerializeField]
		private float resampleTimeResolution = 0.0001f; // "time" step size for resampling (e.g. 1 / sample count)
		[SerializeField]
		private float distanceBetweenPrefabs = 1f;   // distance that needs to be crossed before a new prefab position is stored
		[SerializeField]
		private int numberOfPrefabsOnSpline = 1;
		[SerializeField]
		public SplinePrefabs prefabs = new SplinePrefabs();

		//fixed offsets from the spline
		public Vector3 offset = Vector3.zero;
		public Quaternion rotation = Quaternion.Euler(0,0,0);
		public Vector3 scale = Vector3.one;

		//distance from 0 on the y-axis ("height" of the spline for flattening the spline)
		public float yAxisPosition = 20f;


		//lower and upper limits for generating randomness in the offsets from the spline
		public Vector3 randomOffsetLowerLimit = Vector3.zero;
		public Quaternion randomRotationLowerLimit = Quaternion.Euler(0, 0, 0);
		public Vector3 randomScaleLowerLimit = Vector3.zero;
		
		public Vector3 randomOffsetUpperLimit = Vector3.zero;
		public Quaternion randomRotationUpperLimit = Quaternion.Euler(0,0,0);
		public Vector3 randomScaleUpperLimit = Vector3.zero;

		//booleans to decide whether to add randomness to offsets
		public bool OffsetRandomness = false;
		public bool RotationRandomness = false;
		public bool ScaleRandomness = false;

		//check whether to transpose the generated prefabs onto the ground below, and whether to adjust their angles to the ground they make contact with
		public bool CastToGround = false;
		public bool UseGroundNormal = false;

		private float totalSplineLength = 0f;
		private List<Sample> samples = null;
		private bool resampleDone = false;

		public float ScaleSplinePoints = 1f;

		#endregion
		public void Start()
		{
			Resample();
		}

		[ContextMenu("Redo sampling")]
		public void ResetFromEditor()
		{
			resampleDone = false;
			Resample(true);
		}

        #region resample
        public void Resample(bool forceInitialize = false)
		{
			if (resampleDone && !forceInitialize && samples != null) // in editor upon recompile
			{
				return;
			}

			if (samples == null)
			{
				samples = new List<Sample>();
			}
			else
			{
				samples.Clear();
			}

            float distanceSinceLastSample = 0;
			float totalDistance = 0;
			Vector3 previousPoint = GetPoint(0f);
			Vector3 currentPoint = previousPoint;
			StoreSample(0, currentPoint, totalDistance); // start sample at 0 distance, 0 time

			for (float time = 0; time <= 1f; time += resampleTimeResolution)
			{
				// Increment time in small steps. For every increment, add up the covered distance since last sample.
				// Also increment overall covered distance.
				currentPoint = GetPoint(time);
				float distanceToLastSample = Vector3.Distance(previousPoint, currentPoint);
				totalDistance += distanceToLastSample;
				distanceSinceLastSample += distanceToLastSample;

				// If distanceSinceLastSample crosses some threshold, take a new sample.
				// Samples intervals will be of roughly the same size, provided resampleTimeResolution is small enough.
				if (distanceSinceLastSample >= distanceBetweenPrefabs)
				{
					distanceSinceLastSample -= distanceBetweenPrefabs;
					StoreSample(time, currentPoint, totalDistance);
				}

				previousPoint = currentPoint;
			}

			// Add remaining section.
			// This is the distance all the way to the end of the spline.
			// This last section won't be of (roughly) the same length, but that's ok, we interpolate accordingly
			totalDistance += Vector3.Distance(previousPoint, GetPoint(1f));

			if (loop)
			{
				// Add last point at beginning again for perfect looping.
				StoreSample(1f, GetPoint(0f), totalDistance);
			}
			else
			{
				StoreSample(1f, GetPoint(1f), totalDistance);
			}

			// We'll need this elsewhere.
			totalSplineLength = totalDistance;
			resampleDone = true;
		}

		private void StoreSample(float t, Vector3 point, float totalDistance)
		{
			Sample s = new Sample();
			s.t = t;
			s.totalDistance = totalDistance;
			s.point = point;
			s.forward = GetVelocity(t).normalized;
			s.up = Vector3.up;

			samples.Add(s);
		}
		#endregion

		#region put prefabs on spline

		//method puts the given prefab on each calculated sample position
		public void AddPrefabsToSpline()
        {
			//update number of prefabs on spline so that the visual in the prefabSplineInspector is updated (in case this was changed due to a resample earlier)
			numberOfPrefabsOnSpline = Mathf.FloorToInt(totalSplineLength / distanceBetweenPrefabs);

			//reset index for prefabs so you start at the top of the list
			prefabs.ResetIndex();

			//create a game-object that will store all added prefabs
			GameObject generatedPrefabs = new GameObject("GeneratedPrefabs");
			generatedPrefabs.transform.SetParent(this.transform);
            #region add prefabs to spline
            //for each sample, create a prefab instance and add it to the spline
            for (int i = 0; i < samples.Count - 1; i++)
			{
				Sample sample = samples[i];

				//calculates which way the prefab instance will face according to the data calculated by the sample along the spline
				Quaternion orientation = Quaternion.LookRotation(sample.forward, sample.up);

                //create a random offset, rotation and scale generated with limits provided by the user
                //if-statements are to have a little bit of optimalisation (don't generate random numbers unneccessarily if you're not going to use them
                #region randomness in positions
                Vector3 randomOffset = Vector3.zero;
				Quaternion randomRotation = Quaternion.Euler(Vector3.zero);
				Vector3 randomScale = Vector3.one;
				if(OffsetRandomness)
					randomOffset = new Vector3(Rand.Range(randomOffsetLowerLimit.x, randomOffsetUpperLimit.x), Rand.Range(randomOffsetLowerLimit.y, randomOffsetUpperLimit.y), Rand.Range(randomOffsetLowerLimit.z, randomOffsetUpperLimit.z));
				if(RotationRandomness)
					randomRotation = Quaternion.Euler(Rand.Range(randomRotationLowerLimit.eulerAngles.x, randomRotationUpperLimit.eulerAngles.x), Rand.Range(randomRotationLowerLimit.eulerAngles.y, randomRotationUpperLimit.eulerAngles.y), Rand.Range(randomRotationLowerLimit.eulerAngles.z, randomRotationUpperLimit.eulerAngles.z));
				if(ScaleRandomness)
					randomScale = new Vector3(Rand.Range(randomScaleLowerLimit.x, randomScaleUpperLimit.x), Rand.Range(randomScaleLowerLimit.y, randomScaleUpperLimit.y), Rand.Range(randomScaleLowerLimit.z, randomScaleUpperLimit.z));
				#endregion

				//create an instance of the given prefab and give it the correct position, rotation and scale
				GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefabs.GetNextPrefab()) as GameObject;
				//put the prefab instance on the correct point, with any given fixed offset (in the correct orientation) and random offset
				prefabInstance.transform.position = sample.point + orientation * offset + orientation * randomOffset;
				prefabInstance.transform.rotation = orientation * rotation * randomRotation;
				prefabInstance.transform.localScale = scale + randomScale;

				//put the created prefab instance into the previously created parent object
				prefabInstance.transform.parent = generatedPrefabs.transform;
			}
			#endregion
		}

		//project all points on the bezier-curve to the ground of the level (if ground is found)
		public void ProjectPointsOntoGround()
        {
            for (int i = 0; i < samples.Count - 1; i++)
            {
                Sample sample = samples[i];

				if(Physics.Raycast(sample.point, Vector3.down, out RaycastHit hit))
                {
					sample.point = hit.point;
					if (UseGroundNormal)
                    {
						sample.up = hit.normal;
						sample.forward = Vector3.ProjectOnPlane(sample.forward, hit.normal);
					}
					samples[i] = sample;
				}
            }
        }

		//flatten the bezier-curve along the y-axis
		public void FlattenCurve()
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].y = yAxisPosition;
			}
		}

		//remove all children of the game-object this script is attached to
		public void DestroyChildren()
        {
			for (int i = this.transform.childCount - 1; i >= 0; i--)
			{
				DestroyImmediate(transform.GetChild(i).gameObject);
			}
		}
        #endregion

        #region Get points on spline
        public void GetPositionAndRotationOnSpline(float distance, out Vector3 newPosition, out Quaternion newRotation, bool stayHorizontal = true)
		{
			Resample();

			distance = distance % TotalSplineLength;

			// Look for the samples before and after currentDistanceAlongSpline. Sample is struct.
			Sample sampleBefore = new Sample();
			Sample sampleAfter = new Sample();

			FindSurroundingSamples(distance, ref sampleBefore, ref sampleAfter);

			// Find normalized interpolation point between two distances. At this point we're only working on a "polygon", no longer a spline.
			float lerp = (distance - sampleBefore.totalDistance) / (sampleAfter.totalDistance - sampleBefore.totalDistance);
			lerp = Mathf.Clamp01(lerp);

			// TODO: Lerp these values over time for smoother movement? Currently not yet necessary.
			newPosition = Vector3.Lerp(sampleBefore.point, sampleAfter.point, lerp);
			Vector3 sampledRotation = Vector3.Lerp(sampleBefore.forward, sampleAfter.forward, lerp);

			// for cars etc, this is desired, with only heading determined by spline and other rotation driven by e.g. ground matching
			// But for other things (e.g. plane, fixed wing, etc) we want free rotation.
			if (stayHorizontal)
			{
				sampledRotation = Vector3.ProjectOnPlane(sampledRotation, Vector3.up).normalized;
			}
		
			if(sampledRotation == Vector3.zero)
			{
				sampledRotation = Vector3.forward;
			}

			newRotation = Quaternion.LookRotation(sampledRotation, Vector3.up);
		}

		public Vector3 GetPositionOnSpline(float distance)
        {
			//quick helper function for when you don't need rotation
			GetPositionAndRotationOnSpline(distance, out Vector3 position, out _);
			return position;
        }

		private void FindSurroundingSamples(float distance, ref Sample sampleBefore, ref Sample sampleAfter)
		{
			// Distance between two samples is necessarily >= distanceBetweenPrefabs.
			// Except for in the last segment, which is covered by clamping anyway.
			int minimumIndex = Mathf.FloorToInt(distance / distanceBetweenPrefabs);

			minimumIndex = Mathf.Clamp(minimumIndex, 1, samples.Count - 1); // we check first for sample *after*, so start checking at 1

			bool success = false;

			for (int attempts = 0; attempts < 2; attempts++)
			{
				for (int i = minimumIndex; i < samples.Count; i++)
				{
					Sample sample = samples[i];
					if (sample.totalDistance >= distance)
					{
						sampleAfter = sample;
						sampleBefore = samples[i - 1]; // always starts at 1
						success = true;
						break;
					}
				}

				if (success)
					return;

				// Just in case we somehow didn't find valid samples, fall back to looping through ALL SAMPLES next time. 
				// Much less efficient. Normally speaking, this should never happen. Things that should never happen often happen, though.
				minimumIndex = 1;
			}
		}
		#endregion

		#region Bezier Functions
		public int ControlPointCount
		{
			get
			{
				return points.Length;
			}
		}

		public Vector3 GetControlPoint(int index)
		{
			return points[index];
		}

		public void SetControlPoint(int index, Vector3 point)
		{
			if (index % 3 == 0)
			{
				Vector3 delta = point - points[index];
				if (loop)
				{
					if (index == 0)
					{
						points[1] += delta;
						points[points.Length - 2] += delta;
						points[points.Length - 1] = point;
					}
					else if (index == points.Length - 1)
					{
						points[0] = point;
						points[1] += delta;
						points[index - 1] += delta;
					}
					else
					{
						points[index - 1] += delta;
						points[index + 1] += delta;
					}
				}
				else
				{
					if (index > 0)
					{
						points[index - 1] += delta;
					}
					if (index + 1 < points.Length)
					{
						points[index + 1] += delta;
					}
				}
			}
			points[index] = point;
			EnforceMode(index);
		}

		public void RemoveControlPoint(int index)
		{
			if (points.Length <= 4)
				return;

			List<int> indicesToRemove = new List<int>();
			int mainPointIndex = -1;

			if (index % 3 == 0) // is main control point
			{
				mainPointIndex = index / 3;

				if (index == 0)
				{
					indicesToRemove.Add(0);
					indicesToRemove.Add(1);
					indicesToRemove.Add(2);
				}
				else if (index == points.Length - 1)
				{
					indicesToRemove.Add(index);
					indicesToRemove.Add(index - 1);
					indicesToRemove.Add(index - 2);
				}
				else
				{
					indicesToRemove.Add(index);
					indicesToRemove.Add(index - 1);
					indicesToRemove.Add(index + 1);
				}
			}
			else
			{
				int offset = index % 3;

				if (offset == 1) // control point past the main control point
				{
					indicesToRemove.Add(index);
					indicesToRemove.Add(index - 1);
					mainPointIndex = (index - 1) / 3;

					if (index > 1)
					{
						indicesToRemove.Add(index - 2);
					}
				}
				else if (offset == 2) // control point before the main control point
				{
					indicesToRemove.Add(index);
					indicesToRemove.Add(index + 1);
					mainPointIndex = (index + 1) / 3;

					if (index < points.Length - 2)
					{
						indicesToRemove.Add(index + 2);
					}
				}
			}

			// Efficient? No, but this is an editor script so we'll settle for easy.
			List<Vector3> tempPoints = new List<Vector3>(points);
			indicesToRemove.Sort();
			indicesToRemove.Reverse();

			foreach (int indexToRemove in indicesToRemove)
			{
				tempPoints.RemoveAt(indexToRemove);
			}

			points = tempPoints.ToArray();

			List<BezierControlPointMode> tempModes = new List<BezierControlPointMode>(modes);
			tempModes.RemoveAt(mainPointIndex);
			modes = tempModes.ToArray();

	#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			Undo.RegisterCompleteObjectUndo(this, "Delete control point");
	#endif

		}

		public BezierControlPointMode GetControlPointMode(int index)
		{
			return modes[(index + 1) / 3];
		}

		public void SetControlPointMode(int index, BezierControlPointMode mode)
		{
			int modeIndex = (index + 1) / 3;
			modes[modeIndex] = mode;
			if (loop)
			{
				if (modeIndex == 0)
				{
					modes[modes.Length - 1] = mode;
				}
				else if (modeIndex == modes.Length - 1)
				{
					modes[0] = mode;
				}
			}
			EnforceMode(index);
		}

		private void EnforceMode(int index)
		{
			int modeIndex = (index + 1) / 3;
			BezierControlPointMode mode = modes[modeIndex];
			if (mode == BezierControlPointMode.Free || !loop && (modeIndex == 0 || modeIndex == modes.Length - 1))
			{
				return;
			}

			int middleIndex = modeIndex * 3;
			int fixedIndex, enforcedIndex;
			if (index <= middleIndex)
			{
				fixedIndex = middleIndex - 1;
				if (fixedIndex < 0)
				{
					fixedIndex = points.Length - 2;
				}
				enforcedIndex = middleIndex + 1;
				if (enforcedIndex >= points.Length)
				{
					enforcedIndex = 1;
				}
			}
			else
			{
				fixedIndex = middleIndex + 1;
				if (fixedIndex >= points.Length)
				{
					fixedIndex = 1;
				}
				enforcedIndex = middleIndex - 1;
				if (enforcedIndex < 0)
				{
					enforcedIndex = points.Length - 2;
				}
			}

			Vector3 middle = points[middleIndex];
			Vector3 enforcedTangent = middle - points[fixedIndex];
			if (mode == BezierControlPointMode.Aligned)
			{
				enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, points[enforcedIndex]);
			}
			points[enforcedIndex] = middle + enforcedTangent;
		}

		public int CurveCount
		{
			get
			{
				return (points.Length - 1) / 3;
			}
		}

		public Vector3 GetPoint(float t)
		{
			int i;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length - 4;
			}
			else
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
				i *= 3;
			}
			return transform.TransformPoint(BezierUtility.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
		}

		public Vector3 GetVelocity(float t)
		{
			int i;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length - 4;
			}
			else
			{
				t = Mathf.Clamp01(t) * CurveCount;
				i = (int)t;
				t -= i;
				i *= 3;
			}
			return transform.TransformPoint(BezierUtility.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
		}

		public Vector3 GetDirection(float t)
		{
			return GetVelocity(t).normalized;
		}

		public void AddCurve()
		{
			Vector3 point = points[points.Length - 1];
			Array.Resize(ref points, points.Length + 3);
			point.x += 1f;
			points[points.Length - 3] = point;
			point.x += 1f;
			points[points.Length - 2] = point;
			point.x += 1f;
			points[points.Length - 1] = point;

			Array.Resize(ref modes, modes.Length + 1);
			modes[modes.Length - 1] = modes[modes.Length - 2];
			EnforceMode(points.Length - 4);

			if (loop)
			{
				points[points.Length - 1] = points[0];
				modes[modes.Length - 1] = modes[0];
				EnforceMode(0);
			}
		}

		public void AddCurve(int index)
        {
			int currentNodeIndex = index - (index + 1) % 3 + 1;
			if(currentNodeIndex >= points.Length - 2)
            {
				AddCurve();
				return;
            }
			Vector3 point = points[currentNodeIndex];
			List<Vector3> tempPointsList = new List<Vector3>(points);
			point.x += 1f;
			tempPointsList.Insert(currentNodeIndex + 2, point);
			point.x += 1f;
			tempPointsList.Insert(currentNodeIndex + 3, point);
			point.x += 1f;
			tempPointsList.Insert(currentNodeIndex + 4, point);
			points = tempPointsList.ToArray();

			List<BezierControlPointMode> tempModesList = new List<BezierControlPointMode>(modes);
			int modeIndex = Mathf.FloorToInt((index + 1) / 3);
			tempModesList.Insert(modeIndex, modes[modeIndex]);
			modes = tempModesList.ToArray();

			EnforceMode(index);
			
			if (loop)
			{
				points[points.Length - 1] = points[0];
				modes[modes.Length - 1] = modes[0];
				EnforceMode(0);
			}
		}

		public void Reset()
		{
			points = new Vector3[] {
				new Vector3(1f, 0f, 0f),
				new Vector3(2f, 0f, 0f),
				new Vector3(3f, 0f, 0f),
				new Vector3(4f, 0f, 0f)
			};
			modes = new BezierControlPointMode[] {
				BezierControlPointMode.Free,
				BezierControlPointMode.Free
			};
		}

	#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			/*if(samples != null && samples.Count > 0)
            {
				for (int i = 0; i < samples.Count - 1; i++)
				{
					Sample sample = samples[i];
					Gizmos.color = Color.blue;
					Gizmos.DrawSphere(sample.point, 0.5f);
					Gizmos.color = Color.green;
					Gizmos.DrawLine(sample.point, sample.point + sample.forward * 3);
				}
			}*/
		}

		public void DrawDebugInfo()
		{
			if (samples == null)
			{
				GUILayout.Box("No data. Start play mode first or right-click > redo sampling.");
				return;
			}

			string message = $"Sample count: {samples.Count}." + System.Environment.NewLine;
			message += $"Total spline length: {TotalSplineLength}m." + System.Environment.NewLine;
			message += $"Time resolution step : ±{resampleTimeResolution * TotalSplineLength}m." + System.Environment.NewLine;
			message += $"{((resampleTimeResolution * TotalSplineLength) / distanceBetweenPrefabs * 100)}% error margin of distance step.";
			GUILayout.Box(message);
		}
	#endif

	}
	#endregion
}