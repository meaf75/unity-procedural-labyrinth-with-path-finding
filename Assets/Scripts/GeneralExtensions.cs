using System;
using UnityEngine;
using Random = UnityEngine.Random;

public static class GeneralExtensions {
	public static Vector2 GetOffset(this WallPosition position) {
		switch (position) {
			case WallPosition.NONE:
				Debug.LogError("Getting offset for none");
				break;
			case WallPosition.TOP:
				return new Vector2(0,1);
				break;
			case WallPosition.BOTTOM:
				return new Vector2(0,-1);
				break;
			case WallPosition.RIGHT:
				return new Vector2(1,0);
				break;
			case WallPosition.LEFT:
				return new Vector2(-1,0);
		}

		return new Vector2(-100,-100);
	}
	
	public static WallPosition GetOpositePosition(this WallPosition position) {
		switch (position) {
			case WallPosition.NONE:
				Debug.LogError("Getting opposite for none");
				break;
			case WallPosition.TOP:
				return WallPosition.BOTTOM;
				break;
			case WallPosition.BOTTOM:
				return WallPosition.TOP;
				break;
			case WallPosition.RIGHT:
				return WallPosition.LEFT;
				break;
			case WallPosition.LEFT:
				return WallPosition.RIGHT;
		}

		return WallPosition.NONE;;
	}
	
	public static T[] Randomize<T>(this T[] items)
	{
		// For each spot in the array, pick
		// a random item to swap into that spot.
		for (int i = 0; i < items.Length - 1; i++)
		{
			int j = Random.Range(i, items.Length);
			(items[i], items[j]) = (items[j], items[i]);
		}
		
		return items;
	}
}
