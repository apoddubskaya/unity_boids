using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Jobs;

public class CreateScript : MonoBehaviour
{
    private struct cubeInfo
    {
        public bool isAlive;
        public Vector3 position;
        public Vector3 speed;

        public cubeInfo(bool b, Vector3 p, Vector3 s)
        {
            isAlive = b;
            position = p;
            speed = s;
        }
    }

    public GameObject cubePrefab;
    private List<GameObject> cubes;
    private Vector3 place;
    private float radius = 5.0f;
    private NativeArray<cubeInfo> previousCubes;
    private NativeArray<cubeInfo> currentCubes;
    private TransformAccessArray transforms;

    private struct CubeJob : IJobParallelForTransform
    {

        [ReadOnly]
        public float deltaTime;

        [ReadOnly]
        public float radius;

        [ReadOnly]
        public Vector3 place;

        [ReadOnly]
        public NativeArray<cubeInfo> previousCubes;

        [WriteOnly]
        public NativeArray<cubeInfo> currentCubes;

        private Vector3 moveToCentreMass(int numCube, NativeArray<int> neighbors)
        {
            Vector3 centre = Vector3.zero;
            int count = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                int idx = neighbors[i];
                if (!previousCubes[idx].isAlive)
                    continue;
                count++;
                centre += previousCubes[idx].position;
            }
            if (count == 0)
                return Vector3.zero;
            centre /= count;
            return (centre - previousCubes[numCube].position) / 50;
        }

        private Vector3 moveToSpeedCentreMass(int numCube, NativeArray<int> neighbors)
        {
            Vector3 centre = Vector3.zero;
            int count = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                int idx = neighbors[i];
                if (!previousCubes[idx].isAlive)
                    continue;
                count++;
                centre += previousCubes[idx].speed;
            }
            if (count == 0)
                return Vector3.zero;
            centre /= count;
            return (centre - previousCubes[numCube].speed) / 10;
        }

        private Vector3 moveFromNeighbors(int numCube, NativeArray<int> neighbors)
        {
            Vector3 dist = Vector3.zero;
            Debug.Log(neighbors.Length.ToString());
            for (int i = 0; i < neighbors.Length; i++)
            {
                int idx = neighbors[i];
                if (!previousCubes[idx].isAlive)
                    continue;
                Vector3 curr = previousCubes[numCube].position - previousCubes[idx].position;
                dist += curr.normalized * (radius - curr.magnitude);
            }
            return dist / 13;
        }


        private Vector3 moveToPlace(int numCube)
        {
            return (place - previousCubes[numCube].position);
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

        public void Execute(int index, TransformAccess transform)
        {
            if (!previousCubes[index].isAlive)
                return;
            int amount = 0;
            for (int i = 0; i < previousCubes.Length; i++)
            {
                if (!previousCubes[i].isAlive)
                    continue;
                if (Vector3.Distance(transform.position, previousCubes[i].position) <= radius)
                    amount++;
            }

            NativeArray<int> neighbors = new NativeArray<int>(amount, Allocator.Temp);

            for (int i = 0; i < previousCubes.Length; i++)
            {
                if (!previousCubes[i].isAlive)
                    continue;
                if (Vector3.Distance(transform.position, previousCubes[i].position) <= radius)
                {
                    neighbors[--amount] = i;
                }
            }

            Vector3 speed = moveToCentreMass(index, neighbors)
                            + moveFromNeighbors(index, neighbors)
                            + moveToSpeedCentreMass(index, neighbors)
                            + moveToPlace(index);
            speed = LimitSpeed(speed, 3.5f);

            if (Vector3.Distance(transform.position, place) <= 5.0f)
            {
                currentCubes[index] = new cubeInfo(false, previousCubes[index].position, Vector3.zero);
            }
            else
            {
                transform.position += speed * deltaTime;
                currentCubes[index] = new cubeInfo(true, transform.position, speed);
            }



            neighbors.Dispose();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("nachalo\n");
        place = GameObject.Find("OptimizedTree").transform.position;
        cubes = new List<GameObject>();
        for (int i = 0; i < 200; i++)
        {
            GameObject instance = Instantiate(cubePrefab,
                               cubePrefab.transform.position + new Vector3((float)(2 * i), 0.0f, 0.0f),
                               transform.rotation) as GameObject;
            cubes.Add(instance);
        }

        Transform[] ttransforms = new Transform[cubes.Count];
        previousCubes = new NativeArray<cubeInfo>(cubes.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < cubes.Count; i++)
        {
            previousCubes[i] = new cubeInfo(true, cubes[i].transform.position, Vector3.zero);
            ttransforms[i] = cubes[i].transform;
        }
        transforms = new TransformAccessArray(ttransforms);
        currentCubes = new NativeArray<cubeInfo>(cubes.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    // Update is called once per frame
    void Update()
    {
        new CubeJob
        {
            deltaTime = Time.deltaTime,
            place = place,
            radius = radius,
            previousCubes = previousCubes,
            currentCubes = currentCubes
        }
        .Schedule(transforms)
        .Complete();

        NativeArray<cubeInfo> tmp = currentCubes;
        currentCubes = previousCubes;
        previousCubes = tmp;
    }

    private void OnDestroy()
    {
        previousCubes.Dispose();
        currentCubes.Dispose();
        transforms.Dispose();
    }
}
