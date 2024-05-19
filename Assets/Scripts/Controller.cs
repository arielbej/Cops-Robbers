using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency = new List<int>();

            // Añadir adyacentes
            AddAdjacentTile(i, i - Constants.TilesPerRow); // Arriba
            AddAdjacentTile(i, i + Constants.TilesPerRow); // Abajo
            AddAdjacentTile(i, i - 1, i % Constants.TilesPerRow != 0); // Izquierda
            AddAdjacentTile(i, i + 1, (i + 1) % Constants.TilesPerRow != 0); // Derecha
        }
    }

    private void AddAdjacentTile(int from, int to, bool condition = true)
    {
        if (condition && to >= 0 && to < Constants.NumTiles)
        {
            tiles[from].adjacency.Add(to);
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        int currentTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[currentTile].current = true;
        FindSelectableTiles(false);

        // Lista de casillas seleccionables
        List<Tile> selectableTiles = new List<Tile>();
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                selectableTiles.Add(tile);
            }
        }

        // Elegir una casilla aleatoria entre las seleccionables
        if (selectableTiles.Count > 0)
        {
            int randIndex = Random.Range(0, selectableTiles.Count);
            Tile chosenTile = selectableTiles[randIndex];

            // Mover al caco a la casilla elegida
            robber.GetComponent<RobberMove>().MoveToTile(chosenTile);
            robber.GetComponent<RobberMove>().currentTile = chosenTile.numTile;
        }
    }


    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }
    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();
        nodes.Enqueue(tiles[indexcurrentTile]);
        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;

        while (nodes.Count > 0)
        {
            Tile t = nodes.Dequeue();

            if (t.distance < 2)
            {
                foreach (int i in t.adjacency)
                {
                    Tile adjTile = tiles[i];
                    if (!adjTile.visited && !IsTileOccupiedByCop(i))
                    {
                        adjTile.parent = t;
                        adjTile.visited = true;
                        adjTile.distance = t.distance + 1;
                        adjTile.selectable = true;
                        nodes.Enqueue(adjTile);
                    }
                }
            }
        }
    }

    private bool IsTileOccupiedByCop(int tileIndex)
    {
        foreach (GameObject cop in cops)
        {
            if (cop.GetComponent<CopMove>().currentTile == tileIndex)
                return true;
        }
        return false;
    }
}
