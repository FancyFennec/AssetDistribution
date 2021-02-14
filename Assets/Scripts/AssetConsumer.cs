using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AssetConsumer : MonoBehaviour
{
	public List<DistributedAsset> distributedAssets = new List<DistributedAsset>();

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
	public List<Vector3> verteces = new List<Vector3>();
	public List<GameObject> spawnables = new List<GameObject>();
	public float density = 2f;
	public int currentVertexIndex = 0;
}
