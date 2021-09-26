using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour {

	[Header("References")]
	public UITouchHandler uiTouchHandler;
	public MazeGenerator mazeGenerator;
	public Transform indicator;
	public LineRenderer ln;
	public GameObject uiLoadingPathContainer;
	public Toggle delayToggle;
	public TMP_InputField inputSize;

	private IEnumerator movementRoutine = null;

	[Header("Search State")] 
	public Transform searchStatesContainer;
	public Transform checkPlacePrefab;

	public List<Transform> checkedPlacesGenerated;
	public Queue<Transform> freeCheckedPlaces;

	[Header("Config")] 
	public float moveDelay;
	
	private void Awake() {
		uiTouchHandler.Initialize(this);
		freeCheckedPlaces = new Queue<Transform>();
	}
	
	public void HandleTouch(Vector2 touchPos) {
		
		if(movementRoutine != null || !mazeGenerator.generated)
			return;
		
		RestoreCheckedPlacesPool();
		
		Vector3 pos = Camera.main.ScreenToWorldPoint(touchPos);

		int limit = MazeSize - 1;
		
		pos.x = Mathf.Clamp(Mathf.Ceil(pos.x) + .5f - 1,.5f,limit + .5f);
		pos.y = Mathf.Clamp(Mathf.RoundToInt(pos.y),0,limit);
		pos.z = 0;
		
		ln.positionCount = 0;

		uiLoadingPathContainer.SetActive(true);
		
		movementRoutine = MoveTo(pos.x,pos.y);
		StartCoroutine(movementRoutine);

	}

	private IEnumerator MoveTo(float x, float y) {

		var indicatorPos = indicator.position;
		var startCeil = mazeGenerator.ceils[(int) indicatorPos.x, (int) indicatorPos.y];
		var endCeil = mazeGenerator.ceils[(int) x, (int) y];
		
		var openSet = new List<MazeCeil>();
		var closedSet = new List<MazeCeil>();

		startCeil.prevCeil = null;
		startCeil.g = 0;
		
		openSet.Add(startCeil);

		bool pathFound = false;
		MazeCeil current = null;
		
		// Check next ceils
		while (openSet.Count > 0) {

			Debug.Log("Getting path");
			
			yield return null;

			int winner = 0;

			// Get Best option
			for (var i = 0; i < openSet.Count; i++) {
				if (openSet[i].f < openSet[winner].f)
					winner = i;
			}

			current = openSet[winner];
			
			// Update search state
			GenerateCheckedPlaceAtCeil(current);
			
			if (current == endCeil) {
				Debug.Log("<color=green>Path found, moving</color>");
				pathFound = true;
				break;
			}

			openSet.Remove(current);
			closedSet.Add(current);

			// Only use as reference open walls
			var openCurrentSet = Array.FindAll(current.ceilWalls, c => !c.enabled);
			
			(int cX, int cY) = current.CeilPosition;
			(int eX, int eY) = endCeil.CeilPosition;
			
			// Check neighbours
			foreach (var wallInfo in openCurrentSet) {
				var offset = wallInfo.position.GetOffset();

				int nextX = cX + (int) offset.x;
				int nextY = cY + (int) offset.y;
				
				if(mazeGenerator.IsOutbounds(nextX, nextY))
					continue;	// Invalid position

				var neighbour = mazeGenerator.ceils[nextX, nextY];

				if (closedSet.Contains(neighbour)) {
					continue;
				}
				
				(int nX, int nY) = neighbour.CeilPosition;
				
				// Neighbour weight
				float tempG = current.g + Heuristic(new Vector2(nX,nY),new Vector2(cX,cY));

				bool newPath = false;
				
				if (openSet.Contains(neighbour)) {
					if (tempG < neighbour.g) {
						neighbour.g = tempG;
						newPath = true;
					}
				} else {
					neighbour.g = tempG;
					newPath = true;
					
					// Go to neighbour
					openSet.Add(neighbour);
				}

				if (newPath) {	// Did it find a new wait througt other ceil?
					neighbour.h = Heuristic(new Vector2(nX, nY), new Vector2(eX, eY));
					neighbour.f = neighbour.g + neighbour.h;
					neighbour.prevCeil = current;
				}
			}
		}

		
		uiLoadingPathContainer.SetActive(false);
		
		if (pathFound) {
			var path = new List<MazeCeil> {current};

			// Go backwards and build path
			while (current.prevCeil != null) {
				path.Add(current.prevCeil);
				
				current = current.prevCeil;

				yield return null;
			}

			path.Reverse();

			ln.positionCount = path.Count;

			// Move to position
			for (var i = 0; i < path.Count; i++) {
				var mazeCeil = path[i];
				yield return new WaitForSeconds(moveDelay);

				(x, y) = mazeCeil.CeilPosition;
				x += .5f;

				var newPos = new Vector2(x, y);
				
				// Update line path
				ln.SetPosition(i,newPos);

				// Update future lines as current
				for (int j = i; j < path.Count; j++) {
					ln.SetPosition(j,newPos);
				}

				indicator.position = newPos;
			}
		} else {
			Debug.LogError("No puede ser :(");
		}

		Debug.Log("Finished movement");
		movementRoutine = null;
	}

	private float Heuristic(Vector2 a, Vector2 b) {
		return Vector2.Distance(a, b);
	}

	/// <summary> Reset states on generate a new maze </summary>
	public void OnGenerateMaze() {
		ln.positionCount = 0;

		uiLoadingPathContainer.SetActive(false);
		
		if (movementRoutine != null) {
			StopCoroutine(movementRoutine);
			movementRoutine = null;
		}

		// Refresh pool
		RestoreCheckedPlacesPool();
	}

	/// <summary> Enqueue all generated places to reuse </summary>
	private void RestoreCheckedPlacesPool() {
		freeCheckedPlaces.Clear();
		
		foreach (var checkedPlace in checkedPlacesGenerated) {
			freeCheckedPlaces.Enqueue(checkedPlace);
			checkedPlace.gameObject.SetActive(false);
		}
	}

	/// <summary> Return given size and fix/clamp values </summary>
	public int MazeSize {
		get {
			string value = inputSize.text;
			
			if (String.IsNullOrEmpty(value) || !int.TryParse(value,out var size)) {
				size = 10;
				inputSize.text = size.ToString();
			}

			if (size < 4 || size >= 100) {
				size = Mathf.Clamp(size, 4, 100);
				inputSize.text = size.ToString();
			}

			return size;
		}	
	}

	/// <summary> Interactive preview of the path generation </summary>
	private void GenerateCheckedPlaceAtCeil(MazeCeil ceil) {

		Transform cachedCheckPlace = null;
		
		if (freeCheckedPlaces.Count > 0) {
			cachedCheckPlace = freeCheckedPlaces.Dequeue();
		} else {
			cachedCheckPlace = Instantiate(checkPlacePrefab,searchStatesContainer);
			checkedPlacesGenerated.Add(cachedCheckPlace);
		}
		
		// Restore state
		cachedCheckPlace.gameObject.SetActive(true);

		var pos = ceil.transform.position;
		pos.x += .5f;
		
		cachedCheckPlace.position = pos;
	}
}
