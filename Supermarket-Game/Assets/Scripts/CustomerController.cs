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
    [SerializeField] private Transform sightOrigin;
    [SerializeField] private Color silhouetteColor = Color.black;
    [SerializeField] private float sightTransitionSpeed = 6f;
    [SerializeField] private float sightGracePeriod = 0.12f;

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
    private bool isVisibleToCamera = true;
    private bool targetSightVisible = true;
    private bool reactionVisible;
    private bool cashChangeVisible;
    private float silhouetteAmount;
    private float blockedSightTimer;
    private float clearSightTimer;
    private Renderer[] customerRenderers;
    private MaterialPropertyBlock silhouetteProperties;
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

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
        customerRenderers = GetComponentsInChildren<Renderer>(true);
        silhouetteProperties = new MaterialPropertyBlock();

        if (sightOrigin == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                sightOrigin = player.transform;
            }
        }

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
                        int lostCash = Random.Range(30, 61);
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
        UpdateSightState();

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

    public bool CanBeSeenByCamera()
    {
        return isVisibleToCamera;
    }

    private void UpdateSightState()
    {
        if (sightOrigin == null)
        {
            return;
        }

        Vector3 target = GetSightTarget();
        Vector3 origin = sightOrigin.position + Vector3.up;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            SetSightState(true);
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        bool canSeeCustomer = false;

        foreach (RaycastHit hit in hits)
        {
            CustomerController hitCustomer = hit.collider.GetComponentInParent<CustomerController>();
            if (hitCustomer != null)
            {
                canSeeCustomer = hitCustomer == this;
                break;
            }

            if (hit.collider.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            // Any non-customer collider between the player and this customer blocks sight.
            break;
        }

        UpdateSightTransition(canSeeCustomer);
    }

    private void UpdateSightTransition(bool canSeeCustomer)
    {
        if (canSeeCustomer)
        {
            blockedSightTimer = 0f;
            clearSightTimer += Time.deltaTime;

            if (clearSightTimer >= sightGracePeriod)
            {
                targetSightVisible = true;
            }
        }
        else
        {
            clearSightTimer = 0f;
            blockedSightTimer += Time.deltaTime;

            if (blockedSightTimer >= sightGracePeriod)
            {
                targetSightVisible = false;
            }
        }

        SetSightState(targetSightVisible);
        float targetSilhouetteAmount = targetSightVisible ? 0f : 1f;
        silhouetteAmount = Mathf.MoveTowards(
            silhouetteAmount,
            targetSilhouetteAmount,
            sightTransitionSpeed * Time.deltaTime);

        ApplySilhouetteVisual();
    }

    private void ApplySilhouetteVisual()
    {
        if (customerRenderers == null)
        {
            return;
        }

        foreach (Renderer customerRenderer in customerRenderers)
        {
            customerRenderer.GetPropertyBlock(silhouetteProperties);
            Material material = customerRenderer.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty(BaseColorProperty))
                {
                    Color originalColor = material.GetColor(BaseColorProperty);
                    silhouetteProperties.SetColor(
                        BaseColorProperty,
                        Color.Lerp(originalColor, silhouetteColor, silhouetteAmount));
                }

                if (material.HasProperty(ColorProperty))
                {
                    Color originalColor = material.GetColor(ColorProperty);
                    silhouetteProperties.SetColor(
                        ColorProperty,
                        Color.Lerp(originalColor, silhouetteColor, silhouetteAmount));
                }
            }

            customerRenderer.SetPropertyBlock(silhouetteProperties);
            silhouetteProperties.Clear();
        }
    }

    private Vector3 GetSightTarget()
    {
        if (customerRenderers == null || customerRenderers.Length == 0)
        {
            return transform.position + Vector3.up;
        }

        Bounds bounds = customerRenderers[0].bounds;
        foreach (Renderer customerRenderer in customerRenderers)
        {
            bounds.Encapsulate(customerRenderer.bounds);
        }

        return bounds.center;
    }

    private void SetSightState(bool visible)
    {
        if (isVisibleToCamera == visible)
        {
            return;
        }

        isVisibleToCamera = visible;

        SetReactionTextVisible(reactionVisible && visible);
        SetCashChangePanelVisible(cashChangeVisible && visible);
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
        int earnedCash = Random.Range(20, 41);
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
        cashChangeVisible = true;
        SetCashChangePanelVisible(true);
    }

    private void SetCashChangePanelVisible(bool isVisible)
    {
        cashChangeVisible = isVisible;

        if (cashChangePanel != null)
        {
            cashChangePanel.SetActive(isVisible && isVisibleToCamera);
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
        reactionVisible = isVisible;

        if (reactionPanel != null)
        {
            reactionPanel.SetActive(isVisible && isVisibleToCamera);
        }

        if (reactionText != null)
        {
            if (reactionPanel == null)
            {
                reactionText.gameObject.SetActive(isVisible && isVisibleToCamera);
            }
        }
    }
}
