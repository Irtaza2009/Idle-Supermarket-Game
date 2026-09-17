using UnityEngine;
using UnityEngine.AI;
using TMPro;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform[] aislePoints;
    [SerializeField] private float timeToBrowse = 3f;
    [SerializeField] private float arrivalDistance = 0.5f;
    [SerializeField] private GameObject reactionPanel;
    [SerializeField] private TMP_Text reactionText;

    private NavMeshAgent agent;
    private float browseTimer;
    private CustomerState state;
    private bool isStealer;

    private enum CustomerState
    {
        GoingToAisle,
        Browsing,
        Returning
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        IgnorePlayerCollisions();
        SetReactionTextVisible(false);
    }

    private void IgnorePlayerCollisions()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            return;
        }

        CharacterController playerCollider = player.GetComponent<CharacterController>();
        if (playerCollider == null)
        {
            return;
        }

        Collider[] customerColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider customerCollider in customerColliders)
        {
            Physics.IgnoreCollision(playerCollider, customerCollider, true);
        }
    }

    public void Initialize(Transform entrance, Transform[] shoppingPoints, bool stealer)
    {
        spawnPoint = entrance;
        aislePoints = shoppingPoints;
        isStealer = stealer;
    }

    public bool IsStealer => isStealer;

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

    private void LateUpdate()
    {
        if (reactionPanel == null)
        {
            return;
        }

        reactionPanel.transform.rotation = Quaternion.Euler(0f, 45f, 180f);
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

    public void Accused()
    {
        if (state == CustomerState.Returning)
        {
            return;
        }

        if (isStealer)
        {
            ShowReaction("I got caught!");
        }
        else
        {
            ShowReaction("That is outrageous!");
        }

        ReturnToSpawn();
    }

    private void ShowReaction(string message)
    {
        if (reactionText != null)
        {
            reactionText.text = message;
            SetReactionTextVisible(true);
        }
    }

    private void SetReactionTextVisible(bool isVisible)
    {
        if (reactionPanel != null)
        {
            reactionPanel.SetActive(isVisible);
        }

        if (reactionText != null)
        {
            if (reactionPanel == null)
            {
                reactionText.gameObject.SetActive(isVisible);
            }
        }
    }
}
