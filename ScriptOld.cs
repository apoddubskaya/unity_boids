using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateScript : MonoBehaviour
{
    public GameObject cubePrefab;
    private List<GameObject> cubes;
    private List<Vector3> Speed;
    private Vector3 place;
    private List<List<int>> neighbors;
    private float radius = 5.0f;

    private Vector3 moveToCentreMass(int numCube)
    {
        Vector3 centre = Vector3.zero;
        int count = 0;
        for (int i = 0; i < neighbors[numCube].Count; i++)
        {
            int idx = neighbors[numCube][i];
            if (cubes[idx] == null)
                continue;
            count++;
            centre += cubes[idx].transform.position;
        }
        if (count == 0)
            return Vector3.zero;
        centre /= count;
        return (centre - cubes[numCube].transform.position) / 50;
    }

    private Vector3 moveToSpeedCentreMass(int numCube)
    {
        Vector3 centre = Vector3.zero;
        int count = 0;
        for (int i = 0; i < neighbors[numCube].Count; i++)
        {
            int idx = neighbors[numCube][i];
            if (cubes[idx] == null)
                continue;
            count++;
            centre += Speed[idx];
        }
        if (count == 0)
            return Vector3.zero;
        centre /= count;
        return (centre - Speed[numCube]) / 10;
    }

    private Vector3 moveFromNeighbors(int numCube)
    {
        Vector3 dist = Vector3.zero;
        for (int i = 0; i < neighbors[numCube].Count; i++)
        {
            int idx = neighbors[numCube][i];
            if (cubes[idx] == null)
                continue;
            Vector3 curr = cubes[numCube].transform.position - cubes[idx].transform.position;
            dist += curr.normalized * (radius - curr.magnitude);
        }
        return dist / 13;
    }


    private Vector3 moveToPlace(int numCube)
    {
        return (place - cubes[numCube].transform.position);
    }

    private Vector3 LimitSpeed(Vector3 speed, float limit)
    {
        float speedAbs = speed.magnitude;
        if (speedAbs > limit)
        {
            speed = (speed / speedAbs) * limit;

        }
        return speed;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("nachalo\n");
        place = GameObject.Find("OptimizedTree").transform.position;
        cubes = new List<GameObject>();
        neighbors = new List<List<int>>();
        Speed = new List<Vector3>();
        for (int i = 0; i < 200; i++)
        {
            GameObject instance = Instantiate(cubePrefab,
                               cubePrefab.transform.position + new Vector3((float)(2 * i), 0.0f, 0.0f),
                               transform.rotation) as GameObject;
            cubes.Add(instance);
            neighbors.Add(new List<int>());
            Speed.Add(Vector3.zero);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            if (cubes[i] == null)
                continue;
            for (int j = i + 1; j < cubes.Count; j++)
            {
                if (cubes[j] == null)
                    continue;
                if (Vector3.Distance(cubes[i].transform.position, cubes[j].transform.position) <= radius)
                {
                    neighbors[i].Add(j);
                    neighbors[j].Add(i);
                }
            }
        }
        
        for (int i = 0; i < cubes.Count; i++)
        {
            if (cubes[i] == null)
                continue;
            Vector3 speed = (moveToCentreMass(i) + moveFromNeighbors(i) + moveToSpeedCentreMass(i)) + moveToPlace(i);
            speed = LimitSpeed(speed, 3.5f);
            if (Vector3.Distance(cubes[i].transform.position, place) <= 5.0f)
            {
                cubes[i] = null;
                Speed[i] = Vector3.zero;
                Debug.Log("derevo");
            }
            else
            {
                cubes[i].transform.Translate(speed * Time.deltaTime);
                Speed[i] = speed;
            }
        }
    }
}
