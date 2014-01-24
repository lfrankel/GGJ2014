using UnityEngine;
using System.Collections;
using UnityEditor;

public class GenerateLevelNavMesh : Editor 
{
	[MenuItem("Helper Functionality/Generate Level Mesh")]
	public static void GenerateLevelMesh()
	{	
		ReplaceObjectsWithPrefabs ();
		
		SetupTileConnections ();
		
		FindAttachedSpawnersAndGoal();
	}
	
	static void SetupTileConnections ()
	{
		Tile[] tiles = GameObject.FindObjectsOfType(typeof(Tile)) as Tile[];
		Debug.Log ("Found " + tiles.Length + " tiles!");
		foreach(Tile t in tiles)
		{
			t.NorthTile = null;
			t.SouthTile = null;
			t.EastTile = null;
			t.WestTile = null;
			
			
			Vector3 tilePosition = t.transform.position;
			//Find tiles "north" of us..which is less on the z axis
			foreach(Tile otherTile in tiles)
			{
				if(WithinRange(tilePosition.x, otherTile.transform.position.x, 0.5f) &&
				   WithinRange(tilePosition.z + 4,otherTile.transform.position.z, 0.5f) &&
					(tilePosition.y + 1.5 >= otherTile.transform.position.y))
				{
					if(t.NorthTile == null || (t.NorthTile.transform.position.y < otherTile.transform.position.y))
					{
						t.NorthTile = otherTile;
					}
				}
				
			}
			//Find tiles "south" of us..which is less on the z axis
			foreach(Tile otherTile in tiles)
			{
				if(WithinRange(tilePosition.x, otherTile.transform.position.x, 0.5f) &&
				   WithinRange(tilePosition.z - 4,otherTile.transform.position.z, 0.5f) &&
					(tilePosition.y + 1.5 >= otherTile.transform.position.y))
				{
					if(t.SouthTile == null || (t.SouthTile.transform.position.y < otherTile.transform.position.y))
					{
						t.SouthTile = otherTile;
					}
				}
				
				
			}
			//Find tiles "west" of us..which is less on the x axis
			foreach(Tile otherTile in tiles)
			{
				if(WithinRange(tilePosition.x - 4, otherTile.transform.position.x, 0.5f) &&
				   WithinRange(tilePosition.z, otherTile.transform.position.z, 0.5f) &&
					(tilePosition.y + 1.5 >= otherTile.transform.position.y))
				{
					if(t.WestTile == null || (t.WestTile.transform.position.y < otherTile.transform.position.y))
					{
						t.WestTile = otherTile;
					}
				}
				
				
			}
			
			//Find tiles "east" of us..which is less on the x axis
			foreach(Tile otherTile in tiles)
			{
				if(WithinRange(tilePosition.x + 4, otherTile.transform.position.x, 0.5f) &&
				   WithinRange(tilePosition.z,otherTile.transform.position.z, 0.5f) &&
					(tilePosition.y + 1.5 >= otherTile.transform.position.y))
				{
					if(t.EastTile == null || (t.EastTile.transform.position.y < otherTile.transform.position.y))
					{
						t.EastTile = otherTile;
					}
				}
				
				
			}
	
		}
	}

	static void ReplaceObjectsWithPrefabs ()
	{
		GameObject[] allObjects = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		foreach(GameObject obj in allObjects)
		{
			Object prefab = null;
			if(obj.name.Contains("Tile-Blank"))
			{
				prefab = AssetDatabase.LoadAssetAtPath("Assets/LevelPrefabs/Tile.prefab", typeof(GameObject));
			}
			
			if(obj.name.Contains("Flag-Goal"))
			{
				prefab = AssetDatabase.LoadAssetAtPath("Assets/LevelPrefabs/Flag-Goal.prefab", typeof(GameObject));	
			}
			
			if(obj.name.Contains("Flag-Spawner"))
			{
				prefab = AssetDatabase.LoadAssetAtPath("Assets/LevelPrefabs/Flag-Spawner.prefab", typeof(GameObject));	
			}
			
			if(obj.name.Contains("Player-Spawner"))
			{
				prefab = AssetDatabase.LoadAssetAtPath("Assets/LevelPrefabs/Player-Spawner.prefab", typeof(GameObject));	
			}
			
			if(prefab != null)
			{
				GameObject clone = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
				clone.transform.position = obj.transform.position;
				clone.transform.rotation = obj.transform.rotation;
				
				
				if(obj.name.Contains("Tile-Blank"))
				{
					clone.name = "Tile";
				}
				
				if(obj.name.Contains("Flag-Goal"))
				{
					clone.name = "Flag-Goal";	
				}
				
				if(obj.name.Contains("Flag-Spawner"))
				{
					clone.name = "Flag-Spawner";
					int number = int.Parse(obj.name.Substring(13));
					clone.GetComponent<Spawner>().NumSpawnedFlags = number;
				}
				
				if(obj.name.Contains("Player-Spawner"))
				{
					clone.name = "Player-Spawner";	
				}
				
				DestroyImmediate(obj);
			}			
		}
	}
	
	static void FindAttachedSpawnersAndGoal()
	{
		Tile[] tiles = GameObject.FindObjectsOfType(typeof(Tile)) as Tile[];
		
		GameObject[] allObjects = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		
		foreach(GameObject obj in allObjects)
		{
			if(obj.name.Contains("Flag-Goal"))
			{
				Tile connectedTile = FindConnectedTile(tiles, obj);
				if(connectedTile)
				{
					connectedTile.bFlagGoalIsHere = true;
				}
			}
			
			if(obj.name.Contains("Flag-Spawner"))
			{
				Tile connectedTile = FindConnectedTile(tiles, obj);
				if(connectedTile)
				{
					connectedTile.connectedSpawner = obj.GetComponent<Spawner>();
					
					//Create the flags to go here
					for(int i = 0;i < connectedTile.connectedSpawner.NumSpawnedFlags;++i)
					{
						Object prefab = AssetDatabase.LoadAssetAtPath("Assets/LevelPrefabs/Flag.prefab", typeof(GameObject));	
						GameObject clone = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
						clone.transform.position = obj.transform.position;
						clone.transform.rotation = obj.transform.rotation * Quaternion.Euler (0, 0, Random.Range (-180, 180));
						
						
						connectedTile.connectedSpawner.FlagInstances.Add(clone);
					}
				}
			}
		}
	}
	
	static bool WithinRange(float valOne, float valTwo, float range)
	{
		return Mathf.Abs (valOne - valTwo) < range;
	}
	static Tile FindConnectedTile(Tile[] tiles, GameObject obj)
	{
		foreach(Tile t in tiles)
		{
			if(WithinRange(t.transform.position.x, obj.transform.position.x, 0.25f) &&
				WithinRange(t.transform.position.y, obj.transform.position.y, 0.25f) &&
				WithinRange(t.transform.position.z, obj.transform.position.z, 0.25f) )
			{
				return t;
			}
		}
		
		return null;
	}
}
