using UnityEngine;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [SerializeField] private CustomerController customerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] aislePoints;
    [SerializeField] private Transform[] queuePoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxCustomers = 5;
    [SerializeField, Range(0f, 1f)] private float honestQueueChance = 0.5f;
    [SerializeField] private bool spawnImmediately = true;

    private readonly List<CustomerController> activeCustomers = new();
    private float spawnTimer;

    private void Start()
    {
        spawnTimer = spawnImmediately ? 0f : spawnInterval;
    }

    private void Update()
    {
        RemoveFinishedCustomers();

        if (customerPrefab == null || spawnPoint == null || activeCustomers.Count >= maxCustomers)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnCustomer();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnCustomer()
    {
        CustomerController customer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        bool isStealer = Random.value < 0.2f;
        Transform queuePoint = null;

        if (!isStealer && Random.value < honestQueueChance)
        {
            queuePoint = FindAvailableQueuePoint();
        }

        customer.Initialize(spawnPoint, aislePoints, isStealer, queuePoint);
        activeCustomers.Add(customer);
    }

    private Transform FindAvailableQueuePoint()
    {
        foreach (Transform queuePoint in queuePoints)
        {
            bool occupied = false;

            foreach (CustomerController customer in activeCustomers)
            {
                if (customer != null && customer.AssignedQueuePoint == queuePoint)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
            {
                return queuePoint;
            }
        }

        return null;
    }

    private void RemoveFinishedCustomers()
    {
        activeCustomers.RemoveAll(customer => customer == null);
    }
}
