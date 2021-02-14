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
	private bool movePoints = true;
	private bool addPoints = false;

	private int currentIndex = 0;

	private void OnEnable()
	{
		consumer = (AssetConsumer)target;
		if(consumer != null && consumer.distributedAssets.Count > 0)
		{
			ComputePointsInPolygon();
		}
	}

	private void OnSceneGUI()
	{
		if (consumer != null && consumer.distributedAssets.Count > 0)
		{
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

			if (movePoints)
			{
				if (consumer.getVertices(currentIndex).Count > 0)
				{
					EditorGUI.BeginChangeCheck();
					Vector3 newVertex = Handles.PositionHandle(consumer.transform.position + consumer.getVertices(currentIndex)[consumer.getCurrentVertexIndex(currentIndex)], Quaternion.identity);

					if (EditorGUI.EndChangeCheck())
					{
						consumer.getVertices(currentIndex)[consumer.getCurrentVertexIndex(currentIndex)] = newVertex - consumer.transform.position;
						ComputePointsInPolygon();
					}
				}
			}

			if (addPoints)
			{
				if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
				{
					AddNewVertex();
				}
			}

			DrawPoints();
			DawLines();
			DrawVerteces();
		}
	}

	public override void OnInspectorGUI()
	{
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
			if (GUILayout.Button("Asset Distribution " + index, style))
			{
				currentIndex = index;
				ComputePointsInPolygon();
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

		if (GUILayout.Button("Add Distributed Asset"))
		{
			consumer.distributedAssets.Add(new DistributedAsset());
			currentIndex = consumer.distributedAssets.Count - 1;
		}

		if (EditorGUILayout.Toggle("Edit Distributed Asset", editAssetDistribution))
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
		if(consumer.distributedAssets[currentIndex].spawnables.Count > 0)
		{
			List<GameObject> spawnables = new List<GameObject>(consumer.distributedAssets[currentIndex].spawnables);
			spawnables.ForEach(spawnable =>
			{
				int index = spawnables.IndexOf(spawnable);
				consumer.distributedAssets[currentIndex].spawnables[index] = (GameObject)EditorGUILayout.ObjectField("Spawnable", spawnable, typeof(GameObject), false);
			});
		}

		if (GUILayout.Button("Add Spawnable"))
		{
			consumer.distributedAssets[currentIndex].spawnables.Add(null);
		}

		float newDensity = EditorGUILayout.FloatField("Density", consumer.distributedAssets[currentIndex].density);
		if (newDensity != consumer.distributedAssets[currentIndex].density && newDensity > 0)
		{
			consumer.distributedAssets[currentIndex].density = newDensity;
			ComputePointsInPolygon();
		}

		if (GUILayout.Button("Compute points in Polygon"))
		{
			ComputePointsInPolygon();
		}
		if (GUILayout.Button("Spawn Objects"))
		{
			if (consumer.distributedAssets[currentIndex].spawnables.Count > 0)
			{
				SpawnObjects();
			}
		}
		if (GUILayout.Button("Clear Verteces"))
		{
			consumer.distributedAssets[currentIndex].vertices.Clear();
			points.Clear();
			consumer.distributedAssets[currentIndex].currentVertexIndex = 0;
		}
		if (EditorGUILayout.Toggle("Add Points", addPoints))
		{
			addPoints = true;
			movePoints = false;
			Tools.current = Tool.None;
		}
		else
		{
			addPoints = false;
		}
		if (EditorGUILayout.Toggle("Move Points", movePoints))
		{
			movePoints = true;
			addPoints = false;
			consumer.distributedAssets[currentIndex].currentVertexIndex = Mathf.Clamp(
				consumer.distributedAssets[currentIndex].currentVertexIndex,
				0, consumer.distributedAssets[currentIndex].vertices.Count - 1);
			Tools.current = Tool.None;
		}
		else
		{
			movePoints = false;
		}
	}

	private void SpawnObjects()
	{
		GameObject newChild = new GameObject();
		newChild.name = consumer.distributedAssets[currentIndex].name;
		newChild.transform.parent = consumer.transform;

		points.ForEach(point =>
		{
			GameObject spawnedObject = Instantiate(
				consumer.distributedAssets[currentIndex].spawnables[0],
				new Vector3(point.x, 0f, point.y) + consumer.transform.position,
				Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
				newChild.transform);
			spawnedObject.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
		});
	}

	private void DrawPoints()
	{
		Handles.color = Color.red;
		points.ForEach(point =>
		{
			Handles.DrawSolidDisc(new Vector3(point.x, 0.02f, point.y) + consumer.transform.position, Camera.current.transform.forward, 0.05f);
		});
		Handles.color = Color.white;
	}

	private void DrawVerteces()
	{
		consumer.getVertices(currentIndex).ForEach(vertex =>
		{
			if (consumer.getVertices(currentIndex).IndexOf(vertex) == consumer.getCurrentVertexIndex(currentIndex))
			{
				Handles.color = Color.green;
				Handles.DrawSolidDisc(consumer.transform.position + vertex, Camera.current.transform.forward, 0.1f);
				Handles.color = Color.white;
			}
			else
			{
				Handles.DrawSolidDisc(consumer.transform.position + vertex, Camera.current.transform.forward, 0.1f);
			}
		});
	}

	private void DawLines()
	{
		if (consumer.distributedAssets[currentIndex].vertices.Count > 1)
		{
			List<Vector3> vertices = consumer.distributedAssets[currentIndex].vertices;
			vertices.ForEach(vertex =>
			{
				int index = vertices.IndexOf(vertex);
				Vector3 startPoint = consumer.transform.position + vertex + 0.02f * Vector3.up;
				Vector3 endPoint = consumer.transform.position + vertices[(index + 1) % vertices.Count] + 0.02f * Vector3.up;
				Debug.DrawLine(startPoint, endPoint);
			});
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
		ComputePointsInPolygon();
	}

	private void RemoveVertex()
	{
		int vertexCount = consumer.getVertices(currentIndex).Count;
		int currentVertexIndex = consumer.getCurrentVertexIndex(currentIndex);
		consumer.getVertices(currentIndex).RemoveAt(currentVertexIndex);
		
		consumer.setCurrentVertexIndex(currentIndex, vertexCount == 0 ? 0 : (currentVertexIndex - 1 + vertexCount) % vertexCount);
		ComputePointsInPolygon();
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

	public bool LineLineIntersection(Vector2 linePoint1, Vector2 lineVec1, Vector2 linePoint2, Vector2 lineVec2)
	{
		Matrix A = Matrix.Build.DenseOfArray(new float[,] {
			 {-lineVec1.x, lineVec2.x},
			 {-lineVec1.y, lineVec2.y}});

		Vector b = Vector.Build.Dense(new float[] { (linePoint1 - linePoint2).x, (linePoint1 - linePoint2).y });

		if (A.Determinant() != 0f)
		{
			Vector sol = A.Solve(b);
			return (0 <= sol[0] && sol[0] <= 1) && (0 <= sol[1] && sol[1] <= 1);
		} else
		{
			return false;
		}
	}

	public float Determinant(Vector2 a, Vector2 b)
	{
		return (float)(a.x * b.y - a.y * b.x);
	}
}
