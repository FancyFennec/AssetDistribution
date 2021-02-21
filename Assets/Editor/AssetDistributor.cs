using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<float>;

[CustomEditor(typeof(AssetConsumer))]
public class AssetDistributor : Editor
{
	AssetConsumer consumer;

	public List<Vector2> points = new List<Vector2>();
	private bool editAssetDistribution = true;
	private bool editPolygon = true;
	private float randomness = 1f;

	private int currentIndex = 0;

	private void OnEnable()
	{
		consumer = (AssetConsumer) target;

		foreach (Transform child in consumer.transform)
		{
			int index = consumer.distributedAssets.FindIndex(distAs => distAs.name == child.name);
			if (index < 0 && child.name.StartsWith(DistributedAsset.DIS_AS_PREFIX))
			{
				DistributedAsset distAsset = new DistributedAsset();
				distAsset.name = child.name;
				foreach (Transform spawnable in child.transform)
				{
					GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(spawnable.gameObject);
					int blub = distAsset.spawnables.FindIndex(spwn => spwn.name == prefab.name);
					if (prefab != null &&  0 > blub)
					{
						distAsset.spawnables.Add(prefab);
					}
				}
				consumer.distributedAssets.Add(distAsset);
			}
		}
	}

	private void OnSceneGUI()
	{
		if (editPolygon && consumer != null && consumer.distributedAssets.Count > 0)
		{
			DrawPositionHandle();
			DrawLines();
			DrawVerteces();

			if (!GUI.changed && !Event.current.alt && Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				AddNewVertex();
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp)
			{
				consumer.setCurrentVertexIndex(currentIndex, (consumer.getCurrentVertexIndex(currentIndex) + 1) % consumer.getVertices(currentIndex).Count);
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown)
			{
				consumer.setCurrentVertexIndex(currentIndex, (consumer.getCurrentVertexIndex(currentIndex) - 1 + consumer.getVertices(currentIndex).Count) % consumer.getVertices(currentIndex).Count);
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace && consumer.getVertices(currentIndex).Count > 0)
			{
				RemoveVertex();
			}
		}
	}

	public override void OnInspectorGUI()
	{
		GUILayout.Label("Layers");
		List<DistributedAsset> distributedAssets = new List<DistributedAsset>(consumer.distributedAssets);
		distributedAssets.ForEach(distributedAsset =>
		{
			int index = distributedAssets.IndexOf(distributedAsset);
			GUILayout.BeginHorizontal();
			var style = new GUIStyle(GUI.skin.button);
			if (index == currentIndex)
			{
				style.normal.textColor = Color.yellow;
			}
			if (GUILayout.Button("Layer " + index, style))
			{
				currentIndex = index;
			}
			if (GUILayout.Button("-", GUILayout.Width(20)))
			{
				consumer.distributedAssets.RemoveAt(index);
				if(currentIndex >= index)
				{
					currentIndex = currentIndex == 0 ? 0 : currentIndex - 1;
				}
			}
			GUILayout.EndHorizontal();
		});

		if (GUILayout.Button("Add Layer"))
		{
			consumer.distributedAssets.Add(new DistributedAsset());
			currentIndex = consumer.distributedAssets.Count - 1;
		}

		if (EditorGUILayout.Toggle("Edit Layer", editAssetDistribution))
		{
			editAssetDistribution = true;
		}
		else
		{
			editAssetDistribution = false;
		}

		if(editAssetDistribution && consumer.distributedAssets.Count > 0) EditDistributionGui();
	}

	private void EditDistributionGui()
	{
		GUILayout.Label("Spawnable Objects");
		ListSpawnables();
		if (GUILayout.Button("Add Spawnable"))
		{
			consumer.distributedAssets[currentIndex].spawnables.Add(null);
		}
		GUILayout.Label("Distribution Properties");
		consumer.distributedAssets[currentIndex].density = EditorGUILayout.Slider("Density", consumer.distributedAssets[currentIndex].density, 0.0001f, 5f);
		randomness = EditorGUILayout.Slider("Randomness", randomness, 0f, 1f); ;
		if (GUILayout.Button("Spawn Objects"))
		{
			ComputePointsInPolygon();
			SpawnObjects();
		}
		if (GUILayout.Button("Clear Objects"))
		{
			ClearObjects();
		}
		GUILayout.Label("Edit Polygon");
		if (EditorGUILayout.Toggle("Edit Polygon", editPolygon))
		{
			editPolygon = true;
			consumer.distributedAssets[currentIndex].currentVertexIndex = Mathf.Clamp(
				consumer.distributedAssets[currentIndex].currentVertexIndex,
				0, consumer.distributedAssets[currentIndex].vertices.Count - 1);
			Tools.current = Tool.None;
		}
		else
		{
			editPolygon = false;
		}
		if (GUILayout.Button("Clear Polygon"))
		{
			consumer.distributedAssets[currentIndex].vertices.Clear();
			points.Clear();
			consumer.distributedAssets[currentIndex].currentVertexIndex = 0;
		}
	}

	private void ListSpawnables()
	{
		if (consumer.distributedAssets[currentIndex].spawnables.Count > 0)
		{
			int indexToBeRemoved = 0;
			bool removeSpawnable = false;

			List<GameObject> spawnables = new List<GameObject>(consumer.distributedAssets[currentIndex].spawnables);
			for(int index = 0; index < spawnables.Count; index++)
			{
				GUILayout.BeginHorizontal();
				consumer.distributedAssets[currentIndex].spawnables[index] = (GameObject) EditorGUILayout.ObjectField(spawnables[index], typeof(GameObject), false);

				if (GUILayout.Button("-", GUILayout.Width(20)))
				{
					removeSpawnable = true;
					indexToBeRemoved = index;
				}
				GUILayout.EndHorizontal();
			}
			if(removeSpawnable) consumer.distributedAssets[currentIndex].spawnables.RemoveAt(indexToBeRemoved);
		}
	}

	private void ClearObjects()
	{
		if (consumer.distributedAssets[currentIndex].spawnables.Count > 0 && consumer.transform.childCount > 0)
		{
			Transform assets = consumer.transform.Find(consumer.distributedAssets[currentIndex].name);
			while (assets.childCount > 0)
			{
				foreach (Transform child in assets)
				{
					DestroyImmediate(child.gameObject);
				}
			}
				
		}
	}

	private void SpawnObjects()
	{
		if (consumer.distributedAssets[currentIndex].spawnables.Count > 0)
		{
			GameObject child;
			Transform childTransform = consumer.transform.Find(consumer.distributedAssets[currentIndex].name);
			if(childTransform == null)
			{
				child = new GameObject();
				child.name = consumer.distributedAssets[currentIndex].name;
				child.transform.parent = consumer.transform;
			} else
			{
				child = childTransform.gameObject;
			}
			
			List<GameObject> spawnables = consumer.distributedAssets[currentIndex].spawnables;
			points.ForEach(point =>
			{
				float density = consumer.distributedAssets[currentIndex].density;
				float xOffset = Random.Range(-0.5f / density, 0.5f / density);
				float yOffset =  Random.Range(-0.5f / density, 0.5f / density);
				Vector3 randomOffset = randomness * new Vector3(xOffset, 0f, yOffset);
				Vector3 rayOrigin = new Vector3(point.x, 0f, point.y) + consumer.transform.position + randomOffset + 100f * Vector3.up;

				Ray mouseRay = new Ray(rayOrigin, Vector3.down);
				if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
				{
					GameObject spawnable = spawnables[Random.Range(0, spawnables.Count)];

					if (spawnable != null)
					{
						GameObject spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(spawnable, child.transform);
						spawnedObject.transform.position = hit.point;
						spawnedObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
						spawnedObject.transform.localScale = Vector3.one * (1 - randomness * Random.Range(-0.25f, 0.25f));
					}
				}
			});
		}
	}

	private void DrawPositionHandle()
	{
		if (consumer.getVertices(currentIndex).Count > 0)
		{
			EditorGUI.BeginChangeCheck();
			Vector3 newVertex = Handles.PositionHandle(consumer.transform.position + consumer.getVertices(currentIndex)[consumer.getCurrentVertexIndex(currentIndex)], Quaternion.identity);

			if (EditorGUI.EndChangeCheck())
			{
				Ray mouseRay = new Ray(newVertex + Vector3.up * 100f, Vector3.down);
				if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
				{
					consumer.getVertices(currentIndex)[consumer.getCurrentVertexIndex(currentIndex)] = hit.point - consumer.transform.position;
				} else
				{
					consumer.getVertices(currentIndex)[consumer.getCurrentVertexIndex(currentIndex)] = newVertex - consumer.transform.position;
				}
			}
		}
	}

	private void DrawVerteces()
	{
		consumer.getVertices(currentIndex).ForEach(vertex =>
		{
			int index = consumer.getVertices(currentIndex).IndexOf(vertex);

			Handles.color = index == consumer.getCurrentVertexIndex(currentIndex) ? Color.green : Color.white;
			Handles.DrawSolidDisc(consumer.transform.position + vertex, Camera.current.transform.forward, 0.1f);
			Handles.color = Color.white;
		});
	}

	private void DrawLines()
	{
		if (consumer.distributedAssets[currentIndex].vertices.Count > 1)
		{
			List<Vector3> vertices = consumer.distributedAssets[currentIndex].vertices;

			Handles.DrawAAPolyLine(new List<Vector3>(vertices) { vertices[0] }.ToArray());

			Handles.color = Color.green;
			int currentVertexIndex = consumer.getCurrentVertexIndex(currentIndex);
			Handles.DrawAAPolyLine(vertices[currentVertexIndex], vertices[(currentVertexIndex + 1) % vertices.Count]);
			Handles.color = Color.white;
		}
	}

	private void AddNewVertex()
	{
		List<Vector3> vertices = consumer.distributedAssets[currentIndex].vertices;
		Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
		{
			Vector3 mouseRayHit = hit.point;
			if (vertices.Count == 0)
			{
				vertices.Add(mouseRayHit - consumer.transform.position);
			}
			else
			{
				vertices.Insert(consumer.distributedAssets[currentIndex].currentVertexIndex + 1, mouseRayHit - consumer.transform.position);
				consumer.distributedAssets[currentIndex].currentVertexIndex += 1;
			}
		}
	}

	private void RemoveVertex()
	{
		int currentVertexIndex = consumer.getCurrentVertexIndex(currentIndex);
		consumer.getVertices(currentIndex).RemoveAt(currentVertexIndex);

		int vertexCount = consumer.getVertices(currentIndex).Count;
		consumer.setCurrentVertexIndex(currentIndex, vertexCount == 0 ? 0 : (currentVertexIndex - 1 + vertexCount) % vertexCount);
	}

	private void ComputePointsInPolygon()
	{
		points.Clear();

		if(consumer.getVertices(currentIndex).Count > 2)
		{
			IEnumerable<float> xStream = consumer.getVertices(currentIndex).Select(vertex => vertex.x);
			IEnumerable<float> yStream = consumer.getVertices(currentIndex).Select(vertex => vertex.z);

			float density = consumer.getDensity(currentIndex);

			int xStart = Mathf.FloorToInt(xStream.Min() * density);
			int xAmount = Mathf.CeilToInt(xStream.Max() * density) - xStart;
			int yStart = Mathf.FloorToInt(yStream.Min() * density);
			int yAmount = Mathf.CeilToInt(yStream.Max() * density) - yStart;

			List<int> xRange = Enumerable.Range(xStart, xAmount).ToList();
			List<int> yRange = Enumerable.Range(yStart, yAmount).ToList();

			Vector2 offset = new Vector2(consumer.transform.position.x, consumer.transform.position.z);

			xRange.ForEach(x => yRange.ForEach(y =>
			{
				Vector2 newPoint = new Vector2(x / density, y / density);
				if (IsPointInsidePolygon(consumer, newPoint + offset))
				{
					points.Add(newPoint);
				}
			}));
		}
	}

	private bool IsPointInsidePolygon(AssetConsumer level, Vector2 point)
	{
		List<Vector2> verteces2D = consumer.getVertices(currentIndex).Select(vertex => 
		new Vector2(level.transform.position.x + vertex.x, level.transform.position.z + vertex.z)).ToList();
		float maxX = verteces2D.Select(vertex => vertex.x).Max() + 1;
		Vector2 inftyPoint = new Vector2(maxX, 0);

		int intersectionCount = verteces2D.Select(vertex =>
		{
			Vector2 nextVertex = verteces2D[(verteces2D.IndexOf(vertex) + 1) % verteces2D.Count];
			return LineLineIntersection(point, inftyPoint - point, vertex, nextVertex - vertex) ? 1 : 0;
		}).Sum();
		return intersectionCount % 2 == 1;
	}

	public bool LineLineIntersection(Vector2 point1, Vector2 vec1, Vector2 point2, Vector2 vec2)
	{
		Matrix A = Matrix.Build.DenseOfArray(new float[,] {
			 {-vec1.x, vec2.x},
			 {-vec1.y, vec2.y}});

		Vector b = Vector.Build.Dense(new float[] { (point1 - point2).x, (point1 - point2).y });

		if (A.Determinant() != 0f)
		{
			Vector sol = A.Solve(b);
			return (0 <= sol[0] && sol[0] <= 1) && (0 <= sol[1] && sol[1] <= 1);
		} else
		{
			return false;
		}
	}
}
