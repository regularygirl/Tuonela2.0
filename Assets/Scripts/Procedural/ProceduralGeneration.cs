using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

class ProceduralGeneration : MonoBehaviour
{
    #region fields and properties
    static ProceduralGeneration singleton;
    public static ProceduralGeneration Singleton{
        get {
            if (singleton == null) {
                singleton = FindObjectOfType<ProceduralGeneration>();
            }
            return singleton;
        }
    }
    public int MapWidth { get => maximumMapWidth; set => maximumMapWidth = value; }
    public int MapHeight { get => maximumMapHeight; set => maximumMapHeight = value; }
    public Dictionary<Vector2, Cell> CellsTable { get => cellsTable; set => cellsTable = value; }
    public Dictionary<Cell, Room> AllCellsWithRooms { get => _allCellsWithRooms; set => _allCellsWithRooms = value; }

    RoomType playerType = RoomType.Killer;//killer by default

    //All initialized cells in an ordered list
    List<Vector2> allCellLocations;
    //Initialized cells but with hashing for ease and optimization of access
    Dictionary<Vector2, Cell> cellsTable;
    //Pathfinding collections
    //A minimum heap version of currently considered pathfinding cells
    MinHeap<NodeRecord> openPathFindingCells;
    //All records of rooms in the pathfinding algorithm so far.
    Dictionary<Vector2, NodeRecord> allNodeRecords;
    //The current cell in consideration of the algorithm for pathfinding
    NodeRecord currentRecord;
    //A pathfinded route from the beginning room to "boss room". Generated through the dijikstra algorithm.
    List<Cell> cells = new List<Cell>();
    List<Cell> pathFromStartToGoal = new List<Cell>();
    List<Cell> allCellsUsedByGeneratedDungeon = new List<Cell>();
    Dictionary<Cell, Room> _allCellsWithRooms = new Dictionary<Cell, Room>();
    [Header("Procedurally generated map parameters")]
    [SerializeField] int maximumMapWidth = 100;
    [SerializeField] int maximumMapHeight = 100;    
    [SerializeField] float chanceForPathToBranch = 60f;
    [Tooltip("Dictates how long a path that isnt on the start->goal axis can maximally be.")]
    [SerializeField] int maxBranchingPathLength = 10;
    [Tooltip("Distance is measured in unity units.")]
    [SerializeField] float requiredDistanceBetweenStartRoomAndGoalRoom = 5;
    [SerializeField] float maxDistanceBetweenStartRoomAndGoalRoom = 8;
    Vector2 goalSpace;
    Vector2 startSpace;
    #endregion
    public delegate void GenerationComplete(List<Cell> cellsInDungeon);
    public static event GenerationComplete onGenerationComplete;
    private void Start(){
        if (Globals.GenerationSettings != null){
            requiredDistanceBetweenStartRoomAndGoalRoom = Globals.GenerationSettings.MinDistanceToBoss;
            maxDistanceBetweenStartRoomAndGoalRoom = Globals.GenerationSettings.MaxDistanceToBoss;
            maximumMapHeight = Globals.GenerationSettings.MaxMapHeight;
            maximumMapWidth = Globals.GenerationSettings.MaxMapWidth;
            maxBranchingPathLength = Globals.GenerationSettings.MaxBranchingPathLength;
            chanceForPathToBranch = Globals.GenerationSettings.ChanceForBranchingPathsToDiverge;
        }
        ProcedurallyGenerateAMap();
    }

    private void Update(){
        if (Input.GetKeyDown(KeyCode.Return)){
            SceneManager.LoadScene(2);
        }
    }

    private void ProcedurallyGenerateAMap(){
        //ALGORITHM DESCRIPTION
        /* Generates all the rooms of the game with some algorithms by:
         * 1. Save a location for the start, and the end. There is a set minimum distance between these two.
         * 2. Create a dictionary for all possible room locations
         * 3. Shuffle the afermentioned list. Loop the list until the end.
         * 4. Still on the loop, give each room a weight. This weight will be used in the pathfinding algorithm for the shortest path between start and goal. (gives some randomness to the shape of map).
         * 5. Fire up a version of dijikstras algorithm. Calculate path shortest to goal from start.
         * 6. Once this is done, loop through the path, making the rooms. 
         * 7. Through random chance, make some branching rooms.
         * The level is now done!
         * More info can be found from this stackexchange post, which inspired some general ideas of this particular algorithm.
         * https://gamedev.stackexchange.com/questions/148418/procedurally-generating-dungeons-using-predefined-rooms
         */

        //Initialize needed lists.
        openPathFindingCells = new MinHeap<NodeRecord>(maximumMapWidth * maximumMapHeight);
        CellsTable = new Dictionary<Vector2, Cell>();
        allNodeRecords = new Dictionary<Vector2, NodeRecord>();
        allCellLocations = new List<Vector2>();

        //Create all cells for the map.
        CreateInitialGraph();
        //Lets shuffle the list of available spaces
        //FisherYatesShuffle(ref allCellLocations);
        //Create the first two nodes to be goal and start from the shuffled list, if the distance allows it
        GeneratePathToGoal();

        
        preselectStartAndGoalLocationsFromGraph();
        //Generate a path using dijikstras algorithm to reach goal from the start cell
        //once a path is done, the rooms can be generated on in the gameworld using the path generated by pathfinding
        MakeRooms();
    }
    #region Pre-pathfinding algorithm setup (Initial graphing)
    private void CreateInitialGraph(){
        Debug.Log("Number of Rooms: " + Globals.GenerationSettings.NumberOfRooms);
        if(Globals.playerType == "killer"){
            Debug.Log("Killer Mode");
            playerType = RoomType.Killer;
        } else if(Globals.playerType == "explorer"){
            Debug.Log("Explorer Mode");
            playerType = RoomType.Explorer;
        } else if(Globals.playerType == "thief"){
            Debug.Log("Thief Mode");
            playerType = RoomType.Thief;
        } 
        
        int numberOfRooms = Globals.GenerationSettings.NumberOfRooms;
        while(numberOfRooms>0){
            int x = Random.Range(1, 10);  
            int y = Random.Range(1, 10);
            Vector2 vector = new Vector2(x, y);       
            Cell cell = new Cell(x, y);
            if(!cellsTable.ContainsKey(vector)){                
                cellsTable.Add(vector, cell);
                allCellLocations.Add(vector);
                numberOfRooms--;
            }
        }
        
        DelaunayTriangulation dt = new DelaunayTriangulation();
        var triangulation = dt.bowyerWatson(cellsTable);
        dt.setCellNeigbors(triangulation);
        dt.log(triangulation);                
        
    }

    private Cell findFurthestCell(Cell root){    
        //List<Cell> cells = new List<Cell>();
        cells.Add(root);
        //expand
        int max = 0;
        Cell maxCell = null;
        while(cells.Count > 0){            
            Cell cell = cells[0];
            if(cell.cost >= max){
                max = cell.cost;
                maxCell = cell;
            }
            cell.isVisited = true;
            foreach(Cell neighbor in cell.neighborCells.Values){
                if(!neighbor.isVisited){
                    cells.Add(neighbor);
                    neighbor.cost+= cell.cost + 1;                                        
                }                 
            }
            cells.RemoveAt(0);               
        }
        return  maxCell;
    }


    private void preselectStartAndGoalLocationsFromGraph()
    {
        //The first space from the shuffled list shall be reserved for the start
        //RoomType playerType = RoomType.Killer; // Seleccion del jugador
        RoomType[] roomTypes = new RoomType[]{RoomType.Killer, RoomType.Explorer, RoomType.Thief};
        RoomType[] OrderedRoomTypes = new RoomType[3];
        OrderedRoomTypes[0] = playerType;
        int index = 1;
        foreach(RoomType vroomType in roomTypes){
            if(vroomType != playerType){
                OrderedRoomTypes[index] = vroomType;
                index++;
            }
        }
        RoomType roomType = RoomType.Killer;
        foreach(Vector2 cellAllocation in allCellLocations){
            //Debug.Log(cellAllocation.x + " " + cellAllocation.y); 
            
            int random = Random.Range(1, 101);
            if(random >= 1 && random <= 50){ //50% de probabilidad
                roomType = OrderedRoomTypes[0];
            } else if(random > 50 && random <= 75){ //25% 
                roomType = OrderedRoomTypes[1];
            } else if(random > 75 && random <= 100){ //25% 
                roomType = OrderedRoomTypes[2];
            }
            cellsTable[cellAllocation].RoomType = roomType;
        }
        startSpace = allCellLocations[0];             
        cellsTable[startSpace].CellType = CellType.Start;
        cellsTable[startSpace].RoomType = RoomType.Start;
        
        Cell endCell = findFurthestCell(cellsTable[startSpace]);
        
        goalSpace = new Vector2(endCell.X, endCell.Y);        
        cellsTable[goalSpace].CellType = CellType.End;
        cellsTable[goalSpace].RoomType = RoomType.BossBattle;

        foreach(Cell cell in cellsTable.Values){
            if(cell.RoomType != RoomType.Start && cell.RoomType != RoomType.BossBattle){
                cell.RoomType = RoomType.Shop;
                break;
            }
        }

    }
    #endregion

    #region Pathfinding algorithm (Dijikstras' algorithm of shortest route)
    private void GeneratePathToGoal(){
        foreach(Vector2 cellAllocation in allCellLocations){
            pathFromStartToGoal.Add(cellsTable[cellAllocation]);
        }        
    }

    #endregion
    #region post-pathfinding algorithm room initialization, (Generating actual rooms and room branches)
    public void MakeRooms(){
        //Do one iteration over the path to set all path members to be used, to prevent branching
        //paths thinking that the next location on path is usable as a branch
        for (int i = 0; i < pathFromStartToGoal.Count; i++){
            pathFromStartToGoal[i].CurrentlyUsedOnMap = true;            
            allCellsUsedByGeneratedDungeon.Add(pathFromStartToGoal[i]);
            GenerateBranchingPaths(pathFromStartToGoal[i]);
        }
        
        
        for (int i = 0; i < allCellsUsedByGeneratedDungeon.Count; i++){
            GetComponent<RoomGen>().createRoomForCell(allCellsUsedByGeneratedDungeon[i]);
        }
        CurrentRoomManager.Singleton.currentRoom = AllCellsWithRooms[allCellsUsedByGeneratedDungeon[0]];
        AllCellsWithRooms[allCellsUsedByGeneratedDungeon[0]].gameObject.SetActive(true);
        onGenerationComplete?.Invoke(allCellsUsedByGeneratedDungeon);
    }

    private void GenerateBranchingPaths(Cell cell, int currentBranchingPathCount = 0){
        if (cell.NeighborCells.Count == 0) cell.GetConnections();
            foreach (Cell neighbor in cell.NeighborCells.Values){
                if (neighbor != null){
                    if (neighbor.CurrentlyUsedOnMap) continue;
                    neighbor.CurrentlyUsedOnMap = true;
                    allCellsUsedByGeneratedDungeon.Add(neighbor);
                }
            }
        //Debug.Log(allCellsUsedByGeneratedDungeon.Count); 
    }
    #endregion

    #region methods for supporting algorithm and generation
    bool areSameCells(Cell a, Cell b) {
        if (a.X == b.X) {
            if (a.Y == b.Y) {
                return true;
            }
        }
        return false;
    }

    public Room GetRoomByCell(Cell cell){
        Room room;
        AllCellsWithRooms.TryGetValue(cell, out room);
        return room;
    }

    public void AddCellToRoomInformation(Cell cell, Room room){
        if(!AllCellsWithRooms.ContainsKey(cell)){
            AllCellsWithRooms[cell] = room;   
        }            
    }
    #endregion
}