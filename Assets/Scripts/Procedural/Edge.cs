using System;

public class Edge{
    public Cell A;
    public Cell B;
    public String key;

    public Edge(Cell a, Cell b){
        A = a;
        B = b;
        key = getKey();
    }

    private String getKey(){
        Cell c1;
        Cell c2;
        if(A.X == B.X){
            if(A.Y > B.Y){
                c1 = A;
                c2 = B;
            } else {
                c1 = B;
                c2 = A;
            }
        } else if(A.X > B.X){
            c1 = A;
            c2 = B;
        } else{
            c1 = B;
            c2 = A;
        }
        return "("+c1.X+", "+c1.Y+")->("+c2.X+", "+c2.Y+")";
    }

    public String getSegment(){
        Cell c1;
        Cell c2;
        if(A.X == B.X){
            if(A.Y > B.Y){
                c1 = A;
                c2 = B;
            } else {
                c1 = B;
                c2 = A;
            }
        } else if(A.X > B.X){
            c1 = A;
            c2 = B;
        } else{
            c1 = B;
            c2 = A;
        }
        return "Segment(("+c1.X+", "+c1.Y+"),("+c2.X+", "+c2.Y+"))";
    }
}
