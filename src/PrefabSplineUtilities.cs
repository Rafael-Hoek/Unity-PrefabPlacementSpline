using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SplinePrefabPlacer
{
    [System.Serializable]
    public class SplinePrefabs
    {
        public List<SplinePrefab> prefabs;

        private string pattern;
        public string Pattern 
        { 
            get
            {
                return pattern;
            }
            set
            {
                if (Regex.IsMatch(value.ToUpper(), "^[A-Z][A-Z]*$"))
                    pattern = value.ToUpper();
                else 
                    Debug.LogError("NOT A VALID PATTERN: " + value);
            }
        }

        public enum PlacementMode
        {
            List = 0,
            Pattern = 1,
            Random = 3
        }

        private int index;
        public PlacementMode prefabPlacementMode;
        public SplinePrefabs()
        {
            prefabs = new List<SplinePrefab>();
            index = 0;
            prefabPlacementMode = PlacementMode.List;
        }

        public SplinePrefabs(PlacementMode mode)
        {
            prefabs = new List<SplinePrefab>();
            index = 0;
            this.prefabPlacementMode = mode;
        }

        public void ResetIndex()
        {
            index = 0;
        }
        
        public GameObject GetNextPrefab()
        {
            GameObject prefab = prefabs[0].prefab;
            switch (prefabPlacementMode)
            {
                case PlacementMode.Pattern:
                    int i = pattern[index] - 65;
                    if (i >= prefabs.Count)
                        Debug.LogError("This part of the pattern does not match with any existing index of provided prefabs: " + pattern[index]);
                    else
                        prefab = prefabs[i].prefab;

                    if (index >= pattern.Length - 1)
                        index = 0;
                    else
                        index++;
                    break;
                case PlacementMode.Random:
                    int weightAggregate = 0;
                    foreach(SplinePrefab sp in prefabs)
                    {
                        weightAggregate += sp.weight;
                    }
                    int selectedWeight = UnityEngine.Random.Range(1, weightAggregate + 1);
                    
                    index = 0;
                    weightAggregate = prefabs[index].weight;
                    while (weightAggregate < selectedWeight)
                    {
                        index++;
                        weightAggregate += prefabs[index].weight;
                    }
                    prefab = prefabs[index].prefab;
                    break;
                default:
                    prefab = prefabs[index].prefab;

                    if (index >= prefabs.Count - 1)
                        index = 0;
                    else
                        index++;
                    break;
            }
            return prefab;
        }
    }

    [System.Serializable]
    public class SplinePrefab
    {
        public GameObject prefab;
        public int weight;

        public SplinePrefab()
        {
            this.prefab = null;
            this.weight = 1;
        }

        public SplinePrefab(GameObject prefab, int weight)
        {
            this.prefab = prefab;
            this.weight = weight;
        }
    }

  
}
