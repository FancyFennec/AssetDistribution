using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(SomeScript))]
public class SomeEditor : Editor
{
	private int currentVertexIndex = 0;

	private List<Vector3> vertices = new List<Vector3>(new Vector3[] {
        Vector3.zero,
        Vector3.forward,
        Vector3.forward + Vector3.right,
        Vector3.right
    });

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

	private void DrawPositionHandle()
	{
		if(vertices.Count > 0)
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

	public override void OnInspectorGUI()
    {
        GUILayout.Label("Edit Polygon");
        if(GUILayout.Button("Clear Polygon"))
		{
			vertices.Clear();
			currentVertexIndex = 0;
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
