using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<float>;

[CustomEditor(typeof(SomeScript))]
public class SomeEditor : Editor
{
	private SomeScript level;

	private int currentVertexIndex = 0;
	private GameObject spawnable = null;
	private float density = 1f;
	private float randomness = 1f;

	private List<Vector2> points = new List<Vector2>();
	private List<Vector3> vertices = new List<Vector3>(new Vector3[] {
        Vector3.zero,
        Vector3.forward,
        Vector3.forward + Vector3.right,
        Vector3.right
    });

	private void OnEnable()
	{
		level = (SomeScript) target;
	}

		private void OnSceneGUI()
	{
		DrawPositionHandle();
		DrawVerteces();
		DrawLines();

		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp)
		{
			currentVertexIndex = (currentVertexIndex + 1) % vertices.Count;
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown)
		{
			currentVertexIndex = (currentVertexIndex - 1 + vertices.Count) % vertices.Count;
		}
		// We only do this if we aren't moving the position handle and if we aren't orbiting around
		if (!GUI.changed && !Event.current.alt && Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			AddNewVertex();
		}
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace && vertices.Count > 0)
		{
			RemoveVertex();
		}
	}

	public override void OnInspectorGUI()
    {
		GUILayout.Label("Select Object");
		spawnable = (GameObject) EditorGUILayout.ObjectField(spawnable, typeof(GameObject), false);
		if (GUILayout.Button("Spawn Object"))
		{
			ComputePointsInPolygon();
			SpawnObjects();
		}
		GUILayout.Label("Edit Polygon");
        if(GUILayout.Button("Clear Polygon"))
		{
			vertices.Clear();
			currentVertexIndex = 0;
		}
    }

	private void SpawnObjects()
	{
		if (spawnable != null)
		{
			points.ForEach(point =>
			{
				float xOffset = Random.Range(-0.5f / density, 0.5f / density);
				float yOffset = Random.Range(-0.5f / density, 0.5f / density);
				Vector3 randomOffset = randomness * new Vector3(xOffset, 0f, yOffset);
				Vector3 rayOrigin = new Vector3(point.x, 0f, point.y) + randomOffset + 100f * Vector3.up;

				Ray mouseRay = new Ray(rayOrigin, Vector3.down);
				if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
				{
					GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(spawnable, level.transform);
					spawnedObject.transform.position = hit.point;
					spawnedObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
					spawnedObject.transform.localScale = Vector3.one * (1 - randomness * Random.Range(-0.25f, 0.25f));
				}
			});
		}
	}

	private void ComputePointsInPolygon()
	{
		points.Clear();

		if (vertices.Count > 2)
		{
			IEnumerable<float> xStream = vertices.Select(vertex => vertex.x);
			IEnumerable<float> yStream = vertices.Select(vertex => vertex.z);

			int xStart = Mathf.FloorToInt(xStream.Min() * density);
			int xAmount = Mathf.CeilToInt(xStream.Max() * density) - xStart;
			int yStart = Mathf.FloorToInt(yStream.Min() * density);
			int yAmount = Mathf.CeilToInt(yStream.Max() * density) - yStart;

			List<int> xRange = Enumerable.Range(xStart, xAmount).ToList();
			List<int> yRange = Enumerable.Range(yStart, yAmount).ToList();

			xRange.ForEach(x => yRange.ForEach(y =>
			{
				Vector2 point = new Vector2(x / density, y / density);
				if (IsPointInsidePolygon(point))
				{
					points.Add(point);
				}
			}));
		}
	}

	private bool IsPointInsidePolygon(Vector2 point)
	{
		List<Vector2> verteces2D = vertices.Select(vertex =>
		new Vector2(vertex.x, vertex.z)).ToList();
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
		}
		else
		{
			return false;
		}
	}

	private void AddNewVertex()
	{
		Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
		{
			if (vertices.Count == 0)
			{
				vertices.Add(hit.point);
			}
			else
			{
				vertices.Insert(currentVertexIndex + 1, hit.point);
				currentVertexIndex += 1;
			}
		}
	}

	private void RemoveVertex()
	{
		vertices.RemoveAt(currentVertexIndex);
		currentVertexIndex = vertices.Count == 0 ? 0 : (currentVertexIndex - 1 + vertices.Count) % vertices.Count;
	}

	private void DrawPositionHandle()
	{
		if (vertices.Count > 0)
		{
			EditorGUI.BeginChangeCheck();
			Vector3 newPos = Handles.PositionHandle(vertices[currentVertexIndex], Quaternion.identity);
			if (EditorGUI.EndChangeCheck())
			{
				// This is to make sure that our new vertex will be on top of the terrain
				Ray mouseRay = new Ray(newPos + Vector3.up * 100f, Vector3.down);
				if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
				{
					vertices[currentVertexIndex] = hit.point;
				}
				else
				{
					vertices[currentVertexIndex] = newPos;
				}
			}
		}
	}

	private void DrawVerteces()
	{
		vertices.ForEach(vertex =>
		{
			int index = vertices.IndexOf(vertex);

			// Mark the current vertex green
			Handles.color = index == currentVertexIndex ? Color.green : Color.white;
			Handles.DrawSolidDisc(vertex, Camera.current.transform.forward, 0.1f);
			Handles.color = Color.white;
		});
	}

	private void DrawLines()
	{
		if (vertices.Count > 0)
		{
			Handles.DrawAAPolyLine(new List<Vector3>(vertices) { vertices[0] }.ToArray());

			// This just draws a green line between the current and the next vertex
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(vertices[currentVertexIndex], vertices[(currentVertexIndex + 1) % vertices.Count]);
			Handles.color = Color.white;
		}
	}
}
