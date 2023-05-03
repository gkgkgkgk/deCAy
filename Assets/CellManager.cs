using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class CellManager : MonoBehaviour
{
    public int size = 20;
    private int[] valBuffer;
    private GameObject[] cubes;
    public float tickRate = 1.0f;
    float timer = 1.0f;
    public ComputeShader updateShader;
    private ComputeBuffer cellsBuffer;
    private ComputeBuffer newCellsBuffer;

    public GameObject cellGO;

    private int steps = 10;
    private bool running = false;
    public TMP_InputField stepsInput;

    int kernelID = 0;

    public float drawCooldown = 3.0f;
    public float drawTimer = 3.0f;

    public float decayCooldown = 3.0f;
    public float decayTimer = 3.0f;

    public enum Phase {none, drawing, growing, grown, drawingDecay, decaying};
    public Phase phase = Phase.none;

    public Material stone;
    public Material decayedStone;
    public Material roof;
    public Material decayedRoof;

    bool insertedArchitecture = false;
    int states = 5;
    int initialCubes = 0;
    // Start is called before the first frame update
    void Start()
    {
        timer = tickRate;
        drawTimer = drawCooldown;
        decayTimer = decayCooldown;
        cubes = new GameObject[size * size * size];
        valBuffer = new int[size * size * size];
        int counter = 0;
        for(int z = 0; z < size; z++){
            for(int y = 0; y < size; y++){
                for(int x = 0; x < size; x++){
                    valBuffer[counter] = 0;
                    cubes[counter] = Instantiate(cellGO, new Vector3(x, y, z), transform.rotation);
                    cubes[counter].SetActive(false);
                    counter++;
                }   
            }    
        }

        cellsBuffer = new ComputeBuffer(valBuffer.Length, sizeof(int), ComputeBufferType.Default);
        cellsBuffer.SetData(valBuffer);
        newCellsBuffer = new ComputeBuffer(valBuffer.Length, sizeof(int), ComputeBufferType.Default);
        newCellsBuffer.SetData(valBuffer);
        kernelID = updateShader.FindKernel("UpdateCells");
        updateShader.SetInt("vn", 0);
        updateShader.SetInt("states", states);

        render();
    }

    void setShowing(int x, int y, int z, int val){
        int index = (int)(x + y * size + z * size * size);
        valBuffer[index] = val;
        cubes[index].SetActive(val > 0 ? true: false);
    }

    int getShowing(int x, int y, int z){
        int index = (int)(x + y * size + z * size * size);
        return valBuffer[index];
    }

    public void clear(){
        for(int i = 0; i < size * size * size; i++){
            int x = i % size;
            int y = (i / size) % size;
            int z = i / (size * size);
            setShowing(x, y, z, 0);
        }
        cellsBuffer.SetData(valBuffer);
        newCellsBuffer.SetData(valBuffer);
        render();
    }

    void render(){
        for(int i = 0; i < size * size * size; i++){
            int x = i % size;
            int y = (i / size) % size;
            int z = i / (size * size);

            if(valBuffer[i] % 10 > 0){
                if(cubes[i] == null){
                    cubes[i] = Instantiate(cellGO, new Vector3(x, y, z), transform.rotation);
                } else {
                    cubes[i].SetActive(true);
                }

                

                if(phase == Phase.decaying){
                    if(valBuffer[i] < 5){
                        cubes[i].GetComponent<Renderer>().material = decayedStone;
                    }
                    else if(valBuffer[i] == 5){
                        cubes[i].GetComponent<Renderer>().material = stone;
                    }
                    else if(valBuffer[i] == 15){
                        cubes[i].GetComponent<Renderer>().material = roof;
                    }
                    else if(valBuffer[i] <= 15){
                        cubes[i].GetComponent<Renderer>().material = decayedRoof;
                    }
                }
                else {
                    if(valBuffer[i] < 10){
                        cubes[i].GetComponent<Renderer>().material = stone;
                    }
                    if(valBuffer[i] >= 10){
                        cubes[i].GetComponent<Renderer>().material = roof;
                    }
                }
            }
            else {
                if(cubes[i] != null){
                    cubes[i].SetActive(false);
                }
            }
        }
    }

    void renderCube(int x, int y, int z){
        int i = x + size * y + size * size * z;

        if(valBuffer[i] % 10 > 0){
            if(cubes[i] == null){
                cubes[i] = Instantiate(cellGO, new Vector3(x, y, z), transform.rotation);
            } else {
                cubes[i].SetActive(true);
            }

            if(valBuffer[i] < 10){
                cubes[i].GetComponent<Renderer>().material = stone;
            }

            if(valBuffer[i] >= 10){
                cubes[i].GetComponent<Renderer>().material = roof;
            }
        }
        else {
            if(cubes[i] != null){
                cubes[i].SetActive(false);
            }
        }
    }

    void captureObj () {
        List<Mesh> meshes = new List<Mesh>();
        List<GameObject> activeCubes = new List<GameObject>();
        GameObject combined = new GameObject();

        foreach(GameObject cube in cubes){
            if(cube.activeSelf){
                GameObject clone = Instantiate(cube);
                clone.transform.parent = combined.transform;
            }
        }

        combined.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            captureObj();
        }
        if(phase == Phase.none){
            if (Input.GetMouseButton(0)) {
                phase = Phase.drawing;
                initialCubes = 0;
            }

            drawTimer = drawCooldown;
        }

        if (phase == Phase.drawing){
            if (Input.GetMouseButton(0)) {
                drawTimer = drawCooldown;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Vector3 intersection = Vector3.zero;
                if (Physics.Raycast(ray, out hit)) {
                    intersection = hit.point;
                    if(getShowing((int) intersection.x, 0, (int) intersection.z) % 10 == 0){
                        setShowing((int) intersection.x, 0, (int) intersection.z, states);
                        initialCubes++;
                    }
                }

                renderCube((int) intersection.x, 0, (int) intersection.z);
            }

            if(drawTimer <= 0){
                phase = Phase.growing;
                drawTimer = drawCooldown;
                cellsBuffer.SetData(valBuffer);
                steps = int.Parse(stepsInput.text);
                timer = tickRate;
                updateShader.SetInt("growing", 1);
                updateShader.SetInt("height",  (int)Mathf.Clamp(initialCubes / 10, 5, 15));
            }

            drawTimer -= Time.deltaTime;
        }

        if(phase == Phase.growing){
            timer -= Time.deltaTime;
            if(timer <= 0){
                steps--;
                updateShader.SetBuffer(kernelID, "cells", cellsBuffer);
                updateShader.SetBuffer(kernelID, "newCells", newCellsBuffer);
                updateShader.SetInt("gridsize", size);
                int threadGroupSize = 10;
                int numThreadGroups = Mathf.CeilToInt(size / (float)threadGroupSize);
                updateShader.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

                newCellsBuffer.GetData(valBuffer);
                cellsBuffer.SetData(valBuffer);
                render();
                timer = tickRate;
            }

            if(steps <= 0){
                phase = Phase.grown;
                insertedArchitecture = false;
                steps = (int)Mathf.Clamp(initialCubes / 10, 5, 15);
                timer = tickRate;
                cellsBuffer.SetData(valBuffer);
                updateShader.SetInt("growing", 2);
                updateShader.SetInt("vn", 0);
                // updateShader.SetInt("roofType", 3);
                updateShader.SetInt("roofType", Random.Range(3, 5));
                captureObj();
            }
        }

        if(phase == Phase.grown){
            timer -= Time.deltaTime;
            if(timer <= 0 && steps >= 0){
                timer = tickRate;
                steps--;
                updateShader.SetBuffer(kernelID, "cells", cellsBuffer);
                updateShader.SetBuffer(kernelID, "newCells", newCellsBuffer);
                updateShader.SetInt("gridsize", size);
                int threadGroupSize = 10;
                int numThreadGroups = Mathf.CeilToInt(size / (float)threadGroupSize);
                updateShader.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

                newCellsBuffer.GetData(valBuffer);
                cellsBuffer.SetData(valBuffer);
                render();
                insertedArchitecture = true;
            }

            if (Input.GetMouseButton(0)) {
                phase = Phase.drawingDecay;
                updateShader.SetInt("vn", 0);
                captureObj();
            }

            decayTimer = decayCooldown;
        }

        if(phase == Phase.drawingDecay){
            if (Input.GetMouseButton(0)) {
                decayTimer = decayCooldown;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Vector3 intersection = Vector3.zero;
                if (Physics.Raycast(ray, out hit)) {
                    intersection = hit.transform.position;
                    setShowing((int) intersection.x, (int) intersection.y, (int) intersection.z, 0);
                }

                renderCube((int) intersection.x, (int) intersection.y, (int) intersection.z);
            }

            if(decayTimer <= 0){
                phase = Phase.decaying;
                updateShader.SetInt("growing", 0);
                timer = tickRate;
                cellsBuffer.SetData(valBuffer);
                newCellsBuffer.SetData(valBuffer);
            }

            decayTimer -= Time.deltaTime;
        }

        if(phase == Phase.decaying){
            if (Input.GetMouseButton(0)) {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Vector3 intersection = Vector3.zero;
                if (Physics.Raycast(ray, out hit)) {
                    intersection = hit.transform.position;
                    setShowing((int) intersection.x, (int) intersection.y, (int) intersection.z, 0);
                }

                renderCube((int) intersection.x, (int) intersection.y, (int) intersection.z);

                newCellsBuffer.SetData(valBuffer);
                cellsBuffer.SetData(valBuffer);
            }

            timer -= Time.deltaTime;
            if(timer <= 0){
                steps--;
                updateShader.SetBuffer(kernelID, "cells", cellsBuffer);
                updateShader.SetBuffer(kernelID, "newCells", newCellsBuffer);
                updateShader.SetInt("gridsize", size);
                int threadGroupSize = 10;
                int numThreadGroups = Mathf.CeilToInt(size / (float)threadGroupSize);
                updateShader.Dispatch(0, numThreadGroups, numThreadGroups, numThreadGroups);

                newCellsBuffer.GetData(valBuffer);
                cellsBuffer.SetData(valBuffer);
                render();
                timer = tickRate;
            }
        }
    }
}
