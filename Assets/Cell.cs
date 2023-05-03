using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell
{
    public Vector3 position;
    public int type = 0;
    public bool showing = false;
    public Cell[,,] neighbors;

    public Cell(Vector3 position, int type){
        this.position = position;
        this.type = type;
        this.neighbors = new Cell[9,9,9];
    }
}
