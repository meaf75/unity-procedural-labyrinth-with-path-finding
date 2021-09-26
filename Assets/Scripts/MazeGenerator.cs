using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeGenerator : MonoBehaviour {

	[Header("References")] 
	public GameMaster gm;
	
	[Header("Maze")]
	public List<GameObject> ceilsGenerated;
	private int numberOfCeils;

	public MazeCeil[,] ceils;

	public bool generated;

	[Header("Preview")] 
	public bool showDelay = true;
	public Transform indicator;
	public Transform goalIndicator;
	 
	
	[Header("Prefabs")]
	public MazeCeil mazeCeilPrefab;

	private IEnumerator pathCreation = null;
	
	public void GenerateMaze() {

		generated = false;
		numberOfCeils = gm.MazeSize;
		
		Debug.Log("Creating a new maze");

		// Fix camera position
		var cameraPos = Camera.main.transform.position;
		cameraPos.x = numberOfCeils / 2;
		cameraPos.y = numberOfCeils / 2;

		var cam = Camera.main;
		
		Debug.Assert(cam != null,"Camera not found");
		
		cam.transform.position = cameraPos;
		cam.orthographicSize = (numberOfCeils / 2) + 5;
		
		// Clear previous ceils
		if (ceilsGenerated != null && ceilsGenerated.Count > 0) {
			foreach (var ceilGameObject in ceilsGenerated) {
				if(!Application.isPlaying)
					DestroyImmediate(ceilGameObject);
				else
					Destroy(ceilGameObject);
			}
			
			ceilsGenerated.Clear();
		}

		ceils = new MazeCeil[numberOfCeils, numberOfCeils];
		
		// Generate maze
		for (int x = 0; x < numberOfCeils; x++) {
			for (int y = 0; y < numberOfCeils; y++) {
				GenerateCeil(x, y);
			}	
		}

		if (pathCreation != null) {
			StopCoroutine(pathCreation);
			pathCreation = null;
		}
		
		gm.OnGenerateMaze();
		goalIndicator.gameObject.SetActive(false);
		
		pathCreation = CreatePaths();
		StartCoroutine(pathCreation);
	}

	private void GenerateCeil(int x, int y) {
		var ceilGenerated = Instantiate(mazeCeilPrefab,transform);
		ceilGenerated.transform.position = new Vector3(x, y);

		ceils[x, y] = ceilGenerated;
		
		ceilsGenerated.Add(ceilGenerated.gameObject);
	}

	private IEnumerator CreatePaths() {

		Debug.Log("Creating paths...");
		
		var firstCeil = ceils[0, 0];
		
		// Remove bottom/left wall (Start)
		firstCeil.DestroyWallAtPosition(WallPosition.BOTTOM,false);
		
		var steps = new Queue<Vector2>();
			
		(int x, int y) = (0,0);

		// Fill map
		while (true) {
			
			if(gm.delayToggle.isOn)
				yield return null;
			
			steps.Enqueue(new Vector2(x,y));

			(bool navigated, MazeCeil nextCeil) = VerifyAtCeil(ceils[x,y]);

			indicator.position = new Vector3(x + .5f, y);
			
			if (navigated) {
				var nextPos = nextCeil.transform.position;
				(x, y) = ((int) nextPos.x, (int) nextPos.y);
			} else {
				// Now go back and verify

				MazeCeil rollbackMazeCeil = null;
				
				while (steps.Count > 0) {
					var step = steps.Dequeue();
					
					(navigated, rollbackMazeCeil) = VerifyAtCeil(ceils[(int) step.x,(int) step.y]);
				
					if(navigated)
						break;
				}

				// If a ceil was found use it as reference
				if (rollbackMazeCeil) {
					// A valid cell has been found, save references and go back
					var nextPos = rollbackMazeCeil.transform.position;
					(x, y) = ((int) nextPos.x, (int) nextPos.y);
					continue;
				}
				
				var goalPos = new Vector2(numberOfCeils - .5f, Random.Range(0, numberOfCeils));
				goalIndicator.transform.position = goalPos;
				goalIndicator.gameObject.SetActive(true);

				indicator.position = new Vector3(.5f, 0, 0);
				
				Debug.Log("<color=green>Finished</color>");
				generated = true;
				yield break;
			}

		}
	}
	
	private (bool, MazeCeil) VerifyAtCeil(MazeCeil currentCeil) {
		
		var activeWalls = Array.FindAll(currentCeil.ceilWalls, c => c.enabled).Randomize();
		
		(int x, int y) = currentCeil.CeilPosition;
		
		// Go to next ceil
		foreach (var wallInfo in activeWalls) {
			var wallPos = wallInfo.position;
			var offset = wallPos.GetOffset();

			var nextX = x + offset.x;
			var nextY = y + offset.y;
				
			if(IsOutbounds((int) nextX,(int) nextY))
				continue;	// Invalid position

			var nextCeil = ceils[x + (int) offset.x, y + (int) offset.y];
				
			if(nextCeil.hasBeenVisited)
				continue;

			currentCeil.hasBeenVisited = true;
			nextCeil.hasBeenVisited = true;
				
			// Remove walls
			currentCeil.DestroyWallAtPosition(wallPos);
			nextCeil.DestroyWallAtPosition(wallPos.GetOpositePosition());

			// Now use the next ceil
			return (true,nextCeil);
		}

		return (false,null);
	}

	/// <summary> Check if given position is out bounds of the maze </summary>
	public bool IsOutbounds(int x, int y) {
		if(x < 0 || x >= numberOfCeils || y < 0 || y >= numberOfCeils)
			return  true;	// Invalid position

		return false;
	}
}


#if UNITY_EDITOR
[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		
		GUILayout.Space(10);

		if (GUILayout.Button("Generate maze")) {
			((MazeGenerator) target).GenerateMaze();
		}
	}
}
#endif
