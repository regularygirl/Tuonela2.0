using UnityEngine;
using UnityEngine.SceneManagement;


public class Triangle{
    public Cell A { get; set; }
    public Cell B { get; set; }
    public Cell C { get; set; }
    public Vector2 Position { get; set; }

    public Triangle(Cell a, Cell b, Cell c, Vector2 vector){
        A = a;
        B = b;
        C = c;
        Position = vector;
    }

}