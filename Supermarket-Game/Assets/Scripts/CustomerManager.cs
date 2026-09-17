using UnityEngine;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [SerializeField] private CustomerController customerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] aislePoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxCustomers = 5;
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
        customer.Initialize(spawnPoint, aislePoints, isStealer);
        activeCustomers.Add(customer);
    }

    private void RemoveFinishedCustomers()
    {
        activeCustomers.RemoveAll(customer => customer == null);
    }
}
