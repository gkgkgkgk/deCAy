using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TreeSpawn : MonoBehaviour
{
    public GameObject treePrefab; // Prefab of the tree to be spawned
    public int numTrees; // Number of trees to spawn
    public float minX, maxX, minY, maxY, minZ, maxZ; // Bounding box for spawning trees

    // This method is called when the "Spawn Trees" button is clicked in the inspector
    public void SpawnTrees()
    {
        for (int i = 0; i < numTrees; i++)
        {
            // Generate a random position within the specified bounds
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            float z = Random.Range(minZ, maxZ);
            Vector3 position = new Vector3(x, y, z);

            // Spawn a new tree at the generated position
            GameObject newTree = Instantiate(treePrefab, position, Quaternion.identity);
        }
    }
}