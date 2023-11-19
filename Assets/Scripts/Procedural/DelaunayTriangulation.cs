using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DelaunayTriangulation {		

    
	private Triangle getSupertriangle(Dictionary<Vector2, Cell> inputCells){		
		int maxX = int.MinValue;
		int minX = int.MaxValue;
		int maxY = int.MinValue;
		int minY = int.MaxValue;
        
		foreach (KeyValuePair<Vector2, Cell> cell in inputCells){            
			if(cell.Value.X >= maxX){
				maxX = cell.Value.X;
			}
			if(cell.Value.X <= minX){
				minX = cell.Value.X;
			}
			if(cell.Value.Y >= maxY){
				maxY = cell.Value.Y;
			}
			if(cell.Value.Y <= minY){
				minY = cell.Value.Y;
			}			
		}	
        maxX ++;
        maxY ++;
        minX --;
        minY --;
		// double midX = (|maxX - minX| / 2) + minX 
		int midX = Convert.ToInt32(Math.Round((Math.Abs(maxX - minX)/2.0) + minX, 0.0));
		int midY = Convert.ToInt32(Math.Round((Math.Abs(maxY - minY)/2.0) + minY, 0.0));
		
		double r = Math.Sqrt(Math.Pow(maxX-midX, 2)+Math.Pow(maxY-midY, 2));
		int co = Convert.ToInt32(Math.Ceiling(r*Math.Sqrt(3)));
		int h = Convert.ToInt32(Math.Ceiling(2*r));
			
		Cell p1 = new Cell(midX, midY + h);
		Cell p2 = new Cell(midX + co, midY - Convert.ToInt32(Math.Round(r)));
		Cell p3 = new Cell(midX - co, midY - Convert.ToInt32(Math.Round(r)));
		
		Triangle supertriangle = new Triangle(p1, p2, p3, new Vector2(999,999));
		
		return supertriangle;
		
	}

    public bool IsCellInsideCircumcircle(Cell cell, Triangle triangle){      
        Cell A = triangle.A;
        Cell B = triangle.B;
        Cell C = triangle.C;        
        int d1 = (A.X*B.Y*1)+(A.Y*1*C.X)+(1*B.X*C.Y)-(C.X*B.Y*1)-(C.Y*1*A.X)-(1*B.X*A.Y);
        if(d1 < 0){ // if clockwise then we make it anti-clockwise            
            B = triangle.C;
            C = triangle.B;
        }
        double[][] m = new double[3][];
        m[0] = new double[3]{(A.X-cell.X), (A.Y-cell.Y), Math.Pow(A.X - cell.X,2)+Math.Pow(A.Y - cell.Y,2)};
        m[1] = new double[3]{(B.X-cell.X), (B.Y-cell.Y), Math.Pow(B.X - cell.X,2)+Math.Pow(B.Y - cell.Y,2)};
        m[2] = new double[3]{(C.X-cell.X), (C.Y-cell.Y), Math.Pow(C.X - cell.X,2)+Math.Pow(C.Y - cell.Y,2)};

        double d = (m[0][0]*m[1][1]*m[2][2]) + (m[0][1]*m[1][2]*m[2][0]) + (m[0][2]*m[1][0]*m[2][1])
              - (m[2][0]*m[1][1]*m[0][2]) - (m[2][1]*m[1][2]*m[0][0]) - (m[2][2]*m[1][0]*m[0][1]);

        return d > 0;
    }
	

    public List<Triangle> bowyerWatson(Dictionary<Vector2, Cell> inputCells){
        // pointList is a set of coordinates defining the points to be triangulated
        List<Triangle> triangulation = new List<Triangle>();
        // Adds the supertriangle to the triangulation
        Triangle supertriangle = getSupertriangle(inputCells);
        triangulation.Add(supertriangle); // must be large enough to completely contain all the points in pointList        
        foreach (KeyValuePair<Vector2, Cell> cell in inputCells){ // add all the points one at a time to the triangulation            
            List<Triangle> badTriangles = new List<Triangle>();            
            foreach (Triangle triangle in triangulation){ // first find all the triangles that are no longer valid due to the insertion                 
                if(IsCellInsideCircumcircle(cell.Value, triangle)){                                                          
                    badTriangles.Add(triangle);                    
                }                 
            }
            Dictionary<String, int> edgeIncidences = new Dictionary<String, int>();
            List<Edge> poligon = new List<Edge>(); 
            foreach (Triangle triangle in badTriangles){ // first find all the triangles that are no longer valid due to the insertion            
                Edge edgeAB = new Edge(triangle.A, triangle.B);
                Edge edgeBC = new Edge(triangle.B, triangle.C);
                Edge edgeCA = new Edge(triangle.C, triangle.A);
                if(edgeIncidences.ContainsKey(edgeAB.key)){
                    edgeIncidences[edgeAB.key] += 1;                    
                } else {
                    edgeIncidences[edgeAB.key] = 1;
                }                
                if(edgeIncidences.ContainsKey(edgeBC.key)){
                    edgeIncidences[edgeBC.key] += 1;                    
                } else {
                    edgeIncidences[edgeBC.key] = 1;
                }                
                if(edgeIncidences.ContainsKey(edgeCA.key)){
                    edgeIncidences[edgeCA.key] += 1;                    
                } else {
                    edgeIncidences[edgeCA.key] = 1;
                }                                
            }            
            foreach (Triangle triangle in badTriangles){ // first find all the triangles that are no longer valid due to the insertion            
                Edge edgeAB = new Edge(triangle.A, triangle.B);
                Edge edgeBC = new Edge(triangle.B, triangle.C);
                Edge edgeCA = new Edge(triangle.C, triangle.A);
                if(edgeIncidences[edgeAB.key] == 1){
                    poligon.Add(edgeAB);                    
                } 
                if(edgeIncidences[edgeBC.key] == 1){
                    poligon.Add(edgeBC);                     
                }
                if(edgeIncidences[edgeCA.key] == 1){
                    poligon.Add(edgeCA);                     
                }
            }              
            foreach (Triangle triangle in badTriangles){                 
                triangulation.Remove(triangle);
            }
            foreach(Edge edge in poligon){                
                Triangle newTriangle = new Triangle(edge.A, edge.B, cell.Value, cell.Key);                                
                triangulation.Add(newTriangle);
            } 
        }
        List<Triangle> clean = new List<Triangle>();
        foreach(Triangle triangle in triangulation){
            if(triangle.A == supertriangle.A || triangle.A == supertriangle.B || triangle.A == supertriangle.C
            || triangle.B == supertriangle.A || triangle.B == supertriangle.B || triangle.B == supertriangle.C
            || triangle.C == supertriangle.C || triangle.C == supertriangle.B || triangle.C == supertriangle.C
            ){
                clean.Add(triangle);
            }
        }
        foreach(Triangle triangle in clean){
            triangulation.Remove(triangle);
        }
        return triangulation;
    }

    public void setNeigborToCell(Cell cell1, Cell cell2){
        double angle = getAngle(cell1, cell2);
        if( angle > 315 || angle <= 45){//derecha
            cell1.neighborCells[NeighborType.Right] = cell2;
        } else if(angle > 45 && angle <= 135){//arriba
            cell1.neighborCells[NeighborType.Above] = cell2;
        } else if(angle > 135 && angle <= 225){//izquierda
            cell1.neighborCells[NeighborType.Left] = cell2;
        } else if(angle > 225 && angle <= 315){//abajo
            cell1.neighborCells[NeighborType.Below] = cell2;
        }
    }
	
    public void setCellNeigbors(List<Triangle> triangulation){
        foreach(Triangle triangle in triangulation){
            setNeigborToCell(triangle.A, triangle.B);
            setNeigborToCell(triangle.A, triangle.C);
            setNeigborToCell(triangle.B, triangle.A);
            setNeigborToCell(triangle.B, triangle.C);
            setNeigborToCell(triangle.C, triangle.A);
            setNeigborToCell(triangle.C, triangle.B);
        }
    }

    public double getAngle(Cell cell1, Cell cell2){
        int ca = cell2.X - cell1.X;
        int co = cell2.Y - cell1.Y;
        if(ca == 0){
            if(co > 0){
                return 90.0;
            }
            return 270.0;
        }
        if(co == 0){
            if(ca>0){
                return 0.0;
            }
            return 180;
        }
        double theta = Math.Abs(Math.Atan(co/ca) *180/Math.PI);
        if(ca>0 && co>0){// I 
            return theta;
        } else if(ca<0 && co>0){ // II
            return  180.0 - theta; 
        } else if(ca<0 && co<0){ // II
            return  180 + theta; 
        } else {
            return  360 - theta; 
        }
    }

     // Funciones No necesarias ///////////////////////////////////////////////////////////////////////////////////////////////////////
    public void log(List<Triangle> triangulation){
        Debug.Log(getCells(triangulation));
    }

    public String getCells(List<Triangle> triangulation){
        List<Cell> cells = new List<Cell>();
        foreach(Triangle triangle in triangulation){
            cells.Add(triangle.A);
            cells.Add(triangle.B);
            cells.Add(triangle.C);
        }
        cells = cells.Distinct().ToList();
        String strCells = "{{";
        foreach(Cell cell in cells){
            strCells += "("+cell.X+", "+cell.Y+"),\n";
        }
        return strCells.Remove(strCells.Length-2,2)+"}," + getEdgesPostSet(cells);
    }

    public String getEdges(List<Triangle> triangulation){
        var edges = new List<Edge>(); 
        Dictionary<String, int> edgeIncidences = new Dictionary<String, int>();
        foreach (Triangle triangle in triangulation){ // first find all the triangles that are no longer valid due to the insertion            
            Edge edgeAB = new Edge(triangle.A, triangle.B);
            Edge edgeBC = new Edge(triangle.B, triangle.C);
            Edge edgeCA = new Edge(triangle.C, triangle.A);
            if(!edgeIncidences.ContainsKey(edgeAB.key)){
                edgeIncidences[edgeAB.key] = 1;
                edges.Add(edgeAB);
            } 
            if(!edgeIncidences.ContainsKey(edgeBC.key)){
                edgeIncidences[edgeBC.key] = 1;
                edges.Add(edgeBC);
            } 
            if(!edgeIncidences.ContainsKey(edgeCA.key)){
                edgeIncidences[edgeCA.key] = 1;
                edges.Add(edgeCA);
            }                                                                       
        }                            
        String strSegments = "{";
        foreach(Edge edge in edges){            
            strSegments += edge.getSegment()+",\n";
        }        
        return strSegments.Remove(strSegments.Length-2, 2) + "}}";
    }

    
    public String getEdgesPostSet(List<Cell> cells ){
        var edges = new List<Edge>(); 
        Dictionary<String, int> edgeIncidences = new Dictionary<String, int>();
        foreach(Cell cell in cells){
            foreach(Cell neighbor in cell.neighborCells.Values){
                Edge edge = new Edge(cell, neighbor);
                if(!edgeIncidences.ContainsKey(edge.key)){
                    edgeIncidences[edge.key] = 1;
                    edges.Add(edge);
                } 
            }
        }        
        String strSegments = "{";
        foreach(Edge edge in edges){            
            strSegments += edge.getSegment()+",\n";
        }        
        return strSegments.Remove(strSegments.Length-2, 2) + "}}";
    }

}