using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;

using UnityEngine;

/// <summary>
/// Agent utilisant l'apprentissage par renforcement avec ML-Agents.
/// L'objectif est d'atteindre une cible tout en évitant les murs.
/// L'agent perçoit son environnement via un RayPerceptionSensor3D.
/// </summary>
public class BasicSensorRLAgent : Agent
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
    /// Active le placement aléatoire de la cible autour de la zone de spawn.
    /// </summary>
    public bool randomSpawn = true;

    /// <summary>
    /// Rayon de placement aléatoire autour du point de spawn.
    /// </summary>
    public float randomRange = 3f;

    /// <summary>
    /// Vitesse de déplacement de l'agent.
    /// </summary>
    public float moveSpeed = 5f;

    /// <summary>
    /// Objet visuel utilisé pour afficher un retour (succès ou échec).
    /// </summary>
    public GameObject feedbackObject;

    /// <summary>
    /// Matériau appliqué à l'objet de feedback en cas de succès.
    /// </summary>
    public Material winMaterial;

    /// <summary>
    /// Matériau appliqué à l'objet de feedback en cas d'échec.
    /// </summary>
    public Material loseMaterial;

    /// <summary>
    /// Position initiale de l'agent.
    /// </summary>
    private Vector3 startPosition;

    /// <summary>
    /// Position initiale de la cible.
    /// </summary>
    private Vector3 targetStartPosition;

    /// <summary>
    /// Dernière distance mesurée entre l'agent et la cible.
    /// </summary>
    private float lastDistanceToTarget = -1f;

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
    }

    #endregion

    #region Agent RL Functions

    /// <summary>
    /// Réinitialise l'épisode : repositionne l'agent et la cible, et applique une rotation aléatoire à l'agent.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (randomSpawn && spawnPos != null)
        {
            target.position = spawnPos.position + new Vector3(
                Random.Range(-randomRange, randomRange),
                0,
                Random.Range(-randomRange, randomRange)
            );
        }
        else
        {
            target.position = spawnPos != null ? spawnPos.position : targetStartPosition;
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
    /// Récompense l'agent s'il se rapproche de la cible.
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
    /// </summary>
    /// <param name="actionsOut">Actions manuelles à transmettre à l’agent.</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;

        continuousActionsOut[0] = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.Z) ? 1f : 0f;
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // ←/→ ou Q/D
    }

    #endregion

    #region Trigger & Visuals

    /// <summary>
    /// Déclenché lors d’une collision avec un objet trigger.
    /// Gère la fin d’épisode et attribue les récompenses.
    /// </summary>
    /// <param name="other">Objet avec lequel l’agent entre en collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(1.0f);
            SetFeedbackMaterial(winMaterial);
            EndEpisode();
        }
        else if (other.CompareTag("Wall"))
        {
            SetReward(-1.0f);
            SetFeedbackMaterial(loseMaterial);
            EndEpisode();
        }
    }

    /// <summary>
    /// Applique un matériau à l’objet de feedback visuel.
    /// </summary>
    /// <param name="mat">Matériau à appliquer.</param>
    private void SetFeedbackMaterial(Material mat)
    {
        if (feedbackObject != null && mat != null)
        {
            feedbackObject.GetComponent<Renderer>().material = mat;
        }
    }

    #endregion
}
