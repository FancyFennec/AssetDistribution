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
	AssetConsumer level;
	public List<Vector3> verteces = new List<Vector3>();
	public List<Vector2> points = new List<Vector2>();

	public GameObject spawnable;

	private bool movePoints = true;
	private bool addPoints = false;
	private int editingPointIndex = 0;

	private float density = 2f;

	private void OnEnable()
	{
		level = (AssetConsumer)target;
		if(level != null)
		{
			verteces = level.verteces;
			ComputePointsInPolygon();
		}
	}

	private void OnSceneGUI()
	{
		if (level != null)
		{
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp)
			{
				editingPointIndex = (editingPointIndex + 1) % verteces.Count;
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown)
			{
				editingPointIndex = (editingPointIndex - 1 + verteces.Count) % verteces.Count;
			}
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace && verteces.Count > 0)
			{
				RemoveVertex();
			}

			if (movePoints)
			{
				if (verteces.Count > 0)
				{
					EditorGUI.BeginChangeCheck();
					Vector3 newVertex = Handles.PositionHandle(level.transform.position + verteces[editingPointIndex], Quaternion.identity);

					if (EditorGUI.EndChangeCheck())
					{
						verteces[editingPointIndex] = newVertex - level.transform.position;
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
			DawLines(level, verteces);
			DrawVerteces(level, verteces, editingPointIndex);
		}
	}

	private void DrawPoints()
	{
		Handles.color = Color.red;
		points.ForEach(point =>
		{
			Handles.DrawSolidDisc(new Vector3(point.x, 0.02f, point.y) + level.transform.position, Camera.current.transform.forward, 0.05f);
		});
		Handles.color = Color.white;
	}

	private static void DrawVerteces(AssetConsumer level, List<Vector3> verteces, int editingPointIndex)
	{
		verteces.ForEach(vertex =>
		{
			if(verteces.IndexOf(vertex) == editingPointIndex)
			{
				Handles.color = Color.green;
				Handles.DrawSolidDisc(level.transform.position + vertex, Camera.current.transform.forward, 0.1f);
				Handles.color = Color.white;
			} else
			{
				Handles.DrawSolidDisc(level.transform.position + vertex, Camera.current.transform.forward, 0.1f);
			}
			
		});
	}

	private static void DawLines(AssetConsumer level, List<Vector3> verteces)
	{
		if (verteces.Count > 1)
		{
			for (int i = 0; i < verteces.Count; i++)
			{
				Vector3 startPoint = level.transform.position + verteces[i] + 0.02f * Vector3.up;
				Vector3 endPoint = level.transform.position + verteces[(i + 1) % verteces.Count] + 0.02f * Vector3.up;
				Debug.DrawLine(startPoint, endPoint);
			}
		}
	}

	private void AddNewVertex()
	{
		Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity))
		{
			Vector3 mouseRayHit = hit.point;
			if(verteces.Count == 0)
			{
				verteces.Add(mouseRayHit - level.transform.position);
			} else
			{
				verteces.Insert(editingPointIndex + 1, mouseRayHit - level.transform.position);
				editingPointIndex += 1;
			}
		}
		ComputePointsInPolygon();
	}

	private void RemoveVertex()
	{
		verteces.RemoveAt(editingPointIndex);
		editingPointIndex = verteces.Count == 0 ? 0 : (editingPointIndex - 1 + verteces.Count) % verteces.Count;
		ComputePointsInPolygon();
	}

	public override void OnInspectorGUI()
	{
		spawnable = (GameObject) EditorGUILayout.ObjectField("Spawnable", spawnable, typeof(GameObject), false);

		float newDensity = EditorGUILayout.FloatField("Density", density);
		if(newDensity != density && newDensity > 0)
		{
			density = newDensity;
			ComputePointsInPolygon();
		}

		if (GUILayout.Button("Compute points in Polygon"))
		{
			ComputePointsInPolygon();
		}
		if (GUILayout.Button("Spawn Objects"))
		{
			if (spawnable != null)
			{
				GameObject newChild = new GameObject();
				newChild.name = "NewChild";
				newChild.transform.parent = level.transform;

				points.ForEach(point => {
					GameObject spawnedObject = Instantiate(
						spawnable, 
						new Vector3(point.x, 0f, point.y) + level.transform.position, 
						Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
						newChild.transform);
					spawnedObject.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
				});
			}
		}
		if (GUILayout.Button("Clear Verteces"))
		{
			verteces.Clear();
			points.Clear();
			editingPointIndex = 0;
		}
		if(EditorGUILayout.Toggle("Add Points", addPoints))
		{
			addPoints = true;
			movePoints = false;
			Tools.current = Tool.None;
		} else
		{
			addPoints = false;
		}
		if (EditorGUILayout.Toggle("Move Points", movePoints))
		{
			movePoints = true;
			addPoints = false;
			editingPointIndex = Mathf.Clamp(editingPointIndex, 0, verteces.Count - 1);
			Tools.current = Tool.None;
		}
		else
		{
			movePoints = false;
		}
	}

	private void ComputePointsInPolygon()
	{
		points.Clear();

		if(verteces.Count > 2)
		{
			IEnumerable<float> xStream = verteces.Select(vertex => vertex.x);
			IEnumerable<float> yStream = verteces.Select(vertex => vertex.z);

			int xStart = Mathf.FloorToInt(xStream.Min() * density);
			int xAmount = Mathf.CeilToInt(xStream.Max() * density) - xStart;
			int yStart = Mathf.FloorToInt(yStream.Min() * density);
			int yAmount = Mathf.CeilToInt(yStream.Max() * density) - yStart;

			List<int> xRange = Enumerable.Range(xStart, xAmount).ToList();
			List<int> yRange = Enumerable.Range(yStart, yAmount).ToList();

			Vector2 offset = new Vector2(level.transform.position.x, level.transform.position.z);

			xRange.ForEach(x => yRange.ForEach(y =>
			{
				Vector2 newPoint = new Vector2(x / density, y / density);
				if (IsPointInsidePolygon(level, newPoint + offset))
				{
					points.Add(newPoint);
				}
			}));
		}
	}

	private bool IsPointInsidePolygon(AssetConsumer level, Vector2 point)
	{
		List<Vector2> verteces2D = verteces.Select(vertex => 
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
