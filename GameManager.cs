using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations; //
using UnityEngine;
using UnityEditor;
using Random = System.Random;
using System.Runtime.CompilerServices; //
using System.Numerics;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2; //
using Vector3 = UnityEngine.Vector3;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{
    public static Player[] Players = new Player[4];
    public static GameObject[] TileSet = new GameObject[144]; // convert to private
    public static List<GameObject> TileWalls = new List<GameObject>();
    public static List<GameObject> TileToss = new List<GameObject>();

    public static Vector3 tileSize, tileOffset;
    public enum FacePreset
    {
        Static, Stand = 180, Player = 220, Opened = 270, Closed = 90
    }

    public static Player currentPlayer;
    private Player mano;
    private int diceRoll;

    void Awake()
    {   
        /* Create TileSet */
        string[] names = 
        {
            "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9",
            "s1", "s2", "s3", "s4", "s5", "s6", "s7", "s8", "s9",
            "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9",
            "tN", "tS", "tW", "tE", "tG", "tR", "tW", "fR", "fB"
        };

        GameObject temp;
        for (int i = 0; i < 144; i++)
        {
            string tileName = names[i / 4];
            int tileID = (i % 4) + 1;
            if (tileName.Contains('f'))
            {
                temp = Instantiate(Resources.Load($"{tileName}-{tileID}")) as GameObject;
            }
            else 
            {
                temp = Instantiate(Resources.Load(tileName)) as GameObject;
            }

            temp.name = tileName;
            temp.transform.localScale = new Vector3(1600, 1600, 2000);
            temp.transform.rotation = Quaternion.Euler(0, 0, 0);

            temp.AddComponent<TileManager>();
            temp.AddComponent<DragTile>();

            temp.GetComponent<TileManager>().enabled = false;
            temp.GetComponent<DragTile>().enabled = false;
            temp.SetActive(false);

            TileSet[i] = temp;
        }

        /* Set TileSize */
        Renderer renderer = TileSet[0].GetComponent<MeshRenderer>();
        tileSize = renderer.bounds.size;
        tileOffset = tileSize * 0.5f;

        //. size : (tileWidth, tileLength, tileDepth)
        //. orientation : upright

        /* Create Players */
        GameObject table = GameObject.Find("Table Perimeter");

        Players[0] = table.AddComponent<Human>();
        Players[1] = table.AddComponent<Bot>();
        Players[2] = table.AddComponent<Bot>();
        Players[3] = table.AddComponent<Bot>();

        Players[0].playerID = 0;
        Players[1].playerID = 1;
        Players[2].playerID = 2;
        Players[3].playerID = 3;

        mano = Players[0];
    }
    
    void Start()
    {
        NewRound();
    }

    void NewRound()
    {        
        /* Shuffle TileSet using Fisher-Yates */
        Random random = new Random();
        for (int i = TileSet.Length - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            GameObject temp = TileSet[i];
            TileSet[i] = TileSet[j];
            TileSet[j] = temp;
        }

        // declare diceroll here

        /* Set Tile Positions */
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        IEnumerator<Vector3> row;
        for (int PlayerID = 0; PlayerID < 4; PlayerID++)
        {
            FacePreset face = FacePreset.Closed;
            Quaternion quadrant = Quaternion.Euler(0, -90 * PlayerID, 0);

            /* Assign Tile Walls */
            row = DistributeRow(tileCount: 20, perimSize: 2.24f, face);
            for (int i = 0; i < 20; i++)
            {
                row.MoveNext();
                Vector3 position = quadrant * (row.Current + new Vector3(tileOffset.y, 0, 0));
                Quaternion rotation = quadrant * Quaternion.Euler((int) face, 0, random.Next(2) * 180);

                TileSet[i + PlayerID * 20].transform.position = position + new Vector3(0, 0.5f, 0);
                TileSet[i + PlayerID * 20].transform.rotation = rotation;
                
            }

            /* Assign Tile Blocks Layer 1 */
            row = DistributeRow(tileCount: 8, perimSize: 1.25f, face);
            for (int i = 80; i < 88; i++)
            {
                row.MoveNext();
                Vector3 position = quadrant * row.Current;
                Quaternion rotation = quadrant * Quaternion.Euler((int) face, 0, random.Next(2) * 180);

                TileSet[i + PlayerID * 16].transform.position = position + new Vector3(0, 0.5f, 0);
                TileSet[i + PlayerID * 16].transform.rotation = rotation;
            }

            /* Assign Tile Blocks Layer 2 */
            row = DistributeRow(tileCount: 8, perimSize: 1.25f, face);
            for (int i = 88; i < 96; i++)
            {
                row.MoveNext();
                Vector3 position = quadrant * (row.Current + new Vector3(0, tileSize.z, 0));
                Quaternion rotation = quadrant * Quaternion.Euler((int) face, 0, random.Next(2) * 180);

                TileSet[i + PlayerID * 16].transform.position = position + new Vector3(0, 0.5f, 0);
                TileSet[i + PlayerID * 16].transform.rotation = rotation;
            }
        }

        stopwatch.Stop();
        TimeSpan elapsedTime = stopwatch.Elapsed;
        Debug.Log("Elapsed Time: " + elapsedTime);

        StartCoroutine(SetupTiles());
    }
    public static IEnumerator<Vector3> DistributeRow(int tileCount, float perimSize, bool isUpright)
    {
        float centerOffset = tileOffset.x - (tileCount * tileSize.x) / 2;
        float perimOffset = -tileOffset.y - perimSize;
        float liftOffset = isUpright ? tileOffset.y : tileOffset.z;

        Vector3 position = new Vector3(centerOffset, liftOffset, perimOffset);
        Vector3 spacing = new Vector3(tileSize.x, 0, 0);

        for (int i = 0; i < tileCount; i++)
        {
            yield return position + (spacing * i);
        }
    }
    public static IEnumerator<Vector3> xxDistributeRow(int tileCount, float perimSize, FacePreset face)
    {
        float centerOffset = tileOffset.x - (tileCount * tileSize.x) / 2;
        float perimOffset = -tileOffset.y - perimSize;

        float liftOffset;
        switch (face)
        {
            case FacePreset.Player: liftOffset = 0.11f; break;
            case FacePreset.Stand:  liftOffset = tileOffset.y; break;
            case FacePreset.Opened: liftOffset = tileOffset.z; break;
            case FacePreset.Closed: liftOffset = tileOffset.z; break;
            default: liftOffset = tileOffset.z; break;
        }

        Vector3 position = new Vector3(centerOffset, liftOffset, perimOffset);
        Vector3 spacing = new Vector3(tileSize.x, 0, 0);

        for (int i = 0; i < tileCount; i++)
        {
            yield return position + (spacing * i);
        }
    }

    IEnumerator SetupTiles()
    { 
        currentPlayer = mano;
        diceRoll = 12;
        CompressWalls(diceRoll, mano);

        foreach (GameObject tile in TileSet)
        {
            // yield return new WaitForSeconds(0.02f);
            tile.SetActive(true);

            Vector3 position = tile.transform.position - new Vector3(0, 0.5f, 0);
            Quaternion rotation = tile.transform.rotation;

            tile.GetComponent<TileManager>().SetDestination(position, rotation, 0.2f);
        }

        yield return new WaitForSeconds(0.02f);

        diceRoll = 12;
        for (int i = 0; i < 4; i++)
        {
            int playerID = (i + diceRoll - 1) % 4;
            for (int j = 0; j < 4; j++)
            {
                yield return new WaitForSeconds(0.3f);
                Players[playerID].GrabTile(new List<GameObject>() {
                    TileSet[80 + (i * 2) + (j * 16)],
                    TileSet[81 + (i * 2) + (j * 16)],
                    TileSet[88 + (i * 2) + (j * 16)],
                    TileSet[89 + (i * 2) + (j * 16)],
                });
            }

        }

        /* Wait for setup to finish */
        // use async OR unityEvent
        foreach (GameObject tile in TileSet)
        {
            while (tile.GetComponent<TileManager>().enabled)
            {
                yield return null;
            }
        }


        currentPlayer.GrabTile(TileWalls[0]);
        StackFlower();
    }

    public static void NextTurn()
    {
        currentPlayer = Players[(Array.IndexOf(Players, currentPlayer) + 1) % 4];
        currentPlayer.GrabTile(TileWalls[0]);
    }

    void CompressWalls(int diceRoll, Player mano)
    {
        int wallIndex = (Array.IndexOf(Players, mano) + (diceRoll % 4)) * 20;
        int tileIndex = (20 - diceRoll);

        int index = wallIndex + tileIndex + 1;

        TileWalls.AddRange(TileSet[0..index].Reverse());
        TileWalls.AddRange(TileSet[index..(80)].Reverse());
    }

    public static void StackFlower(int flowerCount = 0)
    {
        GameObject nextTile = TileWalls[TileWalls.Count - 2 - flowerCount];
        GameObject thisTile = TileWalls[TileWalls.Count - 1 - flowerCount];

        Vector3 position = nextTile.transform.position + new Vector3(0, tileSize.z, 0);
        Quaternion rotation = thisTile.transform.rotation;
        thisTile.GetComponent<TileManager>().SetDestination(position, rotation, 0.1f);
    }

    void Update()
    {
        // put this back
        // it's better if player will just press next arrow before next player grab tile
        // so that there's control on pong, eat

        /*if (Input.GetKeyDown(KeyCode.RightArrow) && currentTurnRunning != true)
        {
            currentPlayer = Players[(Array.IndexOf(Players, currentPlayer) + 1) % 4];
            // StartCoroutine(NextTurn());
            NextTurn();
        }*/
    }

}


// use distribuerow to set the baseposition instead

// create public class Tile that extends and inherits from GameObject
// compress list_tilewalls[] to list_tilewalls

/*
enum FacePresets
{
    Stand,
    Player,
    Opened,
    Closed,
    Static
}
*/

/*
for two-sided preset
hand must be on the left half and
open must be on the right half
because stepVector is positive, so that it's easier to left-align
*/

// instead of quadrant, use look at

// replace TileManager with public class Tile
