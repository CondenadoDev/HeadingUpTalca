using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Bulldozer : NetworkBehaviour
{
    [Header("Course Configuration")]
    public GameObject[] courses;
    public GameObject finalCourse;
    public Transform startPoint;
    public float distanceCourse;

    [Networked, Capacity(10)] // Adjust capacity to match the courses count
    public NetworkArray<int> shuffledIndices { get; }

    private bool initialized = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            InitializeCourseOrder();
        }
        ApplyCoursePositions();
    }

    private void InitializeCourseOrder()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < courses.Length; i++)
        {
            indices.Add(i);
        }
        ShuffleIndices(indices);

        // Assign the shuffled order to the networked array
        for (int i = 0; i < courses.Length; i++)
        {
            shuffledIndices.Set(i, indices[i]);
        }

        ApplyCoursePositions();
    }

    private void ApplyCoursePositions()
    {
        if (initialized) return;
        initialized = true;

        Vector3 nextPosition = startPoint != null ? startPoint.position : Vector3.zero;

        for (int i = 0; i < courses.Length; i++)
        {
            int index = shuffledIndices.Get(i);
            courses[index].transform.position = nextPosition;
            nextPosition += new Vector3(0, 0, distanceCourse);
        }

        if (finalCourse != null)
        {
            finalCourse.transform.position = nextPosition;
        }
    }

    private void ShuffleIndices(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}