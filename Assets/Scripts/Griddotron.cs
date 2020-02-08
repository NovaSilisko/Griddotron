using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Griddotron : MonoBehaviour
{
    public GameObject[] prefabs;

    public int randomObjectCount;
    public int randomObjectRange = 4;

    public Transform playerTransform;

    public int gridRadius = 2;
    public float gridCellSize = 4.0f;

    private int2 lastPlayerGrid; //The previous grid position

    private List<int2> currentPositions = new List<int2>(); //Our current set of grid positions 
    private List<int2> workingPositions = new List<int2>(); //The set of grid positions we use to check for differences

    private Dictionary<int2, GameObject> objectPrefabDict = new Dictionary<int2, GameObject>(); //All object spawn positions and their associated prefabs
    private Dictionary<int2, GameObject> spawnedObjectDict = new Dictionary<int2, GameObject>(); //What objects have we actually spawned?

    private List<Vector3> gizmoPrefabPositions = new List<Vector3>(); //Just for test visualization

    private void Start()
    {
        //Add a bunch of random prefab positions
        for (int i = 0; i < randomObjectCount; i++)
        {
            int2 randomPosition = new int2(UnityEngine.Random.Range(-randomObjectRange, randomObjectRange), UnityEngine.Random.Range(-randomObjectRange, randomObjectRange));

            if (!objectPrefabDict.ContainsKey(randomPosition))
            {
                int prefabIndex = UnityEngine.Random.Range(0, prefabs.Length);
                objectPrefabDict.Add(randomPosition, prefabs[prefabIndex]);
                gizmoPrefabPositions.Add(new Vector3(randomPosition.x * gridCellSize, gridCellSize * 0.5f, randomPosition.y * gridCellSize));
            }
        }

        lastPlayerGrid = new int2(999999, 999999); //A really stupid, simple way to guarantee the first update happens ¯\_(ツ)_/¯
    }

    private void Update()
    {
        //Flatten the player's position to a grid and round it
        //Also, yaaay, XY/XZ conversions
        float2 flatPlayerPosition = new float2(playerTransform.position.x, playerTransform.position.z);
        int2 currentPlayerGrid = RoundToGrid(flatPlayerPosition);

        //We use Equals here because int# is a bit odd and uses the "=" operator differently than intuition might suggest
        //I may or may not have spent several hours beating my face against problems caused by this trait before
        if (!currentPlayerGrid.Equals(lastPlayerGrid))
        {
            OnGridUpdate(currentPlayerGrid);
            lastPlayerGrid = currentPlayerGrid;
        }
    }

    private void OnGridUpdate(int2 playerGridPosition)
    {
        //First, we run across a grid surrounding the new player position.
        //Every one of these positions is added to the working position list,
        //but we also compare it to the current positions to see if it's new.

        workingPositions.Clear();

        for (int y = -gridRadius; y <= gridRadius; y++)
        {
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                int2 newPosition = new int2(playerGridPosition.x + x, playerGridPosition.y + y);

                if (!currentPositions.Contains(newPosition)) //This position is new!
                {
                    PositionAdded(newPosition);
                }

                workingPositions.Add(newPosition);
            }
        }

        //Secondly we run over the now old list of positions, and test for
        //whether our new list contains each one - if it doesn't, that
        //position is old, and is to be removed.

        foreach (int2 position in currentPositions)
        {
            if (!workingPositions.Contains(position)) //This position is old, and will be removed, like in Logan's Run
            {
                PositionRemoved(position);
            }
        }

        //With our add and remove events taken care of, we clear the current list
        //and replace its contents with the working list

        currentPositions.Clear();
        currentPositions.AddRange(workingPositions);

        //Disclaimer: There are a LOT of ways you could do this in general; this is just intended as a straightforward example
        //to demonstrate the concepts involve
    }

    //We could do basically anything we want in these add/remove functions, but in this case
    //I'm just checking against two dictionaries to see whether we should spawn a new object,
    //or remove a previously spawned object
    private void PositionAdded(int2 position)
    {
        if (objectPrefabDict.ContainsKey(position) && !spawnedObjectDict.ContainsKey(position))
        {
            GameObject newObj = Instantiate(objectPrefabDict[position]);
            newObj.transform.position = new Vector3(position.x * gridCellSize, 0.0f, position.y * gridCellSize);
            newObj.SetActive(true);
            spawnedObjectDict.Add(position, newObj);
        }
    }

    private void PositionRemoved(int2 position)
    {
        if (spawnedObjectDict.ContainsKey(position))
        {
            Destroy(spawnedObjectDict[position]);
            spawnedObjectDict.Remove(position);
        }
    }

    //And finally, the function that roundomates our positionation to the gridulon
    private int2 RoundToGrid(float2 position)
    {
        int gridx = (int)math.round(position.x / gridCellSize);
        int gridy = (int)math.round(position.y / gridCellSize);

        return new int2(gridx, gridy);
    }

    //Oh, and some visualization fluff
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        foreach (int2 position in currentPositions)
        {
            Vector3 realPosition = new Vector3(position.x * gridCellSize, 1.0f, position.y * gridCellSize);
            Gizmos.DrawWireCube(realPosition, new Vector3(gridCellSize, 1.0f, gridCellSize));
        }

        Gizmos.color = Color.green;

        foreach (Vector3 prefabPosition in gizmoPrefabPositions)
        {
            Gizmos.DrawWireCube(prefabPosition, Vector3.one * gridCellSize);
        }
    }
}