using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AssetConsumer : MonoBehaviour
{
	public List<DistributedAsset> distributedAssets = new List<DistributedAsset>();

	public string getName(int index) { return distributedAssets[index].name; }
	public List<Vector3> getVertices(int index) { return distributedAssets[index].vertices; }
	public List<GameObject> getSpawnables(int index) { return distributedAssets[index].spawnables; }
	public float getDensity(int index) { return distributedAssets[index].density; }
	public float setDensity(int index, float density) { return distributedAssets[index].density = density; }
	public int getCurrentVertexIndex(int index) { return distributedAssets[index].currentVertexIndex; }
	public int setCurrentVertexIndex(int index, int newVertexIndex) { return distributedAssets[index].currentVertexIndex = newVertexIndex; }

	void Start()
	{

	}

	void Update()
	{

	}
}

public class DistributedAsset
{
	public static string RandomString(int length)
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		return new string(Enumerable.Repeat(chars, length)
		  .Select(s => s[Random.Range(0, chars.Length)]).ToArray());
	}

	public string name = RandomString(12);
	public List<Vector3> vertices = new List<Vector3>();
	public List<GameObject> spawnables = new List<GameObject>();
	public float density = 2f;
	public int currentVertexIndex = 0;
}
