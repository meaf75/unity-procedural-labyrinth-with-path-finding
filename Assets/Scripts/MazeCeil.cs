using System;
using UnityEngine;

public enum WallPosition { NONE = 0, TOP = 1,BOTTOM = 2,RIGHT = 3,LEFT = 4 }

[Serializable]
public class WallInfo {
	public GameObject wall;
	public WallPosition position;
	public bool enabled;
}

public class MazeCeil : MonoBehaviour {

	public WallInfo[] ceilWalls;
	public bool hasBeenVisited;

	#region A*
	/// <summary> </summary>
	[NonSerialized] public float f;
	/// <summary> Weight between current & next </summary>
	[NonSerialized] public float g;
	/// <summary> Distance between current & target </summary>
	[NonSerialized] public float h;
	[NonSerialized] public MazeCeil prevCeil;
	#endregion

	public void DestroyWallAtPosition(WallPosition pos,bool markAsDestroyed = true) {
		var wallInfo = Array.Find(ceilWalls, c => c.position == pos);

		if (wallInfo != null) {
			wallInfo.enabled = !markAsDestroyed;
			
			if(Application.isPlaying)
				Destroy(wallInfo.wall);
			else
				DestroyImmediate(wallInfo.wall);
		}
	}

	public (int, int) CeilPosition {
		get {
			var pos = transform.position;
			return ((int) pos.x, (int) pos.y);
		}
	}
}
