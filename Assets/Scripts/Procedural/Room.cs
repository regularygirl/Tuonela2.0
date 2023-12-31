
using System;
using System.Collections.Generic;
using UnityEngine;
//Used by all prefabs marked as rooms, rooms should have (currently) a size
//of either 1 or 2. 1 size room will be depicted as a 1x1 room on the map
//2 room will be depicted as 2x2 size on the map

public class Room : MonoBehaviour
{
    //The room is complete with the start method, this is ran afterwards
    public delegate void RoomReadyForUse();
    public event RoomReadyForUse onRoomReadyForUse;
    public delegate void OnLockState(bool state);
    public event OnLockState onLockStateChange;
    public delegate void OnRoomClear(Room room);
    public static event OnRoomClear onRoomClear;

    public Cell Cell { get => _cell; set => _cell = value; }
    public Transform PickupsDropPointOnRoomClear { get => pickupsDropPointOnRoomClear; set => pickupsDropPointOnRoomClear = value; }
    internal RoomType RoomType { get => roomType; set => roomType = value; }
    public bool RoomLocked { get => roomLocked; set => roomLocked = value; }
    //This information is not needed for designers, thus its hidden.
    [HideInInspector]
    public GameObject RoomPrefab { get => roomPrefab; set => roomPrefab = value; }

    Dictionary<NeighborType, Room> roomNeighbors = new Dictionary<NeighborType, Room>();
    Dictionary<NeighborType, GameObject> roomDoors = new Dictionary<NeighborType, GameObject>();
    [SerializeField] List<DoorLocation> doorLocations = new List<DoorLocation>();
    [SerializeField] List<RoomNeighbor> roomNeighborsList = new List<RoomNeighbor>();

    //The point where the game will drop an item on clear
    [SerializeField] Transform pickupsDropPointOnRoomClear;
    List<BaseMook> roomMooks = new List<BaseMook>();
    int roomMookCount = 0;
    //If a room is empty for example, the room might try to drop loot, this circumvents the problem
    bool roomHasHadMooksAdded = false;
    bool roomLocked = false;
    Cell _cell;
    //This information is used to store the original prefab reference, to make sure e.g that the same rooms dont spawn next to eachother
    GameObject roomPrefab;
    //Is the room a store, normal room, boss room, etc.
    [SerializeField] RoomType roomType;
    public void AddRoomDoor(NeighborType neighborType, GameObject door)
    {
        if (roomDoors.ContainsKey(neighborType))
        {
            Debug.LogError("A room is initializing its doors, but some of them " +
                "have the same doortype, check all doors in the prefab for instantiated room '" + gameObject.name + "'");
        }
        roomDoors.Add(neighborType, door);
        CheckRoomLockState();
    }
    public void AddMookToRoom(BaseMook mook)
    {
        //Add a mook to the room, change the tracking counter, and set the doors to be locked.
        roomMooks.Add(mook);
        mook.onMookDeath += decrementMooksFromRoom;
        roomMookCount++;
        SetDoorsLockState(true);
        roomHasHadMooksAdded = true;
    }
    private void Start(){
        onRoomReadyForUse?.Invoke();
    }
    
    private void Awake(){
        if (CurrentRoomManager.Singleton != null){
            if (this != CurrentRoomManager.Singleton.currentRoom) gameObject.SetActive(false);
        }
        SaveListsToDictionaries();
    }

    private void SaveListsToDictionaries(){
        //Save the list as a dictionary to make lookups a bit better
        for (int i = 0; i < roomNeighborsList.Count; i++)
        {
            roomNeighbors.Add(roomNeighborsList[i].NeighborType, roomNeighborsList[i].NeighborRoom);
        }
        for (int i = 0; i < doorLocations.Count; i++)
        {
            roomDoors.Add(doorLocations[i].NeighborType, doorLocations[i].Location);
        }
    }

    private void SetDoorsLockState(bool state){
        //All lockable items subscribe to this. Will lock or unlock all items in room after clearing or entering room
        onLockStateChange?.Invoke(state);
        RoomLocked = state;
    }

    private void OnDestroy(){
        for (int i = 0; i < roomMooks.Count; i++){
            roomMooks[i].onMookDeath -= decrementMooksFromRoom;
        }
    }

    private void decrementMooksFromRoom(){
        roomMookCount--;
        CheckRoomLockState();
    }

    private void CheckRoomLockState(){
        if (roomMookCount <= 0) {
            SetDoorsLockState(false);
            if (roomHasHadMooksAdded) {
                onRoomClear?.Invoke(this);
            }
        } else {
            SetDoorsLockState(true);
        }
    }

    public Room getNeighbor(NeighborType neighborType){
        Cell neighborCell;
        _cell.NeighborCells.TryGetValue(neighborType, out neighborCell);
        if (neighborCell == null) return null;
        return ProceduralGeneration.Singleton.GetRoomByCell(_cell.NeighborCells[neighborType]);
    }

    public void WarpPlayerToDoorLocation(NeighborType neighborType){
        PlayerController.Singleton.transform.position = roomDoors[neighborType].transform.position + roomDoors[neighborType].transform.TransformDirection(Vector3.forward + Vector3.up);
        //The change in position wont be updated correctly if changes in transforms are not flushed correctly
        Physics.SyncTransforms();
    }
}

[System.Serializable]
class RoomNeighbor {
    [SerializeField] NeighborType neighborType;
    [SerializeField] Room neighborRoom;

    public Room NeighborRoom { get => neighborRoom; set => neighborRoom = value; }
    public NeighborType NeighborType { get => neighborType; set => neighborType = value; }
}

[System.Serializable]
class DoorLocation {
    [SerializeField] NeighborType neighborType;
    [SerializeField] GameObject location;

    public NeighborType NeighborType { get => neighborType; set => neighborType = value; }
    public GameObject Location { get => location; set => location = value; }
}

public enum RoomType {
    NormalBattle,
    BossBattle,
    Start,
    Shop,
    Killer,
    Explorer,
    Thief
}