using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonMap : MonoBehaviour
{
    public delegate void MapGenerationComplete();
    public static event MapGenerationComplete onMapGenerationComplete;
    static DungeonMap singleton;
    public static DungeonMap Singleton
    {
        get
        {
            if (singleton == null)
            {
                singleton = FindObjectOfType<DungeonMap>();
            }
            return singleton;
        }
    }

    public Dictionary<Vector2, MapCell> DungeonCells { get => dungeonCells; set => dungeonCells = value; }

    [SerializeField] GameObject mapNode;
    [SerializeField] GameObject endNode;
    [SerializeField] GameObject startNode;
    [SerializeField] GameObject storeNode;
    [SerializeField] GameObject playerIcon;
    GameObject instantiatedPlayerIcon;
    [SerializeField] Transform mapMask;
    [SerializeField] Transform mapParent;
    [SerializeField] AudioClip mapOpenSound;
    AudioSource _audioSource;
    Vector3 mousePos = Vector3.zero;
    Vector3 oldMousePos = Vector3.zero;
    float mapZoomAmount = 1;
    float mapCellSize = 50;
    bool mapOpen = false;
    [SerializeField] float mapPanStrength = 5f;
    Dictionary<Vector2, MapCell> dungeonCells = new Dictionary<Vector2, MapCell>();


    private void setPlayerIconToCurrentLocation(Room setRoom) {
        try{
            instantiatedPlayerIcon.transform.SetParent(dungeonCells[new Vector2( setRoom.Cell.X, setRoom.Cell.Y)].transform, false);
            instantiatedPlayerIcon.SetActive(true);
        } catch(Exception e){

        }
        
    }

    private void OnDestroy()
    {
        CurrentRoomManager.onPlayerRoomSet -= setPlayerIconToCurrentLocation;
        ProceduralGeneration.onGenerationComplete -= createMap;
    }
    private void createMap(List<Cell> cellsInDungeon)
    {
        //The start node will be located in the middle of the transform for the map container, all other cells of the map are offset by this
        Vector2 offset = new Vector2(cellsInDungeon[0].X * mapCellSize + mapParent.transform.localPosition.x, cellsInDungeon[0].Y * mapCellSize + mapParent.transform.localPosition.y);
        for (int i = 0; i < cellsInDungeon.Count; i++)
        {

            GameObject toInstantiate = mapNode;
            if (cellsInDungeon[i].RoomType == RoomType.Start)
            {
                toInstantiate = startNode;
            }
            if (cellsInDungeon[i].RoomType == RoomType.BossBattle)
            {
                toInstantiate = endNode;
            }
            if (cellsInDungeon[i].RoomType == RoomType.Shop)
            {
                toInstantiate = storeNode;
            }
            GameObject go = Instantiate(toInstantiate, mapParent);
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(mapCellSize/2f, mapCellSize/2f);
            go.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            go.transform.localPosition = new Vector2(mapCellSize * cellsInDungeon[i].X, mapCellSize * cellsInDungeon[i].Y) - offset;
            MapCell mapCell = go.GetComponent<MapCell>();
            mapCell.Initialize(cellsInDungeon[i]);
            try{
                DungeonCells.Add(new Vector2(cellsInDungeon[i].X, cellsInDungeon[i].Y), mapCell);
            } catch(Exception e){

            }             
        }
        onMapGenerationComplete?.Invoke();
    }


    // Start is called before the first frame update
    void Start()
    {
        ProceduralGeneration.onGenerationComplete += createMap;
        CurrentRoomManager.onPlayerRoomSet += setPlayerIconToCurrentLocation;
        instantiatedPlayerIcon = Instantiate(playerIcon);
        _audioSource = GetComponent<AudioSource>();
    }
    public void InvokeFullMapShow()
    {
        foreach (Cell cell in ProceduralGeneration.Singleton.CellsTable.Values)
        {
            cell.InvokeCellDiscover();

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            mapOpen = !mapOpen;
            mapMask.transform.gameObject.SetActive(mapOpen);
            _audioSource.PlayOneShot(mapOpenSound);
        }
        if (mapOpen)
        {
            PanMap();
            ZoomMap();
        }

    }

    private void ZoomMap()
    {
        float zoomAmount = Input.GetAxis("Mouse ScrollWheel");
        mapZoomAmount += zoomAmount;
        mapZoomAmount = Mathf.Clamp(mapZoomAmount, 0.4f, 3);
        if (zoomAmount != 0)
        {
            mapParent.GetComponent<RectTransform>().localScale = new Vector3(mapZoomAmount, mapZoomAmount, 0);
        }
    }

    private void PanMap()
    {
        if (Input.GetMouseButton(1))
        {
            mousePos = Input.mousePosition ;
            mapParent.transform.position =mapParent.transform.position - (oldMousePos - mousePos) * mapPanStrength;

        }
    }

    private void FixedUpdate()
    {
        oldMousePos = Input.mousePosition;
    }
}
