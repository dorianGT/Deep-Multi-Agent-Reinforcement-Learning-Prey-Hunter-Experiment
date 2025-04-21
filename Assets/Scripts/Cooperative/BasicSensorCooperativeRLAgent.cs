using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Agent utilisant l'apprentissage par renforcement avec ML-Agents.
/// L'objectif est d'atteindre une cible tout en évitant les murs.
/// L'agent perçoit son environnement via un RayPerceptionSensor3D.
/// </summary>
public class BasicSensorCooperativeRLAgent : Agent
{
    #region Variables

    /// <summary>
    /// Référence vers la cible à atteindre.
    /// </summary>
    public Transform target;

    /// <summary>
    /// Position de référence pour le spawn de la cible.
    /// </summary>
    public Transform spawnPos;

    /// <summary>
    /// Position de référence pour le spawn de l'agent.
    /// </summary>
    public Transform agentPos;

    /// <summary>
    /// Active le placement aléatoire de l'agent autour de la zone de spawn.
    /// </summary>
    public bool randomAgentSpawn = true;

    /// <summary>
    /// Active le placement aléatoire de la cible autour de la zone de spawn.
    /// </summary>
    public bool randomSpawn = true;

    /// <summary>
    /// Si activé, choisit dynamiquement la position de la cible à chaque épisode.
    /// </summary>
    public bool chooseTargetPos = true;

    /// <summary>
    /// Rayon de placement aléatoire autour du point de spawn pour la cible.
    /// </summary>
    public float randomRangeTarget = 3f;

    /// <summary>
    /// Rayon de placement aléatoire autour du point de spawn pour l'agent.
    /// </summary>
    public float randomRangeAgent = 13f;

    /// <summary>
    /// Vitesse de déplacement de l'agent.
    /// </summary>
    public float moveSpeed = 5f;

    /// <summary>
    /// Objet visuel utilisé pour afficher un retour (succès ou échec).
    /// </summary>
    public GameObject feedbackObject;

    /// <summary>
    /// Matériau appliqué à l'objet de feedback en cas d'échec.
    /// </summary>
    public Material loseMaterial;

    /// <summary>
    /// Active les déplacements manuels via le clavier (mode Heuristic).
    /// </summary>
    public bool canMoveHeuristic;

    /// <summary>
    /// Position initiale de l'agent (définie au démarrage).
    /// </summary>
    private Vector3 startPosition;

    /// <summary>
    /// Position initiale de la cible (définie au démarrage).
    /// </summary>
    private Vector3 targetStartPosition;

    /// <summary>
    /// Dernière distance mesurée entre l'agent et la cible (utilisée pour récompenser les progrès).
    /// </summary>
    private float lastDistanceToTarget = -1f;

    /// <summary>
    /// Référence vers le contrôleur de la cible.
    /// </summary>
    private TargetController targetController;

    /// <summary>
    /// Indique si l'agent se trouve actuellement dans la zone cible.
    /// </summary>
    private bool isOnTarget = false;

    #endregion

    #region Monobehaviour Functions

    /// <summary>
    /// Initialise les positions de départ de l’agent et de la cible.
    /// </summary>
    private void Start()
    {
        startPosition = transform.position;
        targetStartPosition = target.position;

        if (spawnPos == null)
        {
            spawnPos = target; // Valeur de secours
        }

        targetController = target.GetComponent<TargetController>();
    }

    #endregion

    #region Agent RL Functions

    /// <summary>
    /// Réinitialise l'épisode : repositionne l'agent et la cible, et applique une rotation aléatoire à l'agent.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (randomAgentSpawn && agentPos != null)
        {
            transform.position = agentPos.position + new Vector3(
                Random.Range(-randomRangeAgent, randomRangeAgent),
                0,
                Random.Range(-randomRangeAgent, randomRangeAgent)
            );
        }
        else
        {
            transform.position = startPosition;
        }

        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (chooseTargetPos)
        {
            if (randomSpawn && spawnPos != null)
            {
                target.position = spawnPos.position + new Vector3(
                    Random.Range(-randomRangeTarget, randomRangeTarget),
                    0,
                    Random.Range(-randomRangeTarget, randomRangeTarget)
                );
            }
            else
            {
                target.position = spawnPos != null ? spawnPos.position : targetStartPosition;
            }
        }

        lastDistanceToTarget = Vector3.Distance(transform.position, target.position);
    }

    /// <summary>
    /// Collecte les observations de l’agent (actuellement gérées par RayPerceptionSensor3D).
    /// </summary>
    /// <param name="sensor">Capteur utilisé pour enregistrer les observations.</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // L'observation est effectuée par les RayPerceptionSensor3D dans la hiérarchie Unity (enfant de ce GameObject).
    }

    /// <summary>
    /// Applique les actions décidées par le modèle : rotation et déplacement avant.
    /// Récompense l'agent s'il se rapproche de la cible, et pénalise s'il s'en éloigne.
    /// </summary>
    /// <param name="actions">Vecteur d’actions envoyé par le modèle.</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float forwardMovement = Mathf.Clamp(actions.ContinuousActions[0], 0f, 1f);
        float rotation = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        transform.Rotate(0, rotation * 100f * Time.deltaTime, 0);
        transform.position += transform.forward * forwardMovement * moveSpeed * Time.deltaTime;

        float currentDistance = Vector3.Distance(transform.position, target.position);

        if (lastDistanceToTarget > 0f)
        {
            float delta = lastDistanceToTarget - currentDistance;
            if (delta > 0f)
            {
                AddReward(delta);
            }
            else
            {
                AddReward(delta * 2);
            }
        }

        lastDistanceToTarget = currentDistance;

        AddReward(-0.001f); // Légère pénalité par étape pour encourager l'efficacité
    }

    /// <summary>
    /// Permet de tester l'agent manuellement avec les touches du clavier.
    /// Inclut une touche (E) pour activer/désactiver le mouvement.
    /// </summary>
    /// <param name="actionsOut">Actions manuelles à transmettre à l’agent.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        if (canMoveHeuristic)
        {
            continuousActionsOut[0] = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z) ? 1f : 0f;
            continuousActionsOut[1] = Input.GetAxis("Horizontal"); // ←/→ ou Q/D
        }
        else
        {
            continuousActionsOut[0] = 0f;
            continuousActionsOut[1] = 0f;
        }
    }

    #endregion

    #region Trigger & Visuals

    /// <summary>
    /// Met à jour la récompense si l'agent reste sur la cible.
    /// </summary>
    private void Update()
    {
        if (isOnTarget)
        {
            AddReward(0.01f); // Petite récompense incitative
        }
    }

    /// <summary>
    /// Gère les collisions avec les objets tagués. Termine l'épisode si collision avec un mur.
    /// Récompense ou punit selon la cible atteinte.
    /// </summary>
    /// <param name="other">Collider rencontré par l'agent.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            isOnTarget = true;
            AddReward(1f);
            targetController?.AgentEntered(this);
        }
        else if (other.CompareTag("Wall"))
        {
            SetReward(-1.0f);
            SetFeedbackMaterial(loseMaterial);
            EndEpisode();
        }
    }

    /// <summary>
    /// Gère la sortie de la zone cible par l'agent.
    /// </summary>
    /// <param name="other">Collider quitté par l'agent.</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            isOnTarget = false;
            AddReward(-2f); // Pénalité sévère pour être sorti de la cible
            targetController?.AgentExited(this);
        }
    }

    /// <summary>
    /// Change le matériau de l'objet de feedback pour indiquer une issue (par exemple, échec).
    /// </summary>
    /// <param name="mat">Matériau à appliquer à l'objet de feedback.</param>
    private void SetFeedbackMaterial(Material mat)
    {
        if (feedbackObject != null && mat != null)
        {
            feedbackObject.GetComponent<Renderer>().material = mat;
        }
    }

    #endregion
}
