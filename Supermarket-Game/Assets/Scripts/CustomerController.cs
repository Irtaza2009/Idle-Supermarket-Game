using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] aislePoints;
    [SerializeField] private float timeToBrowse = 3f;
    [SerializeField] private float arrivalDistance = 0.5f;

    private NavMeshAgent agent;
    private float browseTimer;
    private CustomerState state;

    private enum CustomerState
    {
        GoingToAisle,
        Browsing,
        Returning
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Initialize(Transform entrance, Transform[] shoppingPoints)
    {
        spawnPoint = entrance;
        aislePoints = shoppingPoints;
    }

    private void Start()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("CustomerController needs a spawn point.", this);
            Destroy(gameObject);
            return;
        }

        if (aislePoints == null || aislePoints.Length == 0)
        {
            ReturnToSpawn();
            return;
        }

        Transform target = aislePoints[Random.Range(0, aislePoints.Length)];
        agent.SetDestination(target.position);
        state = CustomerState.GoingToAisle;
    }

    private void Update()
    {
        if (state == CustomerState.GoingToAisle && HasReachedDestination())
        {
            browseTimer = timeToBrowse;
            state = CustomerState.Browsing;
            agent.isStopped = true;
        }
        else if (state == CustomerState.Browsing)
        {
            browseTimer -= Time.deltaTime;

            if (browseTimer <= 0f)
            {
                ReturnToSpawn();
            }
        }
        else if (state == CustomerState.Returning && HasReachedDestination())
        {
            Destroy(gameObject);
        }
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= arrivalDistance;
    }

    private void ReturnToSpawn()
    {
        agent.isStopped = false;
        agent.SetDestination(spawnPoint.position);
        state = CustomerState.Returning;
    }
}
