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
    [SerializeField] private int minimumAisleVisits = 2;
    [SerializeField] private int maximumAisleVisits = 4;
    [SerializeField] private GameObject reactionPanel;
    [SerializeField] private TMP_Text reactionText;
    [SerializeField] private GameObject cashChangePanel;
    [SerializeField] private TMP_Text cashChangeText;
    [SerializeField] private GameObject carriedItem;
    [SerializeField] private float queueWaitTime = 20f;
    [SerializeField] private float cashChangeDisplayTime = 1.5f;
    [SerializeField] private float exitArrivalDistance = 1.25f;
    [SerializeField] private float returnTimeout = 10f;
    [SerializeField] private float stuckCheckTime = 1.5f;
    [SerializeField, Range(0f, 1f)] private float stealChance = 0.2f;
    [SerializeField] private float stealDisplayTime = 1f;

    private NavMeshAgent agent;
    private float browseTimer;
    private CustomerState state;
    private bool isStealer;
    private int aisleVisits;
    private int totalAisleVisits;
    private int previousAisleIndex = -1;
    private float queueTimer;
    private float cashChangeTimer;
    private float returnTimer;
    private float stuckTimer;
    private Vector3 lastPosition;
    private float stealTimer;
    private bool wasAccused;
    private bool exitCashProcessed;
    private bool hasAttemptedSteal;
    private bool isStealing;

    private enum CustomerState
    {
        GoingToAisle,
        Browsing,
        GoingToQueue,
        Queued,
        Returning
    }
    private Transform assignedQueuePoint;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80);
        IgnorePlayerCollisions();
        SetReactionTextVisible(false);
        SetCashChangePanelVisible(false);
        SetCarriedItemVisible(false);
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

    public void Initialize(Transform entrance, Transform[] shoppingPoints, bool stealer, Transform queuePoint)
    {
        spawnPoint = entrance;
        aislePoints = shoppingPoints;
        isStealer = stealer;
        assignedQueuePoint = queuePoint;
    }

    public bool IsStealer => isStealer;
    public Transform AssignedQueuePoint => assignedQueuePoint;
    public bool IsQueued => state == CustomerState.Queued;
    public bool IsGoingToQueue => state == CustomerState.GoingToQueue;

    private void Start()
    {
        lastPosition = transform.position;

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

        minimumAisleVisits = Mathf.Max(1, minimumAisleVisits);
        maximumAisleVisits = Mathf.Max(minimumAisleVisits, maximumAisleVisits);
        totalAisleVisits = Random.Range(minimumAisleVisits, maximumAisleVisits + 1);
        GoToNextAisle();
        state = CustomerState.GoingToAisle;
    }

    private void Update()
    {
        CheckForStuckAgent();

        if (state == CustomerState.GoingToAisle && HasReachedDestination())
        {
            browseTimer = timeToBrowse;
            state = CustomerState.Browsing;
            agent.isStopped = true;
            TrySteal();
        }
        else if (state == CustomerState.Browsing)
        {
            if (isStealing)
            {
                stealTimer -= Time.deltaTime;
                if (stealTimer <= 0f)
                {
                    isStealing = false;
                    SetReactionTextVisible(false);
                }
            }

            browseTimer -= Time.deltaTime;

            if (browseTimer <= 0f)
            {
                aisleVisits++;

                if (aisleVisits >= totalAisleVisits)
                {
                    FinishBrowsing();
                }
                else
                {
                    SetReactionTextVisible(false);
                    GoToNextAisle();
                    state = CustomerState.GoingToAisle;
                }
            }
        }
        else if (state == CustomerState.GoingToQueue && HasReachedDestination())
        {
            agent.isStopped = true;
            state = CustomerState.Queued;
            StartQueueTimer();
        }
        else if (state == CustomerState.Queued)
        {
            agent.isStopped = true;
            UpdateQueueCountdownText();
            queueTimer -= Time.deltaTime;

            if (queueTimer <= 0f)
            {
                ShowReaction("This is taking too long!");
                ReturnToSpawn();
            }
        }
        else if (state == CustomerState.Returning)
        {
            returnTimer -= Time.deltaTime;

            if (HasReachedExit() || returnTimer <= 0f)
            {
                if (!exitCashProcessed)
                {
                    if (isStealer && !wasAccused)
                    {
                        int lostCash = Random.Range(20, 51);
                        CashSystem cashSystem = FindFirstObjectByType<CashSystem>();
                        if (cashSystem != null)
                        {
                            cashSystem.LoseCash(lostCash);
                        }

                        ShowCashChange(-lostCash);
                    }

                    exitCashProcessed = true;
                    cashChangeTimer = cashChangeDisplayTime;
                    agent.isStopped = true;
                }

                cashChangeTimer -= Time.deltaTime;
                if (cashChangeTimer <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (reactionPanel == null)
        {
            if (cashChangePanel != null)
            {
                cashChangePanel.transform.rotation = Quaternion.Euler(0f, 45f, 180f);
            }

            return;
        }

        reactionPanel.transform.rotation = Quaternion.Euler(0f, 45f, 180f);

        if (cashChangePanel != null)
        {
            cashChangePanel.transform.rotation = Quaternion.Euler(0f, 45f, 180f);
        }
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= arrivalDistance;
    }

    private void CheckForStuckAgent()
    {
        if (state != CustomerState.GoingToAisle && state != CustomerState.GoingToQueue)
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
            return;
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        bool hasUnfinishedPath = !agent.pathPending && agent.remainingDistance > arrivalDistance;

        if (hasUnfinishedPath && distanceMoved < 0.01f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;

        if (stuckTimer < stuckCheckTime)
        {
            return;
        }

        stuckTimer = 0f;
        agent.avoidancePriority = Random.Range(20, 80);

        if (state == CustomerState.GoingToAisle)
        {
            GoToNextAisle();
        }
        else if (assignedQueuePoint != null)
        {
            agent.isStopped = false;
            agent.SetDestination(assignedQueuePoint.position);
        }
    }

    private bool HasReachedExit()
    {
        if (spawnPoint == null)
        {
            return true;
        }

        return Vector3.Distance(transform.position, spawnPoint.position) <= exitArrivalDistance ||
               HasReachedDestination();
    }

    private void ReturnToSpawn()
    {
        SetCarriedItemVisible(false);
        assignedQueuePoint = null;
        returnTimer = returnTimeout;
        agent.isStopped = false;
        agent.SetDestination(spawnPoint.position);
        state = CustomerState.Returning;
    }

    public void FulfillPurchase()
    {
        if (!IsQueued)
        {
            return;
        }

        CashSystem cashSystem = FindFirstObjectByType<CashSystem>();
        int earnedCash = Random.Range(20, 51);
        if (cashSystem != null)
        {
            cashSystem.EarnCash(earnedCash);
        }

        ShowCashChange(earnedCash);
        ReturnToSpawn();
    }

    public void MoveToQueuePoint(Transform queuePoint)
    {
        if (queuePoint == null || (!IsQueued && !IsGoingToQueue))
        {
            return;
        }

        if (assignedQueuePoint == queuePoint)
        {
            return;
        }

        assignedQueuePoint = queuePoint;
        agent.isStopped = false;
        agent.SetDestination(queuePoint.position);
        state = CustomerState.GoingToQueue;
    }

    private void FinishBrowsing()
    {
        if (isStealer || assignedQueuePoint == null)
        {
            ReturnToSpawn();
            return;
        }

        SetCarriedItemVisible(true);
        agent.isStopped = false;
        agent.SetDestination(assignedQueuePoint.position);
        state = CustomerState.GoingToQueue;
    }

    private void TrySteal()
    {
        if (hasAttemptedSteal)
        {
            return;
        }

        hasAttemptedSteal = true;
        if (Random.value >= stealChance)
        {
            return;
        }

        isStealer = true;
        isStealing = true;
        stealTimer = stealDisplayTime;
        ShowReaction("Stealing...");
    }

    private void GoToNextAisle()
    {
        int nextAisleIndex = Random.Range(0, aislePoints.Length);

        if (aislePoints.Length > 1)
        {
            while (nextAisleIndex == previousAisleIndex)
            {
                nextAisleIndex = Random.Range(0, aislePoints.Length);
            }
        }

        previousAisleIndex = nextAisleIndex;
        agent.isStopped = false;
        agent.SetDestination(aislePoints[nextAisleIndex].position);
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

        wasAccused = true;
        ReturnToSpawn();
    }

    private void StartQueueTimer()
    {
        queueTimer = queueWaitTime;
        UpdateQueueCountdownText();
        SetReactionTextVisible(true);
    }

    private void UpdateQueueCountdownText()
    {
        if (reactionText != null)
        {
            reactionText.text = Mathf.CeilToInt(Mathf.Max(0f, queueTimer)).ToString();
        }
    }

    private void SetCarriedItemVisible(bool isVisible)
    {
        if (carriedItem != null)
        {
            carriedItem.SetActive(isVisible);
        }
    }

    private void ShowCashChange(int amount)
    {
        if (cashChangePanel == null || cashChangeText == null)
        {
            return;
        }

        cashChangeText.text = amount >= 0 ? $"+ ${amount}" : $"- ${Mathf.Abs(amount)}";
        cashChangeText.color = amount >= 0 ? Color.green : Color.red;
        cashChangePanel.SetActive(true);
    }

    private void SetCashChangePanelVisible(bool isVisible)
    {
        if (cashChangePanel != null)
        {
            cashChangePanel.SetActive(isVisible);
        }
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
